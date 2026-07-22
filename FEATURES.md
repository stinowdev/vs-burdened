# Burdened design

This document tracks feature status and the design decisions that constrain
future work. User-facing behavior belongs in [README.md](README.md); release history
belongs in [CHANGELOG.md](CHANGELOG.md).

## Feature status

| ID | Status | Feature | Config key | Default | Authority |
|---|---|---|---|---|---|
| F01 | Implemented | Reduced hotbar slots (1 through 10) | `HotbarSlots` | 10 | Server enforces, client renders |
| F02 | Implemented | Reduced bag-equip slots (1 through 4) | `BagSlots` | 4 | Server enforces, client renders |
| F03 | Implemented | Immersive L/B/R bag role rules; D04 visuals remain planned | `ImmersiveCarryingMode` | false | Universal |
| F04 | Implemented | Compact crafting-only inventory dialog | `HideBagContentsInDialog` | true | Client, using server-synced config |
| F05 | Implemented | Concise hotbar scroll across usable slots | Follows F01 | - | Client |
| F06 | Implemented | Offhand manually accepts non-bag items | `OffhandHoldsAnything` | true | Server enforces |
| F07 | Planned | Auto-pickup policy for equipped bag contents | - | - | Server |
| F08 | Implemented | Placed bag open and pickup remap | `ImprovedBagInteractions` | true | Universal |
| F09 | Planned | Remember container dialog placement per identity | - | - | Client |
| F10 | Implemented | Open or place bags directly from equipped slots | `ImprovedBagInteractions` | true | Client input, server placement |

Planned features do not receive runtime config keys until their behavior is
implemented.

## Bag interaction contract (F08 and F10)

The interaction remap applies only to equippable held bags that expose the
vanilla ground-storage bag behaviors described by D08. Chests, vessels, and
other containers retain their vanilla behavior. Bag contents and arrangement
remain attached to the item stack during every transition.

### Placed bags (F08)

| Input | Action |
|---|---|
| Right-click | Open through the vanilla contained-bag workspace |
| Shift + right-click | Request a server-authoritative transfer into a compatible empty bag-equip slot |

If no compatible slot exists, the bag remains placed. Pickup never falls back
to general inventory routing or the offhand.

### Equipped bags (F10)

| Input | Action |
|---|---|
| Right-click a bag slot with an empty mouse cursor | Toggle that bag inventory |
| Right-click another bag slot | Open it alongside existing bag dialogs |
| Shift + click a bag slot with an empty mouse cursor | Place the bag on the targeted block |
| Select a bag slot with Ctrl-scroll, then right-click | Toggle that bag inventory when the target does not consume the interaction |
| Select a bag slot, then Shift + right-click a block | Place the bag on that block |

Ctrl remains the F05 bag-slot selection modifier. Shift owns bag placement and
pickup. Equipped bag windows use the four-column inset layout of placed bags.
While an equipped bag is selected, its vanilla worn copy is hidden and its
active-hand rendering remains visible, with immersive mode on or off.

## Locked decisions

| ID | State | Decision |
|---|---|---|
| D01 | Active | Burdened is standalone and does not depend on or patch other inventory mods. |
| D02 | Active | When configuration locks occupied slots, their items move into valid inventory space first and then drop on the ground. Items are never deleted. |
| D03 | Active | Immersive mode exposes L/B/R. B accepts the normal, sturdy, and hunter backpacks. L/R accept other equippable bag-class storage items and reject those backpacks. |
| D04 | Planned | Burdened-controlled on-body placement will build on or replace vanilla worn-bag rendering: B on the back, L/R at the waist, with the selected bag suppressed while rendered in hand. |
| D05 | Active | The compact E dialog hides bag contents only. Bag-equip slots remain on the hotbar HUD. |
| D06 | Active | The offhand accepts manual placement of non-bag items. Bags and automatic best-slot routing are excluded. Item use remains vanilla. |
| D07 | Planned | F07 will preserve vanilla priority: hotbar first, then equipped bag contents. |
| D08 | Active | Interaction remaps require both an equippable held bag and the vanilla ground-storage bag behaviors. |
| D09 | Active | Shift is the placement and pickup modifier. Rejection leaves the source bag untouched. Open is right-click. |
| D10 | Planned | F09 will remember movable dialog placement per durable container identity rather than per world position, including pickup and replacement where identity can persist. |

## Design notes

### F09 container identity

Vanilla ground-storage dialog placement appears to be associated with the
supporting block or world position. F09 must verify that behavior and define a
durable container identity before implementation. The implementation must not
mistake two containers placed at the same location for the same container.

## Configuration contract

The server stores `burdened.json` under the Vintage Story data directory,
sanitizes it on load, and syncs the effective values to every client. The client
does not read a separate local configuration. Only implemented and functional
settings are serialized.
