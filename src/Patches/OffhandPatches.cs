using System;
using System.Collections.Generic;
using System.Reflection;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Burdened.Patches;

/// <summary>
/// F06 / D06: when <c>OffhandHoldsAnything</c> is true, the offhand slot
/// accepts any non-bag collectible (still subject to PutLocked, slot tags, and
/// <see cref="InventoryBase.CanContain"/>). Usability stays vanilla: this
/// only opens placement into the slot; tools/weapons are not used from offhand.
///
/// Automatic best-slot routing is filtered separately so auto-pickup and
/// shift-click never use the offhand as an overflow destination.
/// </summary>
public static class OffhandPatches
{
    private static readonly object Gate = new object();
    private static bool applied;

    public static void Apply(Harmony harmony, ILogger logger)
    {
        lock (Gate)
        {
            if (applied) return;
            applied = true;

            int patched = 0;
            patched += TryPatch(harmony, logger,
                AccessTools.Method(typeof(ItemSlot), nameof(ItemSlot.CanHold)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(OffhandPatches), nameof(CanHoldPrefix))));
            patched += TryPatch(harmony, logger,
                AccessTools.Method(typeof(ItemSlot), nameof(ItemSlot.CanTakeFrom),
                    new[] { typeof(ItemSlot), typeof(EnumMergePriority) }),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(OffhandPatches), nameof(CanTakeFromPrefix))));
            patched += TryPatch(harmony, logger,
                AccessTools.Method(typeof(InventoryBase), nameof(InventoryBase.GetBestSuitedSlot),
                    new[] { typeof(ItemSlot), typeof(ItemStackMoveOperation), typeof(List<ItemSlot>) }),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(OffhandPatches), nameof(BestSuitedSlotPostfix))));

            logger.Notification("[{0}] offhand patches applied to {1} method(s).", BurdenedModSystem.ModId, patched);
        }
    }

    public static void Reset()
    {
        lock (Gate) applied = false;
    }

    public static bool CanHoldPrefix(ItemSlot __instance, ItemSlot sourceSlot, ref bool __result)
    {
        if (__instance is ItemSlotOffhand && BagClassifier.IsEquippableBag(sourceSlot?.Itemstack))
        {
            __result = false;
            return false;
        }

        if (!AppliesTo(__instance)) return true;

        if (__instance.Inventory is InventoryBase inv && inv.PutLocked)
        {
            __result = false;
            return false;
        }

        ItemStack? stack = sourceSlot?.Itemstack;
        if (stack?.Collectible == null)
        {
            __result = false;
            return false;
        }

        if (!__instance.CanStoreTags.IsEmpty
            && !stack.Collectible.GetTags(stack).Overlaps(__instance.CanStoreTags))
        {
            __result = false;
            return false;
        }

        // Same as ItemSlot.CanHold, but without the Offhand storage-flag bit.
        __result = __instance.Inventory.CanContain(__instance, sourceSlot);
        return false;
    }

    public static bool CanTakeFromPrefix(ItemSlot __instance, ItemSlot sourceSlot, EnumMergePriority priority, ref bool __result)
    {
        if (__instance is ItemSlotOffhand && BagClassifier.IsEquippableBag(sourceSlot?.Itemstack))
        {
            __result = false;
            return false;
        }

        if (!AppliesTo(__instance)) return true;

        if (__instance.Inventory is InventoryBase inv && inv.PutLocked)
        {
            __result = false;
            return false;
        }

        ItemStack? stack = sourceSlot?.Itemstack;
        if (stack?.Collectible == null)
        {
            __result = false;
            return false;
        }

        if (!__instance.CanStoreTags.IsEmpty
            && !stack.Collectible.GetTags(stack).Overlaps(__instance.CanStoreTags))
        {
            __result = false;
            return false;
        }

        // Same as ItemSlot.CanTakeFrom without the storage-flag gate.
        if (__instance.Itemstack == null
            || __instance.Itemstack.Collectible.GetMergableQuantity(__instance.Itemstack, stack, priority) > 0)
        {
            __result = __instance.GetRemainingSlotSpace(stack) > 0;
            return false;
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// Manual moves call the slot acceptance methods directly. Automatic
    /// routing uses GetBestSuitedSlot, so discard an offhand result here while
    /// broad offhand holding is enabled.
    /// </summary>
    public static void BestSuitedSlotPostfix(InventoryBase __instance, ref WeightedSlot __result)
    {
        if (SlotLocks.Config?.OffhandHoldsAnything != true
            || __instance is not InventoryPlayerHotbar
            || __result.slot is not ItemSlotOffhand)
        {
            return;
        }

        __result = new WeightedSlot();
    }

    private static bool AppliesTo(ItemSlot slot)
    {
        return slot is ItemSlotOffhand
            && SlotLocks.Config?.OffhandHoldsAnything == true;
    }

    private static int TryPatch(
        Harmony harmony,
        ILogger logger,
        MethodInfo? target,
        HarmonyMethod? prefix = null,
        HarmonyMethod? postfix = null)
    {
        if (target == null) return 0;
        try
        {
            harmony.Patch(target, prefix: prefix, postfix: postfix);
            return 1;
        }
        catch (Exception e)
        {
            logger.Warning("[{0}] Could not patch {1}.{2}: {3}",
                BurdenedModSystem.ModId, target.DeclaringType?.FullName, target.Name, e.Message);
            return 0;
        }
    }
}
