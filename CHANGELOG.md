# Changelog

All notable changes to this project will be documented in this file.

Feature (F) and decision (D) numbers refer to [FEATURES.md](https://github.com/stinowdev/vs-burdened/blob/main/FEATURES.md).

## [Unreleased]

### Planned

- D04 - Immersive on-body bag rendering (static meshes on back/waist)
- F07 - Auto-pickup flows into equipped bags with vanilla priority (D07)
- F09 - Remember container dialog placement per container identity (D10)

### Added

- F08 / F10 use one `ImprovedBagInteractions` flag for the complete floor and
  equipped bag interaction contract.
- F08 / D08 / D09 - Floor bags: right-click opens through the vanilla
  contained-bag workspace; Shift+right-click transfers directly into a
  compatible empty bag-equip slot and otherwise leaves the bag placed.
- F10 - Equipped bag slots: right-click opens independent mapped views over
  the existing backpack inventory; Shift-click or selected-bag Shift+RMB places
  directly from the equip slot into vanilla ground storage.

### Fixed

- Use one vanilla-equippable-bag classification for immersive slot roles and
  offhand rejection, while checking ground-interaction support separately.
- Hide a selected equipped bag from the player's worn backpack shape while
  vanilla renders that bag in the active hand, with immersive mode on or off.
- Reject bags and backpacks from the offhand. F08 pickup does not use general
  inventory routing, so a full bag bar cannot overflow into the offhand.
- Make floor-bag pickup server-authoritative. The client no longer predicts the
  item transfer or removes ground storage; the server removes the block only
  after exactly one bag reaches a compatible empty equip slot, and audits both
  successful and rejected requests.
- Give every equipped-bag dialog its own local slot map and GUI dirty state;
  clicks still delegate to the real player backpack inventory. This prevents
  one window from consuming another window's updates or passing equip-slot ids
  into `ComposeSlotOverlays`.
- Remap open equipped-bag dialogs when any bag is equipped/unequipped/placed so
  content slot ids and cached slot objects are replaced synchronously with the
  `BagInventory` reload. This removes the one-frame null-slot race in
  `ComposeSlotOverlays` with multiple bag windows open.
- Match vanilla contained-bag dialogs with a four-column contents grid and use
  the same inset/title spacing for equipped bags; keep stable, non-overlapping
  positions for simultaneously open windows.
- Tighten the crafting-only inventory dialog with a proportional output-slot
  gap and no oversized empty area below it.
- Consume the residual world-interaction packet after custom equipped-bag
  placement and initialize a received contained-bag workspace before vanilla
  deserializes it. This prevents duplicate `Backpack Contents` windows and the
  NRE in `InventoryGeneric.FromTreeAttributes`.
- Report bag-placement validation through chat instead of Vintage Story's
  in-game error HUD, whose misplaced hover background produced detached bars.


## [v0.3.0-pre1](https://github.com/stinowdev/vs-burdened/releases/tag/v0.3.0-pre1) - 2026-07-20

### Added

- F04 / D05 - Hide bag contents in the inventory dialog: when
  `HideBagContentsInDialog` is true (default), pressing E shows only the
  crafting grid (and output). Bag-equip slots stay on the hotbar HUD; bag
  contents are not listed in the dialog. Set the key to `false` to restore
  the vanilla bag-contents grid next to crafting.

## [v0.2.0](https://github.com/stinowdev/vs-burdened/releases/tag/v0.2.0) - 2026-07-19

Immersive bag rules, concise scroll, and offhand flexibility. Immersive mode is
**rules only** in this release (no on-body bag meshes yet; that is D04).

### Added

- F05 - Concise hotbar scroll: mouse wheel skips locked hotbar/bag-equip slots
  and wraps both ways within the usable set (Ctrl still reaches bag-equip
  slots, limited to configured `BagSlots`). Number keys that target a locked
  slot are ignored.
- F03 / D03 - Immersive carrying mode (rules only; on-body meshes are D04 later):
  when `ImmersiveCarryingMode` is true, bag-equip is fixed to three typed slots
  **L / B / R** (`BagSlots` is ignored). **B** accepts only `backpack-normal`,
  `backpack-sturdy`, and `hunterbackpack`. **L** and **R** accept other
  bag-class storage (baskets, sacks, etc.) and never those three backpacks.
  Wrong items are rejected by the server and ejected on join; the HUD shows the
  three slots with role icons. Backpack icon added to slot B when enabled.
- F06 / D06 - Offhand holds anything: when `OffhandHoldsAnything` is true
  (default), the offhand slot accepts any item. Usability stays vanilla
  (holding only; no dual-wield tool use). Auto-pickup suitability is unchanged
  so items are not steered into the offhand.

### Fixed

- Hotbar HUD right-border smear when the strip is shrunk below vanilla width
  (re-bake static texture at the new size before recompose).
- Crash on join when config sync tried to recompose the hotbar HUD before the
  player inventory existed.

## [v0.1.0](https://github.com/stinowdev/vs-burdened/releases/tag/v0.1.0) - 2026-07-18

First release. Standalone mod for Vintage Story 1.22 (D01), no dependencies on
or patches into other mods.

### Added

- F01 - Reduced hotbar slots (`hotbarSlots`, 1..10, default 10). The server
enforces the limit; the client hides the locked slots.
- F02 - Reduced bag-equip slots (`bagSlots`, 1..4, default 4). The server
enforces the limit; the client hides the locked slots.
- Server-side config at `ModConfig/burdened.json`: created with defaults on
first run, values clamped on load, synced to every client on join over the
mod's network channel. The client never reads the file; it renders exactly
what the server enforces.
- Server-side slot locking: items cannot be placed into or taken from locked
hotbar/bag slots, regardless of client state.
- Item ejection (D02): when a player joins carrying items in slots the config
has since disabled, those items are returned through the normal give path
(allowed hotbar slots, then equipped bags); anything that does not fit is
dropped at the player's feet. Bags are ejected before hotbar items so a
hotbar item cannot overflow into a bag that is itself about to be
unequipped. Items are never deleted.
- Hotbar HUD repack: the hotbar strip shrinks to a tight, centered
[offhand | hotbar | bags] cluster showing only the usable slots, with
vanilla-style borders at any width.
- Locked-slot visuals: locked hotbar and bag-equip slots are tinted dark and
drawn as unavailable; if the bar shrinks under the current selection, the
active slot is pulled back into range.
