using System;
using System.Collections.Generic;
using System.Reflection;
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
    private static readonly HashSet<BlockEntityContainedBagWorkspace> InitializedClientWorkspaces = new();
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

            MethodInfo? target = AccessTools.Method(
                typeof(BlockEntityGroundStorage),
                nameof(BlockEntityGroundStorage.OnPlayerInteractStart));
            if (target == null)
            {
                logger.Warning("[{0}] Could not find the ground-storage interaction method.", BurdenedModSystem.ModId);
                return;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(
                AccessTools.Method(typeof(BagInteractionPatches), nameof(FloorInteractPrefix))));

            harmony.Patch(
                AccessTools.Method(typeof(CollectibleBehaviorGroundStorable),
                    nameof(CollectibleBehaviorGroundStorable.OnHeldInteractStart)),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagInteractionPatches), nameof(EquippedBagHeldInteractPrefix))));
        }
    }

    public static void ApplyClient(Harmony harmony, ICoreClientAPI api)
    {
        capi = api;
        lock (Gate)
        {
            if (clientApplied) return;
            clientApplied = true;

            harmony.Patch(
                AccessTools.Method(typeof(GuiElementItemSlotGridBase), nameof(GuiElementItemSlotGridBase.SlotClick)),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagInteractionPatches), nameof(BagSlotClickPrefix))));

            harmony.Patch(
                AccessTools.Method(typeof(CollectibleBehaviorGroundStorable),
                    nameof(CollectibleBehaviorGroundStorable.GetHeldInteractionHelp)),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagInteractionPatches), nameof(EquippedBagHelpPostfix))));

            harmony.Patch(
                AccessTools.Method(typeof(BlockEntityContainedBagWorkspace),
                    nameof(BlockEntityContainedBagWorkspace.OnReceivedServerPacket)),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagInteractionPatches), nameof(ContainedBagPacketPrefix))));

            harmony.Patch(
                AccessTools.Method(typeof(BlockGroundStorage),
                    nameof(BlockGroundStorage.GetPlacedBlockInteractionHelp)),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagInteractionPatches), nameof(FloorBagHelpPostfix))));
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
            InitializedClientWorkspaces.Clear();
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
        if (floorSlot == null || floorSlot.Empty || !BagSupport.IsBag(floorSlot.Itemstack)) return true;

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
                    lock (Gate) InitializedClientWorkspaces.Add(workspace);
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

        BagPickupService.Request(pickupClient, blockEntity.Pos);

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
            if (InitializedClientWorkspaces.Contains(__instance)) return;
        }

        if (___slotId < 0 || ___slotId >= ___be.Inventory.Count) return;
        ItemSlot bagSlot = ___be.Inventory[___slotId];
        if (bagSlot.Empty || !BagSupport.IsBag(bagSlot.Itemstack)) return;

        CollectibleBehaviorGroundStoredHeldBag? behavior =
            bagSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStoredHeldBag>();
        if (behavior == null || !__instance.TryLoadBagInv(bagSlot, behavior)) return;

        lock (Gate) InitializedClientWorkspaces.Add(__instance);
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
        if (slot.Empty || !BagSupport.IsBag(slot.Itemstack) || SlotLocks.IsLocked(slot)) return true;
        if (!api.World.Player.InventoryManager.MouseItemSlot.Empty) return true;

        if (shiftPressed && (mouseButton == EnumMouseButton.Left || mouseButton == EnumMouseButton.Right))
        {
            if (BagPlacementService.Request(api, slotId))
            {
                GuiDialogEquippedBag.Close(slotId);
            }
            return false;
        }

        if (mouseButton == EnumMouseButton.Right)
        {
            GuiDialogEquippedBag.Toggle(api, slotId);
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
            || !BagSupport.IsBag(itemslot.Itemstack))
        {
            return true;
        }
        if (itemslot.Inventory is not InventoryPlayerBackpacks backpacks) return true;

        int bagIndex = Array.IndexOf(backpacks.bagSlots, itemslot);
        if (bagIndex < 0) return true;

        // The client sends the authoritative placement request. Consume the
        // matching vanilla held interaction on the server so Shift+RMB cannot
        // run both placement paths.
        if (byEntity.World.Side == EnumAppSide.Server)
        {
            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventSubsequent;
            return false;
        }

        if (capi == null) return true;

        if (byEntity.Controls.ShiftKey
            || capi.Input.KeyboardKeyStateRaw[1]
            || capi.Input.KeyboardKeyStateRaw[2])
        {
            if (BagPlacementService.Request(capi, bagIndex))
            {
                GuiDialogEquippedBag.Close(bagIndex);
            }
        }
        else
        {
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
            || !BagSupport.IsBag(inSlot?.Itemstack)
            || BagSupport.EquipIndexOf(capi.World.Player, inSlot) == null)
        {
            return;
        }

        __result = new[]
        {
            new WorldInteraction
            {
                ActionLangCode = "blockhelp-chest-open",
                MouseButton = EnumMouseButton.Right,
            },
            new WorldInteraction
            {
                ActionLangCode = "heldhelp-place",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "shift",
            },
        };
        handling = EnumHandling.PreventSubsequent;
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
        if (slot == null || slot.Empty || !BagSupport.IsBag(slot.Itemstack)) return;

        __result = new[]
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
    }
}
