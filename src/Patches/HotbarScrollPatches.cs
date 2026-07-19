using System;
using System.Collections.Generic;
using System.Reflection;
using Burdened.Config;
using Burdened.Inventory;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Burdened.Patches;

/// <summary>
/// F05: Concise hotbar scroll. Mouse-wheel (and the same path Ctrl uses to
/// reach bag-equip slots) skips locked hotbar/bag indices and wraps both ways
/// within the usable set. Number-key jumps to locked slots are ignored so the
/// selection cannot land on a hidden slot.
/// Client-only; follows F01/F02 counts from the synced config.
/// </summary>
public static class HotbarScrollPatches
{
    private static readonly object Gate = new object();
    private static readonly MethodInfo? OnKeySlotMethod =
        AccessTools.Method(typeof(HudHotbar), "OnKeySlot", new[] { typeof(int), typeof(bool) });

    private static bool applied;
    private static ICoreClientAPI? capi;

    public static void Apply(Harmony harmony, ICoreClientAPI api)
    {
        capi = api;
        lock (Gate)
        {
            if (applied) return;
            applied = true;

            harmony.Patch(
                AccessTools.Method(typeof(HudHotbar), nameof(HudHotbar.moveToHotbarSlot)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(HotbarScrollPatches), nameof(MoveToHotbarSlotPrefix))));

            harmony.Patch(
                OnKeySlotMethod,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(HotbarScrollPatches), nameof(OnKeySlotPrefix))));
        }
    }

    public static void Reset()
    {
        lock (Gate)
        {
            applied = false;
            capi = null;
        }
    }

    /// <summary>
    /// Replaces vanilla wrap-across-0..9/14 with a ring of only usable indices.
    /// Falls through when the config leaves every slot unlocked.
    /// </summary>
    public static bool MoveToHotbarSlotPrefix(HudHotbar __instance, int delta)
    {
        if (!NeedsConciseScroll(out BurdenedConfig cfg) || capi == null || OnKeySlotMethod == null)
        {
            return true;
        }

        IPlayer player = capi.World.Player;
        bool includeBags = ShouldIncludeBags(player);
        List<int> ring = BuildRing(player, cfg, includeBags);
        if (ring.Count == 0) return false;

        int current = player.InventoryManager.ActiveHotbarSlotNumber;
        int at = ring.IndexOf(current);
        if (at < 0) at = 0;

        // Same sign as vanilla: positive wheel delta moves toward lower indices.
        int next = ring[GameMath.Mod(at - delta, ring.Count)];
        OnKeySlotMethod.Invoke(__instance, new object[] { next, false });
        return false;
    }

    /// <summary>
    /// Blocks direct selection of locked hotbar / bag-equip indices (number keys,
    /// etc.). Usable indices fall through to vanilla <c>OnKeySlot</c>.
    /// </summary>
    public static bool OnKeySlotPrefix(int index)
    {
        if (!NeedsConciseScroll(out BurdenedConfig cfg) || capi?.World?.Player == null)
        {
            return true;
        }

        return IsUsableActiveIndex(capi.World.Player, cfg, index);
    }

    private static bool NeedsConciseScroll(out BurdenedConfig cfg)
    {
        BurdenedConfig? current = SlotLocks.Config;
        if (current == null)
        {
            cfg = null!;
            return false;
        }

        cfg = current;
        return cfg.HotbarSlots < SlotLocks.VanillaHotbarSlots
            || cfg.BagSlots < BurdenedConfig.MaxBagSlots;
    }

    /// <summary>
    /// Vanilla expands the scroll ring to bag-equip slots when Ctrl is held or
    /// the selection is already past the hotbar (+ skill) range.
    /// </summary>
    private static bool ShouldIncludeBags(IPlayer player)
    {
        int skillOffset = SkillOffset(player);
        int bagBase = 10 + skillOffset;
        if (player.InventoryManager.ActiveHotbarSlotNumber >= bagBase) return true;

        // Vanilla uses KeyboardKeyStateRaw[3] for this Ctrl check.
        return capi != null && capi.Input.KeyboardKeyStateRaw[3];
    }

    private static List<int> BuildRing(IPlayer player, BurdenedConfig cfg, bool includeBags)
    {
        List<int> ring = new List<int>(cfg.HotbarSlots + cfg.BagSlots + 1);
        for (int i = 0; i < cfg.HotbarSlots; i++)
        {
            ring.Add(i);
        }

        int skillOffset = SkillOffset(player);
        if (skillOffset > 0)
        {
            ring.Add(10);
        }

        if (includeBags)
        {
            int bagBase = 10 + skillOffset;
            for (int i = 0; i < cfg.BagSlots; i++)
            {
                ring.Add(bagBase + i);
            }
        }

        return ring;
    }

    private static bool IsUsableActiveIndex(IPlayer player, BurdenedConfig cfg, int index)
    {
        if (index < 0) return false;
        if (index < cfg.HotbarSlots) return true;
        if (index < SlotLocks.VanillaHotbarSlots) return false;

        int skillOffset = SkillOffset(player);
        if (skillOffset > 0 && index == 10) return true;

        int bagIndex = index - (10 + skillOffset);
        return bagIndex >= 0 && bagIndex < cfg.BagSlots;
    }

    private static int SkillOffset(IPlayer player)
    {
        IInventory? hotbar = player.InventoryManager.GetHotbarInventory();
        if (hotbar == null || hotbar.Count <= 10) return 0;
        return hotbar[10].Empty ? 0 : 1;
    }
}
