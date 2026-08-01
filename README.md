# Burdened

<img
  width="400"
  alt="Burdened"
  src="https://i.imgur.com/EM0iv3w.png"
/>

Burdened is a universal code mod for Vintage Story that makes carrying more
deliberate through fewer usable slots, immersive bag roles, and direct bag
interactions. It does not add per-item weight. The server owns the carrying
rules and sends them to every client.

You can also check the [original ModDB page](https://mods.vintagestory.at/burdened).

## Carrying space

The number of usable hotbar and bag-equip slots is configurable. The HUD
contracts around those slots, scrolling skips locked positions, and the server
rejects attempts to use them. If a configuration change locks an occupied slot,
its item moves into valid storage or drops at the player's feet.

<img
  width="536"
  alt="A compact hotbar with two item slots and one bag slot"
  src="https://i.imgur.com/kkV9aYR.png"
/>

Immersive carrying mode replaces the normal bag bar with three slots across two
roles:

- **B** takes your pack: a bag worn on the back that holds anything, such as the
  leather, sturdy, and hunter backpacks.
- **L** and **R** take everything else, including bags that only hold one kind
  of thing. A quiver and a mining bag ride at the waist, so they never cost you
  the slot your backpack needs.

Bags added by other mods are sorted the same way with no setup.
`BagRoleOverrides` below changes the placement of any bag.

<img
  width="536"
  alt="The immersive L, B, and R bag slots"
  src="https://i.imgur.com/uU4LFRe.png"
/>

<img
  width="536"
  alt="Items accepted by the immersive bag roles"
  src="https://i.imgur.com/Aywlx48.png"
/>

## Bag interactions

Equipped and placed bags can be used without moving them through the main
inventory. Several equipped bag windows can remain open at once. When an
equipped bag is selected, it appears in the active hand without also appearing
as a worn duplicate.

| Location | Input | Action |
|---|---|---|
| Placed bag | Right-click | Open the bag |
| Placed bag | Shift + right-click | Equip it into a compatible empty bag slot |
| Equipped bag slot | Right-click | Open or close that bag |
| Equipped bag slot | Shift + click | Place it on the targeted block |
| Selected equipped bag | Right-click | Open or close the bag when the targeted block does not use the input |
| Selected equipped bag | Shift + right-click | Place it on the targeted block |

Use **Ctrl + mouse wheel** to include equipped bag slots in hotbar selection.
Rejected pickup and placement leave the bag and its contents at the source.
These interactions apply only to equippable bags that support vanilla ground
storage. Chests, vessels, and other containers keep their normal behavior.
Wall-mounted bags such as quivers keep their vanilla placement gesture:
**Ctrl + Shift** against a wall.

<img
  width="720"
  alt="Several equipped and placed bag inventories open at once"
  src="https://i.imgur.com/NpUYCqf.png"
/>

## Inventory and offhand

The E inventory can be reduced to a compact crafting-only window. Equipped bags
remain available from the hotbar.

The offhand can accept non-bag items without adding dual-wield item use. Bags
are always rejected. While `OffhandHoldsAnything` is enabled, automatic
inventory routing also excludes the offhand.

## Configuration

The server creates
`%APPDATA%\VintagestoryData\ModConfig\burdened.json` on first run.

| Setting | Default | Effect |
|---|---|---|
| `HotbarSlots` | `10` | Usable hotbar slots, from 1 to 10 |
| `BagSlots` | `4` | Usable bag-equip slots, from 1 to 4 |
| `ImmersiveCarryingMode` | `false` | Use the L / B / R layout instead of `BagSlots` |
| `HideBagContentsInDialog` | `true` | Keep bag contents out of the E inventory |
| `OffhandHoldsAnything` | `true` | Allow manual offhand storage for non-bag items |
| `ImprovedBagInteractions` | `true` | Enable direct opening, pickup, and placement of bags |
| `BagRoleOverrides` | `{}` | Assign bags to the B or L / R slots by item code |

After editing the file, restart the dedicated server or reopen the singleplayer
world so the server loads and sends the updated settings.

### Bag roles in immersive mode

With `ImmersiveCarryingMode` on, B accepts a general-purpose pack: an equippable
bag the game wears at its `backpack` position that can also hold ordinary items.
Other equippable bags use L / R, while items that are not bags keep their vanilla
bag-slot rules. In Vintage Story 1.22.3, the normal, sturdy, and hunter backpacks
use B.

`BagRoleOverrides` changes the role of any bag, including bags added by other
mods. Codes accept `*` wildcards, and a code without a domain uses `game:`. The
two accepted roles are `back` and `waist`:

```json
"BagRoleOverrides": {
  "othermod:rucksack-*": "back",
  "game:backpack-sturdy": "waist"
}
```

Bag mods can declare their own default role in item JSON:

```json
"attributes": { "burdened": { "bagRole": "back" } }
```

A server override always wins over that default. Listing an item that is not a
bag does not make it equippable. Entries the server cannot use are skipped and
written to the log at startup so a typo is easy to spot.

## Compatibility

- Built for Vintage Story **1.22.3**.
- Required on both the client and server.
- Standalone, with no required dependencies.
- Immersive Backpacks, Wilderlands Onus Moderatus, and Immersive Modular
  Backpacks modify overlapping behavior and may not be compatible.

Burdened patches private game methods. Other Vintage Story versions remain
unverified until they pass the same review and in-game checks.

### Upgrading from 0.3.0

Existing worlds are unaffected because Burdened stores no save data of its own.
Version 0.4.0 adds an empty `BagRoleOverrides` setting automatically and requires
no manual migration. It also changes the network protocol, so clients and
servers must both update to 0.4.0.

## Installation

1. Download the latest `burdened_*.zip` from
   [GitHub Releases](https://github.com/stinowdev/vs-burdened/releases/latest).
2. Place the zip in the Vintage Story `Mods` directory.
3. Restart the game, or restart the server and reconnect.

## Building

`resources/modinfo.json` is the source of truth for release metadata.

```powershell
dotnet build
./build.ps1
./build.ps1 -Deploy
```

The build script creates `Releases/burdened_<version>.zip`. `-Deploy` also
copies that package into the active Vintage Story `Mods` directory.

## Documentation

- [FEATURES.md](FEATURES.md) records feature status and design decisions.
- [CHANGELOG.md](CHANGELOG.md) records release changes and known limitations.
- [docs/MODDB.html](docs/MODDB.html) contains the maintained Mod DB page.

## License

See [LICENSE](LICENSE). Personal non-commercial use and pull requests back to
this repository are allowed. Redistribution and modpacks require prior written
permission.

## Support

You can support Burdened and other projects on
[Ko-fi](https://ko-fi.com/stinow).
