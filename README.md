# Fortuner City MVP (Unity)

Playable PC prototype: drive a **Fortis XR** (Fortuner-style SUV) through a procedural city district with NPC traffic, chase/hood camera, HUD, pause/restart, and engine audio.

## Requirements
- Unity **6000.0 LTS** (Unity 6) or **2022.3 LTS**
- Modules: Windows Build Support
- Packages resolve on open: URP + Input System (see `Packages/manifest.json`)

## Open & Play
1. Install [Unity Hub](https://unity.com/download) → add Unity 6 LTS (or 2022.3).
2. Hub → **Open** → select folder `fortuner-city-mvp`.
3. If prompted about Input System, choose **Yes** / Input System Package.
4. Open scene `Assets/Scenes/CityDrive` (or press Play on `Boot` — it loads CityDrive).
5. Press **Play**.

City, SUV, traffic, camera, HUD, and audio spawn automatically at runtime (`GameBootstrap` / `AutoSceneStart`).

## Controls
| Key | Action |
|-----|--------|
| W / ↑ | Accelerate |
| S / ↓ | Brake / Reverse |
| A D / ← → | Steer |
| Space | Handbrake |
| C | Chase ↔ Hood camera |
| Esc | Pause |
| R | Restart (while paused) |

Gamepad: triggers = throttle/brake, left stick = steer, South button = handbrake.

## Windows build
In Unity menu: **Fortuner City → Build Windows Standalone**  
Output: `Builds/Windows/FortunerCityMVP.exe`

Or use batchmode (after Editor is installed):

```bat
"<UnityEditor>\Unity.exe" -quit -batchmode -projectPath "d:\office-work\fortuner-city-mvp" -executeMethod FortunerCity.EditorTools.BuildWindows.Build
```

## Project layout
```
Assets/Scripts/Vehicle/   FortunerBuilder, WheelCollider drive, wheel visuals
Assets/Scripts/City/      Procedural 6×6 block district
Assets/Scripts/Traffic/   Waypoints + NPC cars
Assets/Scripts/Camera/    Chase / hood
Assets/Scripts/UI/        Speed, gear, pause
Assets/Scripts/Audio/     Procedural engine / tire / crash
Assets/Scenes/            Boot → CityDrive
Assets/Editor/            Windows build menu
```

## Notes
- In-game name **Fortis XR** — Fortuner silhouette, not Toyota-licensed branding.
- Art is procedural for MVP; swap meshes later and list licenses in `Assets/Art/LICENSES.md`.
