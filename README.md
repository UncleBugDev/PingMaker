# PingMaker

[![GitHub Release](https://img.shields.io/github/v/release/UncleBugDev/PingMaker?color=blue&label=Release)](https://github.com/UncleBugDev/PingMaker/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Thunderstore](https://img.shields.io/badge/Thunderstore-Valheim-orange.svg)](https://valheim.thunderstore.io/)

A modern, highly customizable 3D world ping, tactical targeting, and multiplayer synchronization mod for **Valheim**. Communicate points of interest, hostiles, and fellow vikings instantly in the 3D world and on the map without losing immersion.

**GitHub Repository:** [https://github.com/UncleBugDev/PingMaker](https://github.com/UncleBugDev/PingMaker)

---

## ✨ Features

- **Personal Viking Color Palette**: Choose from 8 vibrant personal colors (Yellow, Orange, Red, Pink, Purple, Blue, Cyan, Lime). Every ping, marker, distance counter, creature lock-on, map tracker, and nameplate you create will be in **YOUR** distinctive color so your clan immediately recognizes who marked what!
- **Tactical Lock-On Reticle (`↘ ⭕`)**:
  - Aiming at hostile creatures or beasts dynamically displays an animated tactical lock-on ring with a diagonal pointing arrow (`↘ ⭕` by default, or top arrow) and the creature's name with star rating (`★`).
  - Friendly players are strictly filtered out so targeting never locks onto allies or distracts during co-op combat.
  - 3 selectable operational modes: *Both on hover and on ping (Default)*, *Only on ping*, or *Only on hover*.
- **Laser Designator 3D Markers**:
  - World and creature pings render as a sleek laser designator — a focused bright laser point in the center with a softly breathing ambient laser haze bloom.
  - Obstructive skull icons have been completely removed; creatures remain 100% visible with a clean transparent lock-on circle.
- **Soft Creature Contour Outline**:
  - Marking a monster applies a subtle, non-blinding contour emission (0.28x) so monster fur, scales, and details remain crisp and clear in the dark.
  - Automatically un-highlights and removes the 3D marker the instant the monster dies.
- **Colored Player Overhead Nameplates**:
  - 3D floating player names in the world and center crosshair hover names are colored according to each player's chosen personal color instead of the standard yellow.
- **Real-Time Player Directional Map Trackers**:
  - Player pins on the minimap and large world map rotate in real-time according to their view/movement direction and glow in each player's personal color.
- **True-Color Minimap Radar Pings**:
  - Custom pure white procedural radar ping sprite eliminates the game's default cyan texture color-blending bug — pink stays vibrant pink, yellow stays bright yellow, and orange stays orange.
- **Viking-Themed In-Game GUI (Key: `'` / Russian `э`)**:
  - Authentic Nordic oak plaque styling with Valheim gold borders, runic banners, and native game font.
  - Interactive one-click key rebinding supporting keyboard and mouse buttons (e.g. `Mouse 4`, `Mouse 3`, etc.).
  - Instant personal color picker with live preview.
  - Automatic mouse cursor release (no need to open `Esc` menu first).
- **Accidental Switch Prevention**:
  - `AlternativePingKey` default set to `None` to prevent conflicts with vanilla emote keys (`T` / `G`).
  - Radial menu on hold is disabled by default (can be toggled on in settings), eliminating accidental color switches during battle.
- **Zero-Lag Multiplayer**:
  - Synchronized across clients via Valheim's `ZRoutedRpc` with zero latency for the pinger.

---

## 🎮 Controls

| Action | Default Input | Description |
| :--- | :--- | :--- |
| **Quick Ping** | `Mouse 4` (or rebindable in GUI) | Places a 3D laser ping at crosshair or locks onto a targeted creature |
| **Open Settings GUI** | `'` (Quote / Russian `э`) | Opens the authentic Viking GUI to pick colors, rebind keys, and adjust styles |
| **Close Settings GUI** | `Esc` or `'` | Closes and immediately persists all changes |

*Keys can be re-bound dynamically in-game via the Settings GUI or in `BepInEx/config/com.author.pingmaker.cfg`.*

---

## 🎨 Personal Color Palette

Choose from 8 high-visibility vibrant colors:
- 🟡 **Жёлтый (Yellow)**
- 🟠 **Оранжевый (Orange)**
- 🔴 **Красный (Red)**
- 🌸 **Розовый (Neon Pink)**
- 🟣 **Фиолетовый (Royal Purple)**
- 🔵 **Синий (Electric Blue)**
- 🔷 **Голубой (Cyan)**
- 🟢 **Лайм (Neon Lime)**

---

## ⚙️ Configuration

Settings are saved in `BepInEx/config/com.author.pingmaker.cfg` and can be adjusted in-game via the GUI (`'`) or Configuration Manager (`F1`):

- **PingKey**: Primary key for placing pings (default: `Mouse4`).
- **AlternativePingKey**: Optional secondary ping key (default: `None`).
- **SettingsKey**: Hotkey to toggle the in-game settings panel (default: `Quote`).
- **EnableRadialColorMenu**: Whether holding the ping key opens the radial color menu (default: `false`).
- **ActiveColorIndex**: Your currently selected personal color index (0–7).
- **TargetReticleMode**: Reticle operational mode (`0` = Both hover and ping, `1` = On-ping only, `2` = Hover only).
- **ReticleArrowStyle**: Arrow style for target lock-on (`0` = Diagonal `↘ ⭕`, `1` = Top `↓ ⭕`).
- **PingDuration**: Lifetime of 3D markers in seconds (default: `20`s).
- **CreatureHighlightDuration**: Duration of monster contour highlight (default: `18`s).
- **MaxPingDistance**: Maximum raycast distance in meters (default: `1000`m).
- **ShowDistance**: Display distance counter `[Xm]` beneath markers (default: `true`).
- **ShowOffscreenArrow**: Show directional edge arrows when marker is off-screen (default: `true`).
- **SyncToMinimap**: Mirror pings onto minimap and world map (default: `true`).
- **ColorizeMapPlayers**: Colorize player direction arrows on maps (default: `true`).

---

## 📦 Installation

### Thunderstore / r2modman (Recommended)
1. Install [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/) or Thunderstore Mod Manager.
2. Search for **PingMaker** and click install.
3. Launch game via Mod Manager.

### Manual
1. Ensure [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) is installed.
2. Download the latest `PingMaker_v1.2.0.zip` from [GitHub Releases](https://github.com/UncleBugDev/PingMaker/releases).
3. Extract `PingMaker.dll` into `Valheim/BepInEx/plugins/UncleBugDev-PingMaker/`.
4. Launch Valheim.

---

## 🤝 Contributing & Support

- Found a bug or have a suggestion? Open an issue on [GitHub Issues](https://github.com/UncleBugDev/PingMaker/issues).
- Pull requests are welcome!
