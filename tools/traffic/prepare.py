"""Derive Unity traffic FBXs from finalized GLBs without changing source packs.
Splits the actual indicator geometry into four lamp meshes, adds a compact helmeted
NPC rider to scooters, and packs a shared URP atlas. Run with Blender in background.
"""
import bpy,bmesh,json,math,shutil
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'assets/vehicles/indonesia_traffic_pack'
OUT=ROOT/'Unity/Assets/Resources/Traffic'
OUT.mkdir(parents=True,exist_ok=True)
manifest=json.loads((SOURCE/'manifest.json').read_text())
def parent_keep(o,p):
    bpy.context.view_layer.update();m=o.matrix_world.copy();o.parent=p;o.matrix_world=m
def split_signals():
    old=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.parent and o.parent.name=='Indicators_Export')
    root=old.parent.parent;oldparent=old.parent
    for front in [True,False]:
        for left in [True,False]:
            group=bpy.data.objects.new('Indicator_'+('Front_' if front else 'Rear_')+('L' if left else 'R')+'_Export',None)
            bpy.context.collection.objects.link(group);parent_keep(group,root)
            cp=old.copy();cp.data=old.data.copy();bpy.context.collection.objects.link(cp);cp.name=group.name.replace('_Export','_Lens')
            bm=bmesh.new();bm.from_mesh(cp.data)
            erase=[]
            for face in bm.faces:
                p=cp.matrix_world @ face.calc_center_median()
                if (p.y>0)!=front or (p.x<0)!=left:erase.append(face)
            bmesh.ops.delete(bm,geom=erase,context='FACES')
            isolated=[v for v in bm.verts if not v.link_faces]
            if isolated:bmesh.ops.delete(bm,geom=isolated,context='VERTS')
            assert len(bm.faces)>0,group.name
            bm.to_mesh(cp.data);bm.free();parent_keep(cp,group)
    bpy.data.objects.remove(old,do_unlink=True);bpy.data.objects.remove(oldparent,do_unlink=True)
def rider(lod):
    atlas=next(m for m in bpy.data.materials if m.name.startswith('Traffic_Atlas'))
    root=next(o for o in bpy.context.scene.objects if o.type=='EMPTY' and o.parent is None)
    seat=next(o for o in bpy.context.scene.objects if o.name=='Socket_Rider_Export').matrix_world.translation
    parts=[]
    def finish(o,key):
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        o.data.materials.clear();o.data.materials.append(atlas)
        for p in o.data.polygons:p.use_smooth=True
        uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
        u,v=(key%4+.5)/4,(key//4+.5)/4
        for p in o.data.polygons:
            for j,idx in enumerate(p.loop_indices):uv.data[idx].uv=(u+.06*math.cos(j*2*math.pi/len(p.loop_indices)),v+.06*math.sin(j*2*math.pi/len(p.loop_indices)))
        parts.append(o)
    def sph(name,p,s,key):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12 if not lod else 8,ring_count=8 if not lod else 6,radius=1,location=seat+Vector(p))
        o=bpy.context.object;o.name=name;o.scale=s;finish(o,key)
    def limb(name,a,b,r,key):
        aa,bb=seat+Vector(a),seat+Vector(b);delta=bb-aa
        bpy.ops.mesh.primitive_cone_add(vertices=8 if not lod else 6,radius1=r,radius2=r*.82,depth=delta.length,location=(aa+bb)/2)
        o=bpy.context.object;o.name=name;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();finish(o,key)
    sph('Rider_Jacket',(0,-.02,.31),(.17,.105,.24),14)
    sph('Rider_Hips',(0,-.02,.075),(.15,.13,.085),5)
    sph('Rider_Full_Helmet',(0,.015,.64),(.12,.135,.15),8)
    sph('Rider_Visor',(0,.122,.65),(.105,.031,.068),2)
    limb('Helmet_Chin_Strap',(-.09,.04,.53),(.09,.04,.53),.013,5)
    for s in [-1,1]:
        limb('Rider_Upper_Arm',(s*.155,0,.43),(s*.22,.26,.25),.055,14)
        limb('Rider_Forearm',(s*.22,.26,.25),(s*.27,.67,.24),.044,14)
        sph('Rider_Glove',(s*.27,.67,.24),(.044,.055,.04),5)
        limb('Rider_Thigh',(s*.10,.015,.06),(s*.165,.29,-.13),.072,5)
        limb('Rider_Shin',(s*.165,.29,-.13),(s*.13,.39,-.43),.052,5)
        sph('Rider_Boot',(s*.13,.44,-.445),(.065,.12,.038),5)
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name='Helmeted_Traffic_Rider'
    parent_keep(o,root)
report=[]
for spec in manifest['models']:
    dest=OUT/spec['id'];dest.mkdir(exist_ok=True)
    authored=ROOT/'assets/vehicles/avanza_inspired'
    if spec['id']=='family_mpv' and (authored/'asset_stats.json').exists():
        # Keep the authored Avanza visual while preserving the original source pack.
        stats=json.loads((authored/'asset_stats.json').read_text())
        for lod,fn in [(0,'Body.fbx'),(1,'Body_LOD1.fbx')]:
            shutil.copy2(authored/'unity'/fn,dest/fn)
            report.append(dict(model=spec['id'],lod=lod,triangles=stats['lods'][lod]['triangles']))
        continue
    for lod in [0,1]:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        source=spec['variants'][0]['lod1' if lod else 'glb']
        bpy.ops.import_scene.gltf(filepath=str(SOURCE/source));split_signals()
        if spec['kind']=='scooter':rider(lod)
        bpy.ops.object.select_all(action='SELECT')
        fn='Body_LOD1.fbx' if lod else 'Body.fbx'
        bpy.ops.export_scene.fbx(filepath=str(dest/fn),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='AUTO',add_leaf_bones=False)
        tri=0
        for o in bpy.context.scene.objects:
            if o.type=='MESH':o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
        report.append(dict(model=spec['id'],lod=lod,triangles=tri))
        print('TRAFFIC_PREPARED',spec['id'],lod,tri,flush=True)
textures=OUT/'Textures';textures.mkdir(exist_ok=True)
src=SOURCE/'sport_scooter/textures'
shutil.copy2(src/'traffic_base_color.png',textures/'BaseColor.png')
for name in ['metallic','roughness']:
    im=bpy.data.images.load(str(src/('traffic_'+name+'.png')));im.colorspace_settings.name='Non-Color'
    arr=np.empty(128*128*4,dtype=np.float32);im.pixels.foreach_get(arr)
    if name=='metallic':metal=arr.reshape(-1,4)[:,0]
    else:rough=arr.reshape(-1,4)[:,0]
rgba=np.zeros((128*128,4),dtype=np.float32);rgba[:,0]=metal;rgba[:,3]=1-rough
im=bpy.data.images.new('MetallicSmoothness',width=128,height=128,alpha=True);im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(textures/'MetallicSmoothness.png');im.file_format='PNG';im.save()
rgba=np.zeros((128,128,4),dtype=np.float32);rgba[:,:,3]=1
for key in [10,11,12]:rgba[key//4*32:key//4*32+32,key%4*32:key%4*32+32,:3]=1
im=bpy.data.images.new('LampMask',width=128,height=128,alpha=True);im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(textures/'LampMask.png');im.file_format='PNG';im.save()
(OUT/'import_report.json').write_text(json.dumps(report,indent=2)+'\n')
print('TRAFFIC_ASSETS_READY',flush=True)
