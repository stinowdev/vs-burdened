# Burdened

A code mod for [Vintage Story](https://www.vintagestory.at/) **1.22** that makes
carrying deliberate: fewer hotbar and bag slots, bags worn visibly on the body,
bags placeable and openable in the world like chests. 

Everything configurable via `ModConfig/burdened.json`.
Configs are enforced by the Server.

NOTE: This mod is **standalone**. Using it alongside Immersive Backpacks, Wilderlands
Onus Moderatus or Immersive Modular Backpacks might break playability.

See [FEATURES.md](FEATURES.md) for the full feature list and design decisions. 

## Configuration

The config file is created at:

`%APPDATA%\VintagestoryData\ModConfig\burdened.json`

Example config (`burdened.json`):

```json
{
  "HotbarSlots": 3,
  "BagSlots": 2,
  "ImmersiveCarryingMode": false,
  "HideBagContentsInDialog": true,
  "OffhandHoldsAnything": true,
  "AutoPickupToBags": true,
  "PlaceableBags": true,
  "RememberDialogPlacement": true
}
```

You can edit these values to customize the mod’s features. The server enforces the config for all players.

## Building

```powershell
dotnet build                # debug build into bin/Debug
./build.ps1                 # release build + Releases/burdened_<version>.zip
./build.ps1 -Deploy         # also copy the zip into the game's Mods folder
```