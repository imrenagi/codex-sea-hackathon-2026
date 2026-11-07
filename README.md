# Jalan Kita

A Bahasa Indonesia road-safety game for the Codex SEA Hackathon. Practise focused skills, play everyday driving missions, or follow the continuous left-hand route in Kampung Kenanga.

## Play

Launch [Jalan Kita.app](<Unity/Builds/macOS/Jalan Kita.app>). Choose **Mobil** or **Motor**, select a vehicle, then **Uji keterampilan**, **Misi sehari-hari**, the continuous journey, or **Jelajahi kantor Sea · Sopo Del**. The five available vehicles are Mobil kota, Sedan, BeAT merah-putih, BeAT biru and Skuter klasik. The selected vehicle is used in both training and exploration. The continuous journey retains its before/after quizzes.

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
| F / G | Ignore / answer the call; F collects the passenger in the market mission |
| Tab or Esc | Progress and pause |
| R | Retry from the current checkpoint |

## Sopo Del / Sea office exploration

Choose **Jelajahi kantor Sea · Sopo Del** in the main menu to drive about 250 m along Jl. Mega Kuningan Barat III. This is the published location of Sea Labs Indonesia. OpenStreetMap supplies the curved road and surrounding footprints; the two Sopo Del towers, dark podium, woven facade spine and entrance canopy are modeled from public architectural references. All five vehicle choices work here. The existing training missions and their NPC activity remain available from the same menu. Exploration does not change lesson progress.

The environment adds low- and mid-rise infill buildings, shop canopies, window sills, rooftop equipment, textured ground, wild grass, shrubs, trees, benches, bins and service fixtures to fill unmapped gaps. The street corridor and office entrance stay clear. These added buildings and small details are visual approximations; they are not a survey of current businesses. The earlier fictional Sea-branded Jaksa office remains a legacy profile, while Sopo Del is the default scene.

Use **C** to switch camera, **R** to restart and **Esc** to pause. This is a single-road slice; expansion to the full 1 km radius is still future work. [Implementation and source notes](tools/jaksa/README.md).

## Focused tests and everyday missions

**Uji keterampilan** offers five independently selectable tests: preparation, signs and lights, priority, lanes, and speed/focus. Each test ends at its own checkpoint. A violation ends that attempt, explains the action and offers a retry or demonstration. A passing attempt completes the test; the result also shows all mistakes and attempts from the learning session.

**Misi sehari-hari** combines related decisions around a destination:

| Mission | Connected decisions | Retry location |
| --- | --- | --- |
| Keluar dari Kampung | Prepare the driver and passenger, stop at the STOP line, yield to cross traffic, then reach the warung destination | Garage, then the departure checkpoint |
| Lewat Sekolah | Follow the posted 30 km/h limit, yield at the school crossing, handle an incoming call safely | School approach, crossing approach, then after the crossing |
| Jemput di Pasar | Follow the one-way road, pass the no-stopping area, signal into the pickup bay, collect the passenger with F, check and signal before rejoining traffic | Market approach, bay approach, then in the bay with the passenger aboard |

All eight activities work with both vehicle types. Each activity and vehicle has a separate local save containing encounter progress, attempts, mistakes, completed sessions and the best session result. Replays change traffic direction or speed between two variants. There is no speed-completion bonus. The original journey save is preserved, including migration of its older rule counters.

The school crossing and market pickup bay appear only in their scenarios. Test prompts state the objective; demonstrations and detailed correction appear after a mistake. The village route currently uses the existing straight STOP intersection, so it assesses stopping and priority rather than a turning manoeuvre.

Everyday scenarios have local street activity: neighbours and a warung in the village, pupils and guardians at SD Kenanga, and market vendors, shoppers and a waiting passenger. Ambient people walk or gesture on sidewalks and forecourts; they do not create extra road hazards. The scored crossing uses the simulation's pedestrian position with articulated walking. Market pickup has a 2.2-second boarding interval: the passenger approaches the stopped vehicle, then appears aboard. Throttle is held during boarding, and a pickup checkpoint resumes with the passenger already aboard. Ambient animation pauses with the simulation.

Use **Jalan Kita → Verify and capture scenario life** for mission regressions and representative views in `Builds/ScenarioPreview`. The NPC layer and local frontages are in `ScenarioAtmosphere.cs`; road encounters and boarding remain in `Journey.cs`.

The new no-stopping lesson cites UU 22/2009 Pasal 118(a). The 2-second stopped-state tolerance, pickup zone, 11-metre merging clearance, checkpoint positions and mission completion conditions are simulation choices, not statutory limits. Pickup errors are labelled **Latihan**, and legal lessons remain draft pending team review.

## Continuous training route

| Checkpoint | Section | What the player practices |
| --- | --- | --- |
| 01 | Garasi Kenanga | Both front seat belts for the car, or both SNI helmets and the headlight for the motorcycle |
| 02 | Jalan Kenanga | Full stop before the line, yielding to cross traffic, and obeying the traffic light |
| 03 | Pasar Bunga | Waiting for a pedestrian and giving priority to traffic on the main road |
| 04 | Jalan Mawar | Following the one-way route, checking the gap, signalling to change lanes, and merging left |
| 05 | Zona Sekolah | Staying within 30 km/h and ignoring a phone call or pulling into the stopping bay |

The route keeps moving at each pass. On a retry or relaunch, the vehicle starts at the last cleared checkpoint and resets the upcoming traffic and lesson checks. Traffic or pedestrian direction can change after a failed attempt. Progress, mistake counts, and quiz scores are stored locally and separately for each vehicle. The feedback screen offers a short in-scene demonstration that leaves the player's save intact.

## Build and verify

Use **Jalan Kita → Verify Missions** to check all eight activities for both vehicles and traffic variants, scenario failures/retries, save isolation, legacy-save migration and the original continuous route. **Verify Checkpoints** checks only the original route. Use **Build macOS** or **Build WebGL** to build. Install Web Build Support for WebGL. Serve a browser build over HTTP with `python3 -m http.server 8080 --directory Unity/Builds/WebGL` and open `http://localhost:8080`. Play uses keyboard and mouse.

[Journey.cs](Unity/Assets/Scripts/Journey.cs) defines the route, thresholds, and lesson sources. [District.cs](Unity/Assets/Scripts/District.cs) lays out the road using the same coordinates. The FBX assets in [Resources/Models](Unity/Assets/Resources/Models) are copied directly from Ujian SIM. Its original material library and model loader are included in [Scripts/UjianSim](Unity/Assets/Scripts/UjianSim) to preserve the material palettes, atlas textures, glass, and light emission. The continuous route and checkpoint logic are implemented in this project.

Lesson wording is draft for the hackathon. The clearance distances, timing thresholds, and exact route maneuvers are choices for the simulation. [UU 22/2009](https://peraturan.bpk.go.id/Details/38654/uu-no-22-tahun-2009), [PM 13/2014 on signs](https://peraturan.bpk.go.id/Details/103683/permenhub-no-13-tahun-2014), [PM 49/2014 on traffic signals](https://peraturan.bpk.go.id/Details/103764/permenhub-no-49-tahun-2014), [PM 111/2015 on speed limits](https://peraturan.bpk.go.id/Details/103508/permenhub-no-111-tahun-2015).
