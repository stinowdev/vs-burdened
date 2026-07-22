using Vintagestory.API.Common;
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

    /// <summary>
    /// D03 back whitelist: leather backpack, sturdy backpack, hunter backpack.
    /// TODO: Make whitelisted backpacks configurable / mod expandable.
    /// </summary>
    public static bool IsTrueBackpack(ItemStack? stack)
    {
        if (stack?.Collectible is not CollectibleObject collectible
            || !IsEquippableBag(stack)) return false;

        string path = collectible.Code.Path;
        return path == "backpack-normal"
            || path == "backpack-sturdy"
            || path == "hunterbackpack";
    }

    /// <summary>
    /// Any equippable held bag outside the back whitelist uses a waist slot.
    /// TODO: Make whitelisted waist bags configurable / mod expandable.
    /// </summary>
    public static bool IsWaistBag(ItemStack? stack)
    {
        return IsEquippableBag(stack) && !IsTrueBackpack(stack);
    }
}
