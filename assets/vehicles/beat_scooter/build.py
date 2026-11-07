"""Build an original BeAT-inspired game scooter in Blender. +Y forward, +Z up; meters.
Run: Blender --background --factory-startup --python build.py
No downloaded meshes or photo textures are used.
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector
from math import sin, cos, pi

OUT = Path(__file__).resolve().parent
for d in ['textures', 'previews']:
    (OUT/d).mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
    if c.name != 'Collection': bpy.data.collections.remove(c)
model = bpy.data.collections.get('Collection'); model.name='SCOOTER • authoring'
stage = bpy.data.collections.new('STUDIO • excluded from exports')
bpy.context.scene.collection.children.link(stage)
helpers = bpy.data.collections.new('COLLISION • optional guides')
bpy.context.scene.collection.children.link(helpers)

# Uniform atlas tiles provide a real UV channel and one shared PBR material.
# color, roughness, metallic, emission (linear intensity)
palette = {
 'paint': ('344E79', .36, .38, 0),
 'paint_light': ('45628C', .39, .36, 0),
 'paint_dark': ('1B2B48', .4, .3, 0),
 'trim': ('202732', .64, .05, 0),
 'rubber': ('161B23', .88, 0, 0),
 'tread': ('0D1117', .95, 0, 0),
 'seat': ('252B34', .79, 0, 0),
 'seam': ('454B53', .74, 0, 0),
 'rim': ('333B46', .33, .72, 0),
 'steel': ('929FA8', .3, .82, 0),
 'silver': ('C8D6DE', .22, .88, 0),
 'darkmetal': ('424C58', .51, .65, 0),
 'headlight': ('DAF3FF', .2, .22, .6),
 'lens': ('829EB5', .2, .5, 0),
 'amber': ('FF9B35', .25, .12, .12),
 'red': ('C32937', .24, .22, .2),
 'mirror': ('9BB6C8', .13, .96, 0),
 'screen': ('73B5BD', .38, .1, .1),
 'white': ('DFE6E8', .42, .12, 0),
 'bronze': ('A9956E', .4, .7, 0),
 'black': ('0B1018', .47, .15, 0),
}
keys=list(palette)
SIZE=128; GRID=8; TILE=SIZE//GRID
def srgb(v): return v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4
def rgb(s): return tuple(int(s[i:i+2],16)/255 for i in (0,2,4))
images={}
for channel in ['base_color','roughness','metallic','emission']:
    im=bpy.data.images.new('Scooter_'+channel,width=SIZE,height=SIZE,alpha=True)
    if channel in ['roughness','metallic']: im.colorspace_settings.name='Non-Color'
    pixels=[]
    for y in range(SIZE):
        for x in range(SIZE):
            idx=(y//TILE)*GRID+x//TILE
            hx,rough,metal,emit=palette[keys[idx]] if idx < len(keys) else ('202732',.6,0,0)
            col=rgb(hx)
            if channel=='roughness': col=(rough,)*3
            elif channel=='metallic': col=(metal,)*3
            elif channel=='emission': col=tuple(srgb(v)*emit for v in col)
            else: col=tuple(srgb(v) for v in col)
            pixels.extend((*col,1.0))
    im.pixels.foreach_set(pixels)
    im.filepath_raw=str(OUT/'textures'/('scooter_'+channel+'.png')); im.file_format='PNG'
    im.save(); im.pack(); images[channel]=im
material=bpy.data.materials.new('Scooter_Atlas_PBR'); material.use_nodes=True
bsdf=material.node_tree.nodes.get('Principled BSDF')
for channel,socket in [('base_color','Base Color'),('roughness','Roughness'),('metallic','Metallic'),('emission','Emission Color')]:
    node=material.node_tree.nodes.new('ShaderNodeTexImage'); node.image=images[channel]
    node.interpolation='Closest'; node.label=channel
    material.node_tree.links.new(node.outputs['Color'],bsdf.inputs[socket])
bsdf.inputs['Emission Strength'].default_value=1.0
bsdf.inputs['Coat Weight'].default_value=.16

def move_collection(obj,col=model):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    col.objects.link(obj)

def empty(name,loc,parent=None):
    o=bpy.data.objects.new(name,None); model.objects.link(o); o.location=loc
    if parent:
        bpy.context.view_layer.update()
        mat=o.matrix_world.copy(); o.parent=parent; o.matrix_world=mat
    o.empty_display_type='PLAIN_AXES'; o.empty_display_size=.08
    return o

root=empty('BeAT_Scooter',(0,0,0)); root['forward']='+Y in Blender; -Z in GLB'
root['units']='meters'; root['wheelbase_m']=1.255
root['asset']='Original manually parameterized, stylized BeAT-inspired prototype'
steer=empty('Steering_Pivot',(0,.43,.88),root)
steer['rotate']='local Z for simplified game steering'
front=empty('Front_Wheel_Pivot',(0,.6275,.2498),steer)
rear=empty('Rear_Wheel_Pivot',(0,-.6275,.2588),root)
front['rotate']=rear['rotate']='local X; axle centered'
body=empty('Body',(0,0,0),root)
lights={}
for label in ['Headlight','Brake_Light','Indicator_Front_L','Indicator_Front_R','Indicator_Rear_L','Indicator_Rear_R']:
    lights[label]=empty(label,(0,0,0),body)

def attach(o,parent):
    bpy.context.view_layer.update()
    m=o.matrix_world.copy(); o.parent=parent; o.matrix_world=m

def uv_tile(o,key):
    if not o.data.uv_layers: o.data.uv_layers.new(name='UVMap')
    i=keys.index(key); u=(i%GRID+.5)/GRID; v=(i//GRID+.5)/GRID
    for p in o.data.polygons:
        # Nonzero UV islands, contained entirely in a uniform color swatch.
        for j,li in enumerate(p.loop_indices):
            theta=2*pi*j/max(3,len(p.loop_indices))
            o.data.uv_layers.active.data[li].uv=(u+.018*cos(theta),v+.018*sin(theta))

def finish(o,key,parent=body,smooth=False,bevel=0):
    move_collection(o); bpy.context.view_layer.objects.active=o
    if o.type=='MESH':
        if bevel:
            mod=o.modifiers.new('Small manufactured edge','BEVEL'); mod.width=bevel; mod.segments=1
            mod.affect='EDGES'; bpy.ops.object.modifier_apply(modifier=mod.name)
        bm=bmesh.new(); bm.from_mesh(o.data); bmesh.ops.recalc_face_normals(bm,faces=bm.faces); bm.to_mesh(o.data); bm.free()
        for p in o.data.polygons: p.use_smooth=smooth
        # Split only actual hard edges, retaining smooth tire and panel surfaces.
        bm=bmesh.new(); bm.from_mesh(o.data)
        for e in bm.edges:
            if e.is_manifold and e.calc_face_angle(0)>math.radians(45): e.smooth=False
        bm.to_mesh(o.data); bm.free()
        o.data.materials.clear(); o.data.materials.append(material); uv_tile(o,key)
    if parent: attach(o,parent)
    return o

def mesh(name,verts,faces,key,parent=body,smooth=False,bevel=0):
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    o=bpy.data.objects.new(name,me); model.objects.link(o)
    return finish(o,key,parent,smooth,bevel)

def box(name,loc,scale,key,parent=body,bevel=.006,rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.name=name
    o.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if rot: o.rotation_euler=rot
    return finish(o,key,parent,False,bevel)

def cylinder(name,a,b,r,key,parent=body,segments=12,r2=None):
    a,b=Vector(a),Vector(b); d=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=segments,radius1=r,radius2=r if r2 is None else r2,depth=d.length,location=(a+b)/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,key,parent,True)

def tube(name,points,r,key,parent=body,sides=6):
    verts=[]; faces=[]
    points=[Vector(p) for p in points]
    for i,p in enumerate(points):
        d=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
        n=d.cross(Vector((1,0,0)))
        if n.length<.01: n=d.cross(Vector((0,0,1)))
        n.normalize(); q=d.cross(n).normalized()
        for j in range(sides): verts.append(tuple(p+r*(cos(2*pi*j/sides)*n+sin(2*pi*j/sides)*q)))
    for i in range(len(points)-1):
        for j in range(sides): faces.append((i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j))
    faces += [tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+j for j in range(sides))]
    return mesh(name,verts,faces,key,parent,True)

def panel(name,points,thickness,key,parent=body):
    n=len(points); verts=points+[tuple(Vector(p)+Vector(thickness)) for p in points]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,verts,faces,key,parent,False)

def loft(name,sections,key,parent=body,smooth=True):
    # sections: y, half width, bottom z, top z. Octagonal cross sections.
    verts=[]; faces=[]
    for y,w,bot,top in sections:
        h=top-bot
        verts += [(-w*.72,y,bot),(-w,y,bot+h*.22),(-w,y,top-h*.22),(-w*.68,y,top),
                  (w*.68,y,top),(w,y,top-h*.22),(w,y,bot+h*.22),(w*.72,y,bot)]
    for i in range(len(sections)-1):
        for j in range(8): faces.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
    faces += [tuple(range(7,-1,-1)),tuple((len(sections)-1)*8+j for j in range(8))]
    return mesh(name,verts,faces,key,parent,smooth)

def ring(name,cy,cz,profile,key,parent,segments=36):
    # Cross section is (x,r), rotational axis X.
    verts=[]; faces=[]; n=len(profile)
    for i in range(segments):
        t=2*pi*i/segments
        for x,r in profile: verts.append((x,cy+r*sin(t),cz+r*cos(t)))
    for i in range(segments):
        for j in range(n): faces.append((i*n+j,((i+1)%segments)*n+j,((i+1)%segments)*n+(j+1)%n,i*n+(j+1)%n))
    return mesh(name,verts,faces,key,parent,True)

def wheel(tag,cy,cz,width,r,parent,disc=False):
    h=width/2
    profile=[(-h*.60,.1778),(-h*.95,.196),(-h,r-.018),(-h*.80,r-.005),(-h*.34,r),
             (h*.34,r),(h*.8,r-.005),(h,r-.018),(h*.95,.196),(h*.6,.1778)]
    ring(tag+'_Tire',cy,cz,profile,'rubber',parent,40)
    ring(tag+'_Cast_Rim',cy,cz,[(-h*.79,.179),(-h*.85,.171),(-h*.65,.157),(h*.65,.157),(h*.85,.171),(h*.79,.179)],'rim',parent,32)
    ring(tag+'_Rim_Lip',cy,cz,[(-h*.87,.172),(-h*.87,.177),(-h*.83,.177),(-h*.83,.172)],'darkmetal',parent,32)
    cylinder(tag+'_Hub',(-h,cy,cz),(h,cy,cz),.038,'rim',parent,16)
    for i in range(5):
        t=2*pi*i/5
        def p(rad,angle): return (0,cy+rad*sin(angle),cz+rad*cos(angle))
        tube(tag+'_Spoke_'+str(i),[p(.025,t),p(.095,t+.14),p(.17,t+.25)],.013,'rim',parent,4)
        tube(tag+'_SpokeBranch_'+str(i),[p(.095,t+.14),p(.17,t-.20)],.009,'rim',parent,4)
    verts=[]; faces=[]
    for i in range(32):
        theta=2*pi*i/32
        for side in [-1,1]:
            pts=[]
            for x,dt,rad in [(0,0,r+.0007),(side*h*.79,.10,r-.0045),(side*h*.79,.119,r-.0045),(0,.019,r+.0007)]:
                pts.append((x,cy+rad*sin(theta+dt),cz+rad*cos(theta+dt)))
            a=len(verts); verts+=pts; faces.append(tuple(range(a,a+4)))
    mesh(tag+'_Tread_Grooves',verts,faces,'tread',parent)
    if disc:
        x=-.055
        ring('Front_Brake_Rotor',cy,cz,[(x-.003,.104),(x+.003,.104),(x+.003,.071),(x-.003,.071)],'steel',parent,32)
        cylinder('Rotor_Hub',(x-.002,cy,cz),(x+.003,cy,cz),.046,'steel',parent,16)
        for i in range(5):
            t=2*pi*i/5
            tube('Rotor_Spider_'+str(i),[(x,cy+.035*sin(t),cz+.035*cos(t)),(x,cy+.076*sin(t+.25),cz+.076*cos(t+.25))],.009,'steel',parent,4)
        # Dark recessed marks suggest drilled holes without expensive boolean geometry.
        for i in range(16):
            t=2*pi*i/16
            cylinder('Rotor_Recess_'+str(i),(x-.0034,cy+.09*sin(t),cz+.09*cos(t)),(x-.0038,cy+.09*sin(t),cz+.09*cos(t)),.004,'black',parent,6)
        cylinder('Axle',(-.083,cy,cz),(.083,cy,cz),.012,'steel',parent,12)

wheel('Front',.6275,.2498,.080,.2498,front,True)
wheel('Rear',-.6275,.2588,.090,.2588,rear)

# Step-through lower chassis and textured footboard.
loft('Lower_Chassis',[(-.53,.105,.19,.34),(-.21,.145,.148,.275),(.20,.155,.148,.285),(.43,.132,.18,.34)],'trim')
loft('Footboard',[(-.25,.148,.265,.29),(-.10,.16,.256,.282),(.24,.16,.256,.282),(.38,.14,.27,.30)],'rubber')
for side in [-1,1]:
    for i in range(6):
        box('Floor_Grip',(side*.095,-.1+i*.061,.285),(.075,.007,.004),'trim',bevel=.001)
    tube('Floor_Edge',[(side*.16,-.19,.277),(side*.174,.12,.277),(side*.148,.35,.296)],.007,'paint_dark',sides=4)

# Rear under-seat volume, faceted side bodywork, and two-level saddle.
loft('Seat_Bucket',[(-.88,.073,.61,.745),(-.76,.15,.50,.735),(-.56,.176,.40,.71),(-.30,.156,.325,.67),(-.12,.127,.30,.66),(-.055,.095,.33,.625)],'trim')
loft('Rear_Painted_Shell',[(-.94,.055,.668,.755),(-.85,.135,.63,.766),(-.68,.184,.49,.741),(-.47,.183,.42,.70),(-.26,.15,.347,.662),(-.12,.113,.329,.628)],'paint')
# Side accents are a connected strip with vertices following the shell, so no
# non-planar overlay can cut through another body panel.
for s in [-1,1]:
    sections=[(-.84,.140,.689,.721),(-.68,.186,.635,.671),(-.47,.185,.581,.614),(-.28,.156,.506,.539)]
    vs=[]
    for y,x,b,t in sections: vs += [(s*x,y,b),(s*x,y,t)]
    mesh('Side_Character_Line',vs,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(3)],'paint_light')
loft('Seat_Base',[(-.90,.08,.752,.768),(-.80,.147,.755,.780),(-.57,.181,.71,.737),(-.35,.159,.68,.705),(-.13,.129,.655,.688),(-.06,.075,.641,.675)],'black')
loft('Saddle',[(-.895,.075,.763,.786),(-.80,.147,.773,.811),(-.62,.177,.734,.793),(-.47,.169,.702,.757),(-.33,.156,.699,.742),(-.15,.129,.673,.742),(-.065,.078,.660,.715)],'seat')
for s in [-1,1]:
    tube('Saddle_Seam',[(s*.077,-.885,.774),(s*.147,-.80,.785),(s*.176,-.62,.752),(s*.167,-.47,.718),(s*.154,-.33,.712),(s*.126,-.15,.689)],.0018,'seam',sides=4)
    tube('Passenger_Grab_Rail',[(s*.14,-.68,.756),(s*.155,-.83,.815),(s*.10,-.94,.798),(0,-.965,.792)],.015,'darkmetal',sides=6)
    box('Passenger_Footpeg',(s*.17,-.35,.34),(.09,.032,.024),'darkmetal',bevel=.004)

# Front apron: tapered central ridge, broad lower shoulders, dark rider-facing surface.
apron_sections=[(.205,.115,.45,.31),(.31,.143,.49,.29),(.45,.175,.55,.295),(.58,.213,.64,.325),(.67,.195,.722,.372),(.75,.163,.673,.33),(.85,.135,.55,.305),(.91,.105,.465,.31)]
verts=[]; faces=[]
for z,w,f,b in apron_sections:
    verts += [(0,f+.01,z),(w*.68,f-.024,z),(w,f-.083,z),(w*.84,b,z),(0,b-.016,z),(-w*.84,b,z),(-w,f-.083,z),(-w*.68,f-.024,z)]
for i in range(len(apron_sections)-1):
    for j in range(8): faces.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
faces += [tuple(range(7,-1,-1)),tuple(56+j for j in range(8))]
apron=mesh('Front_Apron',verts,faces,'paint',smooth=False)
# Reassign back-face UV tiles to dark molded plastic on same material.
for p in apron.data.polygons:
    if p.index<56 and p.index%8 in [3,4]:
        idx=keys.index('trim'); u=(idx%8+.5)/8; v=(idx//8+.5)/8
        for li in p.loop_indices: apron.data.uv_layers.active.data[li].uv=(u,v)

def front_surface(x,z,offset=.008):
    for a,b in zip(apron_sections,apron_sections[1:]):
        if a[0]<=z<=b[0]:
            t=(z-a[0])/(b[0]-a[0]); w=a[1]+t*(b[1]-a[1]); f=a[2]+t*(b[2]-a[2]); break
    else: w=.105; f=.465
    ratio=abs(x)/w
    y=f+.01-ratio*.05 if ratio<=.68 else f-.024-(ratio-.68)/.32*.059
    return (x,y+offset,z)

def apron_patch(name,outline,key,offset,parent=body):
    # Subdivide in 2D, then conform every vertex to the fairing surface. A single
    # non-planar ngon otherwise triangulates straight through the front ridge.
    bm=bmesh.new()
    vv=[bm.verts.new((x,0,z)) for x,z in outline]
    bm.faces.new(vv)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=2,use_grid_fill=True)
    for v in bm.verts: v.co=front_surface(v.co.x,v.co.z,offset)
    bm.verts.ensure_lookup_table(); bm.verts.index_update()
    vs=[tuple(v.co) for v in bm.verts]; fs=[tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    return mesh(name,vs,fs,key,parent)

for s in [-1,1]:
    outline=[(s*.008,.637),(s*.112,.621),(s*.183,.675),(s*.165,.718),(s*.092,.708),(s*.012,.680)]
    apron_patch('Headlight_Black_Surround',outline,'black',.015)
    cx=sum(p[0] for p in outline)/len(outline); cz=sum(p[1] for p in outline)/len(outline)
    lens=[(cx+(x-cx)*.83,cz+(z-cz)*.79) for x,z in outline]
    apron_patch('Headlamp_Lens',lens,'lens',.020,lights['Headlight'])
    strip=[(s*x,z) for x,z in [(0.025,.652),(.103,.642),(.161,.679),(.151,.688),(.098,.656),(.028,.665)]]
    apron_patch('LED_Reflector',strip,'headlight',.025,lights['Headlight'])
    upper=[(s*x,z) for x,z in [(.073,.773),(.116,.752),(.134,.779),(.089,.831)]]
    apron_patch('Indicator_Surround',upper,'black',.015)
    inset=[(s*x,z) for x,z in [(.080,.780),(.113,.766),(.122,.781),(.090,.816)]]
    label='Indicator_Front_L' if s<0 else 'Indicator_Front_R'
    apron_patch('Front_Indicator_Lens',inset,'amber',.021,lights[label])
    crease=[(s*x,z) for x,z in [(.15,.56),(.196,.58),(.171,.633),(.128,.612)]]
    apron_patch('Apron_Shoulder',crease,'paint_light',.008)

# Molded front mudguard follows the tire with a visible air gap.
verts=[];faces=[]
for i in range(17):
    t=math.radians(-58+i*7.25)
    for j in range(5):
        u=(j-2)/2; r=.295-.014*u*u
        verts.append((u*.069,.6275+r*sin(t),.2498+r*cos(t)))
for i in range(16):
    for j in range(4): faces.append((i*5+j,i*5+j+1,(i+1)*5+j+1,(i+1)*5+j))
fender=mesh('Front_Mudguard',verts,faces,'paint',steer,True)
mod=fender.modifiers.new('Plastic thickness','SOLIDIFY'); mod.thickness=.005
bpy.context.view_layer.objects.active=fender; bpy.ops.object.modifier_apply(modifier=mod.name)

for s in [-1,1]:
    cylinder('Fork_Lower',(s*.058,.6275,.25),(s*.058,.545,.49),.022,'darkmetal',steer,12)
    cylinder('Fork_Stanchion',(s*.058,.545,.475),(s*.058,.455,.745),.014,'silver',steer,12)
    cylinder('Fork_Axle_Bolt',(s*.071,.6275,.25),(s*.082,.6275,.25),.014,'silver',steer,6)
box('Brake_Caliper',(-.064,.543,.307),(.027,.048,.064),'darkmetal',steer,bevel=.007)
tube('Front_Brake_Hose',[(-.076,.54,.32),(-.071,.51,.51),(-.08,.43,.70),(-.12,.35,.91)],.003,'rubber',steer,sides=5)
cylinder('Steering_Column',(0,.47,.70),(0,.40,.954),.023,'darkmetal',steer,12)

# Handlebar cowl and dashboard move together around Steering_Pivot.
loft('Handlebar_Under_Cowl',[(.293,.115,.907,.960),(.35,.18,.893,.984),(.415,.19,.911,1.003),(.50,.065,.925,.959)],'trim',steer)
loft('Handlebar_Painted_Cowl',[(.315,.102,.958,.976),(.368,.177,.974,1.006),(.421,.173,.961,1.017),(.495,.062,.942,.974)],'paint',steer)
panel('Dash_Recess',[(-.116,.300,.958),(.116,.300,.958),(.086,.385,1.011),(-.086,.385,1.011)],(0,-.004,.002),'black',steer)
panel('Instrument_Display',[(-.088,.306,.966),(.088,.306,.966),(.066,.368,1.006),(-.066,.368,1.006)],(0,-.001,.001),'screen',steer)
for i in range(7):
    x=-.065+i*.021
    panel('Gauge_Tick',[(x,.346,.996),(x+.002,.346,.996),(x+.002,.355,1.002),(x,.355,1.002)],(0,-.001,.001),'white',steer)
panel('Gauge_Needle',[(0,.317,.976),(.003,.319,.977),(-.024,.350,.998)],(0,-.002,0),'red',steer)

for s in [-1,1]:
    cylinder('Handlebar',(s*.13,.376,.961),(s*.324,.376,.952),.012,'darkmetal',steer,12)
    cylinder('Rubber_Grip',(s*.228,.376,.957),(s*.320,.376,.952),.018,'rubber',steer,12)
    cylinder('Bar_End',(s*.320,.376,.952),(s*.330,.376,.952),.0185,'darkmetal',steer,10)
    box('Switch_Housing',(s*.21,.37,.962),(.039,.043,.040),'trim',steer,bevel=.007)
    box('Switch_Button',(s*.211,.346,.963),(.013,.006,.011),'red' if s>0 else 'darkmetal',steer,bevel=.001)
    tube('Brake_Lever',[(s*.193,.401,.958),(s*.235,.425,.95),(s*.318,.412,.940),(s*.332,.40,.943)],.006,'silver',steer,sides=6)
    tube('Mirror_Stem',[(s*.195,.39,.998),(s*.218,.40,1.082),(s*.275,.415,1.139)],.0065,'darkmetal',steer,sides=8)
    # Polygon mirrors with the glass facing the rider (-Y).
    outline=[(-.050,-.018),(-.040,.023),(.007,.039),(.052,.015),(.045,-.026),(-.003,-.035)]
    pts=[(s*(.277+dx),.427,1.151+dz) for dx,dz in outline]
    panel('Mirror_Housing',pts,(0,-.026,0),'trim',steer)
    pts=[(s*(.277+dx*.83),.399,1.151+dz*.80) for dx,dz in outline]
    panel('Mirror_Glass',pts,(0,-.001,0),'mirror',steer)

# Engine, CVT transmission, exhaust, rear suspension, rear lighting.
loft('Engine_Block',[(-.58,.103,.251,.38),(-.40,.115,.24,.395),(-.29,.095,.245,.355)],'darkmetal')
loft('CVT_Case',[(-.78,.036,.233,.294),(-.67,.057,.213,.331),(-.43,.057,.211,.331),(-.30,.036,.241,.310)],'trim')
cvt=bpy.data.objects['CVT_Case']; cvt.location.x=-.12
cylinder('Engine_Fan_Cover',(.099,-.37,.323),(.13,-.37,.323),.067,'trim',body,20)
for i in range(9):
    t=2*pi*i/9
    cylinder('Fan_Vent',(.131,-.37+.043*sin(t),.323+.043*cos(t)),(.133,-.37+.043*sin(t),.323+.043*cos(t)),.007,'black',body,6)
tube('Exhaust_Header',[(.09,-.30,.26),(.15,-.34,.21),(.19,-.45,.23),(.19,-.59,.28)],.016,'darkmetal',sides=8)
ex=loft('Muffler',[(-.88,.027,.34,.40),(-.80,.055,.292,.423),(-.54,.057,.25,.366),(-.45,.022,.258,.315)],'black')
ex.location.x=.169
panel('Exhaust_Heat_Shield',[(.224,-.46,.304),(.227,-.55,.370),(.222,-.79,.420),(.21,-.86,.376),(.221,-.76,.322),(.223,-.55,.284)],(.009,0,0),'darkmetal')
for y,z in [(-.56,.33),(-.78,.377)]: cylinder('Shield_Bolt',(.232,y,z),(.237,y,z),.007,'silver',body,8)
cylinder('Tailpipe',(.167,-.855,.375),(.169,-.915,.398),.018,'darkmetal',body,10)
cylinder('Tailpipe_Opening',(.169,-.916,.399),(.169,-.918,.40),.013,'black',body,10)
shock_a=Vector((-.11,-.66,.31)); shock_b=Vector((-.12,-.54,.58))
cylinder('Rear_Shock_Damper',shock_a,shock_b,.014,'steel',body,12)
d=(shock_b-shock_a).normalized(); u=Vector((1,0,0)); v=d.cross(u).normalized()
points=[]
for i in range(65):
    t=i/64; p=shock_a.lerp(shock_b,.12+.76*t)+.024*(cos(t*2*pi*7)*u+sin(t*2*pi*7)*v)
    points.append(tuple(p))
tube('Rear_Shock_Coil',points,.004,'darkmetal',sides=5)
tube('Rear_Swingarm',[(-.065,-.31,.27),(-.065,-.48,.245),(-.065,-.6275,.2588)],.024,'darkmetal',sides=6)
panel('Rear_Mudguard',[(-.075,-.84,.69),(.075,-.84,.69),(.06,-.972,.34),(-.06,-.972,.34)],(0,.017,0),'trim')
panel('Tail_Lamp_Housing',[(-.116,-.888,.733),(.116,-.888,.733),(.07,-.958,.670),(-.07,-.958,.670)],(0,.015,0),'black')
panel('Tail_Lamp',[(-.099,-.903,.726),(.099,-.903,.726),(.061,-.967,.682),(-.061,-.967,.682)],(0,.004,0),'red',lights['Brake_Light'])
for s in [-1,1]:
    panel('Rear_Indicator',[(s*.102,-.903,.726),(s*.137,-.866,.712),(s*.109,-.914,.678),(s*.075,-.952,.685)],(s*.002,-.003,0),'amber',lights['Indicator_Rear_L' if s<0 else 'Indicator_Rear_R'])
box('Rear_Number_Plate',(0,-.956,.525),(.218,.013,.077),'white',bevel=.004,rot=(math.radians(-17),0,0))
box('Front_Number_Plate',(0,.567,.876),(.218,.012,.065),'white',bevel=.004,rot=(math.radians(24),0,0))

def text_mesh(name,text,loc,size,rot,key,parent=body):
    cu=bpy.data.curves.new(name,'FONT'); cu.body=text; cu.size=size; cu.align_x='CENTER'; cu.align_y='CENTER'; cu.extrude=0; cu.resolution_u=2
    o=bpy.data.objects.new(name,cu); model.objects.link(o); o.location=loc; o.rotation_euler=rot
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH'); return finish(o,key,parent)
text_mesh('Front_Plate_Text','B 110 SIM',(0,.576,.880),.028,(pi/2+math.radians(24),0,pi),'black')
text_mesh('Rear_Plate_Text','B 110 SIM',(0,-.967,.528),.028,(pi/2-math.radians(17),0,0),'black')
for s in [-1,1]:
    # Discreet nameplate; no copied manufacturer artwork.
    text_mesh('Side_Nameplate','BeAT',(s*.188,-.63,.627),.034,(pi/2,0,s*pi/2),'silver')

# Recessed side stand, rider reference sockets, and simple collision guides.
tube('Side_Stand_Folded',[(-.13,-.23,.245),(-.15,-.43,.215),(-.14,-.50,.23)],.011,'darkmetal',sides=6)
for name,loc in [('Socket_Rider',(0,-.25,.742)),('Socket_Passenger',(0,-.66,.80)),('Socket_Camera_Rider',(0,-.11,1.30)),('Socket_Grip_L',(-.27,.376,.957)),('Socket_Grip_R',(.27,.376,.957))]:
    empty(name,loc,steer if 'Grip' in name else root)
col=box('COLLIDER_Body',(0,-.15,.48),(.38,1.15,.58),'trim',None,bevel=.04)
move_collection(col,helpers); col.display_type='WIRE'; col.hide_render=True
col['usage']='Approximate collision guide only; use engine wheel colliders separately.'
helpers.hide_viewport=True

# Editable native asset remains separate parts. Exports consolidate by moving group.
bpy.context.view_layer.update()
asset_objects=list(model.objects)
for o in asset_objects:
    if o.type=='MESH':
        # Apply rest transforms into mesh, retain animation parents and pivot origins.
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)

def triangle_count(objects):
    n=0
    for o in objects:
        if o.type=='MESH': o.data.calc_loop_triangles(); n+=len(o.data.loop_triangles)
    return n

export_collection=bpy.data.collections.new('GAME_EXPORT • consolidated')
bpy.context.scene.collection.children.link(export_collection)
mapping={}
for old in asset_objects:
    cp=old.copy()
    if old.data: cp.data=old.data.copy()
    export_collection.objects.link(cp); cp.name=old.name+'_Export'; mapping[old]=cp
for old,cp in mapping.items():
    cp.parent=mapping.get(old.parent)
    cp.matrix_parent_inverse=old.matrix_parent_inverse.copy()

for old in [body,steer,front,rear,*lights.values()]:
    parent=mapping[old]
    children=[o for o in export_collection.objects if o.type=='MESH' and o.parent==parent]
    if not children: continue
    bpy.ops.object.select_all(action='DESELECT')
    for c in children: c.select_set(True)
    bpy.context.view_layer.objects.active=children[0]; bpy.ops.object.join()
    joined=bpy.context.object; joined.name=old.name+'_Mesh'
    # Collapse identical atlas material slots introduced by join.
    joined.data.materials.clear(); joined.data.materials.append(material)
    for p in joined.data.polygons: p.material_index=0
    bpy.context.scene.cursor.location=parent.matrix_world.translation
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.context.scene.cursor.location=(0,0,0)

def select_export():
    bpy.ops.object.select_all(action='DESELECT')
    for o in export_collection.objects: o.select_set(True)
    bpy.context.view_layer.objects.active=mapping[root]

select_export()
triangles=triangle_count(export_collection.objects)
bpy.ops.export_scene.gltf(filepath=str(OUT/'beat_scooter.glb'),export_format='GLB',use_selection=True,export_yup=True,export_extras=True,export_animations=False,export_apply=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'beat_scooter.fbx'),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='COPY',embed_textures=True,use_mesh_modifiers=True,add_leaf_bones=False)

# LOD1: preserve pivots, reduce only mesh detail. Native source stays untouched.
for o in export_collection.objects:
    if o.type=='MESH' and len(o.data.polygons)>40:
        mod=o.modifiers.new('LOD1 reduction','DECIMATE'); mod.ratio=.5
        bpy.context.view_layer.objects.active=o
        bpy.ops.object.modifier_apply(modifier=mod.name)
        o.data.validate(verbose=False,clean_customdata=True)
        o.data.update()
lod_triangles=triangle_count(export_collection.objects)
select_export()
bpy.ops.export_scene.gltf(filepath=str(OUT/'beat_scooter_lod1.glb'),export_format='GLB',use_selection=True,export_yup=True,export_extras=True,export_animations=False,export_apply=True)
for o in list(export_collection.objects): bpy.data.objects.remove(o,do_unlink=True)
bpy.data.collections.remove(export_collection)

# Studio presentation uses the actual mesh, with no image generation.
scene=bpy.context.scene
scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.cycles.use_denoising=True
scene.render.resolution_x=1200; scene.render.resolution_y=1050; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world.color=(.18,.18,.18)
scene.view_settings.view_transform='AgX'
groundmat=bpy.data.materials.new('Studio_Ground'); groundmat.diffuse_color=(.075,.095,.13,1); groundmat.use_nodes=True
groundmat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.075,.095,.13,1)
groundmat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.84
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008)); ground=bpy.context.object; ground.name='Studio_Ground'; ground.data.materials.append(groundmat); move_collection(ground,stage)
def area(name,loc,power,size,color):
    da=bpy.data.lights.new(name,'AREA'); da.energy=power; da.shape='DISK'; da.size=size; da.color=color
    o=bpy.data.objects.new(name,da); stage.objects.link(o); o.location=loc; o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-2,2.8,4),400,3.0,(.83,.91,1))
area('Fill',(3,1,2.4),260,2.5,(1,.91,.81))
area('Rim',(0,-3,3),600,2,(.63,.78,1))
ca=bpy.data.cameras.new('Asset_Camera'); camera=bpy.data.objects.new('Asset_Camera',ca); stage.objects.link(camera); scene.camera=camera
ca.type='ORTHO'; ca.ortho_scale=2.4; ca.lens=55
def camera_view(loc,target=(0,-.04,.59),scale=2.4):
    camera.location=loc; camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler(); ca.ortho_scale=scale
camera_view((3.7,2.7,1.9),scale=2.25)

for area_ui in bpy.context.screen.areas if bpy.context.screen else []:
    if area_ui.type=='VIEW_3D':
        area_ui.spaces.active.region_3d.view_distance=2.65
        area_ui.spaces.active.region_3d.view_location=Vector((0,0,.57))
        area_ui.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
        area_ui.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT'); root.select_set(True); bpy.context.view_layer.objects.active=root
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'beat_scooter.blend'))
summary={'lod0_triangles':triangles,'lod1_triangles':lod_triangles,'authoring_meshes':sum(o.type=='MESH' for o in asset_objects),'materials':1,'atlas_size':'128x128','wheelbase_m':1.255,'front_wheel_radius_m':.2498,'rear_wheel_radius_m':.2588,'blender_forward':'+Y','glb_forward':'-Z','glb_up':'+Y'}
(OUT/'asset_stats.json').write_text(json.dumps(summary,indent=2)+'\n')
print('ASSET_STATS',json.dumps(summary),flush=True)
for name,loc,scale in [('hero',(3.7,2.7,1.9),2.25),('side',(3.5,0,1.0),2.15),('rear',(-3.2,-2.8,1.7),2.25),('front',(0,4,1.23),1.8)]:
    camera_view(loc,scale=scale)
    scene.render.filepath=str(OUT/'previews'/(name+'.png'))
    bpy.ops.render.render(write_still=True)
print('COMPLETE',flush=True)
