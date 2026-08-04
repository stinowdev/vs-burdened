using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Burdened.Bags;
using Burdened.Client;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Burdened.Patches;

/// <summary>
/// F08/F10 interaction remaps. Floor interaction is patched on both sides;
/// equipped-slot UI and held interaction are client-only.
/// </summary>
public static class BagInteractionPatches
{
    private static readonly object Gate = new object();
    private static readonly Dictionary<PlacementInteractionKey, long> PendingPlacementInteractions = new();
    private static ConditionalWeakTable<BlockEntityContainedBagWorkspace, object>
        initializedClientWorkspaces = new();
    private static readonly FieldInfo? GridInventoryField =
        AccessTools.Field(typeof(GuiElementItemSlotGridBase), "inventory");

    private static bool sharedApplied;
    private static bool clientApplied;
    private static ICoreClientAPI? capi;

    public static void ApplyShared(Harmony harmony, ILogger logger)
    {
        lock (Gate)
        {
            if (sharedApplied) return;
            sharedApplied = true;

            int patched = 0;
            patched += PatchSupport.TryPatch(harmony, logger,
                AccessTools.Method(typeof(BlockEntityGroundStorage),
                    nameof(BlockEntityGroundStorage.OnPlayerInteractStart)),
                "BlockEntityGroundStorage.OnPlayerInteractStart",
                prefix: PatchSupport.Hook(logger, typeof(BagInteractionPatches), nameof(FloorInteractPrefix)));

            patched += PatchSupport.TryPatch(harmony, logger,
                AccessTools.Method(typeof(CollectibleBehaviorGroundStorable),
                    nameof(CollectibleBehaviorGroundStorable.OnHeldInteractStart)),
                "CollectibleBehaviorGroundStorable.OnHeldInteractStart",
                prefix: PatchSupport.Hook(logger, typeof(BagInteractionPatches), nameof(EquippedBagHeldInteractPrefix)));

            logger.Notification(
                "[{0}] shared bag interaction patches applied to {1} method(s).", BurdenedModSystem.ModId, patched);
        }
    }

    public static void ApplyClient(Harmony harmony, ICoreClientAPI api)
    {
        capi = api;
        ILogger logger = api.Logger;
        lock (Gate)
        {
            if (clientApplied) return;
            clientApplied = true;

            int patched = 0;
            patched += PatchSupport.TryPatch(harmony, logger,
                AccessTools.Method(typeof(GuiElementItemSlotGridBase),
                    nameof(GuiElementItemSlotGridBase.SlotClick)),
                "GuiElementItemSlotGridBase.SlotClick",
                prefix: PatchSupport.Hook(logger, typeof(BagInteractionPatches), nameof(BagSlotClickPrefix)));

            patched += PatchSupport.TryPatch(harmony, logger,
                AccessTools.Method(typeof(CollectibleBehaviorGroundStorable),
                    nameof(CollectibleBehaviorGroundStorable.GetHeldInteractionHelp)),
                "CollectibleBehaviorGroundStorable.GetHeldInteractionHelp",
                postfix: PatchSupport.Hook(logger, typeof(BagInteractionPatches), nameof(EquippedBagHelpPostfix)));

            patched += PatchSupport.TryPatch(harmony, logger,
                AccessTools.Method(typeof(BlockEntityContainedBagWorkspace),
                    nameof(BlockEntityContainedBagWorkspace.OnReceivedServerPacket)),
                "BlockEntityContainedBagWorkspace.OnReceivedServerPacket",
                prefix: PatchSupport.Hook(logger, typeof(BagInteractionPatches), nameof(ContainedBagPacketPrefix)));

            patched += PatchSupport.TryPatch(harmony, logger,
                AccessTools.Method(typeof(BlockGroundStorage),
                    nameof(BlockGroundStorage.GetPlacedBlockInteractionHelp)),
                "BlockGroundStorage.GetPlacedBlockInteractionHelp",
                postfix: PatchSupport.Hook(logger, typeof(BagInteractionPatches), nameof(FloorBagHelpPostfix)));

            logger.Notification(
                "[{0}] client bag interaction patches applied to {1} method(s).", BurdenedModSystem.ModId, patched);
        }
    }

    public static void Reset()
    {
        lock (Gate)
        {
            sharedApplied = false;
            clientApplied = false;
            capi = null;
            PendingPlacementInteractions.Clear();
            initializedClientWorkspaces = new ConditionalWeakTable<BlockEntityContainedBagWorkspace, object>();
        }
    }

    /// <summary>
    /// The input that sends a custom placement request can also have a vanilla
    /// world-interact packet already in flight. Consume that one packet if it
    /// resolves against the bag that the request just placed.
    /// </summary>
    public static void SuppressNextFloorInteraction(IPlayer player, BlockPos position)
    {
        long now = Environment.TickCount64;
        PlacementInteractionKey key = new PlacementInteractionKey(
            player.PlayerUID,
            position.X,
            position.InternalY,
            position.Z,
            player.Entity.Pos.Dimension);

        lock (Gate)
        {
            List<PlacementInteractionKey>? expired = null;
            foreach ((PlacementInteractionKey existingKey, long expiresAt) in PendingPlacementInteractions)
            {
                if (expiresAt >= now) continue;
                (expired ??= new List<PlacementInteractionKey>()).Add(existingKey);
            }

            if (expired != null)
            {
                foreach (PlacementInteractionKey expiredKey in expired)
                {
                    PendingPlacementInteractions.Remove(expiredKey);
                }
            }

            PendingPlacementInteractions[key] = now + 750;
        }
    }

    /// <summary>
    /// Plain RMB opens through vanilla's contained-bag workspace. Shift+RMB
    /// transfers only to a compatible equip slot; no general give route exists.
    /// </summary>
    public static bool FloorInteractPrefix(
        BlockEntityGroundStorage __instance,
        IPlayer player,
        BlockSelection bs,
        ref bool __result)
    {
        if (SlotLocks.Config?.ImprovedBagInteractions != true) return true;

        ItemSlot? floorSlot = __instance.GetSlotAt(bs);
        if (floorSlot == null || floorSlot.Empty
            || !BagSupport.SupportsPlacedBagInteractions(floorSlot.Itemstack)) return true;

        BlockEntity blockEntity = __instance;
        if (blockEntity.Api.Side == EnumAppSide.Server
            && ConsumePlacementInteraction(player, blockEntity.Pos))
        {
            __result = true;
            return false;
        }

        bool shift = player.Entity.Controls.ShiftKey;
        if (blockEntity.Api is ICoreClientAPI clientApi
            && (clientApi.Input.KeyboardKeyStateRaw[1]
                || clientApi.Input.KeyboardKeyStateRaw[2]))
        {
            shift = true;
        }
        if (!shift)
        {
            CollectibleBehaviorGroundStoredHeldBag? behavior =
                floorSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStoredHeldBag>();
            BEBehaviorContainedBagInventory? inventories =
                ((BlockEntity)__instance).GetBehavior<BEBehaviorContainedBagInventory>();
            if (behavior == null || inventories == null) return true;

            int slotId = floorSlot.Inventory.GetSlotId(floorSlot);
            BlockEntityContainedBagWorkspace workspace = inventories.BagInventories[slotId];
            if (workspace.TryLoadBagInv(floorSlot, behavior))
            {
                if (blockEntity.Api.Side == EnumAppSide.Client)
                {
                    lock (Gate)
                    {
                        initializedClientWorkspaces.GetValue(workspace, static _ => new object());
                    }
                }
                workspace.OpenHeldBag(player);
            }

            __result = true;
            return false;
        }

        // Floor pickup is never predicted. The matching vanilla interaction on
        // the server is consumed here; only the custom request handler may
        // transfer the item and remove the block.
        if (blockEntity.Api is not ICoreClientAPI pickupClient)
        {
            __result = true;
            return false;
        }

        ItemSlot? equipSlot = BagSupport.FindEmptyEquipSlot(player, floorSlot);
        if (equipSlot == null)
        {
            pickupClient.ShowChatMessage(Lang.Get("burdened:no-compatible-bag-slot"));

            // The gesture belongs to the floor bag even when it cannot move;
            // report it handled so the active held item cannot receive the same
            // Shift+RMB as a fallback interaction.
            __result = true;
            return false;
        }

        // GetSlotAt resolved which bag the player is pointing at, which the
        // position alone cannot express for a two-slot layout.
        BagPickupService.Request(
            pickupClient, blockEntity.Pos, floorSlot.Inventory.GetSlotId(floorSlot));

        __result = true;
        return false;
    }

    /// <summary>
    /// A contained-bag open packet assumes the client workspace was initialized
    /// by the matching local interaction first. Packet ordering during custom
    /// placement can violate that assumption, so establish it before vanilla
    /// deserializes into the workspace inventory.
    /// </summary>
    public static void ContainedBagPacketPrefix(
        BlockEntityContainedBagWorkspace __instance,
        int packetid,
        BlockEntityContainer ___be,
        int ___slotId)
    {
        if (packetid != 5000 || SlotLocks.Config?.ImprovedBagInteractions != true) return;

        lock (Gate)
        {
            if (initializedClientWorkspaces.TryGetValue(__instance, out _)) return;
        }

        if (___slotId < 0 || ___slotId >= ___be.Inventory.Count) return;
        ItemSlot bagSlot = ___be.Inventory[___slotId];
        if (bagSlot.Empty || !BagSupport.SupportsPlacedBagInteractions(bagSlot.Itemstack)) return;

        CollectibleBehaviorGroundStoredHeldBag? behavior =
            bagSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStoredHeldBag>();
        if (behavior == null || !__instance.TryLoadBagInv(bagSlot, behavior)) return;

        lock (Gate)
        {
            initializedClientWorkspaces.GetValue(__instance, static _ => new object());
        }
    }

    private static bool ConsumePlacementInteraction(IPlayer player, BlockPos position)
    {
        PlacementInteractionKey key = new PlacementInteractionKey(
            player.PlayerUID,
            position.X,
            position.InternalY,
            position.Z,
            player.Entity.Pos.Dimension);

        lock (Gate)
        {
            if (!PendingPlacementInteractions.Remove(key, out long expiresAt)) return false;
            return expiresAt >= Environment.TickCount64;
        }
    }

    private readonly record struct PlacementInteractionKey(
        string PlayerUid,
        int X,
        int Y,
        int Z,
        int Dimension);

    public static bool BagSlotClickPrefix(
        GuiElementItemSlotGridBase __instance,
        ICoreClientAPI api,
        int slotId,
        EnumMouseButton mouseButton,
        bool shiftPressed,
        bool ctrlPressed)
    {
        if (SlotLocks.Config?.ImprovedBagInteractions != true) return true;
        if (GridInventoryField?.GetValue(__instance) is not InventoryPlayerBackpacks backpacks) return true;
        if (slotId < 0 || slotId >= backpacks.bagSlots.Length) return true;

        ItemSlot slot = backpacks.bagSlots[slotId];
        if (slot.Empty
            || !BagSupport.SupportsEquippedBagWindow(slot.Itemstack)
            || SlotLocks.IsLocked(slot)) return true;

        if (!shiftPressed && mouseButton == EnumMouseButton.Right)
        {
            GuiDialogEquippedBag.Toggle(api, slotId);
            return false;
        }

        if (!api.World.Player.InventoryManager.MouseItemSlot.Empty) return true;

        if (shiftPressed && (mouseButton == EnumMouseButton.Left || mouseButton == EnumMouseButton.Right))
        {
            // A wall-mounted bag keeps vanilla's gesture
            if (!BagSupport.SupportsBurdenedPlacement(slot.Itemstack)) return true;

            if (BagPlacementService.Request(api, slotId))
            {
                GuiDialogEquippedBag.Close(slotId);
            }
            return false;
        }

        return true;
    }

    public static bool EquippedBagHeldInteractPrefix(
        ItemSlot itemslot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        bool firstEvent,
        ref EnumHandHandling handHandling,
        ref EnumHandling handling)
    {
        if (byEntity?.World == null || !firstEvent) return true;
        if (SlotLocks.Config?.ImprovedBagInteractions != true
            || itemslot == null
            || !BagSupport.SupportsEquippedBagWindow(itemslot.Itemstack))
        {
            return true;
        }
        if (itemslot.Inventory is not InventoryPlayerBackpacks backpacks) return true;

        int bagIndex = Array.IndexOf(backpacks.bagSlots, itemslot);
        if (bagIndex < 0) return true;

        bool canPlace = BagSupport.SupportsBurdenedPlacement(itemslot.Itemstack);

        // The client sends the authoritative placement request. Consume the
        // matching vanilla held interaction on the server so Shift+RMB cannot
        // run both placement paths. A bag Burdened cannot place is left to
        // vanilla on both sides, so its own gesture still reaches the server.
        if (byEntity.World.Side == EnumAppSide.Server)
        {
            if (!canPlace) return true;

            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventSubsequent;
            return false;
        }

        if (capi == null) return true;

        if (byEntity.Controls.ShiftKey
            || capi.Input.KeyboardKeyStateRaw[1]
            || capi.Input.KeyboardKeyStateRaw[2])
        {
            if (!canPlace) return true;

            if (BagPlacementService.Request(capi, bagIndex))
            {
                GuiDialogEquippedBag.Close(bagIndex);
            }
        }
        else
        {
            // Plain right-click opens the bag, whether or not a block is
            // targeted, and regardless of how the bag would be put down.
            GuiDialogEquippedBag.Toggle(capi, bagIndex);
        }

        handHandling = EnumHandHandling.PreventDefault;
        handling = EnumHandling.PreventSubsequent;
        return false;
    }

    public static void EquippedBagHelpPostfix(
        ItemSlot inSlot,
        ref EnumHandling handling,
        ref WorldInteraction[] __result)
    {
        if (capi == null
            || SlotLocks.Config?.ImprovedBagInteractions != true
            || !BagSupport.SupportsEquippedBagWindow(inSlot?.Itemstack)
            || BagSupport.EquipIndexOf(capi.World.Player, inSlot) == null)
        {
            return;
        }

        WorldInteraction open = new WorldInteraction
        {
            ActionLangCode = "blockhelp-chest-open",
            MouseButton = EnumMouseButton.Right,
        };

        if (!BagSupport.SupportsBurdenedPlacement(inSlot?.Itemstack))
        {
            // Only the opening one is added.
            List<WorldInteraction> withOpen = new List<WorldInteraction>(__result.Length + 1) { open };
            withOpen.AddRange(__result);
            __result = withOpen.ToArray();
            return;
        }

        __result = new[]
        {
            open,
            new WorldInteraction
            {
                ActionLangCode = "heldhelp-place",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
            },
        };
    }

    public static void FloorBagHelpPostfix(
        IWorldAccessor world,
        BlockSelection selection,
        ref WorldInteraction[] __result)
    {
        if (SlotLocks.Config?.ImprovedBagInteractions != true) return;
        if (world.BlockAccessor.GetBlockEntity(selection.Position)
                is not BlockEntityGroundStorage groundStorage)
        {
            return;
        }

        ItemSlot? slot = groundStorage.GetSlotAt(selection);
        if (slot == null || slot.Empty
            || !BagSupport.SupportsPlacedBagInteractions(slot.Itemstack)) return;

        List<WorldInteraction> kept = new List<WorldInteraction>(__result.Length + 2)
        {
            new WorldInteraction
            {
                ActionLangCode = "blockhelp-chest-open",
                MouseButton = EnumMouseButton.Right,
            },
            new WorldInteraction
            {
                ActionLangCode = "blockhelp-behavior-rightclickpickup",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
            },
        };

        foreach (WorldInteraction interaction in __result)
        {
            if (!IsRemappedByImprovedInteractions(interaction)) kept.Add(interaction);
        }

        __result = kept.ToArray();
    }

    private static bool IsRemappedByImprovedInteractions(WorldInteraction interaction)
    {
        return interaction.ActionLangCode switch
        {
            "blockhelp-behavior-rightclickpickup" => interaction.HotKeyCode == null,
            "blockhelp-chest-open" => interaction.HotKeyCode == "ctrl",
            _ => false,
        };
    }
}
