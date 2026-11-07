# Jakarta exploration integration

Branch: codex/integrate-jakarta-landmarks.
Worktree: /Users/rizkyputra/codex-sea-hackathon-2026-jakarta.
Includes current core 95dab10c2010d64a27e9cae00108832fdaefd1f3 (7a1cedc plus
map-preview copy), reviewed PR features, shared input fix and central seed fixture.
Original dirty project, main integration checkout and published Demo are untouched.
Main merge/push and live-app control remain with their assigned owners.

## Behavior

The main menu opens Sopo Del, Bundaran HI and Monas from one destination picker.
All seven vehicle choices, NPC models, traffic, training, ten-stage journey, pitch,
map previews, audio controls and journey saves remain. HI and Monas use the same
RoadVehicleMotion and DrivingMirrors. C cycles chase/driver/landmark cameras.
Pause offers resume, reset, change location and main menu. Two new scenes are cached;
only the selected area renders. These are separate routes, not seamless city roads.
Cross streets outside the loops are scenery. The map overview menu remains explicitly
marked unplayable; its new copy directs drivers to all three playable destinations.

HI: 367.398 m mapped clockwise roundabout, 14 m/four-lane gameplay approximation,
724 building models, 16 traffic actors, 20 pedestrians. Monas: 3852.683 m directed
OSM Medan Merdeka loop, 8 m/two rendered lanes, 551 buildings, 28 traffic actors,
44 pedestrians. Monas building parts at its center are replaced by the 132 m authored
monument. Both areas have planted soil, shrubs, wild grass, benches and drain grilles.
Architecture/sculpture/small details are approximations. OSM/CC0 attribution retained.

## Water and performance

Original URP water/fountain shaders, moving ripples, Fresnel shading, actual planar
building/statue reflections, parabolic streams, soft moving spray and original
filtered-noise fountain audio. No new downloaded bitmap/audio assets. Metal
render-texture Y orientation is corrected and visually reviewed.

One 640x360 ARGBHalf reflection target/camera per cached area, shared by the two
coplanar Monas pools. Captures only for changed viewpoints, at most 11.1 Hz, skipped
when all pool bounds are outside the main-camera frustum. Excludes water layer4;
no recursive rendering, listener, shadow rendering or postprocess on reflection.
768/432 droplets. Water/particles use simulation time and freeze with pause;
mirrors and fountain audio also pause. Mute/volume/ambience controls apply to water.

Apple M4 batch benchmark: 1600x900 main camera + forced640x360 reflection + GPU
readback, ten warm samples. Latest median HI7.95 ms /Monas16.35 ms, maxima9.93/16.63 ms.
An earlier log called max p90; the benchmark label is corrected. These are synchronous
batch timings, not live game FPS. Earlier medians varied16.65/14.59 ms under concurrent
work. Scene vertices approx1.01M/1.89M, meshes1103/1294; normal rendering culls geometry.
Native gameplay performance and final UI acceptance remain pending.

## Lighting/camera hooks for visual-v2 integration

No pipeline, renderer or ProjectSettings changes. LandmarkEnvironment.Activate sets
its own sky, fog, trilight ambient and directional sun; that light is inside its
scene root, intensity1.5/rotation54,-28. A scene-local global Volume (priority3) uses
ACES and saturation-5. StartLandmark disables training/Sopo roots and prior landmark;
CloseCityPreview/ShowMenu disables landmark and calls district.RestoreAtmosphere.
One main camera/listener active across current-core transitions is verified.
The future one-sun/lighting owner must preserve this activation/restoration contract
or redirect Activate to the shared lighting rig. Do not leave a global core sun active
alongside the landmark sun. Three mirrors plus disabled manual reflection camera
have no listeners. Current core keyboard guard includes Page.Landmark in both the
active-key predicate and entry-focus reset. SignalPanel hides on every non-Drive page.

## Verification and current artifact state

PASS LandmarkChecks.Run: all7vehicles in both areas, first vehicle full loops and
280 m for other choices, braking, curb containment, reset, cameras, moving traffic,
no player overlap, frozen water/traffic while paused.
PASS JakartaIntegrationChecks.Run: all3destinations, car/motor categories and chosen
model retention, mirrors bound to scene, audio/light/output-camera lifecycle, pause,
cached-area reuse, immutable saved journey and resume. Repeated after keyboard fix:
focused HUD cannot consume W/S or Space in either new area/category.
Final lifecycle regression PASS: focus loss in HI and application pause in Monas
open LandmarkPause, freeze vehicle/traffic/water/mirrors/fountain audio, and returning
focus requires explicit resume. OnDestroy releases/unbinds all six cached mirror
RenderTextures, verified by JakartaIntegrationChecks standalone teardown gate.
PASS full ResultFlowChecks on merged core d601a12: ten-stage route, checkpoints,
retry, practice, isolated pitch, six maps, signal transitions, keyboard regression,
and Jakarta assertions. Central deterministic seed1 fixture retained, assertions intact.
PASS full IntegrationChecks.RunSimulation on the same merged runtime: 30migration
fixtures,7models,8activities,24boulevard seeds,64mission completions, exploration,
lamps/collision/rosters, steering, sound and six maps.
PASS standalone build at c3b000b with all95dab10 core features, final water shaders
and input guards. The two subsequent focus-loss/teardown changes compile and pass
the standalone lifecycle gate; final native build follows the shared menu-focus fix.
Artifact manifest identifies exact app source separately from current source handoff.
No native app was launched by this task. No push/main write occurred.

## Evidence / merge guidance

Unity/Builds/JakartaEvidence contains logs, manifests, branch bundle and initial
feature/input patches. Unity/Builds/LandmarkPreview contains eight renders, two raw
reflection captures and timing samples. Newest source includes95dab10; only final
map destination wording/docs changed after the successful runtime suites.
Narrow delta versus95dab10: JalanGame menu/update/audio/scene/input hooks (~40lines),
one ResultFlowChecks call, README/this note; new landmark runtime/checks/assets/importer.
No existing simulation, models, Sopo environment, pipeline or fixture is replaced.

Unity6000.3.27f1 reproduction:
- -batchmode -executeMethod JalanKita.EditorTools.LandmarkChecks.Run -quit
- -batchmode -executeMethod JalanKita.EditorTools.JakartaIntegrationChecks.Run
- -batchmode -executeMethod JalanKita.EditorTools.ResultFlowChecks.Run
- -batchmode -executeMethod JalanKita.EditorTools.IntegrationChecks.RunSimulation -quit
- -batchmode -executeMethod JalanKita.EditorTools.LandmarkChecks.RenderReview -quit
- -batchmode -executeMethod JalanKita.EditorTools.ProjectSetup.BuildMac -quit
