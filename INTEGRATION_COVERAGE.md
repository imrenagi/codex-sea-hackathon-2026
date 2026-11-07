# Integration feature and acceptance matrix

This development milestone combines local snapshot cb5f851, main ef1fb13→9a757d8, city/pitch branch93d1722→9ede07a→d35769e (rebases preserve the same unique city/pitch changes), six bounded street/audio commits, and the final StreetLook/traffic/motor-bebek handoffs. Original shared source, v1 saves and authoring packs remain preserved.

| Feature | Source | Candidate behavior | Verification |
|---|---|---|---|
| Seven player choices / lamps / wheels / rider equipment / LOD | Local + final traffic handoff | All playable modes, independent car/motor preferences | VehicleChecks, traffic lamps/state, exploration checks PASS |
| motor bebek wording | Explicit user request + SHA-guarded four-FBX patch | Lowercase visible labels and model lettering; internal IDs unchanged | All7model checks PASS; sourceowner lettering and lamp renders PASS |
| Five tests, three scenarios, boarding, replay/checkpoints/NPCs | Local | Existing bounded catalogues; independent mission progress | Allactivities/bothmodes/variants and64seededtraffic runs PASS |
| Sopo Del/Sea exploration, gang frontage, textures, camera | Local final handoffs | Separate playable exploration; original controls and traffic retained | All7exploration checks and RoadVehicleMotion PASS |
| Ten-stage full journey, rail and ambulance | Main | Continuous journey + checkpoint results; upstream steering contract | Fullroute/violation/retry checks PASS |
| Dynamic boulevard traffic | Main9a757d8 | Seeded mixed traffic across junctions/rail/ambulance | Renamed BoulevardTrafficChecks retained alongside localTrafficChecks;24seededroute cases PASS |
| Mirrors + left/right look | Main9a757d8 + shared camera adaptation | Three mirrors and Z/X/holdbuttons in both playable scenes; no steering mutation | Playmode binding/look/direction/pause checks PASS; finalnative rendering pending |
| HUD icons, feedback/foldouts/citations, checkpoint practice | Main + local | Scenario counters cannot launch unrelated practice | Playmode fullroute/practice and scenario guard PASS |
| Isolated pitch demo | City branch | Actual violations, visible citation, coach, retry, completion and fresh replay | Bothmodes and immutable journey/mission saves PASS |
| Six OSM overview maps | City branch | Clearly labeled non-driving previews; pan/zoom/reset | Sixdatasets, geometry and playmode transitions PASS |
| Crossing landing/tactile/sightline/flower market | 2eec310,8688df1,210e1d6 | Retains local sidewalk width/materials and separate passenger market | Constructed geometry/material/two-market assertions PASS; finalvisual QA pending |
| Pitch audio / master volume / ambience / warning priority | 81c44da→db3b571→3c4a216 + main9a | Persisted settings; grouped audio UI; critical warnings retained | Finite mix, mute, persisted/clamped volume, ambience/criticalaudio assertions PASS |
| Save compatibility | Both formats | Pure-read v1 migration, stable combined counters, v2 writes only gameplay; oldquiz retained |30schema/mode/mission fixtures + flow isolation PASS |
| Responsive menu/catalogues and pause focus | Local + integration | Bounded scrolling menu/catalogues, Tab focus, Esc resume | Core native menu/selection positive; full finalnative acceptance pending |
| Lighting and listener lifecycle | Both + city fix | Exactly one active listener across training/exploration/previews | Playmode sixmaps/exploration/menu transitions PASS |

Exact source hashes and original inputs: INTEGRATION_SNAPSHOT.json, STREETLOOK_HANDOFF.json, TRAFFIC_HANDOFF.json, assets/vehicles/motor_bebek_lettering_patch/patch_manifest.json. The original intermediate commits remain in this repository.

Newer unmerged branches are preserved and listed in UPSTREAM_QUEUE.json with local codex/queued-* refs; they are deliberately outside this tested milestone. Higher-fidelity environment is a separate requested milestone. No queued feature is claimed complete.

## Bounded NPC-safety and vehicle-showroom merge (7 November 2026)

Inputs are accepted checkpoint `27781b90cf09c98843f818c3a7b31ed9149dbe03` and upstream main `de6076ea6a8480ef19bb3fffdef4328011e58e10`. The original dirty workspace and the separately published checkpoint app remain preserved. HI/Monas and the later visual-fidelity work are outside this candidate.

| Retained or added behavior | Merge resolution / acceptance gate |
|---|---|
| Forward road-edge sliding, tangent-aligned Sopo Del spawn, bounded reverse, pure Space braking | Retain checkpoint controllers and signed animation; ReverseMotionChecks and ReverseVisualChecks cover seven choices, all activities and road checkpoints, stationary/boarding rules, actor and boundary contacts. |
| NPC signal/category stop lines, pedestrian/rail priority, following and ambulance yielding | Retain upstream NPC rules and existing seeded route suite. NpcRuleChecks additionally verifies safe equal-speed following and real closing contact. |
| Collision detection during both forward and reverse travel | Preserve reverse sweep API; add upstream two-body relative sweep. NPC blocking compares both trajectories in the same interval instead of freezing the player's end pose or NPC's start pose. |
| Exploration parked-vehicle contact and specific collision feedback | Retain upstream parked footprints/feedback while keeping signed controls, full-pose rollback, road boundaries and reverse room behind spawn. |
| Seven-model 360-degree showroom in menu, briefing and preparation | Retain upstream render texture, automatic/drag/slider/button rotation, selection persistence and camera/material disposal. Horizontal navigation stays on the focused image for repeated keyboard rotation; Tab remains available. |
| Existing catalogues, maps, pitch demo, mission picker, save isolation and keyboard brake guard | Retain checkpoint behavior; combined simulation and ResultFlowChecks remain release gates. |

Native acceptance and the exact built-source SHA are recorded alongside the new immutable app manifest; this section does not claim that a source check alone certifies the native preview or final app.
