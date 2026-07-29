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
    /// L/B/R only sorts real bags. Everything else that can enter a bag-equip
    /// slot is cargo, and immersive mode leaves it alone. A filled skep is the
    /// usual case: it has the Backpack storage flag but no bag behavior, so a
    /// bag slot is the only place vanilla will put it. Treating it as a bag
    /// would leave it with nowhere to go.
    ///
    /// Falling through also keeps the storage-flag check, so nothing gets in
    /// that vanilla would have refused.
    /// </summary>
    public static bool Accepts(Role role, ItemStack? stack)
    {
        if (stack == null) return true;
        if (!BagClassifier.IsEquippableBag(stack)) return true;

        return role switch
        {
            Role.Back => BagClassifier.IsTrueBackpack(stack),
            Role.Waist => BagClassifier.IsWaistBag(stack),
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
