# Burdened

<img width="400" alt="Burdened" src="https://github.com/user-attachments/assets/91542e3a-2351-4324-b76e-9ca5c29fac0c" />

Burdened is a universal code mod for Vintage Story that makes carrying more
deliberate: fewer usable hotbar and bag-equip slots, immersive bag roles, and
direct interaction with equipped or placed bags. The server owns the rules and
syncs them to every client.

## Compatibility

- Tested against Vintage Story **1.22.3**.
- Required on both the client and server.
- Standalone, with no dependency on other carrying or inventory mods.
- Mods such as Immersive Backpacks, Wilderlands Onus Moderatus, and Immersive
  Modular Backpacks modify overlapping behavior and may not be compatible.

Burdened patches some private game methods. Other Vintage Story patch versions
should be treated as unverified until they pass the release regression matrix.

## Installation

1. Download the latest `burdened_*.zip` from
   [GitHub Releases](https://github.com/stinowdev/vs-burdened/releases/latest).
2. Place the zip in the Vintage Story `Mods` directory.
3. Restart the game, or restart the server and reconnect.

## Features

| Feature | Available since |
|---|---|
| **F01**: Configurable usable hotbar slots | 0.1.0 |
| **F02**: Configurable usable bag-equip slots | 0.1.0 |
| Server-enforced slot locks and safe ejection from newly locked slots | 0.1.0 |
| **F03**: Immersive L/B/R bag role rules | 0.2.0 |
| **F05**: Concise hotbar scrolling across usable slots | 0.2.0 |
| **F06**: Manual offhand storage for non-bag items | 0.2.0 |
| **F04**: Compact crafting-only inventory dialog | 0.3.0 |
| **F08**: Direct open and pickup interaction for placed bags | 0.3.0 |
| **F10**: Open or place bags directly from equipped slots | 0.3.0 |

#### Compact slots

<img width="536" alt="Two hotbar slots and one bag slot" src="https://github.com/user-attachments/assets/7c4fb772-8cc7-43cc-9c5a-2307a4b2b6bc" />

#### Immersive mode

<img width="536" alt="Immersive carrying slots" src="https://github.com/user-attachments/assets/4b315e23-ab1b-47f0-a525-03cb4197f422" />
<img width="536" alt="Immersive carrying slot roles" src="https://github.com/user-attachments/assets/74ae724f-3fed-4aca-a664-3319669ca326" />

#### Improved bag interactions

<img width="720" alt="image" src="https://github.com/user-attachments/assets/152af418-42e8-4450-8d5b-34be2b04a34b" />

## Configuration

The server creates `%APPDATA%\VintagestoryData\ModConfig\burdened.json` on
first run. In singleplayer, the local game is the server. Restart or reconnect
after editing the file so clients receive the updated rules.

```json
{
  "HotbarSlots": 10,
  "BagSlots": 4,
  "ImmersiveCarryingMode": false,
  "HideBagContentsInDialog": true,
  "OffhandHoldsAnything": true,
  "ImprovedBagInteractions": true
}
```

- `HotbarSlots` allows 1 through 10 usable hotbar slots.
- `BagSlots` allows 1 through 4 usable bag-equip slots.
- `ImmersiveCarryingMode` replaces `BagSlots` with three typed slots: L and R
  accept waist bags, while B accepts the leather, sturdy, and hunter backpacks.
- `HideBagContentsInDialog` removes bag contents from the E inventory dialog;
  equipped bags remain available from the hotbar HUD.
- `OffhandHoldsAnything` allows manual placement of non-bag items in the
  offhand. Bags and automatic inventory routing are always excluded.
- `ImprovedBagInteractions` enables the complete F08/F10 interaction contract.

## Bag interactions

| Location | Input | Action |
|---|---|---|
| Placed bag | Right-click | Open the bag inventory |
| Placed bag | Shift + right-click | Equip into a compatible empty bag slot |
| Equipped bag slot | Right-click | Toggle that bag inventory |
| Equipped bag slot | Shift + click | Place the bag on the targeted block |
| Selected equipped bag | Shift + right-click | Place the bag on the targeted block |

Multiple equipped bags can remain open at the same time. Selecting an equipped
bag hides its worn copy while the game renders it in the active hand. Rejected
pickup or placement requests leave the bag and its contents untouched.

## Documentation

- [FEATURES.md](FEATURES.md) tracks implementation status and design decisions.
- [CHANGELOG.md](CHANGELOG.md) records release changes and known limitations.
- [docs/TESTING.md](docs/TESTING.md) defines the release regression matrix.

## Building

`resources/modinfo.json` is the source of truth for release metadata.

```powershell
dotnet build
./build.ps1
./build.ps1 -Deploy
```

The build script creates `Releases/burdened_<version>.zip`. `-Deploy` also
copies that package into the active Vintage Story `Mods` directory.

## License

See [LICENSE](LICENSE). Personal non-commercial use and pull requests back to
this repository are allowed. Redistribution and modpacks require prior written
permission.

### Support

You can support Burdened and other projects on
[Patreon](https://patreon.com/stinow).
