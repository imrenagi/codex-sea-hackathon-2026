"""Offline asset inspection only. Never opens Unity or changes project assets.
Blender --background --factory-startup --python tools/vehicles/inspect_avanza.py
"""
import bpy, importlib.util, json, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'assets/vehicles/avanza_inspired'
REQUIRED=['Body','Wheel_FL','Wheel_FR','Wheel_RL','Wheel_RR','Steering_FL','Steering_FR','Headlights','TailLights','Indicator_Front_L','Indicator_Front_R','Indicator_Rear_L','Indicator_Rear_R','Socket_Driver','Socket_Passenger']
report={'method':'Blender FBX/GLB round-trip; no Unity runtime or performance validation','exports':[]}
for path in [OUT/'unity/Body.fbx',OUT/'unity/Body_LOD1.fbx',OUT/'avanza_inspired.glb',OUT/'avanza_inspired_lod1.glb']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(path))
    else:bpy.ops.import_scene.gltf(filepath=str(path))
    bpy.context.view_layer.update()
    objects=list(bpy.context.scene.objects)
    names={o.name:o for o in objects}
    for name in REQUIRED:assert name+'_Export' in names, (path,name)
    points=[];triangles=0
    for o in objects:
        if o.type!='MESH':continue
        assert not o.data.validate(),(path,o.name,'invalid geometry')
        assert o.data.uv_layers and o.data.materials,(path,o.name,'missing UV/material')
        assert all(m and (m.name.startswith('Body_Paint') or m.name.startswith('Traffic_Atlas_PBR')) for m in o.data.materials),(path,o.name,'material contract')
        o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
        points.extend(o.matrix_world@v.co for v in o.data.vertices)
    assert all(math.isfinite(c) for p in points for c in p),(path,'nonfinite')
    bounds={'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)]}
    assert abs(bounds['min'][2])<.005 and 4.3<bounds['max'][1]-bounds['min'][1]<4.55,(path,bounds)
    for n,x,y in [('FL',-.81,1.375),('FR',.81,1.375),('RL',-.81,-1.375),('RR',.81,-1.375)]:
        p=names['Wheel_'+n+'_Export'].matrix_world.translation
        assert (p-Vector((x,y,.32))).length<.001,(path,n,tuple(p))
    lamps={}
    for n in ['Indicator_Front_L','Indicator_Front_R','Indicator_Rear_L','Indicator_Rear_R']:
        parts=[o for o in names[n+'_Export'].children_recursive if o.type=='MESH']
        assert len(parts)==1,(path,n,'not consolidated')
        ps=[parts[0].matrix_world@v.co for v in parts[0].data.vertices]
        center=sum(ps,Vector())/len(ps)
        assert (center.x<0)==n.endswith('_L') and (center.y>0)==('_Front_' in n),(path,n,tuple(center))
        lamps[n]=[round(v,6) for v in center]
    report['exports'].append(dict(file=str(path.relative_to(ROOT)),triangles=triangles,meshes=sum(o.type=='MESH' for o in objects),bounds_m=bounds,required_nodes='present',wheel_centres='preserved',indicator_centres=lamps))
    print('ASSET_INSPECTED',path.name,triangles,flush=True)
# The final import is the actual delivered low-LOD GLB, not the authoring scene.
spec=importlib.util.spec_from_file_location('author',ROOT/'assets/vehicles/indonesia_traffic_pack/build.py')
b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
sc,cam,target=b.studio(dict(kind='car'));sc.cycles.samples=24
cam.location=(5.7,8.1,3.4);cam.rotation_euler=(Vector((0,0,.82))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=5.85
sc.render.filepath=str(OUT/'previews/lod1_roundtrip.png');bpy.ops.render.render(write_still=True)
(OUT/'asset_inspection.json').write_text(json.dumps(report,indent=2)+'\n')
print('OFFLINE_INSPECTION_COMPLETE',flush=True)
