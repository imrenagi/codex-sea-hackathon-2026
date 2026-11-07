"""Convert finalized GLB LODs to Unity FBX without regenerating vehicle geometry."""
from pathlib import Path
import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
for name, source in [("BeatRed", "beat_scooter_realistic"), ("BeatBlue", "beat_scooter")]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / "assets/vehicles" / source / "beat_scooter_lod1.glb"))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(ROOT / "Unity/Assets/Resources/Vehicles" / name / "Body_LOD1.fbx"),
        use_selection=True, object_types={"EMPTY", "MESH"}, axis_forward="-Z", axis_up="Y",
        apply_unit_scale=True, bake_anim=False, path_mode="AUTO", use_mesh_modifiers=True,
        add_leaf_bones=False,
    )
    print("Prepared existing LOD:", name)
    folder = ROOT / "Unity/Assets/Resources/Vehicles" / name
    metal = bpy.data.images.load(str(folder / "scooter_metallic.png"))
    rough = bpy.data.images.load(str(folder / "scooter_roughness.png"))
    metal.colorspace_settings.name = rough.colorspace_settings.name = "Non-Color"
    width, height = metal.size
    m = np.empty(width * height * 4, dtype=np.float32)
    r = np.empty_like(m)
    metal.pixels.foreach_get(m); rough.pixels.foreach_get(r)
    packed = np.zeros((width * height, 4), dtype=np.float32)
    packed[:, 0] = m.reshape(-1, 4)[:, 0]
    packed[:, 3] = 1 - r.reshape(-1, 4)[:, 0]
    image = bpy.data.images.new("Unity metallic smoothness", width=width, height=height, alpha=True)
    image.colorspace_settings.name = "Non-Color"
    image.pixels.foreach_set(packed.ravel())
    image.filepath_raw = str(folder / "scooter_metallic_smoothness.png")
    image.file_format = "PNG"; image.save()
