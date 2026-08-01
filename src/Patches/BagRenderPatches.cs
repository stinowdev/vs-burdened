using System;
using Burdened.Client;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Burdened.Patches;

/// <summary>
/// F10 / F12: wraps vanilla player-inventory shape composition so the selected
/// bag remains hand-only and immersive L / B / R bags can be composed without
/// vanilla discarding duplicate attachment categories.
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

            int patched = PatchSupport.TryPatch(harmony, logger,
                AccessTools.Method(typeof(EntityBehaviorPlayerInventory),
                    nameof(EntityBehaviorPlayerInventory.OnTesselation)),
                "EntityBehaviorPlayerInventory.OnTesselation",
                prefix: PatchSupport.Hook(logger, typeof(BagRenderPatches), nameof(TessellationPrefix)),
                postfix: PatchSupport.Hook(logger, typeof(BagRenderPatches), nameof(TessellationPostfix)),
                finalizer: PatchSupport.Hook(logger, typeof(BagRenderPatches), nameof(TessellationFinalizer)));

            logger.Notification(
                "[{0}] immersive bag render patch applied to {1} method(s).", BurdenedModSystem.ModId, patched);
        }
    }

    public static void Reset()
    {
        lock (Gate) applied = false;
    }

    internal static void TessellationPrefix(
        EntityBehaviorPlayerInventory __instance,
        ref ImmersiveBagRenderer.HiddenBagState? __state)
    {
        ImmersiveBagRenderer.BeforeVanilla(__instance, ref __state);
    }

    internal static void TessellationPostfix(
        EntityBehaviorPlayerInventory __instance,
        ref Shape entityShape,
        string shapePathForLogging,
        ref bool shapeIsCloned,
        ref string[] willDeleteElements,
        ImmersiveBagRenderer.HiddenBagState? __state)
    {
        ImmersiveBagRenderer.AfterVanilla(
            __instance,
            __state,
            ref entityShape,
            shapePathForLogging,
            ref shapeIsCloned,
            ref willDeleteElements);
    }

    internal static Exception? TessellationFinalizer(
        Exception? __exception,
        ImmersiveBagRenderer.HiddenBagState? __state)
    {
        __state?.Restore();
        return __exception;
    }
}
