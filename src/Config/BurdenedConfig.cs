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


    // F01: usable hotbar slots, left-aligned; the rest are locked + hidden.
    public int HotbarSlots { get; set; } = MaxHotbarSlots;

    // F02: usable bag-equip slots. Ignored while ImmersiveCarryingMode is on
    // (that mode owns the bag slot semantics: L / B / R).
    public int BagSlots { get; set; } = MaxBagSlots;

    // F03: L (waist bag) / B (backpack) / R (waist bag) slots + on-body rendering.
    public bool ImmersiveCarryingMode { get; set; } = false;

    // F04: the inventory dialog ("E") shows only the crafting grid and the bag
    // equip slots; bag contents require placing the bag in the world.
    public bool HideBagContentsInDialog { get; set; } = true;

    // F06: the offhand slot accepts any item (usability stays vanilla).
    public bool OffhandHoldsAnything { get; set; } = true;

    // F07: picked-up items overflow into equipped bags (vanilla priority:
    // hotbar first, then bag contents). Off = hotbar only.
    public bool AutoPickupToBags { get; set; } = true;

    // F08: bags can be placed in the world and opened like chests
    // (right-click opens, sneak-interact picks up, arrangement preserved).
    public bool PlaceableBags { get; set; } = true;

    // F09: remember each container's dialog placement (fixed/movable + position)
    // by container identity, surviving pickup and re-placement. Client-side QoL.
    public bool RememberDialogPlacement { get; set; } = true;

    /// <summary>Clamps all values into their valid ranges (bad hand-edits, old versions).</summary>
    public void Sanitize()
    {
        HotbarSlots = GameMath.Clamp(HotbarSlots, MinHotbarSlots, MaxHotbarSlots);
        BagSlots = GameMath.Clamp(BagSlots, MinBagSlots, MaxBagSlots);
    }
}
