using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;
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
                prefix: new HarmonyMethod(AccessTools.Method(typeof(BagRenderPatches), nameof(Prefix))),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(BagRenderPatches), nameof(Postfix))),
                finalizer: new HarmonyMethod(AccessTools.Method(typeof(BagRenderPatches), nameof(Finalizer))));

            logger.Notification("[{0}] selected bag render patch applied.", BurdenedModSystem.ModId);
        }
    }

    public static void Reset()
    {
        lock (Gate) applied = false;
    }

    public static void Prefix(EntityBehaviorPlayerInventory __instance, ref HiddenBagState? __state)
    {
        __state = null;

        IPlayer? player = (__instance.entity as EntityPlayer)?.Player;
        if (player?.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks)
        {
            return;
        }

        ItemSlot? activeSlot = player.InventoryManager.ActiveHotbarSlot;
        if (activeSlot?.Itemstack == null
            || Array.IndexOf(backpacks.bagSlots, activeSlot) < 0)
        {
            return;
        }

        __state = new HiddenBagState(activeSlot, activeSlot.Itemstack);
        activeSlot.Itemstack = null;
    }

    public static void Postfix(HiddenBagState? __state) => __state?.Restore();

    public static Exception? Finalizer(Exception? __exception, HiddenBagState? __state)
    {
        __state?.Restore();
        return __exception;
    }

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
