# Burdened

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

## What's in 0.1.0

| | Feature |
|---|---|
| ✓ | **F01** — Reduced hotbar slots (`HotbarSlots`, 1–10) |
| ✓ | **F02** — Reduced bag-equip slots (`BagSlots`, 1–4) |
| ✓ | Server-enforced locks + config sync on join |
| ✓ | Item ejection from newly locked slots (D02) — never deleted |
| ✓ | Hotbar HUD repack (only usable slots, vanilla-style border) |
| | **F03–F09** — planned (immersive carrying, placeable bags, …) |

## Configuration

Created on first run at:

`%APPDATA%\VintagestoryData\ModConfig\burdened.json`

Keys that actually do something in **0.1.0**:

```json
{
  "HotbarSlots": 5,
  "BagSlots": 1
}
```

The file may also contain keys for upcoming features (`ImmersiveCarryingMode`,
`HideBagContentsInDialog`, `OffhandHoldsAnything`, `AutoPickupToBags`,
`PlaceableBags`, `RememberDialogPlacement`). Those are reserved; changing them
has no effect until the matching feature ships.

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
