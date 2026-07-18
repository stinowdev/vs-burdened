using System;
using Burdened.Config;
using Burdened.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Burdened.Client;

/// <summary>
/// Client-side locked slots are tinted dark and drawn as unavailable, 
/// so the player can see which hotbar and bag-equip slots are locked. 
/// Enforcement lives in Patches.SlotLockPatches. This class only changes 
/// its visual appearance.
/// </summary>
public class SlotVisuals
{
    private const string LockedSlotColor = "#343434";

    private readonly ICoreClientAPI capi;

    public SlotVisuals(ICoreClientAPI capi)
    {
        this.capi = capi;
    }

    public void TryApply()
    {
        BurdenedConfig? cfg = SlotLocks.Config;
        IClientPlayer? player = capi.World?.Player;
        if (cfg == null || player == null) return;

        IInventory? hotbar = player.InventoryManager.GetHotbarInventory();
        if (hotbar != null)
        {
            int count = Math.Min(SlotLocks.VanillaHotbarSlots, hotbar.Count);
            for (int slotId = 0; slotId < count; slotId++)
            {
                Mark(hotbar[slotId], locked: slotId >= cfg.HotbarSlots);
            }
        }

        if (player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName) is InventoryPlayerBackpacks backpacks)
        {
            for (int i = 0; i < backpacks.bagSlots.Length; i++)
            {
                Mark(backpacks.bagSlots[i], locked: i >= cfg.BagSlots);
            }
        }

        // If the bar shrank under the current selection, pull it back in range.
        if (player.InventoryManager.ActiveHotbarSlotNumber >= cfg.HotbarSlots)
        {
            player.InventoryManager.ActiveHotbarSlotNumber = 0;
        }
    }

    private static void Mark(ItemSlot slot, bool locked)
    {
        slot.HexBackgroundColor = locked ? LockedSlotColor : null;
        slot.DrawUnavailable = locked;
    }
}
