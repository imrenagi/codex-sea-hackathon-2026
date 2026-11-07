# Original Indonesian visuals and daylight

Detailed original hatchback, MPV and pickup derivatives load from a new StreetLook
resource directory, with the existing Traffic models retained as fallbacks. New
asphalt/plaster maps, facade details and branching foliage supplement the Indonesian
scene. Original vehicle geometry, textures, map data, signs and gameplay assets remain.
No CARLA models, textures, screenshots or code are included.

FidelityLighting uses one active directional sun (52° elevation, -36° yaw), diffuse
sky/bounce light and an original street reflection capture. Daytime is brighter;
the existing afternoon, night and rain options remain. Local/full-route/Sopo/Jaksa
activation still controls which scene owns the sun. No additional fill lights exist.
The HDR reflection capture validates six finite nonnegative faces after shader
warmup. This fixes black facade patches from an uninitialized cube face. Curved
vehicle glass preserves the original wipers above its surface.

The current-main camera helper now selects MainCamera, so signal/mirror cameras
cannot accidentally become the exported driving view. Controls, mission logic,
legal explanations, maps, UI, saves and motorcycle body/lettering assets are retained.

## Verification

- `JalanKita.EditorTools.FidelityChecks.Run`: combined simulation, ten route stages,
  all seven vehicles/eight activities, 64 seeded missions, saves, traffic/collision,
  audio, six maps, one-sun mode/weather transitions, red/green signal pixels and LODs.
- `JalanKita.EditorTools.FidelityChecks.Flows`: lighting gate followed by the complete
  ResultFlowChecks play-mode UI/input/mirror/signal/retry/save suite.
- Original 782 tracked asset paths were retained; original FBX, PNG, JPG, Blend and
  JSON files are byte-identical to the first integration baseline, 7a1cedc.
- Actual renders: `Unity/Builds/FidelityPreview` and `Unity/Builds/GroundedPreview`.

Near detailed cars are 62–65k imported triangles; far LODs are 18–20k. Opaque glass
and simplified interiors remain. Editor renders/checks do not establish native FPS.
Both HI and Monas now use the same sun/sky/exposure settings, retaining their scene
roots, water and reflection logic. All seven vehicles pass both landmark routes;
JakartaIntegrationChecks passes location switching, pause, focus loss, keyboard,
saved progress, mirror teardown and exactly one active sun. A combined 1600×900
batch render benchmark (including forced water reflection/readback) measured median
8.26 ms for HI and 12.88 ms for Monas; this is not a native FPS measurement.
Native build acceptance follows the coordinated integration.

Generators: `tools/fidelity/vehicles.py` (Blender, original traffic source pack) and
`tools/fidelity/surfaces.py`. Source meshes are under `assets/fidelity_vehicles`.
Unity's [environment lighting model](https://docs.unity.com/en-us/engine/6000.3/manual/lighting-overview/lighting-reference/lighting-window)
provides the sun/sky separation used here.
