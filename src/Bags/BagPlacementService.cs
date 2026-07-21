using System;
using Burdened.Inventory;
using Burdened.Network;
using Burdened.Patches;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Burdened.Bags;

internal static class BagPlacementService
{
    public static bool Request(ICoreClientAPI api, int bagIndex)
    {
        BlockSelection? selection = api.World.Player.CurrentBlockSelection;
        if (selection?.Face != BlockFacing.UP)
        {
            api.ShowChatMessage(Lang.Get("burdened:look-at-block-to-place-bag"));
            return false;
        }

        api.Network.GetChannel(BurdenedModSystem.ModId).SendPacket(new PlaceEquippedBagPacket
        {
            BagIndex = bagIndex,
            X = selection.Position.X,
            Y = selection.Position.InternalY,
            Z = selection.Position.Z,
            FaceIndex = selection.Face.Index,
            HitX = selection.HitPosition?.X ?? 0.5,
            HitY = selection.HitPosition?.Y ?? 1,
            HitZ = selection.HitPosition?.Z ?? 0.5,
        });
        return true;
    }

    public static void Place(ICoreServerAPI api, IServerPlayer player, PlaceEquippedBagPacket packet)
    {
        if (SlotLocks.Config?.ImprovedBagInteractions != true) return;
        if (packet.BagIndex < 0 || packet.BagIndex >= SlotLocks.Config.EffectiveBagSlots()) return;
        if (packet.FaceIndex != BlockFacing.UP.Index) return;

        if (player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks
            || packet.BagIndex >= backpacks.bagSlots.Length)
        {
            return;
        }

        ItemSlot source = backpacks.bagSlots[packet.BagIndex];
        if (source.Empty || SlotLocks.IsLocked(source) || !BagSupport.IsBag(source.Itemstack)) return;

        CollectibleBehaviorGroundStorable? groundBehavior =
            source.Itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>();
        if (groundBehavior?.StorageProps?.Layout != EnumGroundStorageLayout.SingleCenter) return;

        BlockPos supportPos = new BlockPos(packet.X, packet.Y, packet.Z, player.Entity.Pos.Dimension);
        double maxDistance = player.WorldData.PickingRange + 1.5;
        if (player.Entity.Pos.XYZ.SquareDistanceTo(supportPos.ToVec3d().Add(0.5, 0.5, 0.5))
            > maxDistance * maxDistance)
        {
            return;
        }

        if (!api.World.Claims.TryAccess(player, supportPos, EnumBlockAccessFlags.BuildOrBreak)) return;
        if (api.World.GetBlock(new AssetLocation("groundstorage")) is not BlockGroundStorage groundStorage) return;
        if (!api.World.BlockAccessor.GetBlock(supportPos).CanAttachBlockAt(
                api.World.BlockAccessor, groundStorage, supportPos, BlockFacing.UP))
        {
            return;
        }

        BlockPos placePos = supportPos.UpCopy();
        if (api.World.BlockAccessor.GetChunkAtBlockPos(placePos) == null) return;
        if (api.World.BlockAccessor.GetBlock(placePos).Replaceable < 6000) return;

        api.World.BlockAccessor.SetBlock(groundStorage.BlockId, placePos);
        if (api.World.BlockAccessor.GetBlockEntity(placePos) is not BlockEntityGroundStorage placed)
        {
            api.World.BlockAccessor.SetBlock(0, placePos);
            return;
        }

        double y = player.Entity.Pos.X - (supportPos.X + packet.HitX);
        double x = player.Entity.Pos.Z - (supportPos.Z + packet.HitZ);
        double quarterTurn = Math.PI / 2;
        placed.MeshAngle = (float)(Math.Round(Math.Atan2(y, x) / quarterTurn) * quarterTurn);
        placed.DetermineStorageProperties(source);

        ItemSlot transferSource = source;
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
        {
            transferSource = new DummySlot(source.Itemstack.Clone());
        }

        if (transferSource.TryPutInto(api.World, placed.Inventory[0], 1) <= 0)
        {
            api.World.BlockAccessor.SetBlock(0, placePos);
            return;
        }

        placed.DetermineStorageProperties(placed.Inventory[0]);
        BagInteractionPatches.SuppressNextFloorInteraction(player, placePos);
        placed.MarkDirty(true);
        source.MarkDirty();

        if (groundBehavior.StorageProps.PlaceRemoveSound != null)
        {
            api.World.PlaySoundAt(
                groundBehavior.StorageProps.PlaceRemoveSound,
                placePos.X + 0.5,
                placePos.InternalY,
                placePos.Z + 0.5,
                player);
        }

        api.World.Logger.Audit(
            "{0} placed equipped bag {1} from slot {2} at {3}.",
            player.PlayerName,
            placed.Inventory[0].Itemstack?.Collectible.Code,
            packet.BagIndex,
            placePos);
    }
}
