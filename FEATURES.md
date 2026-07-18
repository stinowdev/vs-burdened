# Burdened - Design document

## Features

| # | Feature | Config key | Default | Side |
|---|---|---|---|---|
| F01 | Reduced hotbar slots (1..10) | `hotbarSlots` | 10 | server enforces, client hides |
| F02 | Reduced bag-equip slots (1..4) | `bagSlots` | 4 | server enforces, client hides |
| F03 | Immersive carrying mode (L/B/R + rendering) | `immersiveCarryingMode` | false | universal |
| F04 | Hide bag contents in inventory dialog | `hideBagContentsInDialog` | true | client (server-synced rule) |
| F05 | Concise hotbar scroll (skip locked slots, wrap both ways) | — (follows F01) | — | client |
| F06 | Offhand holds anything | `offhandHoldsAnything` | true | server enforces |
| F07 | Auto-pickup flows into equipped bags (vanilla priority) | `autoPickupToBags` | true | server |
| F08 | Placeable bags: right-click opens, sneak-interact picks up, contents+arrangement preserved | `placeableBags` | true | universal |
| F09 | Remember container dialog placement per container identity | `rememberDialogPlacement` | true | client |

## Locked decisions

| # | Decision |
|---|---|
| D01 | Standalone mod. No dependencies on or patches into mods such as One Hand. |
| D02 | When config shrinks slot counts, items in newly-locked slots are ejected: into bags first, then dropped on the ground. Never deleted. |
| D03 | Immersive mode slot taxonomy: **B (back)** accepts only `backpack`, `sturdy backpack`, `hunter backpack`. **L / R (waist)** accept bag-class storage items (baskets, sacks, etc.), never the three backpacks. |
| D04 | Rendering v1 = static bag meshes on back/waist attachment points; refinement later. |
| D05 | Inventory dialog ("E") hides the bag **contents** grid only; bag equip slots stay visible. Concise-scroll (F05) behavior is kept consistent with this. |
| D06 | Offhand accepts any item but usability stays vanilla (holding only). |
| D07 | Auto-pickup keeps vanilla priority: hotbar first, then bag contents. |
| D08 | Placeable+openable applies to **bags only**. Chests/vessels keep vanilla behavior entirely. Polarity: right-click opens the placed bag (consistent with chests), sneak-interact picks it up. |
| D09 | Pickup modifier is the **sneak-style** key (VS convention), not literal Ctrl. |
| D10 | Container GUI placement memory: when the player sets a container's dialog from fixed to movable and positions it, that placement is remembered **per container identity** (not per block position), surviving pickup/re-placement. Applies to our placed bags and, where identity can persist, vanilla containers. |

## Config

Server-side JSON at `%APPDATA%\VintagestoryData\ModConfig\burdened.json`,
created with defaults on first run, values clamped on load, synced to every
client on join via the mod's network channel. The client never reads the file;
it renders exactly what the server enforces.