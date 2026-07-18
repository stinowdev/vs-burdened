# Changelog

All notable changes to this project will be documented in this file.

Feature (F) and decision (D) numbers refer to [FEATURES.md](https://github.com/stinowdev/vs-burdened/blob/main/FEATURES.md).

## [Unreleased]

### Planned

- F03 - Immersive carrying mode (L/B/R slot taxonomy per D03, bag rendering per D04)
- F04 - Hide bag contents in the inventory dialog (D05)
- F05 - Concise hotbar scroll (skip locked slots, wrap both ways)
- F06 - Offhand holds anything (D06)
- F07 - Auto-pickup flows into equipped bags with vanilla priority (D07)
- F08 - Placeable bags: right-click opens, sneak-interact picks up (D08, D09)
- F09 - Remember container dialog placement per container identity (D10)



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

