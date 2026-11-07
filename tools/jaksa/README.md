# Sopo Del street environment

The main menu's **Jelajahi kantor Sea · Sopo Del** opens the real-location environment for all five player vehicles. It follows roughly 260 m of curved Jl. Mega Kuningan Barat III across 250 local z metres. The mapped frontage extends beyond the playable ends for the view. Training scenarios, the vehicle picker and NPC activity share the main game; exploration does not alter lesson progress.

## Sources and accuracy

Sea Labs Indonesia publishes its address as Sopo Del Building, The Sky, L30, Jl. Mega Kuningan Barat III, Mega Kuningan, Jakarta Selatan. The scene uses an OpenStreetMap extract retrieved on 7 November 2026, containing 270 building footprints and 125 road ways. Origin: -6.229000, 106.823910; bearing: **157 degrees**. The coordinate basis keeps the main road within the controller's steering range.

The main road centerline, mapped 7 m width, two-way designation and building footprints are sourced. Tower A uses its mapped 167 m height. Tower B uses an estimated 116 m height and the developer's 28-floor layout because OSM's floor count conflicts with the developer. The developer, architect and facade supplier's public images inform the glass grid, ulos-inspired spine, crown fins, dark podium and braced lobby canopy. No Google imagery or geometry is bundled.

Authored low- and mid-rise buildings fill unmapped gaps without moving mapped footprints. Plain shop signs, canopies, AC units, ground textures, wild grass, shrubs, trees, benches, bins and utility fixtures make the frontage denser. Their placement reserves mapped buildings, roads and the Sopo Del forecourt. These additions and smaller architectural dimensions are approximations, not a survey of current properties or businesses.

`Unity/Assets/Resources/SopoDel/NOTICE.txt` records reference URLs, map-data licensing, texture authors and uncertainties. `layout.json` is an adapted ODbL 1.0 extract. Surface textures are CC0 Poly Haven assets, including the added grass_ground and withered_grass pairs; foliage is original. Sea is identified as the destination in the UI, with no invented giant Sea mark on the real facade.

## Implementation and verification

- `JaksaStreet.cs`: selects Sopo Del by default, loads geographic data, creates materials and lighting. The class name is retained for compatibility with the shared driving controller.
- `SopoDelEnvironment.cs`: roads, mapped buildings, Sopo Del towers, podium, entrance and street furniture.
- `SopoDelDressing.cs`: authored infill, vegetation and small service details, with placement reservations and diagnostic counts.
- `StreetMesh.cs`: geometry grouped by material in 25 m cells for culling.
- `JaksaDrive.cs`: independent exploration driving state, selected player vehicle and cameras; maintained by the vehicle/traffic workstream.
- `Editor/JaksaChecks.cs`: texture import settings and shader-variant materials, eight renders (including both spawn cameras), all-five-vehicle traversal, curb containment, braking, camera, reset and original-route checks. It also rejects infill buildings in the driving corridor.

Run **RoadSense → Prepare and verify Sea office** after regenerating textures or geometry. Previews go to `Unity/Builds/SeaOfficePreview`. Use **Build macOS** for the main app. The environment is assembled when exploration is selected, rather than stored as a separate editable Unity scene.

`import_sopodel.py` explicitly refreshes the Sopo Del OSM extract. Review its output and update provenance before using another map revision. The game itself makes no network requests. The script retains original OSM IDs.

## Legacy Jaksa profile

`new JaksaStreet(parent, false)` retains the earlier Jl. Jaksa environment, including its fictional Sea-branded office. Its original point is -6.1865309, 106.8289926, at 175 m along the old southbound section. `import_layout.py`, `make_details.py` and `Resources/Jaksa/NOTICE.txt` describe that profile and the shared textures. Its invented Sea-office placement does not represent the current Sopo Del default.

The next geographic expansion needs a road-network controller and extended driving boundaries before covering the full 1 km radius.
