# BeAT scooter — red/white reference revision

Original Blender game model rebuilt to follow the red-and-white BeAT in the Wahana Honda photograph supplied by the user. This revision adds rounded bodywork, tapered white side panels, red fender and handlebar cowl, newly drawn graphics, clear headlight/indicator lenses with underlying reflectors, tire tread, cast wheels, a perforated brake rotor, engine details, and leather/rubber surface grain. Proportions are approximate, with hidden surfaces inferred from the visible view. No marketplace meshes or reference-photo textures are included. The first blue prototype remains in the neighboring `beat_scooter` directory.

## Files

Validated geometry: **48,504 triangles** in the main model and **18,401 triangles** in LOD1, organized into 10 meshes with two materials. Main GLB dimensions are approximately **0.684 m wide × 1.872 m long × 1.184 m high**, including mirrors. Import checks cover valid geometry, UVs, scale, wheel/steering pivot positions, and retention of GLB atlas maps and lens transmission.

- `beat_scooter.blend`: editable source, named component meshes, PBR materials, camera and studio lights. Textures are packed into the file.
- `beat_scooter.glb`: full-detail asset, embedded textures, consolidated meshes, animation pivots and rider sockets.
- `beat_scooter_lod1.glb`: reduced-detail variant with the same hierarchy. Select LOD distance in your game engine.
- `beat_scooter.fbx`: full-detail export with embedded textures for workflows such as Unity.
- `textures/`: five 512 × 512 atlas maps: base color, roughness, metallic, emission, and tangent-space surface normal. All geometry has UVs referencing these maps.
- `previews/`: actual Blender renders from four viewpoints and renders of the re-imported GLB exports.
- `asset_stats.json`: generated triangle counts and scale information.
- `build.py`: reproducible Blender authoring script.
- `render_exports.py`: visual checks of the exported GLBs in the same studio lighting.
- `validate.py`: round-trip validation of both GLB variants and FBX, including bounds, normals/UV validity, and animation pivot positions.

## Import and animation

Dimensions use meters. Reference wheelbase: **1.255 m**; front rolling radius **0.2498 m**, rear **0.2588 m**. The rider reference socket is at **0.742 m**. Mirrors extend above the handlebar body.

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

Wheel rotation in radians is traveled distance divided by the wheel's rolling radius; choose the sign to match your controller. Pivots provide rigid-part animation. The asset needs the game’s driving controller and physics; a rider and skeletal animation are not included.

For Unity, import the FBX and textures. If the importer does not reconstruct the material, create one URP/Lit material, assign `scooter_base_color.png` to Base Map, and adjust smoothness/metallic settings or repack maps for the engine shader. `roughness` and `metallic` maps are linear data, not sRGB. GLB preserves the PBR materials for compatible importers. The second material, `Optical_Lens_Clear`, uses transmission for the headlight covers; it requires an engine/importer supporting glTF transmission or an equivalent glass shader. In Unity, use a suitable glass shader for the lens material, or a lightly tinted transparent material as a simpler fallback. Use a simple chassis collider plus engine wheel colliders; do not use the full vehicle mesh as a dynamic collider. The Blender file contains an optional hidden chassis collision guide, excluded from the model exports.

The full-detail model is intended for the player or nearby traffic; use LOD1 for distant traffic. Performance still depends on visible vehicle count, shadows, and the target platform.

## Rebuild

Run from the repository root on this laptop:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python assets/vehicles/beat_scooter_realistic/build.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python assets/vehicles/beat_scooter_realistic/validate.py
```

The script regenerates the asset files and renders. It starts with an empty scene; run it in a separate Blender process as above, not inside an unsaved working scene.

## References and artwork

Primary visual reference supplied by the user, viewed 7 November 2026:

- [Wahana Honda — red-and-white BeAT photograph](https://www.wahanahonda.com/assets/upload/berita/BERITA_1528186289_c3e5baa808c94409bb765ac1eeb41268.jpg).

The 1.255 m wheelbase and tire rolling radii from the first prototype are retained as an approximate scale baseline. A single perspective photograph does not establish exact geometry or dimensions. The red/white color blocking, front headlight placement, stepped seat, scooter proportions, and graphic treatment follow the supplied photo. Typography and stripes are newly constructed approximations, not extracted decals.

The rear `B 110 SIM` plate is fictional placeholder artwork. Honda/BeAT lettering identifies the reference vehicle; the mesh is independently authored for this project.
