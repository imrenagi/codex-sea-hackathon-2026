# Avanza-inspired MPV

An original, lightweight 3D vehicle authored for Jalan Kita. The design follows the current Avanza's family-MPV proportions, long three-pane glasshouse, short bonnet, large trapezoid grille, slim headlamps, machined split-spoke wheels and horizontal rear lamps. It is a simplified interpretation, not an official Toyota model or a dimensionally exact replica. There are no manufacturer badges, downloaded model parts or reference-photo textures.

The silver body, black window pillars, opaque tinted glazing and restrained trim use the project's existing two-material traffic setup. The authored headlamps, brake lamps, four corner indicator groups and mirror indicators retain the runtime animation contract. Geometry is newly authored using the project's original Blender helpers; the original traffic pack remains unchanged.

## Delivered files

- `avanza_inspired.blend`: editable named component meshes, packed atlas, camera and offline studio lighting.
- `avanza_inspired.glb` and `avanza_inspired_lod1.glb`: reusable exports with embedded materials.
- `unity/Body.fbx` and `unity/Body_LOD1.fbx`: staged visual replacements for `Resources/Traffic/family_mpv`; studio objects are excluded.
- `unity/family_mpv_0.mat`: staged silver default material; preserve its existing Unity `.meta`.
- `unity/TrafficCatalogue.silver.patch`: only the existing MPV's first paint hex changes to `C5CACD`, so material regeneration retains silver.
- `unity/TrafficPrepare.avanza.patch`: preserves the authored MPV when the original traffic-to-Unity conversion is rerun.
- `previews/`: **offline Blender renders, not game screenshots**. `lod1_roundtrip.png` renders the reimported delivered low-LOD GLB.
- `asset_stats.json`: exact mesh counts, bounds and wheel centres.
- `asset_inspection.json`: offline FBX/GLB geometry, UV, material, hierarchy, axis and physical-lamp checks.
- `handoff_manifest.json`: immutable replacement mapping, baseline hashes, preserved meta GUID dependencies and new-file hashes for integration.

The high-detail export has 22,336 triangles; the distance LOD has approximately 9,400. Each has 11 renderers, the same count as the existing integrated MPV, and two shared material slots. The atlas is the existing 128px traffic palette; no extra runtime texture, shader, light, transparency pass or postprocessing is required. Actual frame rate has not been measured.

## Safe integration

Copy only the replacements listed in `handoff_manifest.json`, verify the recorded baseline hashes, retain the target `.meta` files, and apply the silver and regeneration patches. The stable `mpv` selection and `family_mpv` resource ID, 2.75m wheelbase, 0.32m wheel radii and all wheel/steering/lamp nodes are preserved. This bundle changes visual geometry and paint only. It does not modify driving physics, collision dimensions, saves, camera code or vehicle selection logic.

The provided preview colors are silver `C5CACD`; runtime color 1 continues to use the existing deep-blue material. The authoring source also records graphite as an optional preview paint, without adding a runtime choice. The reference proportions are based on 4.395 × 1.73 × 1.70m; mirrors, antenna, crown and trim are simplified, so use exported bounds for rendering. Fine underside/interior details are inferred or omitted. The windows are opaque and the doors do not open. The decimated LOD may sit up to 5mm above the original ground plane.

No Unity editor launch, import, Play session, compilation, build or Unity tests were performed for this bundle. The integration owner controls when to sync it into the user's live project and preview.

## Reproduction

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/vehicles/build_avanza.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/vehicles/inspect_avanza.py
python3 tools/vehicles/freeze_avanza.py
```

The first two commands operate only on this standalone asset folder. The last records the current target baselines and creates the staged material/patch; it does not write into Unity. Run it only when preparing a new handoff, because a manifest is a snapshot of the targets at that time.

## Reference and provenance

Official reference pages consulted 7 November 2026:

- [Toyota-Astra Motor — All New Avanza](https://www.toyota.astra.co.id/product/9): current Indonesian MPV design and wheel treatment.
- [Toyota Motor Philippines — 2025 Avanza leaflet](https://content.toyota.com.ph/uploads/vehicles/41/001_41_1749527273104_000.pdf): front three-quarter visual reference and 4,395 × 1,730 × 1,700mm G-variant dimensions; the brochure image was consulted but is not redistributed here.

All geometry and texture pixels in the deliverable are project-authored. The number plate is fictional. No marketplace assets are included. Toyota's branding and vehicle design are not represented as owned or officially licensed by this project.
