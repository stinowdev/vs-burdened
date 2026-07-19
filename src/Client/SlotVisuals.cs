using System;
using Burdened.Config;
using Burdened.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Burdened.Client;

/// <summary>
/// Client-side locked slots are tinted dark and drawn as unavailable.
/// In immersive mode (F03), the three bag-equip slots get L/B/R icons.
/// Enforcement lives in Patches.SlotLockPatches; this class only changes
/// visual appearance.
/// </summary>
public class SlotVisuals
{
    private const string LockedSlotColor = "#343434";
    private const string WaistBagIcon = "basket";
    private const string BackBagIcon = "backpack";

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
            int usable = cfg.EffectiveBagSlots;
            for (int i = 0; i < backpacks.bagSlots.Length; i++)
            {
                ItemSlot slot = backpacks.bagSlots[i];
                bool locked = i >= usable;
                Mark(slot, locked);
                ApplyBagRoleIcon(slot, i, cfg.ImmersiveCarryingMode && !locked);
            }
        }

        // If the bar shrank under the current selection, pull it back in range.
        if (player.InventoryManager.ActiveHotbarSlotNumber >= cfg.HotbarSlots
            && player.InventoryManager.ActiveHotbarSlotNumber < SlotLocks.VanillaHotbarSlots)
        {
            player.InventoryManager.ActiveHotbarSlotNumber = 0;
        }
    }

    private static void Mark(ItemSlot slot, bool locked)
    {
        slot.HexBackgroundColor = locked ? LockedSlotColor : null;
        slot.DrawUnavailable = locked;
    }

    private static void ApplyBagRoleIcon(ItemSlot slot, int index, bool immersive)
    {
        if (!immersive)
        {
            // Vanilla ItemSlotBackpack default.
            slot.BackgroundIcon = WaistBagIcon;
            return;
        }

        slot.BackgroundIcon = index == BagRoles.SlotB ? BackBagIcon : WaistBagIcon;
    }
}
