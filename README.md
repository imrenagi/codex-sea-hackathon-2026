# Jalan Kita

A Bahasa Indonesia road-safety game for the Codex SEA Hackathon. Drive a car or scooter through one continuous left-hand route in Kampung Kenanga.

## Play

Launch [Jalan Kita.app](<Unity/Builds/macOS/Jalan Kita.app>). Choose **Mobil** or **Motor**, take the short quiz, and complete the safety checklist before driving.

The desktop build supports both Apple Silicon and Intel Macs. Menus also support **Tab** to move between buttons and **Enter** to select.

To run in the editor, open the [Unity project](Unity) in Unity **6000.3.27f1**, open [KampungKenanga.unity](Unity/Assets/Scenes/KampungKenanga.unity), and press **Play**.

| Key | Action |
| --- | --- |
| W or ↑ | Accelerate |
| S, ↓, or Space | Brake |
| A / D or ← / → | Steer |
| Q / E | Left / right indicator |
| L | Motorcycle headlight |
| C | Switch camera |
| F / G | Ignore / answer the call |
| Tab or Esc | Progress and pause |
| R | Retry from the current checkpoint |

## Continuous route

| Checkpoint | Section | What the player practices |
| --- | --- | --- |
| 01 | Garasi Kenanga | Both front seat belts for the car, or both SNI helmets and the headlight for the motorcycle |
| 02 | Jalan Kenanga | Full stop before the line, yielding to cross traffic, and obeying the traffic light |
| 03 | Pasar Bunga | Waiting for a pedestrian and giving priority to traffic on the main road |
| 04 | Jalan Mawar | Following the one-way route, checking the gap, signalling to change lanes, and merging left |
| 05 | Zona Sekolah | Staying within 30 km/h and ignoring a phone call or pulling into the stopping bay |

The route keeps moving at each pass. On a retry or relaunch, the vehicle starts at the last cleared checkpoint and resets the upcoming traffic and lesson checks. Traffic or pedestrian direction can change after a failed attempt. Progress, mistake counts, and quiz scores are stored locally and separately for each vehicle. The feedback screen offers a short in-scene demonstration that leaves the player's save intact.

## Build and verify

Use **Jalan Kita → Verify Checkpoints**, **Build macOS**, or **Build WebGL** in Unity. Install Web Build Support for WebGL. Serve a browser build over HTTP with `python3 -m http.server 8080 --directory Unity/Builds/WebGL` and open `http://localhost:8080`. Play uses keyboard and mouse.

[Journey.cs](Unity/Assets/Scripts/Journey.cs) defines the route, thresholds, and lesson sources. [District.cs](Unity/Assets/Scripts/District.cs) lays out the road using the same coordinates. The FBX assets in [Resources/Models](Unity/Assets/Resources/Models) are copied directly from Ujian SIM. Its original material library and model loader are included in [Scripts/UjianSim](Unity/Assets/Scripts/UjianSim) to preserve the material palettes, atlas textures, glass, and light emission. The continuous route and checkpoint logic are implemented in this project.

Lesson wording is draft for the hackathon. The clearance distances, timing thresholds, and exact route maneuvers are choices for the simulation. [UU 22/2009](https://peraturan.bpk.go.id/Details/38654/uu-no-22-tahun-2009), [PM 13/2014 on signs](https://peraturan.bpk.go.id/Details/103683/permenhub-no-13-tahun-2014), [PM 49/2014 on traffic signals](https://peraturan.bpk.go.id/Details/103764/permenhub-no-49-tahun-2014), [PM 111/2015 on speed limits](https://peraturan.bpk.go.id/Details/103508/permenhub-no-111-tahun-2015).
