using System;
using Burdened.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Burdened.Inventory;

/// <summary>
/// D02 / F03: when a player joins carrying items in slots the config has since
/// disabled — or bags that violate immersive L/B/R taxonomy (D03) — those items
/// are returned through the normal give path (allowed hotbar slots, then
/// equipped bags). Anything that does not fit is dropped at the player's feet.
/// Bags are ejected before hotbar items so a hotbar item cannot overflow into
/// a bag that is itself about to be unequipped.
/// Items are never deleted.
/// </summary>
public static class SlotEjection
{
    public static void EjectLockedSlots(ICoreServerAPI sapi, IServerPlayer player, BurdenedConfig cfg)
    {
        int ejected = 0;

        if (player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName) is InventoryPlayerBackpacks backpacks)
        {
            int usable = cfg.EffectiveBagSlots();

            // F03/D03: wrong-role items in the still-visible L/B/R slots.
            if (cfg.ImmersiveCarryingMode)
            {
                for (int i = 0; i < usable && i < backpacks.bagSlots.Length; i++)
                {
                    ItemSlot slot = backpacks.bagSlots[i];
                    if (slot.Empty) continue;
                    if (!BagRoles.CanEquipInSlot(slot, slot.Itemstack))
                    {
                        ejected += Eject(sapi, player, slot);
                    }
                }
            }

            // F02 / F03: equip slots beyond the effective count.
            for (int i = usable; i < backpacks.bagSlots.Length; i++)
            {
                ejected += Eject(sapi, player, backpacks.bagSlots[i]);
            }
        }

        // F01 hotbar slots beyond the configured count (ids 0..9 only; the
        // offhand and skill slots live at higher ids and are left untouched).
        IInventory? hotbar = player.InventoryManager.GetHotbarInventory();
        if (hotbar != null)
        {
            int count = Math.Min(SlotLocks.VanillaHotbarSlots, hotbar.Count);
            for (int slotId = cfg.HotbarSlots; slotId < count; slotId++)
            {
                ejected += Eject(sapi, player, hotbar[slotId]);
            }
        }

        if (ejected > 0)
        {
            player.SendMessage(GlobalConstants.GeneralChatGroup,
                Lang.Get("burdened:disabled-slots-cleared"), EnumChatType.Notification);
        }
    }

    /// <summary>Empties one slot into the player (or the world), and returns 1 if it held anything.</summary>
    private static int Eject(ICoreServerAPI sapi, IServerPlayer player, ItemSlot slot)
    {
        if (slot.Empty) return 0;

        ItemStack? stack = slot.TakeOutWhole();
        slot.MarkDirty();
        if (stack == null || stack.StackSize <= 0) return 0;

        // This respects Burdened's slot locks + taxonomy via CanHold/suitability,
        // so items only ever land in still-usable slots.
        if (!player.InventoryManager.TryGiveItemstack(stack, true))
        {
            sapi.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ.Add(0, 0.5, 0));
        }
        return 1;
    }
}
