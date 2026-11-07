"""Render round-tripped GLBs using the authoring file's studio for visual QA."""
import bpy
from mathutils import Vector
from pathlib import Path
OUT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(OUT/'beat_scooter.blend'))
for name in ['SCOOTER • authoring','COLLISION • optional guides']:
    col=bpy.data.collections.get(name)
    if col:
        for o in list(col.objects): bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.collections.remove(col)
scene=bpy.context.scene
camera=scene.camera
for filename,preview in [('beat_scooter.glb','game_export'),('beat_scooter_lod1.glb','lod1')]:
    before=set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(OUT/filename))
    imported=set(bpy.data.objects)-before
    camera.location=(3.7,2.7,1.9)
    camera.rotation_euler=(Vector((0,-.04,.59))-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=2.25
    scene.render.filepath=str(OUT/'previews'/(preview+'.png'))
    bpy.ops.render.render(write_still=True)
    if preview=='game_export':
        camera.location=(0,4,1.23)
        camera.rotation_euler=(Vector((0,-.04,.59))-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.ortho_scale=1.8
        scene.render.filepath=str(OUT/'previews'/'front.png')
        bpy.ops.render.render(write_still=True)
    for o in imported: bpy.data.objects.remove(o,do_unlink=True)
print('EXPORT_RENDERS_COMPLETE')
