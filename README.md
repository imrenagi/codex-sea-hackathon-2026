# Jalan Kita

A Bahasa Indonesia road-safety game for the Codex SEA Hackathon. Drive a car or scooter through one continuous left-hand route from Kampung Kenanga into a six-lane city district.

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
| H | Hold to sound the horn |
| M | Mute / unmute all sound |
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
| 06 | Jalan Thamrin | Going straight from the middle lane; waiting on red and for a pedestrian even after green |
| 07 | Thamrin–Diponegoro | Turning left with a signal from the kerb lane, yielding to pedestrians on the exit zebra |
| 08 | Diponegoro–Ahmad Yani | Moving to the right lane before solid markings; waiting for the separate right arrow and pedestrians |
| 09 | Railway | Stopping for warning lights/bell and moving only after the train clears and barriers fully rise |
| 10 | Hospital road | Passing a stopped angkot, then signalling left to give a siren-equipped ambulance room to pass |

The new district follows Ujian SIM missions 12, 9, and 8, reusing its original road arrows, signals, pedestrian lights, rail barriers, locomotive/coaches, angkot, and ambulance. It has three lanes each way, a 50 km/h limit, and 8 m red motorcycle waiting boxes. Cars stop at the rear line; motorcycles may use the front line. Every road uses Ujian SIM’s kinematic bicycle steering: keys gradually turn the wheels, steering sensitivity reduces with speed, and releasing the key centers the wheels while preserving the vehicle’s direction. Car and motorcycle wheelbases and steering limits match the source project. Pedal acceleration and speed limits remain tuned to this route. Choose the lane indicated by the road arrows before the solid section.

Existing five-checkpoint saves retain their progress and mistake history and unlock the added sections. The final quiz now comes after checkpoint 10.

Each passed mission shows a green completion card and saves its checkpoint. The card pauses the simulation; Continue resumes from the same position and speed without loading another scene. On a retry or relaunch, the vehicle starts at the last cleared checkpoint and resets the upcoming traffic and lesson checks. Traffic or pedestrian direction can change after a failed attempt. Progress, mistake counts, and quiz scores are stored locally and separately for each vehicle. The feedback screen offers a short in-scene demonstration that leaves the player's save intact.

## Build and verify

Use **Jalan Kita → Preview** to try an added section without changing player saves. Use **Jalan Kita → Verify Checkpoints**, **Build macOS**, or **Build WebGL** in Unity. Install Web Build Support for WebGL. Serve a browser build over HTTP with `python3 -m http.server 8080 --directory Unity/Builds/WebGL` and open `http://localhost:8080`. Play uses keyboard and mouse.

[Journey.cs](Unity/Assets/Scripts/Journey.cs) defines the route, thresholds, and lesson sources. [District.cs](Unity/Assets/Scripts/District.cs) lays out the road using the same coordinates. The FBX assets in [Resources/Models](Unity/Assets/Resources/Models) are copied directly from Ujian SIM. Its original material library and model loader are included in [Scripts/UjianSim](Unity/Assets/Scripts/UjianSim) to preserve the material palettes, atlas textures, glass, and light emission. The original surface texture generators, world-space mesh builder, and atmosphere are also included: textured asphalt and paving, the source wall palette and shop-sign colors, and afternoon lighting/post-processing. Roadside models include street-name plates, lamps, power poles, trees, road arrows, and parking signs. Result screens follow the original red/green tilted stamps, dark sheets, red buttons, and expandable explanation panels. The continuous route and checkpoint logic are implemented in this project.

Lesson wording is draft for the hackathon. The clearance distances, timing thresholds, and exact route maneuvers are choices for the simulation. [UU 22/2009](https://peraturan.bpk.go.id/Details/38654/uu-no-22-tahun-2009), [PM 13/2014 on signs](https://peraturan.bpk.go.id/Details/103683/permenhub-no-13-tahun-2014), [PM 49/2014 on traffic signals](https://peraturan.bpk.go.id/Details/103764/permenhub-no-49-tahun-2014), [PM 111/2015 on speed limits](https://peraturan.bpk.go.id/Details/103508/permenhub-no-111-tahun-2015).

For automated result-flow checks, run Unity in batch mode with
`-projectPath Unity -executeMethod JalanKita.EditorTools.ResultFlowChecks.Run`
(without `-quit`). This enters Play mode, checks both vehicles through all
ten completion cards, pause/resume, the final quiz, and violation retry,
then exits with the check result. Existing player saves are preserved.

The extended route checks cover both vehicles, turns, red lights and motorcycle stop boxes, pedestrian priority, protected-right signals, railway warnings, ambulance obstruction, retry headings, and upgrading old saves. The trigger distances and signal/rail timings are simulation choices. [Boulevard.cs](Unity/Assets/Scripts/Boulevard.cs) shares junction geometry with [BoulevardView.cs](Unity/Assets/Scripts/BoulevardView.cs).

Audio reuses Ujian SIM’s synthesized `SoundEngine` and WebGL audio bridge: separate car/motor engines responding to speed and throttle, indicator relay clicks, horn, phone ringtone, success/violation chimes, town ambience, approaching ambulance siren, and railway warning bell. The previous temporary siren/bell loops are removed. Mute is available in the menu/pause screen or with M and is remembered locally.
