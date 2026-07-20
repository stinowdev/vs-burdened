# Burdened
<img width="480" height="320" alt="Burdened" src="https://github.com/user-attachments/assets/91542e3a-2351-4324-b76e-9ca5c29fac0c" />

A code mod for [Vintage Story](https://www.vintagestory.at/) **1.22** that makes
carrying deliberate: fewer hotbar and bag-equip slots, enforced by the server
and reflected cleanly on the client HUD.

Everything is configured via `ModConfig/burdened.json`. The server owns the
file and syncs it to clients on join; the client never reads the config itself.

This mod is **standalone** (D01). Using it alongside Immersive Backpacks,
Wilderlands Onus Moderatus, or Immersive Modular Backpacks may break playability.

See [FEATURES.md](FEATURES.md) for the full roadmap and design decisions, and
[CHANGELOG.md](CHANGELOG.md) for release notes.

## Install

1. Grab the latest `burdened_*.zip` from
   [Releases](https://github.com/stinowdev/vs-burdened/releases/latest).
2. Drop the zip into your Vintage Story `Mods` folder.
3. Restart the game (or the server, then reconnect).

Required on both client and server.

## Features

| | Feature | Version |
|---|---|---|
| ✓ | **F01**: Reduced hotbar slots (`HotbarSlots`, 1-10) | 0.1.0 |
| ✓ | **F02**: Reduced bag-equip slots (`BagSlots`, 1-4) | 0.1.0 |
| ✓ | Server-enforced locks + config sync on join | 0.1.0 |
| ✓ | Item ejection from newly locked slots (D02), never deleted | 0.1.0 |
| ✓ | Hotbar HUD repack (only usable slots, vanilla-style border) | 0.1.0 |
| ✓ | **F05**: Concise hotbar scroll (skip locked slots, wrap both ways) | 0.2.0 |
| ✓ | **F03** / **D03**: Immersive L/B/R bag slots (rules only; meshes later) | 0.2.0 |
| ✓ | **F06** / **D06**: Offhand holds anything (usability stays vanilla) | 0.2.0 |
| | **D04, F04, F07-F09**: on-body meshes, placeable bags, … | planned |

#### HotbarSlots: 2, BagSlots: 1
<img width="536" alt="image" src="https://github.com/user-attachments/assets/7c4fb772-8cc7-43cc-9c5a-2307a4b2b6bc" />


#### Immersive mode: true
<img width="536" alt="image" src="https://github.com/user-attachments/assets/4b315e23-ab1b-47f0-a525-03cb4197f422" />
<img width="536" alt="image" src="https://github.com/user-attachments/assets/74ae724f-3fed-4aca-a664-3319669ca326" />

## Configuration

Created on first run at:

`%APPDATA%\VintagestoryData\ModConfig\burdened.json`

Keys that do something in the current build:

```json
{
  "HotbarSlots": 2,
  "BagSlots": 1,
  "ImmersiveCarryingMode": false,
  "OffhandHoldsAnything": true
}
```

When `ImmersiveCarryingMode` is `true`, bag-equip becomes three typed slots
(L / B / R) and `BagSlots` is ignored. **B** accepts only leather / sturdy /
hunter backpacks; **L** and **R** accept other bag-class storage (not those three).

`OffhandHoldsAnything` (default `true`) lets the offhand hold any item; you
still only *use* the main hand as in vanilla.

Other keys (`HideBagContentsInDialog`, `AutoPickupToBags`, `PlaceableBags`,
`RememberDialogPlacement`) are reserved for upcoming features.

Edit on the server (or in singleplayer), then restart / rejoin so clients pick
up the synced values.

## Building

```powershell
dotnet build                # debug build into bin/Debug
./build.ps1                 # release build + Releases/burdened_<version>.zip
./build.ps1 -Deploy         # also copy the zip into the game's Mods folder
```

## License

See [LICENSE](LICENSE). Personal non-commercial use and PRs back to this repo
are allowed; redistribution / modpacks need prior written permission.

---
You can support this and other projects [here](https://patreon.com/stinow). Thank you.
