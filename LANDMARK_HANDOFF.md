# Jakarta exploration integration

Base: fc7ecca28531fa81347ec89a9e7178f24f40f55c. Branch: codex/integrate-jakarta-landmarks.
Worktree: /Users/rizkyputra/codex-sea-hackathon-2026-jakarta.
Original dirty project and /tmp/jalan-integrate-demo are untouched.
Main merge/push and final native app handoff remain with their assigned owners.

## Behavior

The existing main menu now opens a three-destination picker: Sopo Del, Bundaran HI,
Monas. Sopo Del remains the existing environment; HI and Monas use the same seven
player models, NPC models, traffic views, RoadVehicleMotion and DrivingMirrors.
Training, pitch, route, map previews, audio controls and journey saves remain.
Pause offers resume, reset, change location and main menu. The two new scenes are
cached and only the active scene renders. These are separate drivable locations,
not seamless city connections. Cross streets beyond the route remain scenery.

HI follows a 367.398 m mapped clockwise roundabout with a four-lane approximation;
Monas follows a 3852.683 m directed OSM Medan Merdeka loop with two rendered lanes.
724/551 building models respectively; the authored Monas replaces duplicate
building parts. Both have pedestrians, roadside dressing, planted soil, shrubs,
wild grass, benches and drain grilles. Architecture and small details are approximate.

## Water and rendering

Original URP water/fountain shaders, animated small waves, Fresnel shading,
planar building/statue reflections, parabolic streams, soft moving spray, and
procedural spatial fountain audio. No external audio or new bitmap assets.
Metal render-texture Y orientation is explicitly corrected and visually reviewed.
One 640x360 ARGBHalf reflection target/camera per cached area, shared between the
coplanar Monas pools. Captures occur only for changed viewpoints at most 11.1 Hz,
and are skipped when pool bounds are outside the camera frustum. Reflection excludes
water layer4, has no listener or shadows/postprocess and never renders recursively.
768 HI /432 Monas droplets; simulation time drives water and particles. Pause stops
water time, reflections, mirrors and water audio. Global mute/volume/ambience apply.
One active main output camera and listener across locations/menu/map transitions.

## Verification

- LandmarkChecks.Run: all seven vehicles in both new areas; driving full loops for
  first choice and 280 m for each other model; braking, curb containment, reset,
  three camera choices, moving traffic and no player overlap; paused water/traffic PASS.
- JakartaIntegrationChecks.Run: main-menu entry, all three locations, both categories,
  chosen model retention, mirrors bound to active scene, listener/light/camera lifecycle,
  pause, area cache reuse, saved journey unchanged and resume after exploration PASS.
- ResultFlowChecks.Run: full ten-stage route, both categories, checkpoints/retries,
  scenario selection, pitch isolation, six map transitions, saves and new Jakarta
  integration assertions PASS with the diagnostic fixed-seed fixture described below.
- RenderReview: all eight views plus raw water-reflection captures reviewed; no shader
  compile errors. Main camera+forced reflection+GPU readback benchmark on Apple M4,
  1600x900, latest medians HI7.95 ms /Monas16.35 ms, p90 9.93/16.63 ms. This is a
  batch render benchmark, not live gameplay FPS. Scene mesh vertices roughly
  1.01M/1.89M; normal rendering culls distance and offscreen geometry.
- Native final-build interaction/performance remains for the exclusive native QA
  owner after merging this milestone. No live app was launched by this work.

## Shared fixture finding (not included in this commit)

Unchanged fc7 BoulevardChecks.Start uses a new JourneySave with an unspecified
trafficSeed; Journey now randomizes that seed. The deliberately unsafe pedestrian
fixture can collide with random traffic before reaching its expected violation.
Observed expectedPedestrian/gotCollision at stage5(-5.40,459.29), clock18.96024.
Diagnostic seed1 restored the old deterministic fixture and the complete flow suite
passed without relaxing assertions or changing runtime. The PR-review owner owns
this shared fix; it was reverted here to avoid competing edits. Its diagnostic patch
and note are under Unity/Builds/JakartaEvidence. Full core revalidation should use
that owner's centrally committed correction.

## Evidence and reproduction

Evidence under Unity/Builds/LandmarkPreview and Unity/Builds/JakartaEvidence.
Primary logs copied there before handoff. Run using Unity6000.3.27f1 with -batchmode:
- -executeMethod JalanKita.EditorTools.LandmarkChecks.Run -quit
- -executeMethod JalanKita.EditorTools.JakartaIntegrationChecks.Run (no -quit)
- -executeMethod JalanKita.EditorTools.LandmarkChecks.RenderReview -quit
- -executeMethod JalanKita.EditorTools.ResultFlowChecks.Run (after central fixture fix)
- -executeMethod JalanKita.EditorTools.ProjectSetup.BuildMac -quit

Source changed only: JalanGame.cs menu/update/audio/scene routing hooks, one additive
ResultFlowChecks call, README, this note; new landmark runtime/checks/assets/importer.
No existing simulation, vehicles, Sopo Del environment or shared fixture modified.
