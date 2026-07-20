using Vintagestory.API.MathTools;

namespace Burdened.Config;

/// <summary>
/// Loaded server-side from ModConfig/burdened.json and synced to every 
/// client on join (see ConfigSyncPacket). The client never reads the file,
/// it renders exactly what the server enforces.
/// </summary>
public class BurdenedConfig
{
    public const int MinHotbarSlots = 1;
    public const int MaxHotbarSlots = 10;   // vanilla hotbar size
    public const int MinBagSlots = 1;
    public const int MaxBagSlots = 4;       // vanilla bag-equip slot count
    public const int ImmersiveBagSlots = 3; // L / B / R while ImmersiveCarryingMode is on

    // F01: usable hotbar slots, left-aligned; the rest are locked + hidden.
    public int HotbarSlots { get; set; } = MaxHotbarSlots;

    // F02: usable bag-equip slots. Ignored while ImmersiveCarryingMode is on
    // (that mode owns the bag slot semantics: L / B / R).
    public int BagSlots { get; set; } = MaxBagSlots;

    // F03: L (waist bag) / B (backpack) / R (waist bag) slots + on-body rendering.
    public bool ImmersiveCarryingMode { get; set; } = false;

    // F04 / D05: the inventory dialog ("E") shows only crafting; bag contents
    // are hidden. Bag-equip slots stay on the hotbar HUD. Access bag storage
    // by placing the bag (F08) once that ships.
    public bool HideBagContentsInDialog { get; set; } = true;

    // F06 / D06: the offhand manually accepts non-bag items.
    public bool OffhandHoldsAnything { get; set; } = true;

    // F07: picked-up items overflow into equipped bags (vanilla priority:
    // hotbar first, then bag contents). Off = hotbar only.
    public bool AutoPickupToBags { get; set; } = true;

    // F08: floor bags open with RMB and move to a compatible bag-equip slot
    // with literal Ctrl+RMB. Contents and arrangement remain on the bag stack.
    public bool PlaceableBags { get; set; } = true;

    // F09: remember each container's dialog placement (fixed/movable + position)
    // by container identity, surviving pickup and re-placement. Client-side QoL.
    public bool RememberDialogPlacement { get; set; } = true;

    // F10: equipped bag slots open with RMB and place with Ctrl+click/RMB.
    public bool OpenBagsFromHotbar { get; set; } = true;

    /// <summary>
    /// Bag-equip slots the player may use right now. Immersive mode always
    /// exposes exactly three typed slots (L/B/R); otherwise F02's BagSlots.
    /// </summary>
    public int EffectiveBagSlots() => ImmersiveCarryingMode ? ImmersiveBagSlots : BagSlots;

    /// <summary>Clamps all values into their valid ranges (bad hand-edits, old versions).</summary>
    public void Sanitize()
    {
        HotbarSlots = GameMath.Clamp(HotbarSlots, MinHotbarSlots, MaxHotbarSlots);
        BagSlots = GameMath.Clamp(BagSlots, MinBagSlots, MaxBagSlots);
    }
}
