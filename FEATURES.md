# Burdened design

This file defines what Burdened implements and the decisions that constrain
future work. See [README.md](README.md) for player documentation and
[CHANGELOG.md](CHANGELOG.md) for release history.

## Feature status

| ID | Status | Feature | Config key | Default | Authority |
|---|---|---|---|---|---|
| F01 | Implemented | Limit usable hotbar slots | `HotbarSlots` | `10` | Server enforces; client renders |
| F02 | Implemented | Limit usable bag-equip slots | `BagSlots` | `4` | Server enforces; client renders |
| F03 | Implemented | Assign immersive L / B / R bag roles | `ImmersiveCarryingMode` | `false` | Server validates; client labels |
| F04 | Implemented | Show a compact crafting-only E inventory | `HideBagContentsInDialog` | `true` | Client UI from synced config |
| F05 | Implemented | Scroll only through usable hotbar and bag slots | Follows F01 / F02 | - | Client input from synced config |
| F06 | Implemented | Allow manual offhand storage for non-bag items | `OffhandHoldsAnything` | `true` | Server validates; client predicts |
| F07 | Planned | Route automatic pickup into equipped bag contents | - | - | Server routes |
| F08 | Implemented | Open and equip placed bags directly | `ImprovedBagInteractions` | `true` | Server transfers; client opens |
| F09 | Planned | Remember container window positions by identity | - | - | Client |
| F10 | Implemented | Open and place equipped bags directly | `ImprovedBagInteractions` | `true` | Server places; client opens |
| F11 | Implemented | Configure the vanilla offhand hunger penalty | `OffhandHungerPenalty` | `0.2` | Server applies; client mirrors |
| F12 | Implemented | Compose immersive wearable bag models independently | Follows F03 | - | Client presentation |

Planned features have no runtime settings.

## Bag interaction contract

F08 and F10 apply to equippable bags that expose both vanilla ground-storage bag
behaviors required by D08. Chests, vessels, and other containers keep their
vanilla interactions.

A bag that does not rest in the middle of its block, such as the wall-mounted
quiver, is placed with vanilla's own gesture (Ctrl + Shift against a wall) and
follows the table below for everything after that.

| Location | Input | Result | Authority |
|---|---|---|---|
| Placed bag | Right-click | Open its inventory | Vanilla workspace |
| Placed bag | Shift + right-click | Equip into a compatible empty bag slot | Server |
| Equipped bag slot | Right-click | Toggle that bag window | Client |
| Equipped bag slot | Shift + click | Place on the targeted block | Server |
| Selected equipped bag | Right-click | Toggle when the target does not consume the input | Client |
| Selected equipped bag | Shift + right-click | Place on the targeted block | Server |

The following rules apply to every transition:

- Bag contents remain attached to the item stack.
- A rejected pickup or placement leaves the source unchanged.
- Floor pickup never falls back to general inventory routing or the offhand.
- Several equipped bag windows may remain open at once.
- A selected equipped bag remains visible in hand and is hidden from the worn
  player mesh.

Ctrl remains the F05 modifier for selecting equipped bag slots. Shift owns bag
pickup and placement.

## Bag role contract (F03, D03, D13)

An equippable bag is assigned to B or to L / R by the first of these that names
it. Neither mechanism requires a Burdened release.

| Priority | Source | Set by |
|---|---|---|
| 1 | `BagRoleOverrides` in `burdened.json` | The server owner |
| 2 | `burdened.bagRole` item attribute | The bag's mod author |
| 3 | Worn at the game's `backpack` position and unrestricted contents | The game, for any general-purpose pack |

The accepted roles are `back` and `waist`. Anything unrecognized at one level
is ignored and the next level decides, so one bad entry cannot break the rest.
A bag that reaches level 3 without matching is a waist bag.

Level 3 uses the game's own data instead of a list of item codes. A bag lands
in the right place with nothing declared and nothing configured. The check is
simple: does the game wear it on the back, and does it hold ordinary items
rather than one kind of thing. A quiver fails the second part, so it stays at
the waist and leaves the pack slot free.

A mod author ships a default in the item JSON:

```json
"attributes": { "burdened": { "bagRole": "back" } }
```

A server owner overrides anything, including vanilla and including a mod that
declared the wrong role. Codes accept `*` wildcards, and an entry with no
domain is read as `game:`:

```json
"BagRoleOverrides": {
  "othermod:rucksack-*": "back",
  "game:backpack-sturdy": "waist"
}
```

Overrides are server-owned and sync to every client with the rest of the
config, so both sides classify a bag identically. This applies only to items
Burdened already recognizes as equippable bags: naming a non-bag here does not
make it equippable.

## Locked decisions

| ID | State | Decision |
|---|---|---|
| D01 | Active | Burdened is standalone. It does not depend on or patch another inventory mod. |
| D02 | Active | Items in newly locked slots move into valid storage, then drop at the player's feet if no destination remains. They are never deleted. |
| D03 | Active | Immersive mode exposes L / B / R. B accepts a general-purpose pack: a bag the game wears at its `backpack` position that also holds ordinary items. L and R accept every other equippable bag, including specialised back-worn containers such as the quiver, so they never compete for the single B slot. In 1.22.3 that leaves the normal, sturdy and hunter backpacks on B. The roles sort bags only: anything else the game allows in a bag-equip slot, such as a populated skep, is passed through to vanilla's own rules. |
| D04 | Active | In immersive mode, equipped bags with wearable models remain visible at their authored body positions. Selecting a bag shows it only in hand. Burdened does not reposition arbitrary models. |
| D05 | Active | The compact E inventory hides bag contents only. Bag-equip slots remain on the hotbar. |
| D06 | Active | Bags are always rejected by the offhand. With `OffhandHoldsAnything=true`, non-bag items may be placed there manually and automatic best-slot routing excludes it. Item use remains vanilla. |
| D07 | Planned | F07 preserves vanilla priority: hotbar first, then equipped bag contents. |
| D08 | Active | F08 and F10 are gated in three tiers, because Burdened can remap more than it can perform. Opening an equipped bag's own window needs only an equippable bag. Opening and picking up a placed bag need both vanilla ground-storage bag behaviors, and work for any layout, because each resolves the exact slot the player points at. Burdened placing a bag itself also needs a `SingleCenter` layout, since the service can only target the middle of the block above. A bag that fails a tier keeps its vanilla gesture there: claiming an interaction that cannot be carried out leaves the bag stranded. |
| D09 | Active | Right-click opens. Shift picks up or places. Rejection leaves the source unchanged. |
| D10 | Planned | F09 identifies a movable container independently of its world position. Pickup and replacement must not lose that identity. |
| D11 | Active | Equipped-bag placement identifies the source by slot index. The server revalidates that slot when handling the request; no item fingerprint is sent. |
| D12 | Active | An engine method is patched only where no event, behavior, or registered class reaches the same paths. Worn-bag invalidation uses `AfterActiveSlotChanged`, which already covers local and server-forced selection changes. |
| D13 | Active | Extends D03. Roles resolve in order: `BagRoleOverrides` in the config, then the item's `burdened.bagRole` attribute, then the game's own data for where the bag is worn and what it holds. The server owner always has the last word. A bag mod still works with nothing configured. No level keys off a mod id or item code. |
| D14 | Active | F11 configures Vintage Story's existing offhand fallback penalty rather than replacing the hunger system. `0.2` preserves vanilla's 20% increase and `0` disables it. Non-finite and negative values become `0`. Items with their own `statModifier` keep the game's item-defined behavior. The server applies the effective value and syncs it to clients. |
| D15 | Active | Outside immersive mode, a selected bag stays hand-only and a quiver cannot replace an equipped backpack in the worn model. |


## Configuration contract

The server loads and sanitizes `burdened.json` at startup, then sends the
effective values to every client. Clients do not read a separate local
configuration, and only implemented settings are serialized. After editing the
file, restart the dedicated server or reopen the singleplayer world.
