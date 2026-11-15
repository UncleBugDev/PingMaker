# Changelog

All notable changes to **PingMaker** will be documented in this file.

## [1.2.1] - 2026-11-15
- **Fixed Crosshair Aiming & Resolution Scaling**:
  - Implemented exact screen-space crosshair raycasting (`cam.ScreenPointToRay(crosshairPos)`), ensuring pings land precisely where the player's crosshair points regardless of display resolution, aspect ratio (16:9, 16:10, 21:9 Ultrawide, 32:9 Super-Ultrawide), or Valheim GUI scaling settings.
  - Replaced raw screen pixel positioning with `RectTransformUtility.ScreenPointToLocalPointInRectangle` on the UI canvas, eliminating reticle and 3D marker drift/offset on 1440p, 4K, 720p, and custom DPI screens.
  - Fixed creature lock-on jumping/snapping to distant off-screen or off-crosshair creatures: direct raycast has top priority, and proximity search is strictly bounded to a 38-pixel screen radius with full line-of-sight verification (preventing targeting through rocks, trees, or walls).
- **Added Native ESC (Pause) Menu Button**:
  - Added a styled `"ᚱ PingMaker"` button directly inside the Valheim pause/ESC menu next to the "Settings" button. Players can now open PingMaker configuration seamlessly both via the ESC menu and by pressing `'` / `э`.
  - Automatically matches native Valheim button textures, hover animations, click sounds, and vertical layout grouping.

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
