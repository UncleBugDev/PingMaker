# Changelog

All notable changes to **PingMaker** will be documented in this file.

## [1.2.0] - 2026-11-14
- Added tactical lock-on reticle for creatures (circle + diagonal pointing arrow `↘ ⭕`) with 3 selectable operational modes.
- Added laser designator 3D markers with soft pulsing laser haze bloom.
- Removed opaque skull icon from creature pings, keeping creatures fully visible.
- Added colored player overhead 3D nameplates and center crosshair hover names.
- Added true-color procedural radar ping sprite for minimap (eliminating blue tint color distortion).
- Excluded friendly players from creature lock-on targeting.
- Disabled radial color menu on hold by default and unbound secondary G key to prevent accidental color changes.
- Fixed `Character.GetHeadPoint()` crash on creatures without head bones (e.g. Greydwarf Shaman).

## [1.1.0] - 2026-11-12
- Added Viking-themed in-game GUI with Valheim gold styling and rune ornaments.
- Added custom colored player direction arrows on map and minimap (real-time heading tracking).
- Added multiplayer personal color sync across network via RPC.
- Fixed cursor unlock on menu open (no longer requires pressing ESC).
- Auto-cleaning of expired minimap pins.

## [1.0.0] - Initial Release
- Initial implementation of the contextual 3D ping system.
- Added creature highlighting with threat color indicators.
- Added 3D world space distance and directional HUD markers.
- Added minimap and large map pin integration.
- Integrated ZRoutedRpc multiplayer networking for instant synchronization between players.
- Added radial ping wheel for categorized pings.
- Added BepInEx configuration and ConfigurationManager support.
