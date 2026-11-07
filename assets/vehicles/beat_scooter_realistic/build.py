"""Build an original BeAT-inspired game scooter in Blender. +Y forward, +Z up; meters.
Run: Blender --background --factory-startup --python build.py
Reference: the red/white BeAT photograph supplied by the user.
No downloaded meshes or photo textures are used.
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector
from math import sin, cos, pi

OUT = Path(__file__).resolve().parent
bpy.context.preferences.filepaths.save_version=0
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
 'paint': ('E7EBEE', .25, .12, 0),
 'paint_light': ('F7F9FA', .22, .12, 0),
 'paint_dark': ('AEB5BA', .3, .16, 0),
 'trim': ('202732', .64, .05, 0),
 'rubber': ('161B23', .88, 0, 0),
 'tread': ('0D1117', .95, 0, 0),
 'seat': ('15191F', .68, 0, 0),
 'seam': ('454B53', .74, 0, 0),
 'rim': ('151A20', .27, .72, 0),
 'steel': ('929FA8', .3, .82, 0),
 'silver': ('C8D6DE', .22, .88, 0),
 'darkmetal': ('262B30', .4, .65, 0),
 'headlight': ('DAF3FF', .2, .22, .6),
 'lens': ('829EB5', .2, .5, 0),
 'amber': ('FF9B35', .25, .12, .12),
 'red': ('B70D1D', .22, .30, 0),
 'mirror': ('9BB6C8', .13, .96, 0),
 'screen': ('73B5BD', .38, .1, .1),
 'white': ('DFE6E8', .42, .12, 0),
 'bronze': ('A9956E', .4, .7, 0),
 'black': ('080A0E', .52, .05, 0),
 'red_dark': ('630610', .29, .22, 0),
 'tail': ('BE101D', .2, .15, .15),
 'decal_red': ('E31F31', .32, .15, 0),
 'decal_blue': ('318AC2', .32, .15, 0),
 'chrome': ('E1E8ED', .13, .95, 0),
}
keys=list(palette)
SIZE=512; GRID=8; TILE=SIZE//GRID
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
            grain=(sin(x*12.9898+y*78.233)*43758.5453)%1-.5
            if channel=='roughness': col=(max(.04,min(.98,rough+grain*(.13 if idx in [4,5,6,7] else .025))),)*3
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
    node.interpolation='Linear'; node.label=channel
    material.node_tree.links.new(node.outputs['Color'],bsdf.inputs[socket])
bsdf.inputs['Emission Strength'].default_value=1.0
bsdf.inputs['Coat Weight'].default_value=0

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
root['asset']='Original reference-based red/white BeAT-inspired game model'
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

# Dense-enough curved cross sections with deliberate panel edges; no blanket
# subdivision of the prototype. Dimensions remain in meters.
def interp(rows,t):
    i=min(len(rows)-2,max(0,int(t))); u=max(0,min(1,t-i))
    a=rows[max(0,i-1)]; b=rows[i]; c=rows[i+1]; d=rows[min(len(rows)-1,i+2)]
    return [0.5*((2*b[j])+(-a[j]+c[j])*u+(2*a[j]-5*b[j]+4*c[j]-d[j])*u*u+(-a[j]+3*b[j]-3*c[j]+d[j])*u*u*u) for j in range(len(b))]

def smooth_loft(name,rows,key,parent=body,around=24,steps=4,exponent=.72):
    vs=[];fs=[]; count=(len(rows)-1)*steps+1
    for i in range(count):
        y,w,bot,top=interp(rows,i/steps); w=max(.002,w); h=(top-bot)/2
        for j in range(around):
            t=2*pi*j/around; cx=cos(t); sz=sin(t)
            x=w*math.copysign(abs(cx)**exponent,cx)
            z=(top+bot)/2+h*math.copysign(abs(sz)**exponent,sz)
            vs.append((x,y,z))
    for i in range(count-1):
        for j in range(around): fs.append((i*around+j,i*around+(j+1)%around,(i+1)*around+(j+1)%around,(i+1)*around+j))
    fs += [tuple(range(around-1,-1,-1)),tuple((count-1)*around+j for j in range(around))]
    return mesh(name,vs,fs,key,parent,True)

def uv_sphere(name,loc,scale,key,parent=body,segments=24,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,key,parent,True)

def surface_patch(name,coords,project,key,parent=body,offset=.002,cuts=8):
    bm=bmesh.new(); vv=[bm.verts.new((u,v,0)) for u,v in coords]; bm.faces.new(vv)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    if cuts: bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=cuts,use_grid_fill=True)
    bm.verts.ensure_lookup_table(); bm.verts.index_update()
    vs=[project(v.co.x,v.co.y,offset) for v in bm.verts]
    fs=[tuple(v.index for v in f.verts) for f in bm.faces]; bm.free()
    return mesh(name,vs,fs,key,parent,True)

def flat_text(name,text,loc,size,rot,key,parent=body,shear=0):
    cu=bpy.data.curves.new(name,'FONT'); cu.body=text; cu.size=size; cu.shear=shear
    cu.align_x='CENTER'; cu.align_y='CENTER'; cu.resolution_u=4
    o=bpy.data.objects.new(name,cu); model.objects.link(o); o.location=loc; o.rotation_euler=rot
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH'); return finish(o,key,parent)

def decal_text(name,text,project,center,width,height,key,offset=.004,outline=False,direction=1):
    cu=bpy.data.curves.new(name,'FONT'); cu.body=text; cu.size=1; cu.shear=.22; cu.resolution_u=3
    if outline: cu.offset=.025
    o=bpy.data.objects.new(name,cu); model.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH')
    xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]
    xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys)
    for v in o.data.vertices:
        u=center[0]+((v.co.x-xmin)/(xmax-xmin)-.5)*width*direction
        w=center[1]+((v.co.y-ymin)/(ymax-ymin)-.5)*height
        v.co=project(u,w,offset)
    return finish(o,key,body)

# Fine leather/rubber normal map. Only the relevant atlas swatches get grain.
normal=bpy.data.images.new('Scooter_surface_normal',width=SIZE,height=SIZE,alpha=True)
normal.colorspace_settings.name='Non-Color'; data=[]
for y in range(SIZE):
    for x in range(SIZE):
        idx=(y//TILE)*GRID+x//TILE
        strength=.12 if idx in [4,5,6,7] else .008
        a=((sin(x*12.9898+y*78.233)*43758.5453)%1-.5)*strength
        b=((sin((x+17)*15.13+y*61.77)*13891.77)%1-.5)*strength
        data.extend((.5+a,.5+b,1,1))
normal.pixels.foreach_set(data); normal.filepath_raw=str(OUT/'textures'/'scooter_surface_normal.png'); normal.file_format='PNG'; normal.save(); normal.pack()
tn=material.node_tree.nodes.new('ShaderNodeTexImage'); tn.image=normal
nm=material.node_tree.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value=.55
material.node_tree.links.new(tn.outputs['Color'],nm.inputs['Color']); material.node_tree.links.new(nm.outputs['Normal'],bsdf.inputs['Normal'])

glass=bpy.data.materials.new('Optical_Lens_Clear'); glass.use_nodes=True
g=glass.node_tree.nodes.get('Principled BSDF'); g.inputs['Base Color'].default_value=(.91,.97,1,1)
g.inputs['Roughness'].default_value=.09; g.inputs['Transmission Weight'].default_value=.88; g.inputs['IOR'].default_value=1.46
g.inputs['Coat Weight'].default_value=.3

def real_wheel(tag,cy,cz,w,r,parent,disc=False):
    h=w/2
    profile=[(-h*.58,.1778),(-h*.92,.191),(-h,.209),(-h*.97,r-.022),(-h*.79,r-.007),(-h*.42,r-.001),(0,r),
             (h*.42,r-.001),(h*.79,r-.007),(h*.97,r-.022),(h,.209),(h*.92,.191),(h*.58,.1778)]
    ring(tag+'_Tire',cy,cz,profile,'rubber',parent,64)
    ring(tag+'_Rim',cy,cz,[(-h*.8,.180),(-h*.87,.175),(-h*.8,.166),(-h*.57,.158),(h*.57,.158),(h*.8,.166),(h*.87,.175),(h*.8,.180)],'rim',parent,48)
    cylinder(tag+'_Hub',(-h*.9,cy,cz),(h*.9,cy,cz),.034,'rim',parent,24)
    # Swept double spokes of the cast-alloy wheel.
    for i in range(5):
        t=2*pi*i/5
        def p(rad,a,x=0): return (x,cy+rad*sin(a),cz+rad*cos(a))
        for branch in [-1,1]:
            pts=[]
            for j in range(5):
                f=j/4; rad=.03+.139*f; a=t+branch*(.09+.15*f)+.12*f
                pts.append(p(rad,a))
            tube(tag+'_Cast_Spoke',pts,.0085,'rim',parent,sides=8)
    # Recess-colored tread paths, shoulders, sidewall ribs and valve stem.
    vs=[];fs=[]
    for i in range(48):
        t=2*pi*i/48
        for side in [-1,1]:
            rr=[(0,0,r+.00025),(side*h*.42,.032,r-.0005),(side*h*.79,.075,r-.0065),(side*h*.94,.10,r-.017)]
            for j in range(3):
                a=rr[j]; b=rr[j+1]; n=len(vs)
                for xx,dt,rad in [a,b,(b[0],b[1]+.009,b[2]),(a[0],a[1]+.009,a[2])]:
                    vs.append((xx,cy+rad*sin(t+dt),cz+rad*cos(t+dt)))
                fs.append((n,n+1,n+2,n+3))
    mesh(tag+'_Tread',vs,fs,'tread',parent)
    for sign in [-1,1]:
        ring(tag+'_Sidewall_Rib',cy,cz,[(sign*h*.969,.207),(sign*h*.976,.209),(sign*h*.971,.211),(sign*h*.965,.209)],'rubber',parent,48)
    cylinder(tag+'_Valve',(0,cy-.153,cz+.022),(0,cy-.134,cz+.020),.003,'black',parent,8)
    cylinder(tag+'_Axle',(-.082,cy,cz),(.082,cy,cz),.010,'steel',parent,16)
    if disc:
        vs=[];fs=[]; x=-.056; rin=.066; rout=.108; mid=(rin+rout)/2; N=20
        for i in range(N):
            theta=2*pi*i/N; sec=[]
            # Eight boundary points and an eight-sided drilled hole, each side.
            for depth in [-.002,.002]:
                for du,dv in [(-1,-1),(0,-1),(1,-1),(1,0),(1,1),(0,1),(-1,1),(-1,0)]:
                    rad=mid+du*(rout-rin)/2; t=theta+dv*pi/N
                    sec.append((x+depth,cy+rad*sin(t),cz+rad*cos(t)))
                for j in range(8):
                    a=5*pi/4+j*pi/4; rad=mid+.0045*cos(a); t=theta+.0045*sin(a)/mid
                    sec.append((x+depth,cy+rad*sin(t),cz+rad*cos(t)))
            n=len(vs);vs+=sec
            for j in range(8):
                k=(j+1)%8
                fs += [(n+j,n+k,n+8+k,n+8+j),(n+16+j,n+24+j,n+24+k,n+16+k),(n+8+j,n+8+k,n+24+k,n+24+j)]
                if j in [2,3,6,7]: fs.append((n+j,n+16+j,n+16+k,n+k))
        rotor=mesh('Perforated_Brake_Disc',vs,fs,'steel',parent,False)
        bm=bmesh.new();bm.from_mesh(rotor.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bm.to_mesh(rotor.data);bm.free()
        cylinder('Rotor_Hub',(x-.003,cy,cz),(x+.003,cy,cz),.042,'darkmetal',parent,20)
        for i in range(5):
            t=2*pi*i/5
            tube('Rotor_Spider',[(x,cy+.030*sin(t),cz+.030*cos(t)),(x,cy+.075*sin(t+.25),cz+.075*cos(t+.25))],.009,'steel',parent,4)
            cylinder('Rotor_Bolt',(x-.005,cy+.031*sin(t),cz+.031*cos(t)),(x-.007,cy+.031*sin(t),cz+.031*cos(t)),.004,'darkmetal',parent,6)

real_wheel('Front',.6275,.2498,.080,.2498,front,True)
real_wheel('Rear',-.6275,.2588,.090,.2588,rear)

# Rounded lower frame and broad, ribbed footwell.
smooth_loft('Lower_Chassis',[(-.53,.09,.22,.34),(-.34,.12,.18,.31),(-.16,.145,.155,.28),(.17,.15,.15,.275),(.35,.135,.17,.30),(.45,.11,.22,.36)],'trim',around=16,steps=3)
smooth_loft('Footboard',[(-.24,.115,.272,.288),(-.12,.15,.258,.282),(.17,.153,.258,.282),(.34,.132,.276,.30)],'rubber',around=16,steps=3)
for s in [-1,1]:
    for i in range(25):
        box('Floor_Rib',(s*.094,-.12+i*.017,.285),(.075,.003,.002),'trim',bevel=.0007)
    tube('Footboard_Edge',[(s*.13,-.22,.288),(s*.158,-.10,.283),(s*.159,.17,.283),(s*.135,.34,.30)],.004,'black',sides=6)

# The under-seat housing is a tapered curved form, with separate white covers.
smooth_loft('Seat_Bucket',[(-.89,.06,.62,.735),(-.76,.137,.48,.738),(-.62,.155,.38,.70),(-.44,.147,.32,.674),(-.26,.116,.295,.644),(-.11,.087,.32,.621),(-.055,.028,.36,.55)],'trim',around=24,steps=4)
side_rows=[(-.89,.102,.645,.741),(-.78,.165,.545,.730),(-.63,.181,.439,.694),(-.46,.171,.360,.627),(-.28,.137,.325,.488),(-.16,.103,.35,.374)]
def side_surface(s,t,h,offset=0):
    y,w,bot,top=interp(side_rows,max(0,min(1,t))*(len(side_rows)-1))
    x=w-.008*(2*h-1)**2+.010*sin(pi*h)
    return (s*(x+offset),y,bot+(top-bot)*h)
for s in [-1,1]:
    project=lambda t,h,o,s=s: side_surface(s,t,h,o)
    vs=[];fs=[]; NX=31;NY=9
    for i in range(NX):
        for j in range(NY):vs.append(project(i/(NX-1),j/(NY-1),0))
    for i in range(NX-1):
        for j in range(NY-1): fs.append((i*NY+j,(i+1)*NY+j,(i+1)*NY+j+1,i*NY+j+1))
    cover=mesh('White_Side_Cover',vs,fs,'paint',body,True)
    mod=cover.modifiers.new('Panel thickness','SOLIDIFY');mod.thickness=.004
    bpy.context.view_layer.objects.active=cover;bpy.ops.object.modifier_apply(modifier=mod.name)
    tube('Side_Panel_Seam',[project(i/24,0,.001) for i in range(25)],.002,'black',sides=5)
    # Sharp layered graphics follow the surface instead of floating over it.
    surface_patch('Red_Side_Graphic',[(.08,.81),(.31,.86),(.89,.15),(.78,.13),(.27,.67)],project,'decal_red')
    surface_patch('Black_Side_Graphic',[(.15,.82),(.24,.83),(.62,.38),(.82,.15),(.58,.31)],project,'black',offset=.004)
    surface_patch('Silver_Side_Graphic',[(.20,.64),(.46,.51),(.88,.08),(.59,.28)],project,'paint_dark',offset=.006)
    surface_patch('Blue_Side_Accent',[(.30,.63),(.51,.48),(.76,.16),(.70,.23)],project,'decal_blue',offset=.008)
    decal_text('BeAT_Outline','BeAT',project,(.44,.55),.29,.27,'white',offset=.010,outline=True,direction=s)
    decal_text('BeAT_Lettering','BeAT',project,(.44,.55),.26,.23,'black',offset=.0115,direction=s)
    decal_text('Side_eSP','eSP',project,(.80,.11),.10,.09,'darkmetal',offset=.009,direction=s)
    decal_text('Side_CBS','CBS',project,(.13,.86),.08,.09,'black',offset=.009,direction=s)
    cylinder('Panel_Fastener',(s*.121,-.24,.34),(s*.127,-.24,.34),.006,'darkmetal',segments=12)
    cylinder('Panel_Fastener_Slot',(s*.128,-.24,.34),(s*.129,-.24,.34),.002,'black',segments=8)

seat_rows=[(-.90,.065,.748,.773),(-.82,.13,.754,.793),(-.67,.163,.726,.786),(-.51,.162,.692,.754),(-.35,.147,.684,.742),(-.20,.128,.657,.735),(-.11,.088,.642,.704),(-.075,.029,.647,.675)]
smooth_loft('Saddle_Underlip',[(y,w*1.01,b-.006,b+.008) for y,w,b,t in seat_rows],'black',around=24,steps=4)
smooth_loft('Saddle',seat_rows,'seat',around=32,steps=5,exponent=.66)
for s in [-1,1]:
    tube('Stitched_Seat_Edge',[(s*w*.92,y,b+(t-b)*.19) for y,w,b,t in [interp(seat_rows,i/5) for i in range(36)]],.0012,'seam',sides=5)
    tube('Passenger_Grab_Rail',[(s*.13,-.69,.75),(s*.153,-.82,.81),(s*.118,-.91,.815),(s*.058,-.954,.795),(0,-.962,.79)],.014,'trim',sides=10)
    box('Passenger_Footpeg',(s*.162,-.39,.335),(.065,.025,.017),'darkmetal',bevel=.004)
    for i in range(4): box('Footpeg_Grip',(s*(.147+i*.01),-.39,.344),(.004,.024,.002),'black',bevel=.0005)

# Continuous rounded leg shield, with a red/white upper fairing and dark floor.
apron_rows=[(.205,.109,.445,.31),(.32,.14,.49,.29),(.45,.165,.545,.296),(.57,.203,.61,.325),(.655,.182,.717,.368),(.74,.164,.688,.327),(.835,.139,.553,.304),(.92,.095,.457,.304)]
def apron_params(z):
    for i,(a,b) in enumerate(zip(apron_rows,apron_rows[1:])):
        if a[0]<=z<=b[0]:return interp(apron_rows,i+(z-a[0])/(b[0]-a[0]))
    return apron_rows[0] if z<apron_rows[0][0] else apron_rows[-1]
def apron_front(x,z,offset=0):
    _,w,f,b=apron_params(z); ratio=min(1.15,abs(x)/w)
    return (x,f+.008-.083*ratio**1.7+offset,z)
vs=[];fs=[];count=43; R=13
for i in range(count):
    z,w,f,b=interp(apron_rows,i/(count-1)*(len(apron_rows)-1))
    for u in [-1,-.8,-.45,0,.45,.8,1]: vs.append(apron_front(u*w,z))
    for x,y in [(w*.90,b+.025),(w*.62,b),(0,b-.009),(-w*.62,b),(-w*.9,b+.025),(-w*.99,(b+f-.083)/2)]: vs.append((x,y,z))
for i in range(count-1):
    for j in range(R):fs.append((i*R+j,i*R+(j+1)%R,(i+1)*R+(j+1)%R,(i+1)*R+j))
fs += [tuple(range(R-1,-1,-1)),tuple((count-1)*R+j for j in range(R))]
# Split the lower color boundary into actual geometry, avoiding stair-stepped
# material assignments across coarse rows of faces.
def boundary(p):
    width=apron_params(p[2])[1]
    return p[2]-(.435+.13*(1-min(1,abs(p[0])/width))**2)
def clip_boundary(poly,positive):
    result=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        fa,fb=boundary(a),boundary(b)
        ina=(fa>=0) if positive else (fa<=0)
        inb=(fb>=0) if positive else (fb<=0)
        if ina: result.append(a)
        if ina != inb:
            lo,hi=0.0,1.0
            for _ in range(22):
                mid=(lo+hi)/2;p=tuple(a[j]+mid*(b[j]-a[j]) for j in range(3))
                if (boundary(p)>=0)==(fa>=0):lo=mid
                else:hi=mid
            t=(lo+hi)/2;result.append(tuple(a[j]+t*(b[j]-a[j]) for j in range(3)))
    return result
newvs=[];newfs=[];facekeys=[]
for i,face in enumerate(fs):
    points=[vs[j] for j in face]
    if i<(count-1)*R and i%R in [7,8,9,10,11]: pieces=[(points,'trim')]
    else: pieces=[(clip_boundary(points,True),'paint'),(clip_boundary(points,False),'trim')]
    for poly,key in pieces:
        if len(poly)<3:continue
        n=len(newvs);newvs+=poly;newfs.append(tuple(range(n,n+len(poly))));facekeys.append(key)
apron=mesh('Continuous_Leg_Shield',newvs,newfs,'paint',body,True)
for p,key in zip(apron.data.polygons,facekeys):
    idx=keys.index(key);uv=((idx%8+.5)/8,(idx//8+.5)/8)
    for li in p.loop_indices:apron.data.uv_layers.active.data[li].uv=uv
bm=bmesh.new();bm.from_mesh(apron.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(apron.data);bm.free()

# Fairing side graphics are positioned on the actual rounded outside shell.
for s in [-1,1]:
    project=lambda u,v,o,s=s: apron_front(s*u,v,o)
    surface_patch('Red_Front_Wing',[(.125,.47),(.187,.569),(.157,.620),(.12,.60),(.08,.515)],project,'decal_red',offset=.003)
    surface_patch('Black_Front_Wing',[(.126,.485),(.174,.567),(.153,.585),(.14,.55)],project,'black',offset=.004)
    surface_patch('Blue_Front_Wing',[(.128,.50),(.163,.557),(.151,.563),(.134,.53)],project,'decal_blue',offset=.005)
    surface_patch('Silver_Front_Wing',[(.109,.498),(.149,.543),(.139,.547),(.114,.518)],project,'paint_dark',offset=.004)
    # Small vent at the shoulder gives the bodywork a real panel break.
    surface_patch('Apron_Shoulder_Vent',[(.18,.59),(.165,.626),(.13,.625),(.145,.61)],project,'black',offset=.003)
surface_patch('Nose_Vent',[(-.033,.854),(0,.837),(.033,.854),(.021,.858),(0,.848),(-.021,.858)],apron_front,'black',offset=.004)

# Headlight assembly: broad central reflector and clear swept indicator lenses.
def lamp(name,outline,parent,bulb='headlight',offset=.019):
    surface_patch(name+'_Seal',outline,apron_front,'black',offset=offset-.008)
    cx=sum(p[0] for p in outline)/len(outline); cz=sum(p[1] for p in outline)/len(outline)
    inner=[(cx+(x-cx)*.89,cz+(z-cz)*.89) for x,z in outline]
    surface_patch(name+'_Chrome',inner,apron_front,'chrome',parent,offset=offset-.004,cuts=3)
    # Radial folds in the reflector bowl remain visible through the clear lens.
    for i in range(len(inner)):
        a=inner[i]; b=inner[(i+1)%len(inner)]
        surface_patch(name+'_Reflector_Facet',[(cx,cz),a,b],apron_front,'steel' if i%2 else 'silver',parent,offset=offset-.002,cuts=1)
    pos=apron_front(cx,cz,offset+.002)
    uv_sphere(name+'_Bulb',pos,(.012,.009,.013) if bulb=='headlight' else (.007,.007,.012),bulb,parent,16,8)
    def cover(x,z,o):
        d=((x-cx)/max(.03,max(abs(p[0]-cx) for p in outline)))**2+((z-cz)/max(.03,max(abs(p[1]-cz) for p in outline)))**2
        return apron_front(x,z,offset+.012+.007*max(0,1-d))
    lens=surface_patch(name+'_Clear_Lens',inner,cover,'lens',parent,offset=0,cuts=4)
    lens.data.materials.clear();lens.data.materials.append(glass)
    mod=lens.modifiers.new('Lens thickness','SOLIDIFY');mod.thickness=.0015
    bpy.context.view_layer.objects.active=lens;bpy.ops.object.modifier_apply(modifier=mod.name)

lamp('Central_Headlamp',[(-.112,.758),(.112,.758),(.116,.704),(.084,.646),(.035,.633),(-.035,.633),(-.084,.646),(-.116,.704)],lights['Headlight'])
for s in [-1,1]:
    lamp('Front_Turn_Indicator',[(s*.060,.770),(s*.117,.821),(s*.146,.839),(s*.153,.807),(s*.139,.759),(s*.112,.713)],lights['Indicator_Front_L' if s<0 else 'Indicator_Front_R'],'amber',offset=.021)
    # Silver ridges inside the long indicator lenses.
    for j in range(4):
        z=.775+j*.013
        surface_patch('Indicator_Optic_Ridge',[(s*.105,z),(s*.128,z+.006),(s*.127,z+.010),(s*.102,z+.004)],apron_front,'silver',lights['Indicator_Front_L' if s<0 else 'Indicator_Front_R'],offset=.024,cuts=1)

# Red aerodynamic front fender, softly curved with a center ridge.
vs=[];fs=[];NF=35;NW=9
for i in range(NF):
    f=i/(NF-1);t=math.radians(-62+126*f)
    width=.071*(1-.34*(max(0,f-.75)/.25)**2)
    for j in range(NW):
        u=2*j/(NW-1)-1;r=.296-.018*u*u+.003*(1-abs(u))
        vs.append((u*width,.6275+r*sin(t),.2498+r*cos(t)))
for i in range(NF-1):
    for j in range(NW-1):fs.append((i*NW+j,(i+1)*NW+j,(i+1)*NW+j+1,i*NW+j+1))
fender=mesh('Red_Front_Mudguard',vs,fs,'red',steer,True)
mod=fender.modifiers.new('Fender shell','SOLIDIFY');mod.thickness=.004
bpy.context.view_layer.objects.active=fender;bpy.ops.object.modifier_apply(modifier=mod.name)
for s in [-1,1]:
    cylinder('Fork_Lower',(s*.058,.6275,.25),(s*.058,.548,.491),.021,'darkmetal',steer,24)
    cylinder('Fork_Dust_Seal',(s*.058,.547,.486),(s*.058,.543,.499),.0225,'black',steer,24)
    cylinder('Fork_Stanchion',(s*.058,.545,.493),(s*.058,.455,.746),.013,'chrome',steer,20)
    cylinder('Axle_Nut',(s*.075,.6275,.25),(s*.085,.6275,.25),.011,'steel',steer,6)
box('Brake_Caliper',(-.069,.543,.31),(.032,.053,.062),'darkmetal',steer,bevel=.009)
for z in [.297,.326]: cylinder('Caliper_Bolt',(-.086,.544,z),(-.090,.544,z),.005,'steel',steer,8)
tube('Brake_Hose',[(-.079,.547,.32),(-.08,.51,.50),(-.08,.43,.70),(-.13,.35,.935)],.0028,'rubber',steer,sides=6)

# Sculpted red handlebar pod, slim brake levers, ribbed grips and oval mirrors.
cylinder('Steering_Stem',(0,.47,.70),(0,.405,.957),.021,'darkmetal',steer,20)
cowl_rows=[(.308,.055,.944,.979),(.343,.132,.949,1.014),(.397,.174,.925,1.025),(.445,.137,.917,1.000),(.488,.044,.931,.961)]
smooth_loft('Handlebar_Lower_Pod',[(y,w*.99,b-.044,b-.002) for y,w,b,t in cowl_rows],'trim',steer,around=32,steps=4,exponent=.40)
smooth_loft('Red_Handlebar_Cowl',cowl_rows,'red',steer,around=32,steps=4,exponent=.40)
def cowl_surface(x,y,offset=0):
    for i,(a,b) in enumerate(zip(cowl_rows,cowl_rows[1:])):
        if a[0]<=y<=b[0]: _,w,bot,top=interp(cowl_rows,i+(y-a[0])/(b[0]-a[0]));break
    else: _,w,bot,top=cowl_rows[-1]
    z=(top+bot)/2+(top-bot)/2*max(0,1-(abs(x)/w)**5)**.20
    return (x,y,z+offset)
surface_patch('Cowl_Center_Accent',[(-.013,.334),(.013,.334),(.018,.39),(0,.476),(-.018,.39)],cowl_surface,'red_dark',steer,offset=.002,cuts=3)
for s in [-1,1]:
    surface_patch('Handlebar_White_Flash',[(s*.060,.466),(s*.128,.43),(s*.151,.403),(s*.134,.429),(s*.062,.475)],cowl_surface,'white',steer,offset=.003,cuts=3)
    surface_patch('Handlebar_Black_Flash',[(s*.067,.467),(s*.128,.436),(s*.140,.421),(s*.123,.438)],cowl_surface,'black',steer,offset=.004,cuts=2)
panel('Instrument_Housing',[(-.095,.290,.96),(.095,.290,.96),(.072,.363,1.012),(-.072,.363,1.012)],(0,-.003,.001),'black',steer)
panel('Instrument_Face',[(-.08,.292,.966),(.08,.292,.966),(.062,.35,1.007),(-.062,.35,1.007)],(0,-.001,.001),'white',steer)
for i in range(10):
    x=-.067+i*.014
    panel('Speedo_Tick',[(x,.328,.994),(x+.0018,.328,.994),(x+.0018,.337,1.001),(x,.337,1.001)],(0,-.001,0),'black',steer)
panel('Speedometer_Needle',[(0,.299,.975),(.003,.301,.977),(-.035,.333,.998)],(0,-.002,0),'red',steer)
panel('Digital_Odometer',[(-.032,.294,.973),(.032,.294,.973),(.030,.311,.986),(-.030,.311,.986)],(0,-.002,.001),'screen',steer)
for s in [-1,1]:
    cylinder('Handlebar_Core',(s*.135,.366,.958),(s*.324,.364,.952),.011,'darkmetal',steer,20)
    cylinder('Handgrip',(s*.225,.365,.956),(s*.318,.364,.952),.017,'rubber',steer,24)
    for i in range(8):
        x=s*(.233+i*.01)
        cylinder('Grip_Rib',(x,.365,.956),(x+s*.0022,.365,.956),.0176,'black',steer,20)
    cylinder('Grip_End',(s*.318,.364,.952),(s*.326,.364,.952),.0178,'darkmetal',steer,20)
    box('Switch_Housing',(s*.205,.366,.958),(.038,.046,.041),'trim',steer,bevel=.008)
    box('Switch',(s*.205,.339,.962),(.012,.006,.010),'black',steer,bevel=.002)
    tube('Brake_Lever',[(s*.188,.40,.955),(s*.222,.421,.947),(s*.278,.416,.941),(s*.321,.402,.941)],.0045,'chrome',steer,sides=10)
    uv_sphere('Brake_Lever_Ball',(s*.321,.402,.941),(.007,.006,.006),'chrome',steer,16,8)
    cylinder('Mirror_Base',(s*.19,.377,.988),(s*.19,.378,1.016),.012,'black',steer,16)
    tube('Mirror_Stem',[(s*.19,.378,1.012),(s*.201,.382,1.064),(s*.244,.386,1.096),(s*.275,.40,1.12)],.0053,'darkmetal',steer,sides=10)
    uv_sphere('Oval_Mirror_Housing',(s*.281,.41,1.148),(.061,.016,.036),'trim',steer,32,16)
    uv_sphere('Mirror_Recess',(s*.281,.393,1.149),(.054,.0045,.032),'black',steer,28,12)
    uv_sphere('Mirror_Glass',(s*.281,.389,1.15),(.050,.0025,.029),'mirror',steer,28,12)
    # Bowed exposed control cables under the handlebar.
    tube('Control_Cable',[(s*.19,.366,.937),(s*.16,.337,.888),(s*.07,.32,.88),(s*.04,.36,.80)],.003,'black',steer,sides=6)

# CVT drive, fan shroud, bolts, suspension and tapered exhaust.
smooth_loft('Engine_Crankcase',[(-.61,.075,.23,.34),(-.49,.102,.225,.385),(-.34,.089,.24,.365),(-.27,.041,.25,.32)],'darkmetal',around=20,steps=3)
cvt=smooth_loft('CVT_Transmission',[(-.80,.018,.244,.29),(-.73,.048,.218,.325),(-.56,.054,.205,.338),(-.37,.049,.219,.33),(-.29,.025,.242,.30)],'trim',around=20,steps=4);cvt.location.x=-.107
for y,z in [(-.72,.29),(-.58,.31),(-.39,.29),(-.43,.235)]:
    cylinder('CVT_Bolt',(-.158,y,z),(-.164,y,z),.005,'darkmetal',segments=8)
cylinder('Cooling_Fan_Shroud',(.092,-.365,.324),(.134,-.365,.324),.069,'trim',segments=32)
cylinder('Fan_Recess',(.135,-.365,.324),(.137,-.365,.324),.055,'black',segments=32)
cylinder('Fan_Center',(.137,-.365,.324),(.146,-.365,.324),.016,'darkmetal',segments=20)
for i in range(16):
    t=2*pi*i/16
    tube('Fan_Grille',[(.140,-.365+.019*sin(t),.324+.019*cos(t)),(.139,-.365+.049*sin(t+.25),.324+.049*cos(t+.25))],.0022,'darkmetal',sides=5)
tube('Exhaust_Header',[(.08,-.29,.26),(.14,-.34,.208),(.177,-.46,.224),(.185,-.56,.29)],.014,'darkmetal',sides=12)
muffler=smooth_loft('Black_Muffler',[(-.90,.021,.35,.396),(-.83,.044,.302,.42),(-.65,.052,.268,.395),(-.48,.034,.252,.333),(-.43,.012,.27,.305)],'black',around=24,steps=4);muffler.location.x=.171
shield=smooth_loft('Exhaust_Heat_Shield',[(-.865,.012,.361,.396),(-.78,.026,.319,.413),(-.62,.03,.278,.379),(-.50,.015,.278,.326)],'darkmetal',around=16,steps=4);shield.location.x=.207
for y,z in [(-.56,.322),(-.78,.367)]:
    cylinder('Shield_Bolt_Recess',(.237,y,z),(.240,y,z),.010,'black',segments=16)
    cylinder('Shield_Bolt',(.241,y,z),(.244,y,z),.0045,'steel',segments=6)
cylinder('Exhaust_Tip',(.17,-.87,.373),(.17,-.92,.396),.017,'darkmetal',segments=20)
cylinder('Exhaust_Opening',(.17,-.922,.397),(.17,-.924,.398),.012,'black',segments=20)
shock_a=Vector((-.11,-.65,.31));shock_b=Vector((-.116,-.54,.575));d=(shock_b-shock_a).normalized();u=Vector((1,0,0));v=d.cross(u).normalized()
cylinder('Shock_Damper',shock_a,shock_b,.012,'chrome',segments=20)
points=[tuple(shock_a.lerp(shock_b,.12+.75*i/100)+.023*(cos(i/100*2*pi*8)*u+sin(i/100*2*pi*8)*v)) for i in range(101)]
tube('Rear_Shock_Spring',points,.0035,'darkmetal',sides=6)
tube('Rear_Swingarm',[(-.066,-.30,.27),(-.066,-.48,.242),(-.066,-.6275,.2588)],.022,'darkmetal',sides=8)
tube('Side_Stand_Folded',[(-.127,-.22,.245),(-.15,-.40,.203),(-.14,-.51,.221)],.009,'darkmetal',sides=8)

# Rear lights and practical mounting plates.
panel('Rear_Mudguard',[(-.066,-.83,.69),(.066,-.83,.69),(.052,-.973,.33),(-.052,-.973,.33)],(0,.013,0),'trim')
panel('Tail_Light_Seal',[(-.113,-.879,.732),(.113,-.879,.732),(.075,-.958,.668),(-.075,-.958,.668)],(0,.012,0),'black')
panel('Tail_Light',[(-.099,-.895,.725),(.099,-.895,.725),(.065,-.962,.682),(-.065,-.962,.682)],(0,.004,0),'tail',lights['Brake_Light'])
for s in [-1,1]:
    panel('Rear_Clear_Indicator',[(s*.10,-.894,.73),(s*.13,-.86,.708),(s*.11,-.915,.679),(s*.074,-.95,.684)],(s*.001,-.003,0),'lens',lights['Indicator_Rear_L' if s<0 else 'Indicator_Rear_R'])
box('Front_Display_Plate',(0,.551,.872),(.212,.010,.063),'black',bevel=.003,rot=(math.radians(20),0,0))
flat_text('Front_Honda','HONDA',(0,.559,.875),.030,(pi/2+math.radians(20),0,pi),'white')
box('Rear_Number_Plate',(0,-.953,.525),(.21,.010,.072),'black',bevel=.003,rot=(math.radians(-17),0,0))
flat_text('Rear_Plate_Text','B 110 SIM',(0,-.963,.527),.026,(pi/2-math.radians(17),0,0),'white')

for name,loc in [('Socket_Rider',(0,-.28,.742)),('Socket_Passenger',(0,-.65,.785)),('Socket_Camera_Rider',(0,-.11,1.30)),('Socket_Grip_L',(-.27,.365,.956)),('Socket_Grip_R',(.27,.365,.956))]:
    empty(name,loc,steer if 'Grip' in name else root)
col=box('COLLIDER_Body',(0,-.15,.48),(.38,1.15,.58),'trim',None,bevel=.04)
move_collection(col,helpers);col.display_type='WIRE';col.hide_render=True;helpers.hide_viewport=True


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
    slots=list(joined.data.materials)
    pm=[slots[p.material_index] for p in joined.data.polygons]
    joined.data.materials.clear()
    joined.data.materials.append(material); joined.data.materials.append(glass)
    for p,m in zip(joined.data.polygons,pm): p.material_index=1 if m==glass else 0
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
        mod=o.modifiers.new('LOD1 reduction','DECIMATE'); mod.ratio=.38
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
scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.render.resolution_x=1400; scene.render.resolution_y=1050; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.6,.65,.73,1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.45
scene.view_settings.view_transform='AgX'
groundmat=bpy.data.materials.new('Studio_Ground'); groundmat.diffuse_color=(.63,.65,.68,1); groundmat.use_nodes=True
groundmat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.63,.65,.68,1)
groundmat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.84
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008)); ground=bpy.context.object; ground.name='Studio_Ground'; ground.data.materials.append(groundmat); move_collection(ground,stage)
def area(name,loc,power,size,color):
    da=bpy.data.lights.new(name,'AREA'); da.energy=power; da.shape='DISK'; da.size=size; da.color=color
    o=bpy.data.objects.new(name,da); stage.objects.link(o); o.location=loc; o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-2,2.8,4),400,3.0,(.83,.91,1))
area('Fill',(3,1,2.4),260,2.5,(1,.91,.81))
area('Rim',(0,-3,3),600,2,(.63,.78,1))
ca=bpy.data.cameras.new('Asset_Camera'); camera=bpy.data.objects.new('Asset_Camera',ca); stage.objects.link(camera); scene.camera=camera
ca.type='PERSP'; ca.ortho_scale=2.4; ca.lens=58
def camera_view(loc,target=(0,-.04,.59),scale=2.4):
    camera.location=loc; camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler(); ca.ortho_scale=scale
camera_view((2.6,3.0,1.68),scale=2.35)

for area_ui in bpy.context.screen.areas if bpy.context.screen else []:
    if area_ui.type=='VIEW_3D':
        area_ui.spaces.active.region_3d.view_distance=2.65
        area_ui.spaces.active.region_3d.view_location=Vector((0,0,.57))
        area_ui.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
        area_ui.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT'); root.select_set(True); bpy.context.view_layer.objects.active=root
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'beat_scooter.blend'))
summary={'lod0_triangles':triangles,'lod1_triangles':lod_triangles,'authoring_meshes':sum(o.type=='MESH' for o in asset_objects),'materials':2,'atlas_size':'512x512','wheelbase_m':1.255,'front_wheel_radius_m':.2498,'rear_wheel_radius_m':.2588,'blender_forward':'+Y','glb_forward':'-Z','glb_up':'+Y'}
(OUT/'asset_stats.json').write_text(json.dumps(summary,indent=2)+'\n')
print('ASSET_STATS',json.dumps(summary),flush=True)
for name,loc,scale in [('hero',(2.6,3.0,1.68),2.35),('side',(3.5,0,1.0),2.18),('rear',(-2.6,-3.0,1.6),2.35),('front',(0,4,1.23),1.85)]:
    ca.type='ORTHO' if name in ['side','front'] else 'PERSP'
    camera_view(loc,scale=scale)
    scene.render.filepath=str(OUT/'previews'/(name+'.png'))
    bpy.ops.render.render(write_still=True)
print('COMPLETE',flush=True)
