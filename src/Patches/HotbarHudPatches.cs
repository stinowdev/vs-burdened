using System;
using System.Linq;
using System.Reflection;
using Burdened.Config;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace Burdened.Patches;

/// <summary>
/// F01/F02: Shrinks the hotbar HUD to only show usable hotbar and bag slots,
/// centered tightly ([offhand | hotbar | bags]). Locked slots are hidden.
/// The background strip shrinks to fit. Runs after vanilla ComposeGuis.
/// Client-only.
/// </summary>
public static class HotbarHudPatches
{
    private static readonly object Gate = new object();
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
                AccessTools.Method(typeof(HudHotbar), nameof(HudHotbar.ComposeGuis)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(HotbarHudPatches), nameof(RepackPostfix))));
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

    public static void RepackPostfix(HudHotbar __instance)
    {
        try
        {
            BurdenedConfig? cfg = SlotLocks.Config;
            if (cfg == null) return;

            int hotbarCount = cfg.HotbarSlots;
            int bagCount = cfg.EffectiveBagSlots;
            // Immersive mode always needs a 3-slot bag strip (L/B/R)
            bool vanillaLayout = !cfg.ImmersiveCarryingMode
                && hotbarCount >= SlotLocks.VanillaHotbarSlots
                && bagCount >= BurdenedConfig.MaxBagSlots;
            if (vanillaLayout) return;

            GuiComposer? composer = __instance.Composers?["hotbar"];
            if (composer == null) return;

            GuiElementItemSlotGrid? offhand = composer.GetSlotGrid("offhandgrid");
            GuiElementItemSlotGrid? hotbar = composer.GetSlotGrid("hotbargrid");
            GuiElementItemSlotGrid? bags = composer.GetSlotGrid("backpackgrid");
            if (offhand == null || hotbar == null || bags == null) return;

            ElementBounds? strip = hotbar.Bounds?.ParentBounds;
            if (strip == null || hotbar.Bounds == null || offhand.Bounds == null || bags.Bounds == null) return;

            // Slot pitch: height of one slot row (51 in vanilla).
            double pitch = hotbar.Bounds.fixedHeight;
            if (pitch < 1) pitch = 51;
            double margin = offhand.Bounds.fixedX;   // default edge inset (10)
            double gap = 10;

            // Show only allowed slots.
            hotbar.DetermineAvailableSlots(Enumerable.Range(0, hotbarCount).ToArray());
            bags.DetermineAvailableSlots(Enumerable.Range(0, bagCount).ToArray());

            // Layout: [margin][offhand][gap][hotbar][gap][bags][margin]
            offhand.Bounds.fixedWidth = pitch;
            hotbar.Bounds.fixedX = margin + pitch + gap;
            hotbar.Bounds.fixedWidth = hotbarCount * pitch;
            // Ensure bag slots don't overlap strip border.
            bags.Bounds.fixedX = 0;
            bags.Bounds.fixedWidth = bagCount * pitch;

            double width = margin + pitch + gap + hotbarCount * pitch + gap + bagCount * pitch + margin;
            strip.fixedWidth = width;
            if (strip.ParentBounds != null && strip.ParentBounds.fixedWidth > 0)
            {
                strip.ParentBounds.fixedWidth = width;
            }

            // Reset static texture so the hotbar background resizes correctly.
            ResetStaticTexture(composer);

            composer.ReCompose();
        }
        catch (Exception e)
        {
            if (!warned)
            {
                warned = true;
                capi?.Logger.Warning("[{0}] Could not repack the hotbar HUD: {1}", BurdenedModSystem.ModId, e);
            }
        }
    }

    private static readonly FieldInfo? StaticTextureField =
        AccessTools.Field(typeof(GuiComposer), "staticElementsTexture");

    private static void ResetStaticTexture(GuiComposer composer)
    {
        if (StaticTextureField?.GetValue(composer) is not LoadedTexture texture) return;
        if (texture.TextureId == 0) return;

        texture.Dispose();
        StaticTextureField.SetValue(composer, new LoadedTexture(composer.Api));
    }
}
