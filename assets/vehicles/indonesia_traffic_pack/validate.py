"""Round-trip every GLB and FBX, validate pivots, materials, UVs and scale."""
import bpy, json, math, hashlib
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
report=[]
for folder in sorted(p for p in OUT.iterdir() if (p/'asset_stats.json').exists()):
    spec=json.loads((folder/'asset_stats.json').read_text())
    files=sorted(folder.glob('*.glb'))+sorted(folder.glob('*.fbx'))
    assert len(files)==5, f'Missing export: {folder.name}'
    for path in files:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        if path.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(path))
        else:bpy.ops.import_scene.fbx(filepath=str(path))
        bpy.context.view_layer.update()
        meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[];triangles=0
        assert len(meshes)==spec['export_meshes'], 'Export mesh count changed'
        for o in meshes:
            assert o.data.uv_layers and o.data.materials, f'Missing UV/material: {o.name}'
            assert not o.data.validate(verbose=True,clean_customdata=False), f'Invalid mesh: {o.name}'
            for v in o.data.vertices:assert all(math.isfinite(x) for x in v.co)
            for uv in o.data.uv_layers.active.data:assert all(math.isfinite(x) for x in uv.uv)
            o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
            points.extend(o.matrix_world @ v.co for v in o.data.vertices)
        mn=[min(p[i] for p in points) for i in range(3)];mx=[max(p[i] for p in points) for i in range(3)]
        for i in range(3):
            assert abs(mn[i]-spec['bounds_m']['min'][i])<.035, 'Incorrect bounds / units'
            assert abs(mx[i]-spec['bounds_m']['max'][i])<.035, 'Incorrect bounds / units'
        assert abs(mn[2])<.008, 'Vehicle not at ground level'
        assert bpy.data.objects.get(spec['id']+'_Export'), 'Missing root'
        for wheel in spec['wheels']:
            pivot=bpy.data.objects.get(wheel['node']+'_Export');assert pivot,'Missing wheel pivot'
            assert (pivot.matrix_world.translation-Vector(wheel['position'])).length<.0001, 'Incorrect wheel center'
            children=[o for o in meshes if o.parent==pivot];assert len(children)==1, 'Wheel should be one mesh'
            local=[pivot.matrix_world.inverted() @ (children[0].matrix_world @ v.co) for v in children[0].data.vertices]
            radius=max(math.hypot(p.y,p.z) for p in local)
            assert abs(radius-wheel['radius'])<.006, 'Wheel is not centered on its axle'
            pivot.rotation_euler.x+=1.2;bpy.context.view_layer.update()
            spun=[pivot.matrix_world.inverted() @ (children[0].matrix_world @ v.co) for v in children[0].data.vertices]
            assert max((a-b).length for a,b in zip(local,spun))<1e-5, 'Wheel spin changes local geometry'
        for key in ['Headlights','TailLights','Indicators']:
            assert bpy.data.objects.get(key+'_Export'), 'Missing lamp group'
        if path.suffix=='.glb':
            atlas=next(m for m in bpy.data.materials if m.name.startswith('Traffic_Atlas'))
            images=[n.image for n in atlas.node_tree.nodes if n.type=='TEX_IMAGE']
            assert len(images)>=3 and all(im and im.size[0]==128 for im in images),'Embedded atlas missing'
            paint=next(m for m in bpy.data.materials if m.name.startswith('Body_Paint'))
            shader=next(n for n in paint.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            chosen=next(hx for label,hx in spec['colors'] if '_'+label in path.stem)
            def lin(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
            expected=[lin(int(chosen[j:j+2],16)/255) for j in (0,2,4)]
            actual=shader.inputs['Base Color'].default_value
            assert max(abs(actual[i]-expected[i]) for i in range(3))<.001,'Wrong paint option'
        lod=int('_lod1' in path.stem)
        assert triangles==spec['lod'+str(lod)+'_triangles'], 'Triangle count changed on export'
        report.append(dict(file=str(path.relative_to(OUT)),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),triangles=triangles,mesh_count=len(meshes),bounds_min=mn,bounds_max=mx,result='PASS'))
        print('PASS',report[-1]['file'],triangles,flush=True)
assert len(report)==30, 'Expected all six vehicles, two paints, two LODs and FBX'
(OUT/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
print('ALL_30_EXPORTS_PASSED',flush=True)
