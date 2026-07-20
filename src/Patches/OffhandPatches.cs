using System;
using System.Reflection;
using Burdened.Bags;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Common;

namespace Burdened.Patches;

/// <summary>
/// F06 / D06: when <c>OffhandHoldsAnything</c> is true, the offhand slot
/// accepts any non-bag collectible (still subject to PutLocked, slot tags, and
/// <see cref="InventoryBase.CanContain"/>). Usability stays vanilla: this
/// only opens placement into the slot; tools/weapons are not used from offhand.
///
/// Suitability is left alone so auto-pickup / "best slot" does not dump
/// random items into the offhand.
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

            logger.Notification("[{0}] offhand patches applied to {1} method(s).", BurdenedModSystem.ModId, patched);
        }
    }

    public static void Reset()
    {
        lock (Gate) applied = false;
    }

    public static bool CanHoldPrefix(ItemSlot __instance, ItemSlot sourceSlot, ref bool __result)
    {
        if (__instance is ItemSlotOffhand && BagSupport.IsBag(sourceSlot?.Itemstack))
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
        if (__instance is ItemSlotOffhand && BagSupport.IsBag(sourceSlot?.Itemstack))
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

    private static bool AppliesTo(ItemSlot slot)
    {
        return slot is ItemSlotOffhand
            && SlotLocks.Config?.OffhandHoldsAnything == true;
    }

    private static int TryPatch(Harmony harmony, ILogger logger, MethodInfo? target, HarmonyMethod? prefix)
    {
        if (target == null) return 0;
        try
        {
            harmony.Patch(target, prefix: prefix);
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
