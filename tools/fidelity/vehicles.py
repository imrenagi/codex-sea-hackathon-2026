"""Detailed original derivatives of this project's authored traffic vehicles.
Keeps animation sockets, light groups, axle positions and original source files.
Run Blender --background --python tools/fidelity/vehicles.py -- [source project root]
"""
import bpy,bmesh,math,json,inspect,importlib.util,sys,ast
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path(sys.argv[sys.argv.index('--')+1]) if '--' in sys.argv else ROOT
src=SOURCE/'assets/vehicles/indonesia_traffic_pack/build.py'
spec=importlib.util.spec_from_file_location('original_traffic',src);b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
# Increase rounded edge and wheel sampling without changing vehicle dimensions.
oldbox=b.box
b.box=lambda *a,**kw:oldbox(*a,**dict(kw,segments=max(4,kw.get('segments',2))))
oldring=b.ring
b.ring=lambda *a,**kw:oldring(*a,**dict(kw,n=max(64,kw.get('n',28))))
oldpatch=b.patch
b.patch=lambda *a,**kw:oldpatch(*a,**dict(kw,cuts=max(5,kw.get('cuts',2))))
source=inspect.getsource(b.car)
source=source.replace('return hw*(.91+.09*max(0,end))','e=max(0,end);return hw*(.89+.11*(e*e*(3-2*e)))')
source=source.replace('range(17)', 'range(49)').replace('j*pi/16','j*pi/48').replace("n=5)","n=10)")
source=source.replace("for u in [-1,-.92,0,.92,1]:", "for u in [i/8 for i in range(-8,9)]:")
source=source.replace('top(y)+.022*(1-u*u)','top(y)+.052*(1-u*u)')
source=source.replace('for j in range(4):fs.append((i*5+j,i*5+j+1,(i+1)*5+j+1,(i+1)*5+j))','for j in range(16):fs.append((i*17+j,i*17+j+1,(i+1)*17+j+1,(i+1)*17+j))')
# Continuous roof transitions shared by cabin, glazing, window frames and wipers.
source=source.replace('t=(y-a[0])/(b[0]-a[0]);return a[1]*(1-t)+b[1]*t,a[2]*(1-t)+b[2]*t',
'''t=(y-a[0])/(b[0]-a[0])
                if t<.12:
                    q=t/.12;t=.12*q*q*(2-q)
                elif t>.88:
                    q=(1-t)/.12;t=1-.12*q*q*(2-q)
                return a[1]*(1-t)+b[1]*t,a[2]*(1-t)+b[2]*t''')
a=source.index('    vs=[];fs=[]\n    for y,w,z in rows:');z=source.index('    if pickup:\n        windows=',a)
source=source[:a]+'''    vs=[];fs=[]
    sampleYs=sorted(set([rows[0][0]+(rows[-1][0]-rows[0][0])*i/52 for i in range(53)]+[r[0] for r in rows]))
    for y in sampleYs:
        w,z=roof(y)
        section=[(-hw*.94,belt),(-hw*.94*.55-w*.45,belt*.55+(z-.035)*.45),(-w,z-.035),(-w*.97,z-.008),(-w*.88,z+.012),(-w*.5,z+.034),(0,z+.045),(w*.5,z+.034),(w*.88,z+.012),(w*.97,z-.008),(w,z-.035),(hw*.94*.55+w*.45,belt*.55+(z-.035)*.45),(hw*.94,belt)]
        vs.extend([(x,y,h) for x,h in section])
    for i in range(len(sampleYs)-1):
        for j in range(12):fs.append((i*13+j,i*13+j+1,(i+1)*13+j+1,(i+1)*13+j))
    mesh('Cabin_Structure',vs,fs,'paint',smooth=True)
''' + source[z:]
source=source.replace('return (x*w,y,z+.008)','return (x*w,y,z+.008+.045*(1-x*x))')
# Wipers sit above the new curved glass, retaining the original visible detail.
source=source.replace('[(x,y,z+.018),(x+s*.16,y-.22,roof(y-.22)[1]+.018)]',
    '[(x,y,topglass(x/roof(y)[0],y)[2]+.015),(x+s*.16,y-.22,topglass((x+s*.16)/roof(y-.22)[0],y-.22)[2]+.015)]')
source=source.replace('[(0,ya+.06,roof(ya+.06)[1]+.015),(.32,ya+.18,roof(ya+.18)[1]+.015)]',
    '[(0,ya+.06,topglass(0,ya+.06)[2]+.015),(.32,ya+.18,topglass(.32/roof(ya+.18)[0],ya+.18)[2]+.015)]')
exec(source,b.__dict__)
# Only import the existing semantic signal splitter, never execute its asset-writing entrypoint.
prepare=ast.parse((SOURCE/'tools/traffic/prepare.py').read_text())
split_namespace={'bpy':bpy,'bmesh':bmesh}
exec(compile(ast.Module(body=[n for n in prepare.body if isinstance(n,ast.FunctionDef) and n.name in ['parent_keep','split_signals']],type_ignores=[]),'<signal helpers>','exec'),split_namespace)
report=[]
for cfg in b.CONFIGS:
 if cfg['kind']!='car':continue
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
 folder=ROOT/'assets/fidelity_vehicles'/cfg['id'];(folder/'textures').mkdir(parents=True,exist_ok=True)
 dest=ROOT/'Unity/Assets/Resources/StreetLook/Vehicles'/cfg['id'];dest.mkdir(parents=True,exist_ok=True)
 b.MODEL=bpy.data.collections.new('VEHICLE_authoring');bpy.context.scene.collection.children.link(b.MODEL)
 b.ROOT=b.empty(cfg['id']);b.BODY=b.empty('Body',parent=b.ROOT)
 b.LIGHTS={n:b.empty(n,parent=b.ROOT) for n in ['Headlights','TailLights','Indicators']}
 b.materials(cfg,folder);b.car(cfg)
 # Clear panel shut lines, extra tread and modeled alloy lug nuts.
 W=cfg['width'];r=cfg['radius'];wb=cfg['wb']
 for wheel in [o for o in b.MODEL.objects if o.type=='EMPTY' and o.name.startswith('Wheel_')]:
  c=wheel.matrix_world.translation
  for side in [-1,1]:
   for j in range(5):
    t=j*math.tau/5;yy=c.y+math.sin(t)*r*.16;zz=c.z+math.cos(t)*r*.16
    b.cyl('Alloy_lug',(c.x+side*.095,yy,zz),(c.x+side*.10,yy,zz),.012,'chrome',wheel,n=8)
  for j in range(42):
   t=j*math.tau/42
   b.tube('Tire_sipe',[(c.x-.065,c.y+r*math.sin(t-.018),c.z+r*math.cos(t-.018)),(c.x,c.y+(r+.0005)*math.sin(t),c.z+(r+.0005)*math.cos(t)),(c.x+.065,c.y+r*math.sin(t+.018),c.z+r*math.cos(t+.018))],.002,'black',wheel,n=3)
 # Small real edge radii on fascias and glass, keep native source assets unchanged.
 for o in list(b.MODEL.objects):
  if o.type!='MESH':continue
  if o.name.startswith(('Lower_Fascia','Headlamp_','Tail_Housing','Door_Mirror')):
   bpy.context.view_layer.objects.active=o
   mod=o.modifiers.new('Lens and pressed edge radii','BEVEL');mod.width=.012 if 'Fascia' not in o.name else .045;mod.segments=4;mod.limit_method='ANGLE'
   bpy.ops.object.modifier_apply(modifier=mod.name)
   for p in o.data.polygons:p.use_smooth=True
   weighted=o.modifiers.new('Face weighted manufactured normals','WEIGHTED_NORMAL');weighted.keep_sharp=True;weighted.weight=50
   bpy.ops.object.modifier_apply(modifier=weighted.name)
 # Rounded bumper corners across the complete assembly, so lamps and trim follow
 # the new pressing rather than floating off the body. No dimension expansion.
 for o in list(b.MODEL.objects):
  if o.type!='MESH':continue
  if o.name.startswith(('Lower_Fascia','Rear_Bumper_Trim','Front_Grille','Lower_Intake','Grille_Slat')):
   bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=3,use_grid_fill=True);bm.to_mesh(o.data);bm.free()
  inverse=o.matrix_world.inverted()
  for vertex in o.data.vertices:
   p=o.matrix_world @ vertex.co
   front=max(0,min(1,(p.y-(cfg['length']/2-.48))/.48));rear=max(0,min(1,(-p.y-(cfg['length']/2-.48))/.48))
   bow=(min(1,abs(p.x)/(cfg['width']*.5)))**3
   p.y+=(-.16*front*front*(3-2*front)+.12*rear*rear*(3-2*rear))*bow
   vertex.co=inverse @ p
  o.data.update()
  if o.name.startswith(('Lower_Fascia','Rear_Bumper_Trim','Front_Grille','Lower_Intake','Grille_Slat')):
   o.data.normals_split_custom_set([(0,0,0)]*len(o.data.loops))
 # Open panel meshes need explicit outward winding; generic volume-based recalc
 # can choose the cabin interior. Preserve this before FBX and both LOD exports.
 for o in b.MODEL.objects:
  if o.type!='MESH':continue
  axis=None;sign=1
  if o.name.startswith(('Cabin_Structure','Hood_And_Deck','Windshield','Rear_Window')):axis=2
  elif o.name.startswith(('Body_Side_Shell','Side_Window')):
   axis=0;sign=1 if sum(v.co.x for v in o.data.vertices)>0 else -1
  if axis is not None and sum(p.normal[axis] for p in o.data.polygons)*sign<0:
   bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
  if o.name.startswith('Body_Side_Shell'):
   normals=[]
   for v in o.data.vertices:
    edge=(max(0,(v.co.y-(cfg['length']/2-.4))/.4)-max(0,(-v.co.y-(cfg['length']/2-.4))/.4))*.5
    normal=b.Vector((sign,edge,.12*(v.co.z-.64)));normal.normalize();normals.append(tuple(normal))
   o.data.normals_split_custom_set_from_vertices(normals)
 # Preserve the original gameplay envelope exactly, including rounded fascia extents.
 original=next(v for v in json.loads((SOURCE/'assets/vehicles/indonesia_traffic_pack/manifest.json').read_text())['models'] if v['id']==cfg['id'])['bounds_m']
 for o in b.MODEL.objects:
  if o.type!='MESH':continue
  inverse=o.matrix_world.inverted()
  for vertex in o.data.vertices:
   point=o.matrix_world @ vertex.co
   for axis in [0,1]:point[axis]=max(original['min'][axis],min(original['max'][axis],point[axis]))
   vertex.co=inverse @ point
 bpy.ops.wm.save_as_mainfile(filepath=str(folder/(cfg['id']+'_detail.blend')))
 col=b.consolidate()
 # The source authoring collection is excluded from exports, only consolidated copies are split.
 for o in list(b.MODEL.objects):bpy.data.objects.remove(o,do_unlink=True)
 split_namespace['split_signals']()
 for lod in [0,1]:
  if lod:
   for o in col.objects:
    if o.type=='MESH' and len(o.data.polygons)>50:
     bpy.context.view_layer.objects.active=o;m=o.modifiers.new('Distance LOD','DECIMATE');m.ratio=.30;m.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=m.name)
  bpy.ops.object.select_all(action='DESELECT')
  for o in col.objects:o.select_set(True)
  # New signal meshes are created in the active collection by the shared splitter.
  for o in bpy.context.scene.objects:
   if o.type in ['MESH','EMPTY']:o.select_set(True)
  # Decimation can move boundary vertices. Reapply the same envelope to each LOD.
  for o in bpy.context.scene.objects:
   if o.type!='MESH':continue
   inverse=o.matrix_world.inverted()
   for vertex in o.data.vertices:
    point=o.matrix_world @ vertex.co
    for axis in [0,1]:point[axis]=max(original['min'][axis]+.001,min(original['max'][axis]-.001,point[axis]))
    vertex.co=inverse @ point
  bpy.context.view_layer.update()
  pts=[o.matrix_world @ v.co for o in bpy.context.scene.objects if o.type=='MESH' for v in o.data.vertices]
  print('DETAIL_BOUNDS',cfg['id'],lod,[[min(p[i] for p in pts),max(p[i] for p in pts)] for i in range(3)],flush=True)
  bpy.ops.export_scene.fbx(filepath=str(dest/('Body_LOD1.fbx' if lod else 'Body.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
  count=b.triangles(bpy.context.scene.objects);report.append(dict(model=cfg['id'],lod=lod,triangles=count))
  print('DETAIL_EXPORTED',cfg['id'],lod,count,flush=True)
(ROOT/'assets/fidelity_vehicles/report.json').write_text(json.dumps(report,indent=2)+'\n')
