# Burdened - Design document

## Features

| # | Feature | Config key | Default | Side |
|---|---|---|---|---|
| F01 | Reduced hotbar slots (1..10) | `HotbarSlots` | 10 | server enforces, client hides |
| F02 | Reduced bag-equip slots (1..4) | `BagSlots` | 4 | server enforces, client hides |
| F03 | Immersive carrying mode (L/B/R rules; on-body rendering is D04) | `ImmersiveCarryingMode` | false | universal |
| F04 | Hide bag contents in a compact crafting-only inventory dialog | `HideBagContentsInDialog` | true | client (server-synced rule) |
| F05 | Concise hotbar scroll (skip locked slots, wrap both ways) | - (follows F01) | - | client |
| F06 | Offhand manually holds any non-bag item | `OffhandHoldsAnything` | true | server enforces |
| F07 | Auto-pickup flows into equipped bags (vanilla priority) | `AutoPickupToBags` | true | server |
| F08 | Floor bag interaction remap (open / pick up; bags are already placeable in vanilla) | `ImprovedBagInteractions` | true | universal |
| F09 | Remember container dialog placement per container identity | `RememberDialogPlacement` | true | client |
| F10 | Hotbar bag slots: right-click opens, Shift-click places directly from the equip slot | `ImprovedBagInteractions` | true | client (+ server place) |

## Bag interactions (F08 + F10)

Applies to bags/backpacks only (D08). Vanilla already lets bags sit in ground
storage; F08 only remaps how you open and pick them up. Contents and
arrangement are preserved.

### On the floor (placed) — F08

| Input | Action |
|---|---|
| Right-click | Open inventory (chest-like; vanilla used Ctrl+RMB) |
| Shift + right-click | Server-authoritative pickup into a compatible empty bag-equip slot; otherwise leave it placed |

### On the hotbar HUD (equipped) — F10

| Input | Action |
|---|---|
| Right-click (hover bag slot, empty mouse) | Toggle that bag's inventory open/closed |
| Right-click additional bag slots | Open their inventories alongside existing bag dialogs |
| Shift + click (hover bag slot, empty mouse) | Place the bag on the looked-at block |
| Bag slot selected (Ctrl-scroll) + RMB on empty/non-interactive target | Toggle that bag's inventory |
| Bag slot selected + Shift + RMB on a block | Place the bag (also shown in held help) |

Ctrl remains the F05 bag-slot select/scroll modifier; Shift is the floor action modifier.
Equipped bag windows match the four-column inset layout used by placed bags.
While an equipped bag slot is selected, its worn copy is hidden and only its
active-hand rendering remains. This applies with immersive mode on or off.

## Locked decisions

| # | Decision |
|---|---|
| D01 | Standalone mod. No dependencies on or patches into mods such as One Hand. |
| D02 | When config shrinks slot counts, items in newly-locked slots are ejected: into bags first, then dropped on the ground. Never deleted. |
| D03 | Immersive mode slot taxonomy: **B (back)** accepts only `backpack`, `sturdy backpack`, `hunter backpack`. **L / R (waist)** accept bag-class storage items (baskets, sacks, etc.), never the three backpacks. |
| D04 | Rendering v1 = static bag meshes on back/waist attachment points; refinement later. |
| D05 | Inventory dialog ("E") hides the bag **contents** grid only; bag equip slots stay visible. Concise-scroll (F05) behavior is kept consistent with this. |
| D06 | Offhand accepts any non-bag item by manual placement; bags/backpacks and automatic best-slot routing are excluded. Usability stays vanilla (holding only). |
| D07 | Auto-pickup keeps vanilla priority: hotbar first, then bag contents. |
| D08 | Interaction remaps apply to **bags only**. Chests/vessels keep vanilla behavior entirely. |
| D09 | Bag place/pick-up modifier is **Shift**: floor Shift+right-click requests a server-authoritative pickup only into a compatible bag-equip slot; rejection leaves the floor bag untouched. HUD Shift-click places. Open is right-click. |
| D10 | Container GUI placement memory: when the player sets a container's dialog from fixed to movable and positions it, that placement is remembered **per container identity** (not per block position), surviving pickup/re-placement. Applies to our placed bags and, where identity can persist, vanilla containers. |

## Config

Server-side JSON at `%APPDATA%\VintagestoryData\ModConfig\burdened.json`,
created with defaults on first run, values clamped on load, synced to every
client on join via the mod's network channel. The client never reads the file;
it renders exactly what the server enforces.
