# Changelog

All notable changes to this project will be documented in this file.

Feature (F) and decision (D) numbers refer to [FEATURES.md](FEATURES.md).

## Unreleased

The 0.5.0 development line makes Vintage Story's offhand hunger penalty
configurable and makes immersive carrying visible on the player.

**Clients and servers must update together.** The network protocol moves to
version 1.4.0.

### Added

- F11 / D14 - `OffhandHungerPenalty` controls Vintage Story's extra hunger rate
  for an ordinary offhand item. The default `0.2` preserves vanilla's 20%
  increase, while `0` disables it.
- F12 / D04 - In immersive mode, backpacks and other wearable bags remain visible
  together. Selecting one moves only that bag to the hand.

### Changed

- The network protocol moves to version 1.4.0 because the config sync carries
  `OffhandHungerPenalty`.

### Fixed

- F12 / D04 - Wearable bags keep their authored attachment instead of being
  forced onto a different body bone.
- F10 / D15 - A quiver no longer hides an equipped backpack outside immersive
  mode.

### Upgrade notice

- `OffhandHungerPenalty` is added to `burdened.json`. Its default preserves
  vanilla behavior, and no existing setting is renamed or removed.
- Saved worlds are unaffected. Burdened stores no data of its own.
- Clients and servers must both run the same Burdened version.

## [v0.4.0](https://github.com/stinowdev/vs-burdened/releases/tag/v0.4.0)

Bags from other mods now work with Burdened. A bag is placed on the back or at
the waist by what it is rather than by a list of names, mods can ship their own
placement, and `BagRoleOverrides` gives the server owner the last word.

**Clients and servers must update together.** The network protocol moved to
version 1.3.0.

### Added

- F03 / D13 - `BagRoleOverrides` assigns any bag to the B or L / R slots by item
  code, including bags from other mods and including vanilla. Codes accept `*`
  wildcards, and the accepted roles are `back` and `waist`. An entry the server
  cannot use is named in the log at startup rather than ignored in silence.
- F03 / D13 - Bag mods can ship a default with
  `"attributes": { "burdened": { "bagRole": "back" } }` in the item, so their
  bags land correctly with nothing configured. A `BagRoleOverrides` entry always
  wins over it. Bags that match neither keep their current slot.

### Changed

- F10 / D12 - Worn-bag visibility follows the game's own active-slot event
  instead of patching the client inventory manager. Behavior is unchanged, and
  Burdened patches two fewer engine internals.
- The network protocol is now version 1.3.0, because the config sync carries
  `BagRoleOverrides`. Clients and servers must update together.
- F03 / D03 / D13 - The **B** slot now recognises a pack by what it is rather
  than by three named backpacks: worn on the back, and able to hold anything.
  Bags from other mods are placed correctly with nothing declared or configured.
  Every vanilla bag keeps the slot it had, and specialised back-worn containers
  such as the quiver stay at the waist where they do not compete with a pack.

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
- F08 / F10 - Interaction hints from other mods stay visible on bags. Burdened
  now removes only the two vanilla hints its remap makes wrong and leaves the
  rest of the list alone, instead of replacing all of it.
- F03 / D03 - Immersive mode no longer refuses items that are not bags. A
  populated skep can only be carried in a bag slot, so the L / B / R rules were
  leaving it with nowhere to go. Bag slots now judge bags and pass everything
  else to the game's own rules, as they do outside immersive mode.
- F08 / F10 / D08 - Wall-mounted bags such as the quiver work again.
  A placed quiver now opens with right-click and picks up
  with Shift + right-click, like every other bag. A selected one opens with
  right-click whether or not a block is targeted, and an equipped one opens
  from its slot. Putting it down stays vanilla: Ctrl + Shift against a wall,
  which is a position Burdened cannot express.

### Upgrade notice

- `BagRoleOverrides` is added to `burdened.json` and is empty by default. No
  other setting changed, and no existing setting was renamed or removed.
- Saved worlds are unaffected. Burdened stores no data of its own.
- Every vanilla bag keeps the slot it had.
- Clients and servers must both run 0.4.0. A client on 0.3.0 cannot join a
  0.4.0 server.

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
