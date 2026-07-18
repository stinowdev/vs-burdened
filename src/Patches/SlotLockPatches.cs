using System;
using System.Collections.Generic;
using System.Reflection;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Burdened.Patches;

/// <summary>
/// Enforces the slot locks (F01/F02). A locked slot rejects every incoming item
/// via <see cref="ItemSlot.CanHold"/> / <see cref="ItemSlot.CanTakeFrom"/> and
/// scores below everything in the "best suited slot" search that drives
/// auto-pickup and shift-click. Taking items OUT of a locked slot is left alone
/// so nothing can ever be trapped inside one.
///
/// This is server-authoritative; the client runs the same patches so its
/// prediction and rendering agree with the server. In singleplayer the two
/// sides share the patched method bodies, so a static gate applies them once.
/// </summary>
public static class SlotLockPatches
{
    private static readonly object Gate = new object();
    private static bool applied;

    public static void Apply(Harmony harmony, ILogger logger)
    {
        lock (Gate)
        {
            if (applied) return;
            applied = true;

            HarmonyMethod rejectPrefix = new HarmonyMethod(
                AccessTools.Method(typeof(SlotLockPatches), nameof(RejectWhenLockedPrefix)));

            int patched = 0;
            foreach (MethodInfo target in FindSlotOverrides(nameof(ItemSlot.CanHold)))
                patched += TryPatch(harmony, logger, target, prefix: rejectPrefix);
            foreach (MethodInfo target in FindSlotOverrides(nameof(ItemSlot.CanTakeFrom)))
                patched += TryPatch(harmony, logger, target, prefix: rejectPrefix);

            HarmonyMethod suitabilityPostfix = new HarmonyMethod(
                AccessTools.Method(typeof(SlotLockPatches), nameof(SuitabilityPostfix)));

            patched += TryPatch(harmony, logger,
                AccessTools.Method(typeof(InventoryPlayerHotbar), "GetSuitability",
                    new[] { typeof(ItemSlot), typeof(ItemSlot), typeof(bool) }),
                postfix: suitabilityPostfix);
            patched += TryPatch(harmony, logger,
                AccessTools.Method(typeof(InventoryPlayerBackpacks), "GetSuitability",
                    new[] { typeof(ItemSlot), typeof(ItemSlot), typeof(bool) }),
                postfix: suitabilityPostfix);

            logger.Notification("[{0}] slot lock patches applied to {1} method(s).", BurdenedModSystem.ModId, patched);
        }
    }

    /// <summary>Re-arms the static gate so a later world load re-applies patches.</summary>
    public static void Reset()
    {
        lock (Gate) applied = false;
    }

    /// <summary>
    /// Shared prefix for <see cref="ItemSlot.CanHold"/> and
    /// <see cref="ItemSlot.CanTakeFrom"/> across the whole ItemSlot hierarchy:
    /// a locked slot accepts nothing (result false, original skipped), and any 
    /// other slot falls through to the original method unchanged.
    /// </summary>
    public static bool RejectWhenLockedPrefix(ItemSlot __instance, ref bool __result)
    {
        if (!SlotLocks.IsLocked(__instance)) return true;
        __result = false;
        return false;
    }

    /// <summary>
    /// Locked slots must also lose the "best suited slot" search used by
    /// auto-pickup and shift-click. A negative score removes them from contention.
    /// </summary>
    public static void SuitabilityPostfix(ItemSlot targetSlot, ref float __result)
    {
        if (SlotLocks.IsLocked(targetSlot)) __result = -1f;
    }

    private static int TryPatch(Harmony harmony, ILogger logger, MethodInfo? target,
        HarmonyMethod? prefix = null, HarmonyMethod? postfix = null)
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

    /// <summary>
    /// Every <see cref="ItemSlot"/>-derived type in the loaded assemblies that
    /// DECLARES a method named <paramref name="methodName"/>. All mod assemblies
    /// are loaded before ModSystem.Start runs.
    /// </summary>
    private static IEnumerable<MethodInfo> FindSlotOverrides(string methodName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type?[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }
            catch { continue; }

            foreach (Type? type in types)
            {
                if (type == null || !typeof(ItemSlot).IsAssignableFrom(type)) continue;

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }
                catch { continue; }

                foreach (MethodInfo method in methods)
                {
                    if (method.Name != methodName) continue;
                    if (method.IsAbstract || method.IsGenericMethodDefinition) continue;
                    yield return method;
                }
            }
        }
    }
}
