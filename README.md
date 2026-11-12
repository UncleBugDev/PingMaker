# PingMaker

[![GitHub Release](https://img.shields.io/github/v/release/UncleBugDev/PingMaker?color=blue&label=Release)](https://github.com/UncleBugDev/PingMaker/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Thunderstore](https://img.shields.io/badge/Thunderstore-Valheim-orange.svg)](https://valheim.thunderstore.io/)

A modern, multiplayer 3D world ping and creature silhouette highlighting mod for **Valheim**. Communicate points of interest, hostile creatures, and dangerous areas with friends instantly in the 3D world without opening the map.

**GitHub Repository:** [https://github.com/UncleBugDev/PingMaker](https://github.com/UncleBugDev/PingMaker)

---

## ✨ Features

- **Personal Player Color Palette**: Choose your own distinctive personal color (Yellow, Orange, Red, Pink, Purple, Blue, Cyan, Lime). Every ping, marker, distance counter, and creature highlight you create will be in **YOUR** unique color so your clan instantly knows who placed it!
- **3D World Space Pinging**: Aim directly at terrain, rocks, trees, or structures with your crosshair and ping with a quick press of `T`.
- **Creature Silhouette Highlighting**: Pinging any creature (troll, greydwarf, wolf, boss) outlines and pulses its 3D silhouette in your personal color for 18 seconds, visible through trees, mountains, and darkness.
- **Dynamic Distance Counter & Billboard UI**: Real-time distance in meters (`[142m]`) and your nickname above the marker.
- **Off-Screen Edge Indicators**: Never lose track of a friend's ping; off-screen pings clamp smoothly to your screen border with a directional pointer arrow.
- **Radial Color Palette**: Hold `T` for 0.2s to open an in-game radial color wheel and pick your personal color on the fly.
- **Minimap Integration**: Automatically mirrors your ping onto the mini-map and world map with your personal color.
- **Zero-Lag Multiplayer**: Synchronized across clients via Valheim's `ZRoutedRpc` with zero input lag for the sender.

---

## 🎮 Controls

| Action | Default Key / Input | Description |
| :--- | :--- | :--- |
| **Quick Ping** | `T` (Quick Tap) | Instantly places a 3D ping at your crosshair in your active personal color |
| **Personal Color Wheel** | `T` (Hold > 0.2s) | Opens the circular color palette; hover a color and release to set it |

*Keybindings, durations, and colors can be customized in `BepInEx/config/com.author.pingmaker.cfg` or in-game via Configuration Manager (`F1`).*

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

Settings are saved in `BepInEx/config/com.author.pingmaker.cfg`:
- **ActiveColorIndex**: Your currently selected color (persisted across restarts).
- **PingKey**: Hotkey for pinging and opening the radial palette (default: `T`).
- **HoldTimeForMenu**: How long to hold the key to open the radial wheel (default: `0.20`s).
- **PingDuration**: Lifetime of 3D markers in seconds (default: `20`s).
- **CreatureHighlightDuration**: Duration of monster silhouette glow (default: `18`s).
- **MaxPingDistance**: Maximum raycast distance (default: `1000`m).
- **SyncToMinimap**: Mirror pings onto minimap (default: `true`).

---

## 📦 Installation

### Thunderstore / r2modman (Recommended)
1. Install [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/) or Thunderstore Mod Manager.
2. Search for **PingMaker** and install.
3. Launch game via Mod Manager.

### Manual
1. Ensure [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) is installed.
2. Download the latest `PingMaker.zip` from [GitHub Releases](https://github.com/UncleBugDev/PingMaker/releases).
3. Place `PingMaker.dll` inside `Valheim/BepInEx/plugins/PingMaker/`.
4. Launch Valheim.

---

## 🤝 Contributing & Support

- Found a bug or have a suggestion? Open an issue on [GitHub Issues](https://github.com/UncleBugDev/PingMaker/issues).
- Pull requests are welcome!
