using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Burdened.Patches;

/// <summary>
/// A selected bag-equip slot is rendered by vanilla as the active hand item.
/// Exclude that same slot while vanilla composes the player's worn backpack
/// shape so one bag is not shown on the body and in the hand simultaneously.
/// </summary>
public static class BagRenderPatches
{
    private static readonly object Gate = new object();
    private static bool applied;

    public static void Apply(Harmony harmony, ILogger logger)
    {
        lock (Gate)
        {
            if (applied) return;
            applied = true;

            var target = AccessTools.Method(
                typeof(EntityBehaviorPlayerInventory),
                nameof(EntityBehaviorPlayerInventory.OnTesselation));

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(BagRenderPatches), nameof(TessellationPrefix))),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(BagRenderPatches), nameof(TessellationPostfix))),
                finalizer: new HarmonyMethod(AccessTools.Method(typeof(BagRenderPatches), nameof(TessellationFinalizer))));

            var activeSlotSetter = AccessTools.PropertySetter(
                typeof(ClientPlayerInventoryManager),
                nameof(ClientPlayerInventoryManager.ActiveHotbarSlotNumber));
            harmony.Patch(
                activeSlotSetter,
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagRenderPatches), nameof(ActiveSlotChangingPrefix))),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagRenderPatches), nameof(ActiveSlotChangedPostfix))));

            var serverSlotChange = AccessTools.Method(
                typeof(ClientPlayerInventoryManager),
                nameof(ClientPlayerInventoryManager.SetActiveHotbarSlotNumberFromServer));
            harmony.Patch(
                serverSlotChange,
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagRenderPatches), nameof(ActiveSlotChangingPrefix))),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(BagRenderPatches), nameof(ActiveSlotChangedPostfix))));

            logger.Notification("[{0}] selected bag render patch applied.", BurdenedModSystem.ModId);
        }
    }

    public static void Reset()
    {
        lock (Gate) applied = false;
    }

    public static void TessellationPrefix(
        EntityBehaviorPlayerInventory __instance,
        ref HiddenBagState? __state)
    {
        __state = null;

        IPlayer? player = (__instance.entity as EntityPlayer)?.Player;
        ItemSlot? activeSlot = SelectedEquippedBagSlot(player);
        if (activeSlot?.Itemstack == null) return;

        __state = new HiddenBagState(activeSlot, activeSlot.Itemstack);
        activeSlot.Itemstack = null;
    }

    public static void TessellationPostfix(HiddenBagState? __state) => __state?.Restore();

    public static Exception? TessellationFinalizer(Exception? __exception, HiddenBagState? __state)
    {
        __state?.Restore();
        return __exception;
    }

    /// <summary>
    /// The selected item is rendered by the hand renderer, while the body mesh
    /// is cached. Invalidate that mesh when selection enters or leaves an
    /// equipped bag so its worn copy changes immediately.
    /// </summary>
    public static void ActiveSlotChangingPrefix(
        ClientPlayerInventoryManager __instance,
        ref ActiveBagSelectionState __state)
    {
        __state = new ActiveBagSelectionState(
            __instance.ActiveHotbarSlotNumber,
            SelectedEquippedBagSlot(__instance.player) != null);
    }

    public static void ActiveSlotChangedPostfix(
        ClientPlayerInventoryManager __instance,
        ActiveBagSelectionState __state)
    {
        if (__state.SlotNumber == __instance.ActiveHotbarSlotNumber) return;

        bool selectedBagNow = SelectedEquippedBagSlot(__instance.player) != null;
        if (!__state.SelectedBag && !selectedBagNow) return;

        (__instance.player?.Entity as EntityPlayer)?.MarkShapeModified();
    }

    private static ItemSlot? SelectedEquippedBagSlot(IPlayer? player)
    {
        if (player?.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks)
        {
            return null;
        }

        ItemSlot? activeSlot = player.InventoryManager.ActiveHotbarSlot;
        if (activeSlot?.Itemstack == null) return null;

        return Array.IndexOf(backpacks.bagSlots, activeSlot) >= 0 ? activeSlot : null;
    }

    public readonly record struct ActiveBagSelectionState(int SlotNumber, bool SelectedBag);

    public sealed class HiddenBagState
    {
        private readonly ItemSlot slot;
        private ItemStack? stack;

        public HiddenBagState(ItemSlot slot, ItemStack stack)
        {
            this.slot = slot;
            this.stack = stack;
        }

        public void Restore()
        {
            if (stack == null) return;

            slot.Itemstack = stack;
            stack = null;
        }
    }
}
