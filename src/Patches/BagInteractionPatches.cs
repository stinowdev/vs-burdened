using System;
using System.Reflection;
using Burdened.Bags;
using Burdened.Client;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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
                    nameof(CollectibleBehaviorGroundStorable.OnHeldInteractStart)),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagInteractionPatches), nameof(EquippedBagHeldInteractPrefix))));

            harmony.Patch(
                AccessTools.Method(typeof(CollectibleBehaviorGroundStorable),
                    nameof(CollectibleBehaviorGroundStorable.GetHeldInteractionHelp)),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagInteractionPatches), nameof(EquippedBagHelpPostfix))));
        }
    }

    public static void Reset()
    {
        lock (Gate)
        {
            sharedApplied = false;
            clientApplied = false;
            capi = null;
        }
    }

    /// <summary>
    /// Plain RMB opens through vanilla's contained-bag workspace. Ctrl+RMB
    /// transfers only to a compatible equip slot; no general give route exists.
    /// </summary>
    public static bool FloorInteractPrefix(
        BlockEntityGroundStorage __instance,
        IPlayer player,
        BlockSelection bs,
        ref bool __result)
    {
        if (SlotLocks.Config?.PlaceableBags != true) return true;

        ItemSlot? floorSlot = __instance.GetSlotAt(bs);
        if (floorSlot == null || floorSlot.Empty || !BagSupport.IsBag(floorSlot.Itemstack)) return true;

        bool ctrl = player.Entity.Controls.CtrlKey;
        if (((BlockEntity)__instance).Api is ICoreClientAPI clientApi
            && clientApi.Input.KeyboardKeyStateRaw[3])
        {
            ctrl = true;
        }
        if (!ctrl)
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
                workspace.OpenHeldBag(player);
            }

            __result = true;
            return false;
        }

        if (!BlockBehaviorReinforcable.AllowRightClickPickup(
                ((BlockEntity)__instance).Api.World,
                ((BlockEntity)__instance).Pos,
                player))
        {
            __result = true;
            return false;
        }

        ItemSlot? equipSlot = BagSupport.FindEmptyEquipSlot(player, floorSlot);
        if (equipSlot == null)
        {
            if (((BlockEntity)__instance).Api is ICoreClientAPI errorApi)
            {
                errorApi.ShowChatMessage(Lang.Get("burdened:no-compatible-bag-slot"));
            }

            // The gesture belongs to the floor bag even when it cannot move;
            // report it handled so the active held item cannot receive the same
            // Ctrl+RMB as a fallback interaction.
            __result = true;
            return false;
        }

        if (floorSlot.TryPutInto(((BlockEntity)__instance).Api.World, equipSlot, 1) <= 0)
        {
            __result = true;
            return false;
        }

        equipSlot.MarkDirty();
        ((BlockEntity)__instance).MarkDirty(true);
        if (__instance.Inventory.Empty)
        {
            ((BlockEntity)__instance).Api.World.BlockAccessor.SetBlock(0, ((BlockEntity)__instance).Pos);
        }

        if (__instance.StorageProps?.PlaceRemoveSound != null)
        {
            ((BlockEntity)__instance).Api.World.PlaySoundAt(
                __instance.StorageProps.PlaceRemoveSound,
                ((BlockEntity)__instance).Pos.X + 0.5,
                ((BlockEntity)__instance).Pos.InternalY,
                ((BlockEntity)__instance).Pos.Z + 0.5,
                player);
        }

        __result = true;
        return false;
    }

    public static bool BagSlotClickPrefix(
        GuiElementItemSlotGridBase __instance,
        ICoreClientAPI api,
        int slotId,
        EnumMouseButton mouseButton,
        bool ctrlPressed)
    {
        if (SlotLocks.Config?.OpenBagsFromHotbar != true) return true;
        if (GridInventoryField?.GetValue(__instance) is not InventoryPlayerBackpacks backpacks) return true;
        if (slotId < 0 || slotId >= backpacks.bagSlots.Length) return true;

        ItemSlot slot = backpacks.bagSlots[slotId];
        if (slot.Empty || !BagSupport.IsBag(slot.Itemstack) || SlotLocks.IsLocked(slot)) return true;
        if (!api.World.Player.InventoryManager.MouseItemSlot.Empty) return true;

        if (ctrlPressed && (mouseButton == EnumMouseButton.Left || mouseButton == EnumMouseButton.Right))
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
        if (byEntity?.World?.Side != EnumAppSide.Client || capi == null || !firstEvent) return true;
        if (SlotLocks.Config?.OpenBagsFromHotbar != true || !BagSupport.IsBag(itemslot?.Itemstack)) return true;

        int? bagIndex = BagSupport.EquipIndexOf(capi.World.Player, itemslot);
        if (bagIndex == null) return true;

        if (byEntity.Controls.ShiftKey)
        {
            handHandling = EnumHandHandling.PreventDefault;
            handling = EnumHandling.PreventSubsequent;
            return false;
        }

        if (byEntity.Controls.CtrlKey || capi.Input.KeyboardKeyStateRaw[3])
        {
            if (BagPlacementService.Request(capi, bagIndex.Value))
            {
                GuiDialogEquippedBag.Close(bagIndex.Value);
            }
        }
        else
        {
            GuiDialogEquippedBag.Toggle(capi, bagIndex.Value);
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
            || SlotLocks.Config?.OpenBagsFromHotbar != true
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
                HotKeyCode = "ctrl",
            },
        };
        handling = EnumHandling.PreventSubsequent;
    }
}
