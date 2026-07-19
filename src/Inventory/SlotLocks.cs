using System;
using Burdened.Config;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Burdened.Inventory;

/// <summary>
/// The lock is evaluated dynamically on every accept-attempt rather than by
/// swapping slot instances at construction time. The player inventories are
/// built during login, and a per-call check needs no rebuild when the config
/// changes and never races inventory construction.
/// </summary>
public static class SlotLocks
{
    /// <summary>
    /// The scrollable hotbar is ids 0..9. The offhand and skill slots live 
    /// in the same <see cref="InventoryPlayerHotbar"/> at higher ids, so locking 
    /// must never touch anything at or beyond this index.
    /// </summary>
    public const int VanillaHotbarSlots = 10;

    /// <summmary>
    /// Assigned by the server once the config is loaded. While null, nothing is locked.
    /// </summary>
    public static BurdenedConfig? Config;

    /// <summary>
    /// True when slot is a player hotbar or bag-equip slot that the configured
    /// slot counts have disabled. Everything else, including the offhand slot, 
    /// bag CONTENT slots, and any non-player inventory is never locked.
    /// </summary>
    public static bool IsLocked(ItemSlot? slot)
    {
        BurdenedConfig? cfg = Config;
        if (cfg == null || slot?.Inventory == null) return false;

        // F01: hotbar slots beyond the configured count. The offhand slot is an
        // ItemSlotOffhand inside this same inventory and must stay usable.
        if (slot.Inventory is InventoryPlayerHotbar hotbar)
        {
            if (slot is ItemSlotOffhand) return false;
            int slotId = hotbar.GetSlotId(slot);
            return slotId >= cfg.HotbarSlots && slotId < VanillaHotbarSlots;
        }

        // F02 / F03: bag EQUIP slots beyond the effective count (BagSlots, or
        // exactly 3 while immersive). Only the equip slots (ItemSlotBackpack)
        // are lockable. Bag CONTENT slots follow their bag.
        if (slot.Inventory is InventoryPlayerBackpacks backpacks && slot is ItemSlotBackpack)
        {
            int index = Array.IndexOf(backpacks.bagSlots, slot);
            return index < 0 || index >= cfg.EffectiveBagSlots;
        }

        return false;
    }
}
