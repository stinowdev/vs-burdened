using System;
using System.Linq;
using Burdened.Config;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Burdened.Patches;

/// <summary>
/// F01: Hotbar presentation. Locked hotbar slots are completely removed from the HUD,
/// not merely greyed out. The on-screen bar shrinks to the configured usable slot count.
/// This is achieved by patching the <see cref="GuiElementItemSlotGrid"/> constructor:
/// only the grid for the local player's hotbar inventory is modified. Slot ids at or beyond
/// the configured count are removed from visibleSlots, and the column count / bounds
/// are narrowed accordingly. The offhand slot is a separate element and left untouched. 
/// Applies client-side only.
/// </summary>
public static class HotbarHudPatches
{
    private static readonly object Gate = new object();
    private static bool applied;
    private static bool warned;

    public static void Apply(Harmony harmony, ICoreClientAPI capi)
    {
        lock (Gate)
        {
            if (applied) return;
            applied = true;

            var ctor = AccessTools.Constructor(typeof(GuiElementItemSlotGrid), new[]
            {
                typeof(ICoreClientAPI), typeof(IInventory), typeof(Action<object>),
                typeof(int), typeof(int[]), typeof(ElementBounds)
            });

            if (ctor == null)
            {
                capi.Logger.Warning("[{0}] GuiElementItemSlotGrid ctor not found; hotbar slots stay full width.", BurdenedModSystem.ModId);
                return;
            }

            harmony.Patch(ctor, prefix: new HarmonyMethod(
                AccessTools.Method(typeof(HotbarHudPatches), nameof(NarrowHotbarGridPrefix))));
        }
    }

    public static void Reset()
    {
        lock (Gate) applied = false;
    }

    public static void NarrowHotbarGridPrefix(ICoreClientAPI capi, IInventory inventory, ref int cols, ref int[] visibleSlots, ElementBounds bounds)
    {
        try
        {
            BurdenedConfig? cfg = SlotLocks.Config;
            if (cfg == null || visibleSlots == null || inventory == null || capi == null) return;

            int usable = cfg.HotbarSlots;
            if (usable >= SlotLocks.VanillaHotbarSlots) return;

            // Only the local player's hotbar grid; every other slot grid (chests,
            // creative, bag contents...) is left exactly as vanilla built it.
            IInventory? hotbar = capi.World?.Player?.InventoryManager?.GetHotbarInventory();
            if (hotbar == null || !ReferenceEquals(inventory, hotbar)) return;

            int origCount = visibleSlots.Length;
            int[] kept = visibleSlots.Where(id => id >= 0 && id < usable).ToArray();
            if (kept.Length == 0 || kept.Length == origCount) return;

            visibleSlots = kept;
            if (cols >= origCount) cols = kept.Length; // single-row hotbar
            if (bounds != null && bounds.fixedWidth > 0)
                bounds.fixedWidth = bounds.fixedWidth * kept.Length / origCount;
        }
        catch (Exception e)
        {
            if (!warned)
            {
                warned = true;
                capi?.Logger.Warning("[{0}] Could not narrow the hotbar grid: {1}", BurdenedModSystem.ModId, e);
            }
        }
    }
}
