# Burdened

A code mod for [Vintage Story](https://www.vintagestory.at/) **1.22** that makes
carrying deliberate: fewer hotbar and bag slots, bags worn visibly on the body,
bags placeable and openable in the world like chests. 

Everything configurable via `ModConfig/burdened.json`.
Configs are enforced by the Server.

NOTE: This mod is **standalone**. Using it alongside Immersive Backpacks, Wilderlands
Onus Moderatus or Immersive Modular Backpacks might break playability.

See [FEATURES.md](FEATURES.md) for the full feature list and design decisions. 

## Building

```powershell
dotnet build                # debug build into bin/Debug
./build.ps1                 # release build + Releases/burdened_<version>.zip
./build.ps1 -Deploy         # also copy the zip into the game's Mods folder
```
