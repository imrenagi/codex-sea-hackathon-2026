# RoadSense

An Indonesian road-safety learning prototype by **We Will Handle**, built in Unity **6000.3.27f1**. Open `Unity/Assets/Scenes/KampungKenanga.unity` and enter Play mode, or build with `Jalan Kita/Build macOS` / `Build WebGL`.

## Experiences

- **Uji keterampilan:** five short tests, with independent attempts and results.
- **Misi sehari-hari:** village shopping, school crossing, and market passenger pickup. Scenarios have their own checkpoints, variations, people, and saved progress; boarding takes time before the vehicle can move.
- **Perjalanan lengkap:** ten continuous stages covering preparation, signs/lights, priority, lane changes, school-zone speed/phone attention, straight/left/right boulevard junctions, railway gates, and an ambulance encounter. Each completed stage pauses for feedback. The numbered route buttons start isolated practice without replacing the saved journey.
- **Demo singkat · Hak utama:** a separate presentation session at Pasar Bunga. Show a violation and visible citation, inspect the explanation, play the driving-coach example, retry, complete, and replay. Its mistake count is real for that session; it never replaces journey or mission progress.
- **Jelajahi kantor Sea · Sopo Del:** a playable approximately 250-metre exploration of Jl. Mega Kuningan Barat III with traffic, sidewalks, office surroundings, and roadside people.
- **Peta Jakarta:** six explicitly non-driving OSM overviews: Monas, Bundaran HI, Semanggi, Bundaran Senayan, Morrissey, and Sea Labs. Pan/zoom/reset and return to the game; these maps do not contain missions or traffic simulation.

Seven player choices are available: hatchback, family MPV, city car, sedan, **motor bebek merah-putih**, **motor bebek biru**, and classic scooter. Choice is remembered separately for cars and motorcycles across playable experiences. Car seat-belt requirements and motorcycle helmet/headlight requirements remain distinct. The internal imported asset identifiers are retained for save/resource compatibility.

The local village activities and Sopo Del retain their deterministic bicycle controller and damped chase camera. The full journey, route practice, and pitch demo retain the upstream steering contract. Both controllers keep vehicle heading when the steering wheels return to centre. NPC traffic uses persistent cosmetic seeds, physical footprints, wheel/lamp animation, and reduced-detail models. Player models retain animated lamps/wheels, rider equipment, boarding visibility, and practical LODs.

## Pitch demo

For the presenting laptop, fetch and check out `codex/demo-ready`. This alias follows the reviewed RoadSense integration; PR #1 is already merged and new changes use a successor PR.

From the **Play / Options / Exit** home, choose **Play**, choose **Mobil** or **Motor**, then **Demo singkat · Hak utama**. Read the paused first-drive controls guide and press **Enter** on its continue action. This opens Pasar Bunga; the demo leaves saved progress unchanged. **F1** reopens controls, and **Options** contains audio settings. WebGL omits Exit.

1. Use **F2** on the main menu to enter the demo, or choose it from **Play**. Complete the controls guide before driving. Hold **W** toward the occupied zebra crossing to show a violation. Release the key when the feedback opens.
2. Show the explanation, correct action, and visible legal citation. **Lihat contoh** plays the existing driving coach. **Tab** returns to the feedback.
3. Press **R** to retry. Use **Space** to stop before the crossing. Wait for the pedestrian. At the next intersection, stop before the yield line and give way to cross traffic. **Lihat contoh** is the live backup for this full sequence.
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

Before presenting, use Unity **6000.3.27f1** on the other laptop. Check the demo in both vehicle modes at **1280 × 720**. Try violation, retry, **Lihat contoh**, pause/resume, completion, replay, and return to the saved journey. Confirm that the earlier save is unchanged. Build and rehearse on that laptop. See the verification section below for automated checks; rehearse on the presentation hardware as well.

## Controls

| Input | Action |
|---|---|
| F1 | Open the controls guide |
| W / Up | Accelerate |
| S / Down / Space | Brake |
| A / D or Left / Right | Steer |
| Q / E | Left / right indicator; press again to cancel |
| L | Motorcycle headlight |
| C | Change driving camera |
| Z / X or hold the look buttons | Look left / right without steering |
| H | Horn |
| M | Mute/unmute audio |
| F | Ignore a phone call, or pick up the market passenger when safely stopped |
| G | Answer the phone after safely pulling over |
| Tab / Esc while driving | Open progress/pause |
| Tab / Shift+Tab in a dialog | Move keyboard focus; Enter activates |
| Esc while paused | Resume |
| R | Retry the current checkpoint or restart exploration |
| D on feedback | Show the correct driving demonstration |

Map previews use WASD/arrows to pan, +/- or the mouse wheel to zoom, R to fit the full map, and Esc to choose another map. A north marker shows orientation. Decorative window detail is capped at 24,000 quads near the selected location; roads and building geometry remain visible throughout the area.

## Saved progress

Car/motorcycle journeys, each mission/test, and vehicle choices are independent. The combined build uses `JalanKita.Journey.v2.<mode>` and `JalanKita.Mission.v2.<mode>.<id>` for new progress. It reads older v1 saves without modifying the originals. Migration maps divergent rule-counter indices explicitly: original 16-rule saves, intermediate local 17–19-rule saves, and upstream 23-rule saves retain the meaning of every counter. A completed original five-stage route continues at the boulevard; its previous quiz result remains recorded. Browsing menus, previews, demonstration, pitch, and isolated checkpoint practice does not overwrite persistent progress.

## Verification

Run batch Unity against an isolated copy, not an editor project that another person is changing. The integrated source has these entry points:

- `JalanKita.EditorTools.IntegrationChecks.RunSimulation`: save migration, local driving physics, all seven player models, traffic/lamp state, all eight activities and variations, full ten-stage route, steering/audio, exploration, and six map datasets. Use `-batchmode -nographics -quit -executeMethod`.
- `JalanKita.EditorTools.ResultFlowChecks.Run`: Play-mode completion/feedback cards, checkpoint practice, save isolation, visible citations, pitch retry/demo/completion/replay, scenario-step navigation guards, arrows, map transitions, and listener lifecycle. Use `-batchmode -nographics -executeMethod` without `-quit`; the runner exits with its result.
- `StreetLookChecks.Run`, `ScenarioAtmosphereChecks.Run`, `VehicleChecks.Capture`, and `TrafficChecks.Capture` create isolated visual evidence. Rendering checks need graphics enabled.
- `ProjectSetup.BuildMac` produces `Unity/Builds/macOS/RoadSense.app` for native Apple Silicon ARM64 with Mono; `ProjectSetup.BuildWeb` builds WebGL and installs the RoadSense favicons. `BrandChecks.Run` checks the web icon exports and template links without requiring the WebGL module. Both builds set the public product/team identity while preserving the standalone bundle ID `id.jalankita.hackathon`, existing PlayerPrefs keys, and canonical editor storage names. They build the current prepared project. Do not rerun `CreateProjectAssets` merely to build: it recreates project settings and the scene.

Native QA additionally checks layout/keyboard navigation at normal and small windows, each vehicle in playable modes, lights/steering, actual menu transitions, and real saved sessions. Automated audio tests check signal/mute behavior and finite output; they do not replace listening tests.

## Sources and asset preparation

Legal feedback links its cited official sources in the game. Map data is attributed to OpenStreetMap contributors under ODbL. Surface imagery uses the bundled Poly Haven CC0 resources. Sopo Del layout follows mapped roads/building footprints; small geometry, facade detail, vegetation, and furniture are illustrative approximations. The six overview maps are not surveyed models.

Original authoring packs remain under `assets/`; runtime resources and metadata are under `Unity/Assets/Resources`. Preparation utilities are documented under `tools/jaksa`, `tools/traffic`, and `tools/vehicles`. Preserve both high/low-detail geometry and material/texture metadata when transferring assets.

`INTEGRATION_COVERAGE.md` records the source/feature/test matrix; `INTEGRATION_SNAPSHOT.json`, `STREETLOOK_HANDOFF.json`, and `TRAFFIC_HANDOFF.json` preserve source provenance and handoff hashes.

The integrated HUD has three rear-view mirrors, with mirror rendering paused outside driving. Boulevard traffic follows lanes and signals, waits for pedestrians/trains, and interacts with the ambulance. Audio settings group master volume, mute, ambience and preview controls; disabling ambience retains horn, rail, phone and siren warnings. See INTEGRATION_VERIFICATION.md for completed source gates and remaining native QA.

## Web deployment

Run `make deploy/web` to rebuild WebGL and deploy it to Cloud Run.
See [deployment setup and GitHub Actions](docs/deployment/web.md) for Unity
activation, Google identity federation, and automatic deployments from `main`.
