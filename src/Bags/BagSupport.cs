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
    public static bool IsBag(ItemStack? stack)
    {
        if (stack?.Collectible == null) return false;

        IHeldBag? heldBag = stack.Collectible.GetCollectibleInterface<IHeldBag>();
        return heldBag != null
            && heldBag.GetQuantitySlots(stack) > 0
            && stack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>() != null
            && stack.Collectible.GetBehavior<CollectibleBehaviorGroundStoredHeldBag>() != null;
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
