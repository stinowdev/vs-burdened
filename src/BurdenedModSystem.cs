using Burdened.Bags;
using Burdened.Client;
using Burdened.Config;
using Burdened.Inventory;
using Burdened.Network;
using Burdened.Patches;
using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Burdened;

/// <summary>
/// Entry point for the mod. 
/// </summary>
public class BurdenedModSystem : ModSystem
{
    public const string ModId = "burdened";
    private const string ConfigFile = $"{ModId}.json";

    // -------------- Shared --------------

    /// <summary>On the server: loaded from ModConfig. On the client: defaults until the join sync arrives.</summary>
    public BurdenedConfig Config { get; private set; } = new BurdenedConfig();

    private Harmony? harmony;


    // -------------- Client --------------
    private ICoreClientAPI? capi;
    private SlotVisuals? slotVisuals;

    /// <summary>Raised on the client when the server pushes its config.</summary>
    public event Action<BurdenedConfig>? ConfigReceived;


    private void OnConfigSync(ConfigSyncPacket packet)
    {
        Config = packet.ToConfig();
        
        SlotLocks.Config = Config;

        capi?.Logger.Notification(
            "[{0}] config received from server. hotbarSlots={1}, bagSlots={2}, immersive={3}",
            ModId, Config.HotbarSlots, Config.BagSlots, Config.ImmersiveCarryingMode);

        ConfigReceived?.Invoke(Config);
        // Config often arrives while still on the connecting screen.
        // LevelFinalize will recompose once the world is up.
        RecomposeHotbarHud(); //TODO: This is not working as expected.
        if (capi != null) InventoryDialogPatches.RecomposeIfPresent(capi);
    }

    private void RecomposeHotbarHud()
    {
        // HudHotbar.ComposeGuis assumes World.Player + hotbar inventory exist.
        if (capi?.World?.Player?.InventoryManager?.GetOwnInventory("hotbar") == null) return;

        foreach (GuiDialog gui in capi.Gui.LoadedGuis)
        {
            if (gui is HudHotbar hotbar)
            {
                hotbar.ComposeGuis();
                break;
            }
        }
    }

    // -------------- Server --------------
    private ICoreServerAPI? sapi;
    private IServerNetworkChannel? serverChannel;


    private static BurdenedConfig LoadOrCreateConfig(ICoreServerAPI api)
    {
        BurdenedConfig? config = null;
        try
        {
            config = api.LoadModConfig<BurdenedConfig>(ConfigFile);
        }
        catch (Exception e)
        {
            api.Logger.Error("[{0}] Failed to parse {1}, using defaults settings. Error: {2}", ModId, ConfigFile, e.Message);
        }

        config ??= new BurdenedConfig();
        config.Sanitize();

        // Writes back so the file always exists and reflects sanitized values
        api.StoreModConfig(config, ConfigFile);
        
        return config;
    }

    private void OnPlayerJoin(IServerPlayer player)
    {
        serverChannel?.SendPacket(ConfigSyncPacket.From(Config), player);
    }

    // Fired once the player entity is fully in-world. PlayerJoin is too early.
    private void OnPlayerNowPlaying(IServerPlayer player)
    {
        if (sapi != null) SlotEjection.EjectLockedSlots(sapi, player, Config);
    }

    // -------------- Pipeline --------------

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.Network
            .RegisterChannel(ModId)
            .RegisterMessageType<ConfigSyncPacket>()
            .RegisterMessageType<PlaceEquippedBagPacket>()
            .RegisterMessageType<PickupFloorBagPacket>();
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        capi = api;

        api.Network.GetChannel(ModId)
            .SetMessageHandler<ConfigSyncPacket>(OnConfigSync);

        harmony = new Harmony(ModId);
        SlotLockPatches.Apply(harmony, api.Logger);
        OffhandPatches.Apply(harmony, api.Logger);
        HotbarHudPatches.Apply(harmony, api);
        HotbarScrollPatches.Apply(harmony, api);
        InventoryDialogPatches.Apply(harmony, api);
        BagInteractionPatches.ApplyShared(harmony, api.Logger);
        BagInteractionPatches.ApplyClient(harmony, api);
        BagRenderPatches.Apply(harmony, api.Logger);

        // Grey the locked slots whenever the config becomes known.
        // On the sync (ConfigReceived) and again once the world is ready
        // that second pass also recomposes the hotbar HUD if config arrived early.
        slotVisuals = new SlotVisuals(api);
        ConfigReceived += _ => slotVisuals?.TryApply();
        api.Event.LevelFinalize += () =>
        {
            slotVisuals?.TryApply();
            RecomposeHotbarHud();
            InventoryDialogPatches.RecomposeIfPresent(api);
        };

        api.Logger.Notification("[{0}] client side loaded.", ModId);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        sapi = api;

        Config = LoadOrCreateConfig(api);
        SlotLocks.Config = Config;

        harmony = new Harmony(ModId);
        SlotLockPatches.Apply(harmony, api.Logger);
        OffhandPatches.Apply(harmony, api.Logger);
        BagInteractionPatches.ApplyShared(harmony, api.Logger);

        serverChannel = api.Network.GetChannel(ModId);
        serverChannel.SetMessageHandler<PlaceEquippedBagPacket>(
            (player, packet) => BagPlacementService.Place(api, player, packet));
        serverChannel.SetMessageHandler<PickupFloorBagPacket>(
            (player, packet) => BagPickupService.Pickup(api, player, packet));

        api.Event.PlayerJoin += OnPlayerJoin;
        api.Event.PlayerNowPlaying += OnPlayerNowPlaying;

        api.Logger.Notification(
            "[{0}] server side loaded. Config: hotbarSlots={1}, bagSlots={2}, immersiveCarryingMode={3}",
            ModId, Config.HotbarSlots, Config.BagSlots, Config.ImmersiveCarryingMode);
    }

    public override void Dispose()
    {
        if (sapi != null)
        {
            sapi.Event.PlayerJoin -= OnPlayerJoin;
            sapi.Event.PlayerNowPlaying -= OnPlayerNowPlaying;
        }

        if (harmony != null)
        {
            harmony.UnpatchAll(ModId);
            harmony = null;
        }
        
        SlotLockPatches.Reset();
        OffhandPatches.Reset();
        HotbarHudPatches.Reset();
        HotbarScrollPatches.Reset();
        InventoryDialogPatches.Reset();
        BagInteractionPatches.Reset();
        BagRenderPatches.Reset();
        GuiDialogEquippedBag.CloseAll();
        SlotLocks.Config = null;

        slotVisuals = null;
        capi = null;
        sapi = null;
        serverChannel = null;

        base.Dispose();
    }
}
