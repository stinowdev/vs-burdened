using System;
using System.Collections.Generic;
using Burdened.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Burdened.Inventory;

/// <summary>
/// Shared classification for items that vanilla recognizes as equippable
/// held bags. Interaction-specific capabilities are checked separately.
/// </summary>
internal static class BagClassifier
{
    public static bool IsEquippableBag(ItemStack? stack)
    {
        if (stack?.Collectible == null) return false;

        IHeldBag? heldBag = stack.Collectible.GetCollectibleInterface<IHeldBag>();
        return heldBag != null
            && heldBag.GetQuantitySlots(stack) > 0
            && (stack.Collectible.GetStorageFlags(stack) & EnumItemStorageFlags.Backpack) != 0;
    }

    private const string RoleAttribute = "bagRole";
    private const string AttachmentAttribute = "attachableToEntity";
    private const string AttachmentCategoryKey = "categoryCode";
    private const string BackAttachmentCategory = "backpack";

    public static bool IsTrueBackpack(ItemStack? stack)
    {
        if (stack?.Collectible is not CollectibleObject collectible
            || !IsEquippableBag(stack)) return false;

        return ResolvedRole(collectible) switch
        {
            BurdenedConfig.BackRole => true,
            BurdenedConfig.WaistRole => false,
            _ => AttachesAtTheBack(collectible),
        };
    }

    /// <summary>
    /// Fallback when no override or bagRole is set. Uses the game's attachment
    /// category instead of a hardcoded item list: if it says `backpack`, the bag
    /// sits on the back. That way any mod's bag that renders on the back counts,
    /// with no attribute and no config.
    ///
    /// Callers already checked IsEquippableBag, so saddles and bedrolls that
    /// share the attachment system never reach this.
    /// </summary>
    private static bool AttachesAtTheBack(CollectibleObject collectible)
    {
        JsonObject? attributes = collectible.Attributes;
        if (attributes == null) return false;

        string? category = attributes[AttachmentAttribute][AttachmentCategoryKey].AsString();
        return string.Equals(category, BackAttachmentCategory, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// D13: a `BagRoleOverrides` entry in burdened.json beats the item's own
    /// `burdened.bagRole` attribute, which beats the game's attachment category.
    /// The server owner always has the last word, while a mod that declares a
    /// role still works with nothing configured.
    /// </summary>
    private static string? ResolvedRole(CollectibleObject collectible)
    {
        AssetLocation? code = collectible.Code;
        IReadOnlyList<KeyValuePair<AssetLocation, string>>? overrides = SlotLocks.Config?.RoleOverrides;

        if (code != null && overrides != null)
        {
            for (int i = 0; i < overrides.Count; i++)
            {
                if (WildcardUtil.Match(overrides[i].Key, code)) return overrides[i].Value;
            }
        }

        return DeclaredRole(collectible);
    }

    /// <summary>
    /// Any equippable held bag that is not a back bag uses a waist slot. 
    /// Waist is the default, so a modded bag stays usable without declaring anything.
    /// </summary>
    public static bool IsWaistBag(ItemStack? stack)
    {
        return IsEquippableBag(stack) && !IsTrueBackpack(stack);
    }

    /// <summary>
    /// The role the item declares for itself, or null when it declares none:
    /// <code>"attributes": { "burdened": { "bagRole": "back" } }</code>
    /// This is what a bag mod ships so its content lands correctly with no
    /// configuration. Attributes is a plain field and is null for content
    /// without an attributes block, so it is checked before indexing.
    /// </summary>
    private static string? DeclaredRole(CollectibleObject collectible)
    {
        JsonObject? attributes = collectible.Attributes;
        if (attributes == null) return null;

        JsonObject role = attributes[BurdenedModSystem.ModId][RoleAttribute];
        return role.Exists ? role.AsString()?.ToLowerInvariant() : null;
    }
}
