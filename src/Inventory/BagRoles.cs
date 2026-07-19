using System;
using Burdened.Config;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Burdened.Inventory;

/// <summary>
/// F03/D03: immersive bag-equip taxonomy.
/// Indices 0/1/2 are L (waist) / B (back) / R (waist). Slot 3 is unused
/// while immersive mode is on (locked via <see cref="SlotLocks"/>).
/// </summary>
public static class BagRoles
{
    public const int SlotL = 0;
    public const int SlotB = 1;
    public const int SlotR = 2;
    public const int ImmersiveSlotCount = 3;

    public enum Role
    {
        Waist,
        Back,
    }

    /// <summary>L / B / R for immersive indices 0..2; null otherwise.</summary>
    public static Role? RoleForIndex(int bagEquipIndex)
    {
        return bagEquipIndex switch
        {
            SlotL or SlotR => Role.Waist,
            SlotB => Role.Back,
            _ => null,
        };
    }

    /// <summary>
    /// D03 back whitelist: leather backpack, sturdy backpack, hunter backpack.
    /// TODO: Make whitelisted backpacks configurable / mod expandable
    /// </summary>
    public static bool IsTrueBackpack(ItemStack? stack)
    {
        string? path = stack?.Collectible?.Code?.Path;
        if (path == null) return false;
        return path == "backpack-normal"
            || path == "backpack-sturdy"
            || path == "hunterbackpack";
    }

    /// <summary>
    /// Bag-class storage that is not one of the three true backpacks
    /// (baskets, linen sack, mining bag, quiver, etc.).
    /// TODO: Make whitelisted waist bags configurable / mod expandable
    /// </summary>
    public static bool IsWaistBag(ItemStack? stack)
    {
        if (stack?.Collectible == null || IsTrueBackpack(stack)) return false;
        if (stack.Collectible.GetCollectibleInterface<IHeldBag>() == null) return false;
        return (stack.Collectible.GetStorageFlags(stack) & EnumItemStorageFlags.Backpack) != 0;
    }

    public static bool Accepts(Role role, ItemStack? stack)
    {
        if (stack == null) return true;
        return role switch
        {
            Role.Back => IsTrueBackpack(stack),
            Role.Waist => IsWaistBag(stack),
            _ => false,
        };
    }

    /// <summary>
    /// Whether <paramref name="stack"/> may sit in this bag-equip slot under
    /// the current config. Non-bag slots and non-immersive mode always allow
    /// (vanilla / F02 locking handle the rest).
    /// </summary>
    public static bool CanEquipInSlot(ItemSlot? slot, ItemStack? stack)
    {
        BurdenedConfig? cfg = SlotLocks.Config;
        if (cfg == null || !cfg.ImmersiveCarryingMode) return true;
        if (slot is not ItemSlotBackpack) return true;
        if (slot.Inventory is not InventoryPlayerBackpacks backpacks) return true;

        int index = Array.IndexOf(backpacks.bagSlots, slot);
        Role? role = RoleForIndex(index);
        if (role == null) return false; // immersive locks index 3+
        return Accepts(role.Value, stack);
    }
}
