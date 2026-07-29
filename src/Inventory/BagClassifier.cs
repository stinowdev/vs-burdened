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
    private const string BagAttribute = "backpack";
    private const string StorageFlagsKey = "storageFlags";

    /// <summary>
    /// CollectibleBehaviorHeldBag.GetStorageFlags fallback.
    /// Only used when a bag implements IHeldBag without the behavior.
    /// </summary>
    private const int UnrestrictedBagContents = 189;

    public static bool IsTrueBackpack(ItemStack? stack)
    {
        if (stack?.Collectible is not CollectibleObject collectible
            || !IsEquippableBag(stack)) return false;

        return ResolvedRole(collectible) switch
        {
            BurdenedConfig.BackRole => true,
            BurdenedConfig.WaistRole => false,
            _ => BelongsOnTheBack(stack, collectible),
        };
    }

    /// <summary>
    /// Fallback when no override or bagRole is set. Uses the game's own data
    /// instead of a list of item codes. A back bag must wear at the `backpack`
    /// position and hold ordinary items.
    ///
    /// The second check keeps the B slot free for a real pack. Quivers and
    /// mining bags also wear on the back, but they only take one kind of thing,
    /// so they count as waist bags here.
    ///
    /// Callers already checked IsEquippableBag, so saddles and bedrolls that
    /// share the attachment system never reach this.
    /// </summary>
    private static bool BelongsOnTheBack(ItemStack stack, CollectibleObject collectible)
    {
        JsonObject? attributes = collectible.Attributes;
        if (attributes == null) return false;

        string? category = attributes[AttachmentAttribute][AttachmentCategoryKey].AsString();
        if (!string.Equals(category, BackAttachmentCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HoldsOrdinaryItems(stack, attributes);
    }

    /// <summary>
    /// Asks vanilla what the bag's own slots accept, so a bag that overrides the
    /// behavior is answered by its own code. Falls back to reading the attribute
    /// with vanilla's default for a bag that implements IHeldBag some other way.
    /// </summary>
    private static bool HoldsOrdinaryItems(ItemStack stack, JsonObject attributes)
    {
        CollectibleBehaviorHeldBag? heldBag =
            stack.Collectible.GetBehavior<CollectibleBehaviorHeldBag>();

        EnumItemStorageFlags contents = heldBag != null
            ? heldBag.GetStorageFlags(stack)
            : (EnumItemStorageFlags)attributes[BagAttribute][StorageFlagsKey].AsInt(UnrestrictedBagContents);

        return (contents & EnumItemStorageFlags.General) != 0;
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
