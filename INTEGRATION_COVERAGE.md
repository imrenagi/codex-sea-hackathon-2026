# Integrated demo coverage

Baseline: local snapshot cb5f851 (2026-11-07 11:39 WIB), upstream ef1fb13, city previews/pitch 9ede07a (rebased from preserved 93d1722). Shared source is not edited. Final StreetLook/Gang and traffic manifests are captured; source is frozen.

| Feature | Source | Integration target | Verification |
|---|---|---|---|
| Seven player choices, persisted per vehicle class, motor bebek labels; original BeAT IDs, LOD/riders/lamps | Local | All playable modes | VehicleChecks + native QA |
| Five skill tests, three scenarios, boarding, repeat variations/checkpoints | Local | Existing catalogue, bounded missions | MissionChecks + ScenarioAtmosphereChecks |
| Sopo Del/Sea exploration, textures/environment, traffic | Local | Separate playable exploration entry | JaksaChecks + TrafficChecks + native QA |
| RoadVehicleMotion, DrivingCamera, StreetLook/Gang | Local | Village/scenarios/exploration, retained visuals | JourneyChecks + StreetLookChecks; frozen manifest imported |
| Longer route: village, straight/left/right junctions, railway, ambulance | Main | One complete ten-stage journey | BoulevardChecks + JourneyChecks |
| VehicleSteering, continuous checkpoint poses | Main | Long-route stages and coach contract | Steering checks + route replay |
| Signal/headlamp icons, sound/horn/mute, results/foldouts | Main | Unified HUD/results, scenario-safe | ResultFlowChecks + native QA |
| Clickable checkpoint practice, preserving journey saves | Main ef1fb13 | Full-route HUD only; scenario steps noninteractive | ResultFlowChecks + integration regressions |
| Correct lane-arrow orientation | Main ef1fb13 | Village and boulevard geometry | ResultFlowChecks |
| Isolated pitch demo: violation, visible citation, coach, retry, completion/replay | City branch9ede07a (rebased from preserved 93d1722) | Separate no-save session starting at Hak utama | Integration regressions + native QA |
| Audio listener and atmosphere restoration across modes | Both + city lighting fix | One active listener; restored light/sky after preview | Integration regressions + native QA |
| Six OSM overview maps | City-preview branch | Clearly labeled non-driving overview menu | CityMapChecks + native QA |
| Saved progress across old local/upstream formats | Both | Versioned named-rule migration, mode/mission isolation | Migration regressions |
| Responsive picker/catalogues and pause keyboard navigation | Local | Retain verified UI layout/theme | Native QA at 1280x720 and small window |
| All upstream/local generated source assets and authoring files | Both | Preserved in candidate; no generation repeated | Hash inventory |

Order: preserve originals → simulation/save contracts → scene and rendering → combined UI/audio/maps → final handoffs → isolated checks → QA build/UI.

The city branch was rebased onto ef1fb13 as 9ede07a; its tree delta is the already integrated latest main selector/arrow fix. User-visible names are now `motor bebek merah-putih` and `motor bebek biru`; derivative model-lettering resource handoff remains pending.

## Follow-on inputs explicitly pending

- Four derivative body FBXs replacing visible lettering with `motor bebek` (internal IDs/source packs retained).
- Crossing-landings2eec310, crossing-sightlines8688df1, PasarBunga frontage210e1d6. Preserve separate passenger-pickup market and scenario curbs.
- Audio chain81c44da→db3b571→3c4a216: result/retry cues, speed wind, persisted mastervolume/preview, persisted ambience preference with critical sounds retained.
- Higher-fidelity environment: isolated next milestone; no shared runtime changes.

Core gates passed: cleancompile, RunSimulation, ResultFlowChecks.Run. Independent native QA pending.
