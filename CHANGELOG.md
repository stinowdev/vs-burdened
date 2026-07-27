# Changelog

All notable changes to this project will be documented in this file.

Feature (F) and decision (D) numbers refer to [FEATURES.md](FEATURES.md).

## [Unreleased]

### Changed

- F10 / D12 - Worn-bag visibility follows the game's own active-slot event
  instead of patching the client inventory manager. Behavior is unchanged, and
  Burdened patches two fewer engine internals.

### Fixed

- A game method that Burdened can no longer find now disables only the feature
  that needed it, instead of stopping the whole mod from loading. Each group of
  patches reports how many it applied, and names anything it could not.
- F03 / D03 - Immersive bag-role rules now attach to every slot type in the
  game. 8 vanilla slot types refused the previous attachment and logged a
  warning on every world load, and a bag-equip slot added by another mod could
  have skipped the role rules with no sign that it had.
- F10 - A malformed placement request can no longer leave a placed bag that
  cannot be opened or picked up again. The server checks the hit position it is
  sent before deriving the bag's rotation from it.

## [v0.3.0](https://github.com/stinowdev/vs-burdened/releases/tag/v0.3.0)

This release is about using bags directly. Equipped bags can be opened or
placed without moving them through the inventory, and placed bags can be opened
or equipped again.

<img
  width="600"
  alt="Several equipped and placed bag inventories open at once"
  src="https://github.com/user-attachments/assets/152af418-42e8-4450-8d5b-34be2b04a34b"
/>

### Added

- F04 / D05 - The E inventory can show a compact crafting-only view. Set
  `HideBagContentsInDialog` to `false` to restore the vanilla layout.
- F08 / D08 / D09 - Right-click a placed bag to open it. Shift + right-click
  equips it into a compatible empty bag slot.
- F10 / D09 - Right-click an equipped bag slot to open it. Shift + click places
  it on the targeted block. Several equipped bag windows can remain open.

### Changed

- F08 / F10 use one `ImprovedBagInteractions` setting.
- Only implemented settings remain in `burdened.json`. F07 automatic pickup and
  F09 remembered dialog placement remain planned.
- Vintage Story **1.22.3** is the compatibility baseline for this release.
- The network protocol is now version 1.2.0. Clients and servers must update
  together.

### Fixed

- Bag windows now stay attached to the correct bag while bags are equipped,
  removed, or placed. This fixes stale contents, locked windows, and crashes
  when several bags are open.
- Placing an equipped bag no longer opens duplicate inventory windows or
  crashes while loading its contents.
- Failed pickup and placement leave the bag and its contents at the source.
  Pickup also stays out of the offhand when no compatible bag slot is available.
- Selecting an equipped bag shows it in the active hand without also showing
  the worn copy on the player.
- Equipped and placed bag windows now use the same four-column layout and
  spacing. The crafting output slot also sits closer to the grid.
- Config changes no longer leave the active selection on a locked slot.
- Bag-placement errors now use chat instead of leaving detached bars beside the
  hotbar.

### Known limitations

- With extreme latency, replacing an equipped bag immediately after requesting
  placement can place the replacement bag instead. This does not duplicate or
  delete items.

## [v0.3.0-pre1](https://github.com/stinowdev/vs-burdened/releases/tag/v0.3.0-pre1)

### Added

- F04 / D05 - Added the optional crafting-only E inventory. Bag-equip slots
  remain on the hotbar, and `HideBagContentsInDialog=false` restores the
  vanilla inventory layout.

## [v0.2.0](https://github.com/stinowdev/vs-burdened/releases/tag/v0.2.0)

This release defines the immersive carrying rules. It changes which bag slots
are available, how they are selected, and what the offhand can hold.

### Added

- F03 / D03 - Immersive mode replaces the normal bag bar with L / B / R slots.
  B accepts the normal, sturdy, and hunter backpacks. L and R accept other
  equippable bags. Custom on-body rendering remains planned as D04.
- F05 - Mouse-wheel selection skips locked slots and wraps across the usable
  hotbar. Ctrl still includes available bag slots.
- F06 / D06 - `OffhandHoldsAnything` allows non-bag items to be placed in the
  offhand without adding dual-wield item use.

### Fixed

- The hotbar no longer leaves a smeared right border when configured below its
  vanilla width.
- Config sync during connection no longer tries to rebuild the hotbar before
  the player inventory exists.

## [v0.1.0](https://github.com/stinowdev/vs-burdened/releases/tag/v0.1.0)

The first release establishes Burdened's smaller, server-owned carrying layout.

### Added

- F01 - `HotbarSlots` controls how many of the ten hotbar slots can be used.
- F02 - `BagSlots` controls how many of the four bag-equip slots can be used.
- D02 - Items found in newly disabled slots move into valid inventory space or
  drop at the player's feet. Bags are handled before hotbar items so they cannot
  receive overflow while being unequipped.
- The hotbar contracts into a centered offhand, hotbar, and bag cluster. Locked
  slots are darkened, and the active selection returns to a usable slot when
  needed.
- The server owns `burdened.json` and sends the effective settings to every
  client.
