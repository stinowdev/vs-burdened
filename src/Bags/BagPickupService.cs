using Burdened.Inventory;
using Burdened.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Burdened.Bags;

/// <summary>
/// Server-authoritative floor-to-equip transfer. The client only requests the
/// operation and never predicts removal of the item or ground-storage block.
/// </summary>
internal static class BagPickupService
{
    public static void Request(ICoreClientAPI api, BlockPos position)
    {
        api.Network.GetChannel(BurdenedModSystem.ModId).SendPacket(new PickupFloorBagPacket
        {
            X = position.X,
            Y = position.InternalY,
            Z = position.Z,
        });
    }

    public static void Pickup(ICoreServerAPI api, IServerPlayer player, PickupFloorBagPacket packet)
    {
        if (SlotLocks.Config?.ImprovedBagInteractions != true) return;

        BlockPos position = new BlockPos(packet.X, packet.Y, packet.Z, player.Entity.Pos.Dimension);
        double maxDistance = player.WorldData.PickingRange + 1.5;
        if (player.Entity.Pos.XYZ.SquareDistanceTo(position.ToVec3d().Add(0.5, 0.5, 0.5))
            > maxDistance * maxDistance)
        {
            return;
        }

        if (api.World.BlockAccessor.GetChunkAtBlockPos(position) == null) return;
        if (api.World.BlockAccessor.GetBlockEntity(position) is not BlockEntityGroundStorage groundStorage) return;
        if (groundStorage.StorageProps?.Layout != EnumGroundStorageLayout.SingleCenter) return;

        ItemSlot floorSlot = groundStorage.Inventory[0];
        if (floorSlot.Empty || !BagSupport.IsBag(floorSlot.Itemstack)) return;

        if (!BlockBehaviorReinforcable.AllowRightClickPickup(api.World, position, player)) return;

        ItemSlot? equipSlot = BagSupport.FindEmptyEquipSlot(player, floorSlot);
        if (equipSlot == null)
        {
            api.World.Logger.Audit(
                "{0} could not pick up floor bag {1} at {2}: no compatible empty equip slot.",
                player.PlayerName,
                floorSlot.Itemstack?.Collectible.Code,
                position);
            return;
        }

        string? bagCode = floorSlot.Itemstack?.Collectible.Code?.ToString();
        if (floorSlot.TryPutInto(api.World, equipSlot, 1) != 1)
        {
            api.World.Logger.Audit(
                "{0} could not pick up floor bag {1} at {2}: slot transfer was rejected.",
                player.PlayerName,
                bagCode,
                position);
            return;
        }

        equipSlot.MarkDirty();
        ((BlockEntity)groundStorage).MarkDirty(true);

        if (groundStorage.Inventory.Empty)
        {
            api.World.BlockAccessor.SetBlock(0, position);
        }

        if (groundStorage.StorageProps?.PlaceRemoveSound != null)
        {
            api.World.PlaySoundAt(
                groundStorage.StorageProps.PlaceRemoveSound,
                position.X + 0.5,
                position.InternalY,
                position.Z + 0.5,
                player);
        }

        api.World.Logger.Audit(
            "{0} picked up floor bag {1} into equip slot {2} at {3}.",
            player.PlayerName,
            bagCode,
            equipSlot.Inventory.GetSlotId(equipSlot),
            position);
    }
}
