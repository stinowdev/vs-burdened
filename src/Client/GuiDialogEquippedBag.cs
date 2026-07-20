using System;
using System.Collections.Generic;
using Burdened.Bags;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Burdened.Client;

/// <summary>
/// A selective view over the player's existing backpack inventory. It owns no
/// inventory state or packet protocol; slot clicks use the normal player
/// inventory packets.
/// </summary>
internal sealed class GuiDialogEquippedBag : GuiDialog
{
    private static readonly Dictionary<int, GuiDialogEquippedBag> Dialogs = new();

    private readonly InventoryPlayerBackpacks backpacks;
    private readonly int bagIndex;
    private readonly int[] contentSlotIds;
    private readonly int collectibleId;
    private BagContentsSlotGrid? contentsGrid;

    private GuiDialogEquippedBag(ICoreClientAPI api, InventoryPlayerBackpacks backpacks, int bagIndex)
        : base(api)
    {
        this.backpacks = backpacks;
        this.bagIndex = bagIndex;
        contentSlotIds = BagSupport.ContentSlotIds(backpacks, bagIndex);
        collectibleId = backpacks.bagSlots[bagIndex].Itemstack!.Collectible.Id;
        backpacks.SlotModified += OnBackpackSlotModified;
        Compose();
    }

    public override string ToggleKeyCombinationCode => null!;

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
            || !BagSupport.IsBag(backpacks.bagSlots[bagIndex].Itemstack))
        {
            return;
        }

        GuiDialogEquippedBag dialog = new GuiDialogEquippedBag(api, backpacks, bagIndex);
        if (dialog.contentSlotIds.Length == 0)
        {
            dialog.Dispose();
            return;
        }

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
        // Vanilla ground-stored bag dialogs use four columns.
        int columns = Math.Min(4, Math.Max(1, contentSlotIds.Length));
        int rows = (int)Math.Ceiling(contentSlotIds.Length / (double)columns);
        double padding = GuiStyle.ElementToDialogPadding;

        ElementBounds gridBounds = ElementStdBounds.SlotGrid(
            EnumDialogArea.None, 0, 32, columns, rows);
        ElementBounds dialogBounds = gridBounds
            .ForkBoundingParent(padding, padding, padding, padding)
            .WithAlignment(EnumDialogArea.CenterMiddle)
            .WithFixedAlignmentOffset((bagIndex - 1.5) * 70, (bagIndex % 2) * 45 - 22.5);

        string title = backpacks.bagSlots[bagIndex].GetStackName();
        GuiComposer composer = capi.Gui
            .CreateCompo($"burdened-equipped-bag-{bagIndex}", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar(title, () => TryClose());

        contentsGrid = new BagContentsSlotGrid(
            capi, backpacks, SendInventoryPacket, columns, contentSlotIds, gridBounds);
        composer.AddInteractiveElement(contentsGrid, "contents");
        GuiElementItemSlotGridBase.UpdateLastSlotGridFlag(composer);
        SingleComposer = composer.Compose();
    }

    private void SendInventoryPacket(object packet)
    {
        capi.Network.SendPacketClient(packet);
    }

    private void OnBackpackSlotModified(int slotId)
    {
        if (slotId != bagIndex) return;

        contentsGrid?.RequestRefresh();

        ItemSlot equip = backpacks.bagSlots[bagIndex];
        int[] currentIds = BagSupport.ContentSlotIds(backpacks, bagIndex);
        if (equip.Empty
            || !BagSupport.IsBag(equip.Itemstack)
            || equip.Itemstack.Collectible.Id != collectibleId
            || currentIds.Length != contentSlotIds.Length)
        {
            Close(bagIndex);
        }
    }

    public override void OnGuiClosed()
    {
        SingleComposer?.GetSlotGrid("contents")?.OnGuiClosed(capi);
        base.OnGuiClosed();
    }

    public override void Dispose()
    {
        backpacks.SlotModified -= OnBackpackSlotModified;
        base.Dispose();
    }

    /// <summary>
    /// InventoryPlayerBackpacks reports bag-content changes by dirtying the
    /// owning equip-slot id. A normal selective grid assumes every dirty id is
    /// visible and crashes when it receives that equip id. This adapter maps
    /// the notification to this bag's visible content ids while preserving the
    /// original dirty ids for the vanilla hotbar grid.
    /// </summary>
    private sealed class BagContentsSlotGrid : GuiElementItemSlotGrid
    {
        private readonly InventoryBase owner;
        private readonly HashSet<int> visibleIds;
        private bool refreshRequested = true;

        public BagContentsSlotGrid(
            ICoreClientAPI api,
            InventoryBase owner,
            Action<object> sendPacket,
            int columns,
            int[] visibleSlots,
            ElementBounds bounds)
            : base(api, owner, sendPacket, columns, visibleSlots, bounds)
        {
            this.owner = owner;
            visibleIds = new HashSet<int>(visibleSlots);
        }

        public void RequestRefresh()
        {
            refreshRequested = true;
        }

        public override void PostRenderInteractiveElements(float deltaTime)
        {
            List<int> hiddenDirtyIds = new List<int>();
            foreach (int dirtyId in owner.DirtySlots)
            {
                if (!visibleIds.Contains(dirtyId)) hiddenDirtyIds.Add(dirtyId);
            }

            foreach (int dirtyId in hiddenDirtyIds) owner.DirtySlots.Remove(dirtyId);
            if (refreshRequested)
            {
                foreach (int visibleId in visibleIds) owner.DirtySlots.Add(visibleId);
            }

            try
            {
                base.PostRenderInteractiveElements(deltaTime);
                refreshRequested = false;
            }
            finally
            {
                foreach (int dirtyId in hiddenDirtyIds) owner.DirtySlots.Add(dirtyId);
            }
        }
    }
}
