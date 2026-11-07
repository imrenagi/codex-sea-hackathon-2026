# Player vehicle integration

The menu offers the two existing cars, the original training scooter, and both completed BeAT variants. `VehicleCatalogue` persists a selection for each vehicle category independently from learning progress. All car choices use car lessons; all scooters use motorcycle lessons. Selecting a different appearance does not reset progress.

`PlayerVehicleView` presents the selected model in both Kampung Kenanga and Jl. Jaksa. Its root faces +Z, while imported FBX basis transforms remain on child objects. The new BeAT atlas materials use URP/Lit with normal maps where provided, packed metallic/red and smoothness/alpha maps, and transparent lens materials. Their LODGroups switch between the finalized full and reduced meshes at screen-relative heights 0.16 and 0.015; both use the existing helmeted rider/passenger meshes. No driving physics is delegated to the mesh. Sedan visuals are fitted to the existing shared car collision envelope.

Wheels rotate with traveled distance; steering pivots follow the simulation heading; lamps respond to headlight/indicator state and deceleration. Passenger appearance follows boarding completion in the market mission. Driver and passenger helmets follow the preflight gear state. Rider bodies are the existing rigid seated meshes, not newly rigged human animation.

## Reproduce

1. Run Blender with `--background --factory-startup --python tools/vehicles/prepare_lods.py` from the repository root to convert existing finalized GLB LODs to FBX and repack the existing texture channels. This does not regenerate vehicle geometry.
2. Run `JalanKita.EditorTools.VehicleChecks.Prepare` in an isolated Unity project copy to import textures, create/persist URP materials, run checks, and capture each selectable model. Bring the `Resources/Vehicles` assets and metadata back together.
3. `VehicleChecks.Run` verifies visible meshes, materials, meter scale, the car envelope, BeAT forward orientation, wheel motion, two actual geometry LODs, lamp responses, and helmet/passenger states. `VehicleChecks.Capture` writes Unity renders to `Builds/VehiclePreview`.

Controls in training use the existing simulation. Jl. Jaksa also supports Q/E indicators and L for the motorcycle headlight. The shared category saves are unchanged.

Source meshes and their reference/provenance notes remain in `assets/vehicles/beat_scooter` and `assets/vehicles/beat_scooter_realistic`; the original rider/car assets remain in `Resources/Models`.
