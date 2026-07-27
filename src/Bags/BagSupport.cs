using System;
using System.Collections.Generic;
using Burdened.Inventory;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Burdened.Bags;

internal static class BagSupport
{
    public static bool SupportsGroundInteractions(ItemStack? stack)
    {
        if (stack?.Collectible is not CollectibleObject collectible
            || !BagClassifier.IsEquippableBag(stack)) return false;

        return collectible.GetBehavior<CollectibleBehaviorGroundStorable>() != null
            && collectible.GetBehavior<CollectibleBehaviorGroundStoredHeldBag>() != null;
    }

    /// <summary>
    /// Vanilla shifts the bag-equip range up by one when the skill slot at
    /// hotbar index 10 is occupied. See PlayerInventoryManager.ActiveHotbarSlot,
    /// which resolves the active number against the same offset (1.22.3).
    /// </summary>
    public static int SkillSlotOffset(IPlayer player)
    {
        IInventory? hotbar = player.InventoryManager.GetHotbarInventory();
        if (hotbar == null || hotbar.Count <= SlotLocks.VanillaHotbarSlots) return 0;
        return hotbar[SlotLocks.VanillaHotbarSlots].Empty ? 0 : 1;
    }

    /// <summary>
    /// The occupied bag-equip slot addressed by an active hotbar slot number,
    /// or null when that number addresses the hotbar, the skill slot, or an
    /// empty bag slot. The active number spans hotbar 0..9, an optional skill
    /// slot, then the bag-equip slots.
    /// </summary>
    public static ItemSlot? EquippedBagSlotAt(IPlayer? player, int activeSlotNumber)
    {
        if (player?.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks)
        {
            return null;
        }

        int bagIndex = activeSlotNumber - SlotLocks.VanillaHotbarSlots - SkillSlotOffset(player);
        if (bagIndex < 0 || bagIndex >= backpacks.bagSlots.Length) return null;

        ItemSlot slot = backpacks.bagSlots[bagIndex];
        return slot.Itemstack == null ? null : slot;
    }

    /// <summary>Whichever occupied bag-equip slot is selected right now, if any.</summary>
    public static ItemSlot? SelectedEquippedBagSlot(IPlayer? player)
    {
        return player == null
            ? null
            : EquippedBagSlotAt(player, player.InventoryManager.ActiveHotbarSlotNumber);
    }

    public static int? EquipIndexOf(IPlayer player, ItemSlot? slot)
    {
        if (slot == null
            || player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks)
        {
            return null;
        }

        for (int i = 0; i < backpacks.bagSlots.Length; i++)
        {
            if (ReferenceEquals(backpacks.bagSlots[i], slot)) return i;
        }

        return null;
    }

    public static ItemSlot? FindEmptyEquipSlot(IPlayer player, ItemSlot source)
    {
        if (player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks
            || SlotLocks.Config == null)
        {
            return null;
        }

        int count = Math.Min(SlotLocks.Config.EffectiveBagSlots(), backpacks.bagSlots.Length);
        for (int i = 0; i < count; i++)
        {
            ItemSlot target = backpacks.bagSlots[i];
            if (!target.Empty || SlotLocks.IsLocked(target)) continue;
            if (!BagRoles.CanEquipInSlot(target, source.Itemstack)) continue;
            if (target.CanHold(source)) return target;
        }

        return null;
    }

    public static int[] ContentSlotIds(InventoryPlayerBackpacks backpacks, int bagIndex)
    {
        List<int> ids = new List<int>();
        for (int i = backpacks.bagSlots.Length; i < backpacks.Count; i++)
        {
            if (backpacks[i] is ItemSlotBagContent content && content.BagIndex == bagIndex)
            {
                ids.Add(i);
            }
        }

        return ids.ToArray();
    }
}
