# Unity traffic integration

Six lightweight models are available in both training encounters and Sopo Del exploration: sport, retro and maxi scooters, a city hatchback, family MPV and utility pickup. Each has two shared paint materials. Scooter derivatives include a helmeted rider in both LODs.

The original Blender/GLB vehicle packs remain unchanged. `prepare.py` derives the Unity FBXs, separates the four physical indicator lenses, adds riders, and packs three shared 128px textures into `Unity/Assets/Resources/Traffic`.

## Regeneration and verification

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python tools/traffic/prepare.py
```

Then use Unity's **Jalan Kita → Prepare traffic vehicles**, or run the following against a separate project copy when the main editor/build is in use:

```sh
/Applications/Unity/Hub/Editor/6000.3.27f1-arm64/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath /path/to/copied/Unity \
  -executeMethod JalanKita.EditorTools.TrafficChecks.Prepare \
  -quit -logFile /tmp/traffic-verify.log
```

`TrafficChecks.Run` runs verification without reimporting assets. `TrafficChecks.Capture` writes actual Unity renders into `Builds/TrafficEvidence`. `TrafficChecks.BuildMac` writes a separate `Builds/TrafficPreview/Jalan Kita Traffic.app` with its own bundle identifier.

## Runtime behavior

- Training saves one cosmetic `trafficSeed`; a new save gets random models/colors. The existing retry variant changes the roster, while save/reload and demonstration preserve the same roster. Old saves acquire a seed without changing progress or scores.
- Existing scripted traffic timing, lesson checkpoints, boarding, feedback and scoring remain in place. Collision checks use the displayed model's oriented footprint. Unsafe bay rejoining still reports the gap rule when a turned nose reaches traffic before the vehicle center crosses the bay edge.
- Exploration maintains eight moving and three parked vehicles. Moving vehicles follow the loaded road, keep left, brake for traffic/player obstructions, and resume when clear. Player movement checks sweep position and rotation; blocked movement rejects the full proposed pose.
- Turn signals illuminate the actual front/rear amber lenses on the physical left/right side. The cycle is 0.72 seconds with a 50% duty cycle. Straight-moving NPCs leave their indicators off; a parked loading pickup uses both sides as hazard lights.
- Headlights, tail lamps and brake lamps use masked emissive shared materials. There are no per-NPC realtime lights, physics bodies, or material clones. Wheel rotation follows distance, and steering follows heading change.
- Each vehicle has two LODs. LOD1 disables shadows; far moving vehicles are culled. The pool is reused during driving. Model meshes are imported without a CPU-readable copy or animation clips.

## Verification coverage

The checks cover six models × two paints, actual lens positions and blink phases, grounding/forward axes, wheel rotation, lamp switching, LOD reduction, player lamp handedness, every selectable player model, the existing mission/retry/boarding/save suites, 64 seeded mission completions, 16 natural gameplay retry/reload/demo roster cases, six exploration traversals, stopping/recovery, swept collision and 24 stationary-steering adjacency cases. The existing exploration checks still verify every player vehicle, curb containment, braking, end stop, camera switching and reset.

The exploration test driver uses `RoadVehicleMotion.TrackLane` with a target at `RoadX(z) - 1.2` and the road tangent measured over the next two metres. This stays in the left lane and within the existing conservative curb envelope, using the shared bicycle controller's steering semantics. `JaksaChecks` shares this helper. Stationary-adjacency checks require no body pivot at rest, test the collision sweep for rotation directly, and then test creeping turns beside traffic.

This is a lightweight traffic system: it has no general intersection planner, overtaking or lane-changing AI. NPC headlights glow but do not cast individual light beams. Visuals use simplified geometry and rigid riders. Automated checks and editor captures do not replace live playtesting of the release build.
