using System;
using Burdened.Bags;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Burdened.Patches;

/// <summary>
/// F10: a selected bag-equip slot is rendered by vanilla as the active hand
/// item. Exclude that same slot while vanilla composes the player's worn
/// backpack shape so one bag is not shown on the body and in the hand at once.
///
/// D12 exception: the effect must wrap the vanilla body, which no behavior can.
/// Client only. Runs on the client main thread, not a tesselation worker
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
        ItemSlot? activeSlot = BagSupport.SelectedEquippedBagSlot(player);
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
