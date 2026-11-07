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
| Z / X | Hold to look left / right; release to face forward |
| H | Hold to sound the horn |
| M | Mute / unmute all sound |
| F / G | Ignore / answer the call |
| Tab or Esc | Progress and pause |
| R | Retry from the current checkpoint |

## Pitch demo

Choose **Mobil** or **Motor**, then **Demo singkat · Hak utama**. This opens Pasar Bunga directly. The demo uses a separate session and leaves saved progress unchanged.

1. Hold **W** toward the occupied zebra crossing to show a violation. Release the key when the feedback opens.
2. Show the explanation, correct action, and visible legal citation. **Lihat cara benar** plays the existing driving coach. **Tab** returns to the feedback.
3. Press **R** to retry. Use **Space** to stop before the crossing. Wait for the pedestrian. At the next intersection, stop before the yield line and give way to cross traffic. **Lihat cara benar** is the live backup for this full sequence.
4. Finish at the green checkpoint. The demo ends with its actual mistake count. **Ulangi demo** starts a fresh session.

| Pitch segment | Time |
| --- | --- |
| Problem | 45 seconds |
| Approach | 30 seconds |
| Game demo | 90 seconds |
| Legal sources and learning loop | 45 seconds |
| Prototype scope | 45 seconds |
| Next steps | 45 seconds |
| Transitions and retry buffer | 60 seconds |

The map preview is optional. Limit it to 15 seconds within the prototype section. Keep a recording of the rehearsed demo on the presenting laptop.

Before presenting, use Unity **6000.3.27f1** on the other laptop. Check the demo in both vehicle modes at **1280 × 720**. Try violation, retry, **Lihat cara benar**, pause/resume, completion, replay, and return to the saved journey. Confirm that the earlier save is unchanged. Build and rehearse on that laptop. This change has not been built or tested locally.

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

The big roads carry mixed cars and motorcycles in both directions through the straight lanes. Traffic uses Ujian SIM’s following/braking behavior, randomized vehicle speeds and spacing, and original vehicle models/colors. It queues at red lights and motorcycle stop boxes, waits for pedestrians and railway barriers, and avoids the player. Traffic keeps moving between checkpoints and resets safely on retry or mission selection.

The new district follows Ujian SIM missions 12, 9, and 8, reusing its original road arrows, signals, pedestrian lights, rail barriers, locomotive/coaches, angkot, and ambulance. It has three lanes each way, a 50 km/h limit, and 8 m red motorcycle waiting boxes. Cars stop at the rear line; motorcycles may use the front line. Every road uses Ujian SIM’s kinematic bicycle steering: keys gradually turn the wheels, steering sensitivity reduces with speed, and releasing the key centers the wheels while preserving the vehicle’s direction. Car and motorcycle wheelbases and steering limits match the source project. Pedal acceleration and speed limits remain tuned to this route. Choose the lane indicated by the road arrows before the solid section.

Click any mission number in the top-right HUD to jump directly to its starting checkpoint in practice mode. All ten missions are available; mission 01 opens the safety checklist. Practice can continue along the route, but it never overwrites saved journey progress or quiz scores. Return to the main menu and choose **Lanjutkan perjalanan** to resume the saved route.

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

The driving HUD includes live left, centre and right rear views for both vehicles. Mirror images are horizontally flipped; cameras take turns rendering to limit GPU work. Hold Z/X or the on-screen look buttons to check either side without steering. The ambulance siren is audible as it approaches from 70 m, ducks the engine mix, and still respects M/mute. The siren begins after passing the parked angkot in mission 10.

Run `JalanKita.EditorTools.TrafficChecks.Run` for the traffic seed sweep and `JalanKita.EditorTools.DrivingViewChecks.Run` in a graphical Editor for live mirror/audio validation.

## Jakarta map previews

From the main menu choose **Jelajahi peta Jakarta**, then Morrissey, Bundaran HI, Sea Labs, Semanggi, Monas, or Bundaran Senayan. Each area covers approximately 1.7 × 1.5 km, three times the original linear extent. Use WASD/arrows to pan, +/- to zoom, R to reset, and Esc to return to the selector. The existing Kampung Kenanga lessons and saved progress remain separate.

These are OSM-based city previews, not playable lesson routes. Building heights and road widths may be estimated; facade colors and windows are illustrative. Source ways are clipped to the preview area. Tunnels are omitted; positive road layers receive approximate elevation. OSM coverage is incomplete and does not guarantee surveyed geometry. © OpenStreetMap contributors, ODbL: https://www.openstreetmap.org/copyright

Only the selected area is generated. Geometry is batched into meshes under the 16-bit vertex limit rather than creating thousands of building objects. **Jalan Kita → Verify City Maps** checks the six datasets and geometry helpers. Runtime/editor verification must be run once Unity 6000.3.27f1 is available.
