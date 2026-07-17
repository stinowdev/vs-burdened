using Burdened.Config;
using Burdened.Network;
using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

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

    /// <summary>Raised on the client when the server pushes its config.</summary>
    public event Action<BurdenedConfig>? ConfigReceived;


    private void OnConfigSync(ConfigSyncPacket packet)
    {
        Config = packet.ToConfig();
        
        capi?.Logger.Notification(
            "[{0}] config received from server. hotbarSlots={1}, bagSlots={2}",
            ModId, Config.HotbarSlots, Config.BagSlots);

        ConfigReceived?.Invoke(Config);
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

    // -------------- Pipeline --------------

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.Network
            .RegisterChannel(ModId)
            .RegisterMessageType<ConfigSyncPacket>();
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        capi = api;

        api.Network.GetChannel(ModId)
            .SetMessageHandler<ConfigSyncPacket>(OnConfigSync);

        harmony = new Harmony(ModId);

        api.Logger.Notification("[{0}] client side loaded.", ModId);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        sapi = api;

        Config = LoadOrCreateConfig(api);
        
        harmony = new Harmony(ModId);

        serverChannel = api.Network.GetChannel(ModId);

        api.Event.PlayerJoin += OnPlayerJoin;

        api.Logger.Notification(
            "[{0}] server side loaded. Config: hotbarSlots={1}, bagSlots={2}, immersiveCarryingMode={3}",
            ModId, Config.HotbarSlots, Config.BagSlots, Config.ImmersiveCarryingMode);
    }

    public override void Dispose()
    {
        if (sapi != null)
        {
            sapi.Event.PlayerJoin -= OnPlayerJoin;
        }

        if (harmony != null)
        {
            harmony.UnpatchAll(ModId);
            harmony = null;
        }

        capi = null;
        sapi = null;
        serverChannel = null;

        base.Dispose();
    }
}
