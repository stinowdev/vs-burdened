using System;
using System.Collections.Generic;
using Burdened.Bags;
using Burdened.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Burdened.Client;

/// <summary>
/// F12 / D04: composes each immersive bag slot independently so vanilla's
/// attachment-category deduplication cannot discard one worn bag for another.
/// The game calls player shape composition synchronously on the client main
/// thread before it queues mesh construction.
/// </summary>
internal static class ImmersiveBagRenderer
{
    public static void BeforeVanilla(
        EntityBehaviorPlayerInventory behavior,
        ref HiddenBagState? state)
    {
        state = null;
        IPlayer? player = (behavior.entity as EntityPlayer)?.Player;
        if (player?.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks)
        {
            return;
        }

        ItemSlot? activeSlot = BagSupport.SelectedEquippedBagSlot(player);
        HiddenBagState candidate = new HiddenBagState(SlotLocks.Config?.ImmersiveCarryingMode == true);
        state = candidate;

        try
        {
            if (candidate.IsImmersive)
            {
                int count = Math.Min(BagRoles.ImmersiveSlotCount, backpacks.bagSlots.Length);
                for (int index = 0; index < count; index++)
                {
                    ItemSlot slot = backpacks.bagSlots[index];
                    if (BagClassifier.IsEquippableBag(slot.Itemstack))
                    {
                        candidate.Hide(slot, index, ReferenceEquals(slot, activeSlot));
                    }
                }
            }
            else
            {
                HideNonImmersiveCollisions(candidate, backpacks, activeSlot);
            }

            if (!candidate.HasHiddenSlots) state = null;
        }
        catch
        {
            candidate.Restore();
            state = null;
            throw;
        }
    }

    public static void AfterVanilla(
        EntityBehaviorPlayerInventory behavior,
        HiddenBagState? state,
        ref Shape entityShape,
        string shapePathForLogging,
        ref bool shapeIsCloned,
        ref string[] willDeleteElements)
    {
        if (state == null) return;

        state.Restore();
        if (!state.IsImmersive) return;

        foreach (HiddenBag bag in state.HiddenBags)
        {
            if (bag.WasSelected) continue;

            EnsureShapeClone(ref entityShape, ref shapeIsCloned);
            AddBag(behavior, bag, ref entityShape, shapePathForLogging, ref willDeleteElements);
        }
    }

    private static void HideNonImmersiveCollisions(
        HiddenBagState state,
        InventoryPlayerBackpacks backpacks,
        ItemSlot? activeSlot)
    {
        HashSet<string> packCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ItemSlot slot in backpacks.bagSlots)
        {
            if (ReferenceEquals(slot, activeSlot)
                || slot.Itemstack == null
                || !BagClassifier.IsTrueBackpack(slot.Itemstack))
            {
                continue;
            }

            string? category = AttachmentCategory(slot.Itemstack);
            if (category != null) packCategories.Add(category);
        }

        state.Hide(activeSlot, -1, wasSelected: true);

        if (packCategories.Count == 0) return;

        foreach (ItemSlot slot in backpacks.bagSlots)
        {
            if (ReferenceEquals(slot, activeSlot)
                || slot.Itemstack == null
                || !BagClassifier.IsWaistBag(slot.Itemstack))
            {
                continue;
            }

            string? category = AttachmentCategory(slot.Itemstack);
            if (category != null && packCategories.Contains(category))
            {
                state.Hide(slot, -1, wasSelected: false);
            }
        }
    }

    private static string? AttachmentCategory(ItemStack stack)
    {
        return stack.ItemAttributes?["attachableToEntity"]?["categoryCode"].AsString();
    }

    private static void AddBag(
        EntityBehaviorPlayerInventory behavior,
        HiddenBag bag,
        ref Shape entityShape,
        string shapePathForLogging,
        ref string[] willDeleteElements)
    {
        IAttachableToEntity? attachable = IAttachableToEntity.FromCollectible(bag.Stack.Collectible);
        if (attachable == null) return;

        string slotCode = bag.Index switch
        {
            BagRoles.SlotL => "burdened-l",
            BagRoles.SlotB => "burdened-b",
            BagRoles.SlotR => "burdened-r",
            _ => "burdened-bag"
        };

        entityShape = behavior.addGearToShape(
            entityShape,
            bag.Stack,
            new SlotAttachable(attachable, slotCode + "-"),
            slotCode,
            shapePathForLogging,
            ref willDeleteElements);
    }

    private static void EnsureShapeClone(ref Shape entityShape, ref bool shapeIsCloned)
    {
        if (shapeIsCloned) return;
        entityShape = entityShape.Clone();
        shapeIsCloned = true;
    }

    internal sealed class HiddenBagState
    {
        private readonly List<HiddenBag> hiddenBags = new List<HiddenBag>();

        public HiddenBagState(bool isImmersive)
        {
            IsImmersive = isImmersive;
        }

        public bool IsImmersive { get; }

        public bool HasHiddenSlots => hiddenBags.Count > 0;

        public IReadOnlyList<HiddenBag> HiddenBags => hiddenBags;

        public void Hide(ItemSlot? slot, int index, bool wasSelected)
        {
            if (slot?.Itemstack == null) return;

            hiddenBags.Add(new HiddenBag(slot, slot.Itemstack, index, wasSelected));
            slot.Itemstack = null;
        }

        public void Restore()
        {
            foreach (HiddenBag bag in hiddenBags)
            {
                if (bag.Slot.Itemstack == null) bag.Slot.Itemstack = bag.Stack;
            }
        }
    }

    internal sealed class HiddenBag
    {
        public HiddenBag(ItemSlot slot, ItemStack stack, int index, bool wasSelected)
        {
            Slot = slot;
            Stack = stack;
            Index = index;
            WasSelected = wasSelected;
        }

        public ItemSlot Slot { get; }

        public ItemStack Stack { get; }

        public int Index { get; }

        public bool WasSelected { get; }
    }

    /// <summary>
    /// Gives each bag unique element and texture names while keeping the
    /// wearable model's own attachment point unchanged.
    /// </summary>
    private sealed class SlotAttachable : IAttachableToEntity
    {
        private readonly IAttachableToEntity inner;
        private readonly string prefix;

        public SlotAttachable(IAttachableToEntity inner, string prefix)
        {
            this.inner = inner;
            this.prefix = prefix;
        }

        public int RequiresBehindSlots
        {
            get => inner.RequiresBehindSlots;
            set => inner.RequiresBehindSlots = value;
        }

        public bool IsAttachable(Entity toEntity, ItemStack itemStack) => inner.IsAttachable(toEntity, itemStack);

        public void CollectTextures(
            ItemStack stack,
            Shape shape,
            string texturePrefixCode,
            Dictionary<string, CompositeTexture> intoDict)
        {
            inner.CollectTextures(stack, shape, texturePrefixCode, intoDict);
        }

        public string? GetCategoryCode(ItemStack stack) => inner.GetCategoryCode(stack);

        public CompositeShape? GetAttachedShape(ItemStack stack, string slotCode) =>
            inner.GetAttachedShape(stack, slotCode);

        public string[]? GetDisableElements(ItemStack stack) => inner.GetDisableElements(stack);

        public string[]? GetKeepElements(ItemStack stack) => inner.GetKeepElements(stack);

        public string GetTexturePrefixCode(ItemStack stack) => prefix + inner.GetTexturePrefixCode(stack);
    }
}
