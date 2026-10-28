# Fortuner City (Unity)

## ▶ Play online: **https://fortuner-city.vercel.app**

Runs in Chrome / Edge on a computer (keyboard + mouse). Click the game once, then drive with **W A S D**.

---

An open-city driving game where almost everything is generated in C# at runtime:

- **City** — 8×8 blocks: downtown glass skyscrapers, mid-rise offices and apartments, suburbs, parks, avenues with tree medians, zebra crossings and an elevated ring motorway with ramps
- **Working traffic signals** at every junction — AI traffic stops at red, pedestrians cross on the zebra when cars are held
- **Real-size cars** (Fortuner-style SUV plus Haval / Lamborghini / Range Rover / Jaecoo / Land Cruiser / MG-style traffic, no brand logos) and **motorbikes** (CD70-style commuter, sports bike) that lean into corners
- **People** walking the sidewalks with individual gaits; get out of your car and walk, run or take **any** car or bike (F)
- **Weapons** (pistol, rifle, shotgun) with car damage, smoke and fire
- **Engine sound** from a physical model: cylinder pulses through an exhaust pipe and muffler, gearbox, turbo, overrun crackle

Third-party art: [Kenney](https://kenney.nl) (CC0) houses, trees and street props.

## Controls
| Key | Driving | On foot |
|-----|---------|---------|
| W / S | Accelerate / brake, reverse | Run / back |
| A / D | Steer | Turn (side-step with a gun out) |
| Shift / Ctrl | — | Sprint / walk |
| Space | Handbrake | Jump |
| H | Horn | — |
| C | Chase ↔ driver's-eye camera | — |
| F | Get out | Get into the nearest car or bike |
| 1 / 2 / 3, Q | — | Pistol / rifle / shotgun, holster |
| Mouse, left click, R | — | Aim, fire, reload |
| Esc (then R) | Pause (restart) | Pause (restart) |

## Open in Unity
1. Install [Unity Hub](https://unity.com/download) and Unity **6000.6.7f1**.
2. Hub → **Add → Add project from disk** → select the `fortuner-city-mvp` folder.
3. Open `Assets/Scenes/CityDrive` and press **Play**.

## Builds
- **Chrome / WebGL:** menu **Fortuner City → Build for Chrome (WebGL)** → `Builds/WebGL` (play locally with `play-in-chrome.bat`)
- **Windows:** menu **Fortuner City → Build Windows Standalone** → `Builds/Windows/FortunerCityMVP.exe`

## Project layout
```
Assets/Scripts/City/      City, roads, buildings, traffic signals, motorway, procedural textures
Assets/Scripts/Vehicle/   Car / bike builders and catalog, arcade drive
Assets/Scripts/Traffic/   AI traffic and spawner
Assets/Scripts/People/    Pedestrians and the person builder / animator
Assets/Scripts/Player/    On-foot player, enter / exit vehicles
Assets/Scripts/Weapons/   Guns, car damage, effects
Assets/Scripts/Audio/     Engine model, vehicle audio, synthesised sounds
Assets/Scripts/Core/      Bootstrap and scene start
Assets/Editor/            Build menu items
```
