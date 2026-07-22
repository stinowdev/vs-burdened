# Release testing

This matrix is the minimum manual verification required before a Burdened
release. Run it against the Vintage Story version declared in
`resources/modinfo.json`.

## Test environments

- [ ] Singleplayer world
- [ ] Dedicated server with a matching client mod
- [ ] Fresh default configuration
- [ ] Non-default configuration with reduced hotbar and bag slots
- [ ] `ImmersiveCarryingMode` both false and true

## Regression matrix

| Area | Action | Expected result |
|---|---|---|
| Config sync | Join with non-default server settings | HUD, inventory dialog, slot locks, and bag roles match the server configuration |
| Hotbar limits | Select, scroll, number-key, and Ctrl-scroll across the slot boundaries | Selection skips locked slots and wraps only across usable slots |
| Config shrink | Reduce usable slots while they contain items, then rejoin | Items move into valid storage or drop once; none disappear |
| Immersive roles | Try normal, sturdy, and hunter backpacks in B and waist bags in L/R | Valid roles equip; invalid roles are rejected or safely ejected |
| Offhand | Manually place non-bag items, bags, and backpacks into the offhand | Non-bag items follow config; all equippable bags are rejected |
| Automatic routing | Pick up items with hotbar, bag storage, and offhand in different states | Automatic routing never selects the offhand |
| Compact inventory | Open E with `HideBagContentsInDialog` true and false | Crafting-only and vanilla layouts compose without gaps or crashes |
| Equipped bag open | Right-click each equipped bag slot | Each bag toggles independently; several dialogs can remain open |
| Equipped bag contents | Move items while two or more equipped bag dialogs are open | Correct bag updates; no null-slot overlays, duplicated stacks, or locked dialogs |
| Equipped bag placement | Shift-click a bag slot and use selected-bag Shift + right-click | Exactly one bag is placed with its contents intact |
| Placed bag open | Right-click a placed supported bag | One contained-bag dialog opens with the correct layout and contents |
| Placed bag pickup | Shift + right-click with a compatible slot available | Exactly one bag equips and the ground storage is removed |
| Rejected pickup | Shift + right-click with no compatible slot available | The bag remains placed and the player receives one concise message |
| Rapid input | Repeatedly open, equip, place, and pick up supported bags | No item loss, duplicate dialogs, offhand overflow, or detached HUD bars |
| Selected rendering | Select and leave each equipped bag slot in both carrying modes | Selected bag appears in hand but not simultaneously on the worn player mesh |
| Reconnect | Leave and rejoin with bags equipped and containing items | Configuration, item contents, slot roles, and rendering recover correctly |

## Package checks

- [ ] `dotnet format Burdened.slnx --verify-no-changes --no-restore` passes.
- [ ] `dotnet build Burdened.csproj -c Release --no-restore` passes without warnings.
- [ ] `./build.ps1` creates the versioned zip from `modinfo.json`.
- [ ] The zip contains `Burdened.dll`, `modinfo.json`, and the `assets` tree.
- [ ] The zip excludes PDB, deps.json, local caches, and game assemblies.
- [ ] The release tag exactly matches `v<modinfo.version>`.
- [ ] The matching `CHANGELOG.md` section is used as the release description.
