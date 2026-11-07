# Jalan Kita

An Indonesian road-safety learning prototype built in Unity **6000.3.27f1**. Open `Unity/Assets/Scenes/KampungKenanga.unity` and enter Play mode, or build with `Jalan Kita/Build macOS` / `Build WebGL`.

## Experiences

- **Uji keterampilan:** five short tests, with independent attempts and results.
- **Misi sehari-hari:** village shopping, school crossing, and market passenger pickup. Scenarios have their own checkpoints, variations, people, and saved progress; boarding takes time before the vehicle can move.
- **Perjalanan lengkap:** ten continuous stages covering preparation, signs/lights, priority, lane changes, school-zone speed/phone attention, straight/left/right boulevard junctions, railway gates, and an ambulance encounter. Each completed stage pauses for feedback. The numbered route buttons start isolated practice without replacing the saved journey.
- **Demo singkat · Hak utama:** a separate presentation session at Pasar Bunga. Show a violation and visible citation, inspect the explanation, play the driving-coach example, retry, complete, and replay. Its mistake count is real for that session; it never replaces journey or mission progress.
- **Jelajahi kantor Sea · Sopo Del:** a playable approximately 250-metre exploration of Jl. Mega Kuningan Barat III with traffic, sidewalks, office surroundings, and roadside people.
- **Peta Jakarta:** six explicitly non-driving OSM overviews: Monas, Bundaran HI, Semanggi, Bundaran Senayan, Morrissey, and Sea Labs. Pan/zoom/reset and return to the game; these maps do not contain missions or traffic simulation.

Seven player choices are available: hatchback, family MPV, city car, sedan, **motor bebek merah-putih**, **motor bebek biru**, and classic scooter. Choice is remembered separately for cars and motorcycles across playable experiences. Car seat-belt requirements and motorcycle helmet/headlight requirements remain distinct. The internal imported asset identifiers are retained for save/resource compatibility.

The local village activities and Sopo Del retain their deterministic bicycle controller and damped chase camera. The full journey, route practice, and pitch demo retain the upstream steering contract. Both controllers keep vehicle heading when the steering wheels return to centre. NPC traffic uses persistent cosmetic seeds, physical footprints, wheel/lamp animation, and reduced-detail models. Player models retain animated lamps/wheels, rider equipment, boarding visibility, and practical LODs.

## Controls

| Input | Action |
|---|---|
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

Map previews use WASD/arrows to pan, +/- to zoom, R to reset, and Esc to choose another map.

## Saved progress

Car/motorcycle journeys, each mission/test, and vehicle choices are independent. The combined build uses `JalanKita.Journey.v2.<mode>` and `JalanKita.Mission.v2.<mode>.<id>` for new progress. It reads older v1 saves without modifying the originals. Migration maps divergent rule-counter indices explicitly: original 16-rule saves, intermediate local 17–19-rule saves, and upstream 23-rule saves retain the meaning of every counter. A completed original five-stage route continues at the boulevard; its previous quiz result remains recorded. Browsing menus, previews, demonstration, pitch, and isolated checkpoint practice does not overwrite persistent progress.

## Verification

Run batch Unity against an isolated copy, not an editor project that another person is changing. The integrated source has these entry points:

- `JalanKita.EditorTools.IntegrationChecks.RunSimulation`: save migration, local driving physics, all seven player models, traffic/lamp state, all eight activities and variations, full ten-stage route, steering/audio, exploration, and six map datasets. Use `-batchmode -nographics -quit -executeMethod`.
- `JalanKita.EditorTools.ResultFlowChecks.Run`: Play-mode completion/feedback cards, checkpoint practice, save isolation, visible citations, pitch retry/demo/completion/replay, scenario-step navigation guards, arrows, map transitions, and listener lifecycle. Use `-batchmode -nographics -executeMethod` without `-quit`; the runner exits with its result.
- `StreetLookChecks.Run`, `ScenarioAtmosphereChecks.Run`, `VehicleChecks.Capture`, and `TrafficChecks.Capture` create isolated visual evidence. Rendering checks need graphics enabled.
- `ProjectSetup.BuildMac` / `ProjectSetup.BuildWeb` build the current prepared project. Do not rerun `CreateProjectAssets` merely to build: it recreates project settings and the scene.

Native QA additionally checks layout/keyboard navigation at normal and small windows, each vehicle in playable modes, lights/steering, actual menu transitions, and real saved sessions. Automated audio tests check signal/mute behavior and finite output; they do not replace listening tests.

## Sources and asset preparation

Legal feedback links its cited official sources in the game. Map data is attributed to OpenStreetMap contributors under ODbL. Surface imagery uses the bundled Poly Haven CC0 resources. Sopo Del layout follows mapped roads/building footprints; small geometry, facade detail, vegetation, and furniture are illustrative approximations. The six overview maps are not surveyed models.

Original authoring packs remain under `assets/`; runtime resources and metadata are under `Unity/Assets/Resources`. Preparation utilities are documented under `tools/jaksa`, `tools/traffic`, and `tools/vehicles`. Preserve both high/low-detail geometry and material/texture metadata when transferring assets.

`INTEGRATION_COVERAGE.md` records the source/feature/test matrix; `INTEGRATION_SNAPSHOT.json`, `STREETLOOK_HANDOFF.json`, and `TRAFFIC_HANDOFF.json` preserve source provenance and handoff hashes.

The integrated HUD has three rear-view mirrors, with mirror rendering paused outside driving. Boulevard traffic follows lanes and signals, waits for pedestrians/trains, and interacts with the ambulance. Audio settings group master volume, mute, ambience and preview controls; disabling ambience retains horn, rail, phone and siren warnings. See INTEGRATION_VERIFICATION.md for completed source gates and remaining native QA.


Jakarta exploration now includes Sopo Del, Bundaran HI and Monas in the main game's
**Jelajahi Jakarta** menu. Select a car or motorcycle on the main menu, then choose
one of the three playable areas. All seven vehicle models, traffic, pedestrians,
mirrors, engine audio and existing training activities are retained. C cycles chase,
driver and landmark cameras in HI/Monas; Esc opens pause and **Pindah lokasi**.
The areas are separate routes rather than a continuous drive between districts.

HI and Monas have animated reflective water, fountains and spatial water ambience.
Mute, volume and ambience settings also control fountain sound. Pause freezes the
water, spray, traffic and pedestrians. Landmarks and small architectural details are
approximations; map attribution and provenance are in
`Unity/Assets/Resources/JakartaLandmarks/NOTICE.txt`.
