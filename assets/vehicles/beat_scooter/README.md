# BeAT-inspired Indonesian scooter

Original low-poly prototype modeled in Blender from visual references to the Indonesian Honda BeAT Deluxe. The silhouette and dimensions are approximate; this is not an official manufacturer model or an engineering replica. No marketplace meshes or reference-photo textures are included.

## Files

- `beat_scooter.blend`: editable source, named component meshes, shared PBR material, camera and studio lights. Textures are packed into the file.
- `beat_scooter.glb`: full-detail asset, embedded textures, consolidated meshes, animation pivots and rider sockets.
- `beat_scooter_lod1.glb`: reduced-detail variant with the same hierarchy. Select LOD distance in your game engine.
- `beat_scooter.fbx`: full-detail export with embedded textures for workflows such as Unity.
- `textures/`: four 128 × 128 atlas maps: base color, roughness, metallic, emission. All geometry has UVs referencing these maps.
- `previews/`: actual Blender renders from four viewpoints.
- `asset_stats.json`: generated triangle counts and scale information.
- `build.py`: reproducible Blender authoring script.
- `validate.py`: round-trip validation of both GLB variants and FBX, including bounds, normals/UV validity, and animation pivot positions.

## Import and animation

Dimensions use meters. Reference wheelbase: **1.255 m**; front rolling radius **0.2498 m**, rear **0.2588 m**. The lowest rider seat point is approximately **0.742 m**. Mirrors extend above the handlebar body.

Blender coordinates: **+Y forward, +Z up, X axle**. GLB coordinates: **−Z forward, +Y up, X axle**. If a controller expects +Z forward, rotate the imported root 180 degrees around the up axis. FBX importers may wrap axis conversion in an extra transform; check the mesh direction after import.

The root is at ground level between the wheel axles. Use these named nodes (exports append `_Export` to empty names):

| Node | Use |
|---|---|
| `Steering_Pivot` | Simplified steering around local Z in Blender / local Y in GLB. Includes handlebar, mirrors, fork, fender and front wheel hierarchy. The front apron stays with the body. |
| `Front_Wheel_Pivot` | Spin around local X at the front axle. |
| `Rear_Wheel_Pivot` | Spin around local X at the rear axle. |
| `Headlight`, `Brake_Light`, `Indicator_*` | Separate meshes for engine-driven lamp behavior. Materials are initially shared; duplicate the material or use per-instance shader values to animate independently. |
| `Socket_Rider`, `Socket_Passenger` | Approximate seating reference points. |
| `Socket_Grip_L`, `Socket_Grip_R` | Approximate hand positions; follow the steering node. |
| `Socket_Camera_Rider` | Suggested starting position for a rider-view camera. |

Wheel rotation in radians is traveled distance divided by the wheel's rolling radius; choose the sign to match your controller. There is no skeletal rig, rider, driving controller, suspension simulation, or baked animation. Pivots provide rigid-part animation.

For Unity, import the FBX and textures. If the importer does not reconstruct the material, create one URP/Lit material, assign `scooter_base_color.png` to Base Map, and adjust smoothness/metallic settings or repack maps for the engine shader. `roughness` and `metallic` maps are linear data, not sRGB. GLB preserves the PBR material for compatible importers. Use a simple chassis collider plus engine wheel colliders; do not use the full vehicle mesh as a dynamic collider. The Blender file contains an optional hidden chassis collision guide, excluded from the model exports.

The full-detail model is intended for the player or nearby traffic; use LOD1 for distant traffic. Performance still depends on visible vehicle count, shadows, and the target platform.

## Rebuild

Run from the repository root on this laptop:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python assets/vehicles/beat_scooter/build.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python assets/vehicles/beat_scooter/validate.py
```

The script regenerates the asset files and renders. It starts with an empty scene; run it in a separate Blender process as above, not inside an unsaved working scene.

## References

Viewed 7 November 2026. References informed proportions and visible design; they are not bundled as reusable asset textures.

- [Honda Cengkareng — Indonesian BeAT variants and dimensions](https://www.hondacengkareng.com/motor/honda-beat/): length 1,876 mm, wheelbase 1,255 mm, seat height 742 mm, standard BeAT 14-inch wheel/tire sizes.
- [Mitra Krida Mandiri — BeAT](https://mkm-honda.co.id/en/products/beat): Deluxe Matte Blue product photograph, including front fairing, seat, side panels, mirrors and exhaust.
- [Wahana Honda — 2024 BeAT brochure](https://www.wahanahonda.com/assets/upload/produk/brosur/PRODUK_BROSUR_20_2024-06-03.pdf).

The `B 110 SIM` plate is fictional placeholder artwork. Honda/BeAT naming identifies the visual reference; the mesh is independently authored for this project.
