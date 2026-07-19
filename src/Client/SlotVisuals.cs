using System;
using Burdened.Config;
using Burdened.Inventory;
using Cairo;
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
    /// <summary>Custom icon key — Cairo-drawn, not SVG (DrawSvg was painting the HUD black).</summary>
    private const string BackBagIcon = "burdened-backpack";

    private readonly ICoreClientAPI capi;
    private bool iconsRegistered;

    public SlotVisuals(ICoreClientAPI capi)
    {
        this.capi = capi;
    }

    /// <summary>
    /// Registers a Cairo backpack silhouette for the immersive B slot.
    /// Drawn the same way as vanilla built-ins (no DrawSvg).
    /// </summary>
    public void RegisterIcons()
    {
        if (iconsRegistered) return;

        capi.Gui.Icons.CustomIcons[BackBagIcon] = DrawBackpackIcon;
        iconsRegistered = true;
    }

    public void TryApply()
    {
        RegisterIcons();

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
            int usable = cfg.EffectiveBagSlots();
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

    /// <summary>
    /// Chunky stair-stepped backpack silhouette in the same 450² design space
    /// and draw style as vanilla <c>Drawbasket_svg</c> (orthogonal LineTo pixels,
    /// SolidPattern, slight lean) so it reads as a game icon, not a modern glyph.
    /// </summary>
    private static void DrawBackpackIcon(Context ctx, int x, int y, float width, float height, double[] rgba)
    {
        ctx.Save();
        Pattern? pattern = null;
        try
        {
            const float design = 450f;
            float scale = Math.Min(width / design, height / design);
            Matrix matrix = ctx.Matrix;
            matrix.Translate(
                x + Math.Max(0f, (width - design * scale) / 2f),
                y + Math.Max(0f, (height - design * scale) / 2f));
            matrix.Scale(scale, scale);
            ctx.Matrix = matrix;
            ctx.Operator = Operator.Over;

            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            ctx.SetSource(pattern);
            ctx.NewPath();

            // Main pack body, stepped outline, lean matching the basket tilt.
            ctx.MoveTo(160, 90);
            ctx.LineTo(140, 90);
            ctx.LineTo(140, 100);
            ctx.LineTo(120, 100);
            ctx.LineTo(120, 110);
            ctx.LineTo(110, 110);
            ctx.LineTo(110, 130);
            ctx.LineTo(100, 130);
            ctx.LineTo(100, 160);
            ctx.LineTo(90, 160);
            ctx.LineTo(90, 220);
            ctx.LineTo(80, 220);
            ctx.LineTo(80, 300);
            ctx.LineTo(90, 300);
            ctx.LineTo(90, 340);
            ctx.LineTo(100, 340);
            ctx.LineTo(100, 360);
            ctx.LineTo(120, 360);
            ctx.LineTo(120, 370);
            ctx.LineTo(160, 370);
            ctx.LineTo(160, 380);
            ctx.LineTo(240, 380);
            ctx.LineTo(240, 370);
            ctx.LineTo(300, 370);
            ctx.LineTo(300, 360);
            ctx.LineTo(330, 360);
            ctx.LineTo(330, 350);
            ctx.LineTo(350, 350);
            ctx.LineTo(350, 330);
            ctx.LineTo(360, 330);
            ctx.LineTo(360, 280);
            ctx.LineTo(370, 280);
            ctx.LineTo(370, 200);
            ctx.LineTo(360, 200);
            ctx.LineTo(360, 150);
            ctx.LineTo(350, 150);
            ctx.LineTo(350, 120);
            ctx.LineTo(330, 120);
            ctx.LineTo(330, 100);
            ctx.LineTo(300, 100);
            ctx.LineTo(300, 90);
            ctx.LineTo(240, 90);
            ctx.ClosePath();

            // Top carry strap (U), chunky steps.
            ctx.MoveTo(180, 90);
            ctx.LineTo(180, 70);
            ctx.LineTo(190, 70);
            ctx.LineTo(190, 50);
            ctx.LineTo(210, 50);
            ctx.LineTo(210, 40);
            ctx.LineTo(260, 40);
            ctx.LineTo(260, 50);
            ctx.LineTo(280, 50);
            ctx.LineTo(280, 70);
            ctx.LineTo(290, 70);
            ctx.LineTo(290, 90);
            ctx.LineTo(270, 90);
            ctx.LineTo(270, 70);
            ctx.LineTo(250, 70);
            ctx.LineTo(250, 60);
            ctx.LineTo(220, 60);
            ctx.LineTo(220, 70);
            ctx.LineTo(200, 70);
            ctx.LineTo(200, 90);
            ctx.ClosePath();

            // Front pocket lip, solid block like basket weave nubs.
            ctx.MoveTo(160, 210);
            ctx.LineTo(160, 200);
            ctx.LineTo(180, 200);
            ctx.LineTo(180, 190);
            ctx.LineTo(280, 190);
            ctx.LineTo(280, 200);
            ctx.LineTo(300, 200);
            ctx.LineTo(300, 210);
            ctx.LineTo(310, 210);
            ctx.LineTo(310, 290);
            ctx.LineTo(300, 290);
            ctx.LineTo(300, 300);
            ctx.LineTo(180, 300);
            ctx.LineTo(180, 290);
            ctx.LineTo(160, 290);
            ctx.LineTo(160, 210);
            ctx.ClosePath();

            // Pocket interior hole (even-odd via reverse subpath).
            ctx.MoveTo(180, 220);
            ctx.LineTo(280, 220);
            ctx.LineTo(280, 280);
            ctx.LineTo(180, 280);
            ctx.ClosePath();

            // Flap buckle nub.
            ctx.MoveTo(210, 140);
            ctx.LineTo(210, 120);
            ctx.LineTo(250, 120);
            ctx.LineTo(250, 140);
            ctx.LineTo(240, 140);
            ctx.LineTo(240, 160);
            ctx.LineTo(220, 160);
            ctx.LineTo(220, 140);
            ctx.ClosePath();

            ctx.FillRule = FillRule.EvenOdd;
            ctx.Fill();
        }
        finally
        {
            pattern?.Dispose();
            ctx.Restore();
        }
    }
}
