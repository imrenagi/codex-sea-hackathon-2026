"""Render actual LOD1 imports using the native scenes' studio setup."""
import bpy,json
from pathlib import Path
OUT=Path(__file__).resolve().parent
for folder in sorted(p for p in OUT.iterdir() if (p/'asset_stats.json').exists()):
    spec=json.loads((folder/'asset_stats.json').read_text())
    bpy.ops.wm.open_mainfile(filepath=str(folder/(spec['id']+'.blend')))
    collection=bpy.data.collections.get('VEHICLE_authoring')
    for o in list(collection.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(collection)
    bpy.ops.import_scene.gltf(filepath=str(folder/(spec['id']+'_'+spec['colors'][0][0]+'_lod1.glb')))
    sc=bpy.context.scene;sc.cycles.samples=16;sc.render.resolution_percentage=70
    sc.render.filepath=str(folder/'previews'/'lod1.png');bpy.ops.render.render(write_still=True)
    print('LOD_PREVIEW_COMPLETE',spec['id'],flush=True)
