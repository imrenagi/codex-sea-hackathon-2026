"""Round-trip checks for geometry, scale, UVs, and rigid animation pivots."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

OUT=Path(__file__).resolve().parent
results=[]
for filename in ['beat_scooter.glb','beat_scooter_lod1.glb','beat_scooter.fbx']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path=OUT/filename
    if path.suffix=='.glb': bpy.ops.import_scene.gltf(filepath=str(path))
    else: bpy.ops.import_scene.fbx(filepath=str(path))
    bpy.context.view_layer.update()
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert meshes, filename+' contains no meshes'
    if path.suffix=='.glb':
        lens=next(m for m in bpy.data.materials if m.name.startswith('Optical_Lens_Clear'))
        shader=next(n for n in lens.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        assert shader.inputs['Transmission Weight'].default_value>.8, 'Lens transmission was lost'
        atlas=next(m for m in bpy.data.materials if m.name.startswith('Scooter_Atlas_PBR'))
        tex=[n.image for n in atlas.node_tree.nodes if n.type=='TEX_IMAGE']
        assert len(tex)>=4 and all(im and im.size[0]>=512 and im.size[1]>=512 for im in tex), 'Missing or reduced atlas maps'
    tris=0; points=[]; changed=[]
    for o in meshes:
        assert len(o.data.uv_layers)>0, o.name+' missing UVs'
        assert len(o.data.materials)>0, o.name+' missing materials'
        if o.data.validate(verbose=True,clean_customdata=False): changed.append(o.name)
        for v in o.data.vertices:
            assert all(math.isfinite(f) for f in v.co), 'Non-finite coordinate'
        o.data.calc_loop_triangles(); tris+=len(o.data.loop_triangles)
        points.extend(o.matrix_world @ v.co for v in o.data.vertices)
    assert not changed, 'Invalid geometry repaired on import: '+str(changed)
    mn=[min(p[i] for p in points) for i in range(3)]
    mx=[max(p[i] for p in points) for i in range(3)]
    dims=[mx[i]-mn[i] for i in range(3)]
    assert .64<dims[0]<.70, 'Width is not in expected meter scale: '+str(dims)
    assert 1.8<dims[1]<2.0, 'Length is not in expected meter scale: '+str(dims)
    assert 1.1<dims[2]<1.3, 'Height is not in expected meter scale: '+str(dims)
    assert abs(mn[2])<.005, 'Tires are not grounded'
    def obj(name):
        matches=[o for o in bpy.context.scene.objects if o.name==name+'_Export']
        assert len(matches)==1, 'Missing or ambiguous node: '+name
        return matches[0]
    fw=obj('Front_Wheel_Pivot'); rw=obj('Rear_Wheel_Pivot'); steer=obj('Steering_Pivot')
    expected=[(fw,(0,.6275,.2498)),(rw,(0,-.6275,.2588)),(steer,(0,.43,.88))]
    for o,pos in expected:
        assert (o.matrix_world.translation-Vector(pos)).length<.001, 'Incorrect pivot: '+o.name+' '+str(o.matrix_world.translation)
    assert fw.parent==steer, 'Front wheel must follow steering'
    # Spin transforms must retain the tire geometry at its axle (no orbiting).
    wheel_children=[o for o in meshes if o.parent==fw]
    assert len(wheel_children)==1
    tire=wheel_children[0]
    before=[fw.matrix_world.inverted() @ (tire.matrix_world @ v.co) for v in tire.data.vertices]
    maxradius=max(math.hypot(p.y,p.z) for p in before)
    assert .249<maxradius<.253, 'Front wheel mesh offset from axle'
    fw.rotation_euler.x+=math.pi/2; bpy.context.view_layer.update()
    after=[fw.matrix_world.inverted() @ (tire.matrix_world @ v.co) for v in tire.data.vertices]
    assert max((a-b).length for a,b in zip(before,after))<1e-5
    for name in ['Socket_Rider','Socket_Passenger','Socket_Grip_L','Socket_Grip_R','Headlight','Brake_Light']:
        obj(name)
    result={'file':filename,'bytes':path.stat().st_size,'triangles':tris,'mesh_count':len(meshes),
            'bounds_m':{'min':mn,'max':mx},'dimensions_m':dims,'pivots_and_uvs':'PASS'}
    results.append(result)
    print('VALIDATED',json.dumps(result),flush=True)
(OUT/'validation.json').write_text(json.dumps(results,indent=2)+'\n')
print('ALL_EXPORTS_PASSED',flush=True)
