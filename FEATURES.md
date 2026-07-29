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

Planned features have no runtime settings.

## Bag interaction contract

F08 and F10 apply only to equippable bags that expose both vanilla
ground-storage bag behaviors required by D08. Chests, vessels, and other
containers keep their vanilla interactions.

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
| 3 | `attachableToEntity.categoryCode` of `backpack` | The game, for anything worn on the back |

The accepted roles are `back` and `waist`. Anything unrecognized at one level
is ignored and the next level decides, so one bad entry cannot break the rest.
A bag that reaches level 3 without matching is a waist bag.

Level 3 reads the game's own answer rather than a list of item codes, so a bag
that already renders on the player's back is treated as a back bag with nothing
declared or configured anywhere.

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
| D03 | Active | Immersive mode exposes L / B / R. B accepts bags the game itself attaches at its `backpack` position; L and R accept every other equippable bag. In 1.22.3 that is the normal, sturdy and hunter backpacks plus the quiver on B. The roles sort bags only: anything else the game allows in a bag-equip slot, such as a populated skep, is passed through to vanilla's own rules. |
| D04 | Planned | Custom on-body rendering will place B on the back and L / R at the waist. A selected bag remains hand-only. |
| D05 | Active | The compact E inventory hides bag contents only. Bag-equip slots remain on the hotbar. |
| D06 | Active | Bags are always rejected by the offhand. With `OffhandHoldsAnything=true`, non-bag items may be placed there manually and automatic best-slot routing excludes it. Item use remains vanilla. |
| D07 | Planned | F07 preserves vanilla priority: hotbar first, then equipped bag contents. |
| D08 | Active | Improved interactions require an equippable bag with both vanilla ground-storage bag behaviors. |
| D09 | Active | Right-click opens. Shift picks up or places. Rejection leaves the source unchanged. |
| D10 | Planned | F09 identifies a movable container independently of its world position. Pickup and replacement must not lose that identity. |
| D11 | Active | Equipped-bag placement identifies the source by slot index. The server revalidates that slot when handling the request; no item fingerprint is sent. |
| D12 | Active | An engine method is patched only where no event, behavior, or registered class reaches the same paths. Worn-bag invalidation uses `AfterActiveSlotChanged`, which already covers local and server-forced selection changes. |
| D13 | Active | Extends D03. An immersive role is resolved as `BagRoleOverrides` in the config, then the item's own `burdened.bagRole` attribute, then the game's own attachment category. The server owner therefore always has the last word, and a bag mod still works with nothing configured. Burdened keys behavior off no mod id and no item code at any level. |


## Configuration contract

The server loads and sanitizes `burdened.json` at startup, then sends the
effective values to every client. Clients do not read a separate local
configuration, and only implemented settings are serialized. After editing the
file, restart the dedicated server or reopen the singleplayer world.
