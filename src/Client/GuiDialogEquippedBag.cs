using System;
using System.Collections.Generic;
using Burdened.Bags;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Burdened.Client;

/// <summary>
/// A per-bag view over the player's backpack inventory. Each dialog owns its
/// GUI dirty state while slot operations are delegated to the real inventory.
/// </summary>
internal sealed class GuiDialogEquippedBag : GuiDialog
{
    private static readonly Dictionary<int, GuiDialogEquippedBag> Dialogs = new();

    private readonly InventoryPlayerBackpacks backpacks;
    private readonly BagViewInventory viewInventory;
    private readonly int bagIndex;
    private readonly int collectibleId;
    private GuiElementItemSlotGrid? contentsGrid;
    private bool remapQueued;
    private bool disposed;

    private GuiDialogEquippedBag(
        ICoreClientAPI api,
        InventoryPlayerBackpacks backpacks,
        int bagIndex,
        int[] contentSlotIds)
        : base(api)
    {
        this.backpacks = backpacks;
        this.bagIndex = bagIndex;
        collectibleId = backpacks.bagSlots[bagIndex].Itemstack!.Collectible.Id;
        viewInventory = new BagViewInventory(api, backpacks, bagIndex, contentSlotIds);
        backpacks.SlotModified += OnBackpackSlotModified;
        Compose();
    }

    public override string ToggleKeyCombinationCode => null!;
    public override bool PrefersUngrabbedMouse => false;

    public static void Toggle(ICoreClientAPI api, int bagIndex)
    {
        if (Dialogs.TryGetValue(bagIndex, out GuiDialogEquippedBag? existing))
        {
            if (existing.IsOpened()) existing.TryClose();
            else existing.TryOpen();
            return;
        }

        if (api.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)
                is not InventoryPlayerBackpacks backpacks
            || bagIndex < 0
            || bagIndex >= backpacks.bagSlots.Length
            || backpacks.bagSlots[bagIndex].Empty
            || !BagSupport.SupportsGroundInteractions(backpacks.bagSlots[bagIndex].Itemstack))
        {
            return;
        }

        int[] contentSlotIds = BagSupport.ContentSlotIds(backpacks, bagIndex);
        if (contentSlotIds.Length == 0) return;

        GuiDialogEquippedBag dialog = new GuiDialogEquippedBag(
            api, backpacks, bagIndex, contentSlotIds);
        Dialogs[bagIndex] = dialog;
        dialog.TryOpen();
    }

    public static void Close(int bagIndex)
    {
        if (!Dialogs.Remove(bagIndex, out GuiDialogEquippedBag? dialog)) return;
        if (dialog.IsOpened()) dialog.TryClose();
        dialog.Dispose();
    }

    public static void CloseAll()
    {
        foreach (GuiDialogEquippedBag dialog in Dialogs.Values)
        {
            if (dialog.IsOpened()) dialog.TryClose();
            dialog.Dispose();
        }

        Dialogs.Clear();
    }

    private void Compose()
    {
        // Match GuiDialogBlockEntityInventory: contained bags always reserve
        // four columns, use a six-pixel inset, and leave title-bar clearance.
        // The four equip positions retain a stable 2x2 screen layout.
        const int columns = 4;
        int rows = (int)Math.Ceiling(viewInventory.Count / (double)columns);
        double padding = GuiStyle.ElementToDialogPadding;
        double slotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;

        ElementBounds gridBounds = ElementStdBounds.SlotGrid(
            EnumDialogArea.None, slotPadding, slotPadding, columns, rows);
        ElementBounds insetBounds = gridBounds.ForkBoundingParent(6, 6, 6, 6);
        (double offsetX, double offsetY) = DialogOffset(bagIndex);
        ElementBounds dialogBounds = insetBounds
            .ForkBoundingParent(padding, padding + 20, padding, padding)
            .WithAlignment(EnumDialogArea.CenterMiddle)
            .WithFixedAlignmentOffset(offsetX, offsetY);

        string title = backpacks.bagSlots[bagIndex].GetStackName();
        int[] visibleSlots = new int[viewInventory.Count];
        for (int i = 0; i < visibleSlots.Length; i++) visibleSlots[i] = i;

        GuiComposer composer = capi.Gui
            .CreateCompo($"burdened-equipped-bag-{bagIndex}", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar(title, () => TryClose())
            .AddInset(insetBounds);

        contentsGrid = new GuiElementItemSlotGrid(
            capi, viewInventory, SendInventoryPacket, columns, visibleSlots, gridBounds);
        composer.AddInteractiveElement(contentsGrid, "contents");
        GuiElementItemSlotGridBase.UpdateLastSlotGridFlag(composer);
        SingleComposer = composer.Compose();
    }

    private static (double X, double Y) DialogOffset(int index)
    {
        double x = index % 2 == 0 ? -180 : 180;
        double y = index / 2 == 0 ? -120 : 120;
        return (x, y);
    }

    private void SendInventoryPacket(object packet)
    {
        capi.Network.SendPacketClient(packet);
    }

    private void OnBackpackSlotModified(int slotId)
    {
        // Equipping/unequipping any bag reloads BagInventory and can renumber
        // every content slot id and replace the ItemSlotBagContent instances.
        // Remap in this callback: queueing it leaves one render pass where the
        // view can resolve a stale parent id to null.
        if (slotId < backpacks.bagSlots.Length)
        {
            viewInventory.ClearDirty();
            if (TryRemapNow()) return;
            QueueRemapOrClose();
            return;
        }

        // The view has independent dirty state, so one dialog cannot consume or
        // reinterpret another dialog's (or the hotbar's) dirty slot ids.
        viewInventory.MarkAllDirty();
    }

    private void QueueRemapOrClose()
    {
        if (remapQueued) return;
        remapQueued = true;
        capi.Event.EnqueueMainThreadTask(RemapOrClose, "burdened-remap-equipped-bag");
    }

    private void RemapOrClose()
    {
        remapQueued = false;
        if (!Dialogs.ContainsKey(bagIndex)) return;
        if (TryRemapNow()) return;
        Close(bagIndex);
    }

    private bool TryRemapNow()
    {
        if (!Dialogs.ContainsKey(bagIndex)
            || bagIndex < 0
            || bagIndex >= backpacks.bagSlots.Length)
        {
            return false;
        }

        ItemSlot equip = backpacks.bagSlots[bagIndex];
        if (equip.Empty
            || !BagSupport.SupportsGroundInteractions(equip.Itemstack)
            || equip.Itemstack.Collectible.Id != collectibleId)
        {
            return false;
        }

        int[] currentIds = BagSupport.ContentSlotIds(backpacks, bagIndex);
        if (currentIds.Length != viewInventory.Count)
        {
            return false;
        }

        viewInventory.Remap(currentIds);
        RefreshGridSlotReferences();
        return true;
    }

    private void RefreshGridSlotReferences()
    {
        if (contentsGrid == null) return;

        // BagInventory reload creates new ItemSlotBagContent objects. The grid
        // caches the objects separately from the inventory indexer, so refresh
        // both caches together with the numeric mapping.
        for (int localId = 0; localId < viewInventory.Count; localId++)
        {
            ItemSlot current = viewInventory[localId];
            contentsGrid.availableSlots[localId] = current;
            contentsGrid.renderedSlots[localId] = current;
        }
    }

    public override void OnGuiClosed()
    {
        SingleComposer?.GetSlotGrid("contents")?.OnGuiClosed(capi);
        base.OnGuiClosed();
    }

    public override void Dispose()
    {
        if (disposed) return;
        disposed = true;
        backpacks.SlotModified -= OnBackpackSlotModified;
        base.Dispose();
    }

    /// <summary>
    /// Presents local ids 0..N to one GUI. Item slots remain owned by the real
    /// player inventory and activation is translated back to the current real
    /// slot id, preserving vanilla player-inventory packets.
    /// </summary>
    private sealed class BagViewInventory : InventoryGeneric
    {
        private readonly InventoryPlayerBackpacks parent;
        private int[] parentSlotIds;

        public BagViewInventory(
            ICoreClientAPI api,
            InventoryPlayerBackpacks parent,
            int bagIndex,
            int[] parentSlotIds)
            : base(parentSlotIds.Length, "burdenedbagview", $"{api.World.Player.PlayerUID}-{bagIndex}", api)
        {
            this.parent = parent;
            this.parentSlotIds = (int[])parentSlotIds.Clone();
            MarkAllDirty();
        }

        public override int Count => parentSlotIds.Length;

        public override ItemSlot this[int slotId]
        {
            // A server inventory update can replace BagInventory between GUI
            // callbacks. The local empty slot is a render-safe fallback until
            // the synchronous remap callback refreshes the mapping.
            get => parent[ParentSlotId(slotId)] ?? base[slotId];
            set => parent[ParentSlotId(slotId)] = value;
        }

        public override int GetSlotId(ItemSlot slot)
        {
            int parentId = parent.GetSlotId(slot);
            return Array.IndexOf(parentSlotIds, parentId);
        }

        public override object ActivateSlot(
            int slotId,
            ItemSlot sourceSlot,
            ref ItemStackMoveOperation op)
        {
            int parentSlotId = ParentSlotId(slotId);
            if (parentSlotId < 0 || parentSlotId >= parent.Count || parent[parentSlotId] == null)
            {
                return null!;
            }

            return parent.ActivateSlot(parentSlotId, sourceSlot, ref op);
        }

        public override void PerformNotifySlot(int slotId)
        {
            parent.PerformNotifySlot(ParentSlotId(slotId));
        }

        public void Remap(int[] newParentSlotIds)
        {
            if (newParentSlotIds.Length != parentSlotIds.Length)
            {
                throw new ArgumentException("Bag view capacity cannot change while composed.", nameof(newParentSlotIds));
            }

            parentSlotIds = (int[])newParentSlotIds.Clone();
            MarkAllDirty();
        }

        public void MarkAllDirty()
        {
            for (int i = 0; i < Count; i++) DirtySlots.Add(i);
        }

        public void ClearDirty()
        {
            DirtySlots.Clear();
        }

        private int ParentSlotId(int localSlotId)
        {
            if (localSlotId < 0 || localSlotId >= parentSlotIds.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(localSlotId));
            }

            return parentSlotIds[localSlotId];
        }
    }
}
