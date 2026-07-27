using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;

namespace Burdened.Patches;

/// <summary>
/// Shared failure containment for every patch class. A renamed or removed
/// engine member must cost the one behavior that needed it, never the mod load:
/// an unresolved lookup and a refused patch both degrade to a warning and a
/// zero, so the remaining Apply calls in Start*Side still run.
///
/// Every call site names its target, because the name is the only part of a
/// failure that survives into a user's log file.
/// </summary>
internal static class PatchSupport
{
    /// <summary>
    /// Applies one patch. Returns 1 when it took effect, 0 when the target was missing or rejected the patch
    /// </summary>
    public static int TryPatch(
        Harmony harmony,
        ILogger logger,
        MethodBase? target,
        string targetName,
        HarmonyMethod? prefix = null,
        HarmonyMethod? postfix = null,
        HarmonyMethod? finalizer = null)
    {
        if (target == null)
        {
            logger.Warning(
                "[{0}] Could not resolve {1}. The behavior that needs it is disabled.",
                BurdenedModSystem.ModId, targetName);
            return 0;
        }

        try
        {
            harmony.Patch(target, prefix: prefix, postfix: postfix, finalizer: finalizer);
            return 1;
        }
        catch (Exception e)
        {
            logger.Warning(
                "[{0}] Could not patch {1}: {2}",
                BurdenedModSystem.ModId, targetName, e.Message);
            return 0;
        }
    }

    /// <summary>
    /// One of Burdened's own patch methods, resolved by name. 
    /// A null here indicates a bug in this mod rather than a game-update issue.
    /// </summary>
    public static HarmonyMethod? Hook(ILogger logger, Type owner, string methodName)
    {
        MethodInfo? method = AccessTools.Method(owner, methodName);
        if (method != null) return new HarmonyMethod(method);

        logger.Error(
            "[{0}] Patch method {1}.{2} is missing. This is a Burdened bug.",
            BurdenedModSystem.ModId, owner.Name, methodName);
        return null;
    }
}
