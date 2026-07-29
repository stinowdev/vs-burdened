using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
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

    public const string BackRole = "back";
    public const string WaistRole = "waist";

    // F01: usable hotbar slots, left-aligned; the rest are locked + hidden.
    public int HotbarSlots { get; set; } = MaxHotbarSlots;

    // F02: usable bag-equip slots. Ignored while ImmersiveCarryingMode is on
    // (that mode owns the bag slot semantics: L / B / R).
    public int BagSlots { get; set; } = MaxBagSlots;

    // F03 / D03: L (waist bag) / B (backpack) / R (waist bag) role rules.
    public bool ImmersiveCarryingMode { get; set; } = false;

    // F04 / D05: the inventory dialog ("E") shows only crafting; bag contents
    // are hidden. Bag-equip slots stay on the hotbar HUD. F08 / F10 provide
    // direct access to placed and equipped bag storage.
    public bool HideBagContentsInDialog { get; set; } = true;

    // F06 / D06: the offhand manually accepts non-bag items.
    public bool OffhandHoldsAnything { get; set; } = true;

    // F08 / F10: Burdened's complete bag interaction contract: floor bags open
    // with RMB and Shift+RMB equips them; equipped bags open with RMB and
    // Shift+click/RMB places them. Contents remain on the bag stack.
    public bool ImprovedBagInteractions { get; set; } = true;

    // F03 / D13: item code -> "back" or "waist", highest priority in the role
    // lookup. Codes may use wildcards ("othermod:rucksack-*") and an entry with
    // no domain is read as "game:". This is how a server owner classifies a bag
    // whose author never declared a role, or overrides one that declared wrong.
    public Dictionary<string, string>? BagRoleOverrides { get; set; }

    private readonly List<KeyValuePair<AssetLocation, string>> parsedRoleOverrides = new();

    /// <summary>
    /// <see cref="BagRoleOverrides"/> parsed once by <see cref="Sanitize"/>.
    /// The lookup runs per bag-equip check, so codes are not re-parsed there.
    /// Ordered as written, and the first pattern that matches wins.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<KeyValuePair<AssetLocation, string>> RoleOverrides => parsedRoleOverrides;

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
        ParseRoleOverrides();
    }

    /// <summary>
    /// Drops entries a hand-edit or an old version could have left unusable, so
    /// one bad line cannot take the rest of the map with it.
    /// </summary>
    private void ParseRoleOverrides()
    {
        parsedRoleOverrides.Clear();
        if (BagRoleOverrides == null) return;

        foreach (KeyValuePair<string, string> entry in BagRoleOverrides)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)) continue;

            string role = entry.Value?.Trim().ToLowerInvariant() ?? string.Empty;
            if (role != BackRole && role != WaistRole) continue;

            AssetLocation? code;
            try
            {
                code = AssetLocation.Create(entry.Key.Trim().ToLowerInvariant());
            }
            catch (Exception)
            {
                continue;
            }

            if (code == null) continue;
            parsedRoleOverrides.Add(new KeyValuePair<AssetLocation, string>(code, role));
        }
    }
}
