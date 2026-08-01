using Burdened.Config;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Burdened.Inventory;

/// <summary>
/// F11 / D14: applies the server-owned offhand hunger penalty through
/// Vintage Story's public hotbar property. The server refreshes the current
/// modifier immediately and remains authoritative.
/// </summary>
public static class OffhandHunger
{
    public static void Apply(IPlayer? player, BurdenedConfig config)
    {
        if (player?.InventoryManager.GetHotbarInventory() is not InventoryPlayerHotbar hotbar)
        {
            return;
        }

        hotbar.OffHandHungerPenalty = config.OffhandHungerPenalty;

        hotbar.updateSlotStatMods(player.InventoryManager.OffhandHotbarSlot);
    }
}
