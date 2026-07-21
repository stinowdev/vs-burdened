using System;
using System.Linq;
using System.Reflection;
using Burdened.Config;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace Burdened.Patches;

/// <summary>
/// F04 / D05: when <c>HideBagContentsInDialog</c> is true, the survival
/// inventory dialog ("E") shows only the crafting grid (and output). Bag
/// contents are omitted; bag-equip slots stay on the hotbar HUD. A zero-slot
/// excl grid is still registered under "slotgrid" so vanilla OnGuiClosed
/// keeps working. Client-only presentation of the server-synced config.
/// </summary>
public static class InventoryDialogPatches
{
    private static readonly object Gate = new object();

    private static readonly FieldInfo? SurvivalInvDialogField =
        AccessTools.Field(typeof(GuiDialogInventory), "survivalInvDialog");
    private static readonly FieldInfo? CreativeInvDialogField =
        AccessTools.Field(typeof(GuiDialogInventory), "creativeInvDialog");
    private static readonly FieldInfo? CraftingInvField =
        AccessTools.Field(typeof(GuiDialogInventory), "craftingInv");
    private static readonly FieldInfo? BackpackInvField =
        AccessTools.Field(typeof(GuiDialogInventory), "backpackInv");
    private static readonly FieldInfo? PrevRowsField =
        AccessTools.Field(typeof(GuiDialogInventory), "prevRows");
    private static readonly FieldInfo? CapiField =
        AccessTools.Field(typeof(GuiDialog), "capi");

    private static readonly MethodInfo? CloseIconPressedMethod =
        AccessTools.Method(typeof(GuiDialogInventory), "CloseIconPressed");
    private static readonly MethodInfo? SendInvPacketMethod =
        AccessTools.Method(typeof(GuiDialogInventory), "SendInvPacket");

    private static bool applied;
    private static bool warned;
    private static ICoreClientAPI? capi;

    public static void Apply(Harmony harmony, ICoreClientAPI api)
    {
        capi = api;
        lock (Gate)
        {
            if (applied) return;
            applied = true;

            harmony.Patch(
                AccessTools.Method(typeof(GuiDialogInventory), "ComposeSurvivalInvDialog"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(InventoryDialogPatches), nameof(ComposeSurvivalPrefix))));
        }
    }

    public static void Reset()
    {
        lock (Gate)
        {
            applied = false;
            warned = false;
            capi = null;
        }
    }

    /// <summary>
    /// Rebuilds the inventory dialog after a late config sync. No-op while
    /// still connecting (player/inventories missing) or before vanilla has
    /// composed once; LevelFinalize retries once the world is up.
    /// </summary>
    public static void RecomposeIfPresent(ICoreClientAPI api)
    {
        // ComposeGui assumes World.Player + backpack/crafting inventories exist.
        if (api?.World?.Player?.InventoryManager?.GetOwnInventory("backpack") == null) return;
        if (api.World.Player.InventoryManager.GetOwnInventory("craftinggrid") == null) return;

        foreach (GuiDialog gui in api.Gui.LoadedGuis)
        {
            if (gui is not GuiDialogInventory inv) continue;

            // Dialog is registered early; skip until OnOwnPlayerDataReceived
            // has built survival or creative composers.
            bool composed = SurvivalInvDialogField?.GetValue(inv) != null
                || CreativeInvDialogField?.GetValue(inv) != null;
            if (!composed) return;

            inv.ComposeGui(firstBuild: false);
            break;
        }
    }

    public static bool ComposeSurvivalPrefix(GuiDialogInventory __instance)
    {
        BurdenedConfig? cfg = SlotLocks.Config;
        if (cfg == null || !cfg.HideBagContentsInDialog) return true;

        try
        {
            if (!ComposeCraftingOnly(__instance)) return true;
            return false;
        }
        catch (Exception e)
        {
            if (!warned)
            {
                warned = true;
                capi?.Logger.Warning(
                    "[{0}] Could not hide bag contents in the inventory dialog: {1}",
                    BurdenedModSystem.ModId, e);
            }
            return true;
        }
    }

    private static bool ComposeCraftingOnly(GuiDialogInventory dialog)
    {
        if (SurvivalInvDialogField == null || CraftingInvField == null
            || BackpackInvField == null || PrevRowsField == null || CapiField == null
            || CloseIconPressedMethod == null || SendInvPacketMethod == null)
        {
            return false;
        }

        if (CapiField.GetValue(dialog) is not ICoreClientAPI clientApi) return false;
        if (CraftingInvField.GetValue(dialog) is not IInventory craftingInv) return false;
        if (BackpackInvField.GetValue(dialog) is not IInventory backpackInv) return false;

        Action closePressed = (Action)Delegate.CreateDelegate(typeof(Action), dialog, CloseIconPressedMethod);
        Action<object> sendPacket = (Action<object>)Delegate.CreateDelegate(typeof(Action<object>), dialog, SendInvPacketMethod);

        double pad = GuiStyle.ElementToDialogPadding;
        double slotPad = GuiElementItemSlotGridBase.unscaledSlotPadding;
        double slotSize = GuiElementPassiveItemSlot.unscaledSlotSize;

        // Crafting-only layout. ForkBoundingParent rewrites crafting.fixedX/Y to
        // the padding, so the output slot must be placed after that (vanilla
        // centers it with +slotPad +slotSize under a 3-col grid).
        ElementBounds craftingBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 3, 3);
        ElementBounds outputBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 1, 1);

        const double outputGap = 10.0;
        ElementBounds dialogBounds = craftingBounds.ForkBoundingParent(
            pad,
            pad + 30.0,
            pad,
            pad + outputGap + outputBounds.fixedHeight);

        outputBounds.FixedUnder(craftingBounds, outputGap);
        outputBounds.fixedX = craftingBounds.fixedX + slotPad + slotSize;

        if (clientApi.Settings.Bool["immersiveMouseMode"])
        {
            dialogBounds.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-12.0, 0.0);
        }
        else
        {
            dialogBounds.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(20.0, 0.0);
        }

        // Keep a "slotgrid" excl element so OnGuiClosed does not NRE. Exclude
        // every backpack index so nothing renders or accepts clicks.
        int[] excludeAll = Enumerable.Range(0, backpackInv.Count).ToArray();
        ElementBounds hiddenBagBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 0.0);

        PrevRowsField.SetValue(dialog, (int)Math.Ceiling(backpackInv.Count / 6f));

        GuiComposer composer = clientApi.Gui.CreateCompo("inventory-backpack", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar(Lang.Get("Crafting"), closePressed)
            .AddItemSlotGridExcl(backpackInv, sendPacket, 6, excludeAll, hiddenBagBounds, "slotgrid")
            .AddItemSlotGrid(craftingInv, sendPacket, 3, new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, craftingBounds, "craftinggrid")
            .AddItemSlotGrid(craftingInv, sendPacket, 1, new int[1] { 9 }, outputBounds, "outputslot")
            .Compose();

        SurvivalInvDialogField.SetValue(dialog, composer);
        return true;
    }
}
