"""Original lightweight Indonesian traffic vehicles. Blender 5.x, meters, +Y forward.
Run Blender --background --factory-startup --python build.py [-- model_id ...]
Six independently shaped vehicles; references guide silhouettes, not exact replicas.
"""
import bpy, bmesh, json, math, sys
from pathlib import Path
from math import sin, cos, pi, sqrt
from mathutils import Vector

OUT=Path(__file__).resolve().parent
CONFIGS=[
 dict(id='sport_scooter',name='Sport 160',kind='scooter',style='sport',colors=[('cobalt','245DA5'),('crimson','A92936')],length=1.93,wb=1.277,radius=.258),
 dict(id='retro_scooter',name='Retro 110',kind='scooter',style='retro',colors=[('mint','67B7AA'),('terracotta','B66C4A')],length=1.86,wb=1.25,radius=.245),
 dict(id='maxi_scooter',name='Touring 155',kind='scooter',style='maxi',colors=[('slate','546679'),('pearl','D5DBD9')],length=2.03,wb=1.35,radius=.263),
 dict(id='city_hatchback',name='Kota Hatch',kind='car',style='hatch',colors=[('sunflower','E3B534'),('coral','C74F39')],length=3.81,width=1.68,height=1.485,wb=2.405,radius=.293),
 dict(id='family_mpv',name='Keluarga MPV',kind='car',style='mpv',colors=[('pearl','DCE1DD'),('deep_blue','254859')],length=4.395,width=1.73,height=1.70,wb=2.75,radius=.32),
 dict(id='utility_pickup',name='Niaga Pickup',kind='car',style='pickup',colors=[('silver','A3B1B4'),('forest','42685A')],length=4.15,width=1.675,height=1.825,wb=2.205,radius=.288),
]
PALETTE={
 'rubber':('181F26',.86,.0,0), 'trim':('29323B',.65,.05,0),
 'glass':('233D4D',.21,.40,0), 'chrome':('C4D5DD',.24,.85,0),
 'alloy':('718591',.32,.75,0), 'black':('0B141E',.5,.12,0),
 'seat':('35353A',.83,.0,0), 'brown':('70503D',.78,.0,0),
 'cream':('E6DDC5',.38,.12,0), 'gold':('A69462',.38,.65,0),
 'headlamp':('DBF4F9',.21,.32,.22), 'tail':('C9283C',.27,.15,.10),
 'amber':('F4A741',.33,.12,.12), 'white':('DBE2DF',.55,.05,0),
 'vent':('32414B',.7,.15,0), 'screen':('6FBDC0',.35,.15,.05),
}
KEYS=list(PALETTE)
def linear(v): return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
def color(h): return tuple(linear(int(h[i:i+2],16)/255) for i in (0,2,4))
def attach(o,p):
    bpy.context.view_layer.update(); m=o.matrix_world.copy(); o.parent=p; o.matrix_world=m
def move(o,col):
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o)
def empty(name,loc=(0,0,0),parent=None):
    o=bpy.data.objects.new(name,None); MODEL.objects.link(o); o.location=loc
    o.empty_display_type='PLAIN_AXES'; o.empty_display_size=.1
    if parent: attach(o,parent)
    return o
def finish(o,key='paint',parent=None,smooth=False):
    move(o,MODEL)
    if o.type=='MESH':
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        for e in bm.edges:
            if e.is_manifold: e.smooth=e.calc_face_angle(0)<math.radians(48)
        bm.to_mesh(o.data);bm.free()
        o.data.materials.clear();o.data.materials.append(PAINT if key=='paint' else ATLAS)
        uv=o.data.uv_layers.new(name='UVMap') if not o.data.uv_layers else o.data.uv_layers.active
        idx=KEYS.index(key) if key!='paint' else 0
        u=(idx%4+.5)/4;v=(idx//4+.5)/4
        for p in o.data.polygons:
            p.use_smooth=smooth
            for j,li in enumerate(p.loop_indices):
                a=j*2*pi/len(p.loop_indices);uv.data[li].uv=(u+.075*cos(a),v+.075*sin(a))
    attach(o,parent or BODY)
    return o
def mesh(name,vs,fs,key='paint',parent=None,smooth=False):
    d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update()
    o=bpy.data.objects.new(name,d);MODEL.objects.link(o);return finish(o,key,parent,smooth)
def box(name,loc,dim,key='paint',parent=None,bevel=.01,segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dim
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Manufactured edges','BEVEL');mod.width=bevel;mod.segments=segments
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o,key,parent,False)
def cyl(name,a,b,r,key='alloy',parent=None,n=12,r2=None):
    a,b=Vector(a),Vector(b);d=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r,radius2=r if r2 is None else r2,depth=d.length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,key,parent,True)
def tube(name,pts,r,key='trim',parent=None,n=6):
    pts=[Vector(p) for p in pts];vs=[];fs=[]
    for i,p in enumerate(pts):
        d=(pts[min(len(pts)-1,i+1)]-pts[max(0,i-1)]).normalized();u=d.cross(Vector((1,0,0)))
        if u.length<.01:u=d.cross(Vector((0,1,0)))
        u.normalize();v=d.cross(u)
        for j in range(n):vs.append(tuple(p+r*(cos(2*pi*j/n)*u+sin(2*pi*j/n)*v)))
    for i in range(len(pts)-1):
        for j in range(n):fs.append((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j))
    fs.extend([tuple(range(n-1,-1,-1)),tuple((len(pts)-1)*n+j for j in range(n))])
    return mesh(name,vs,fs,key,parent,True)
def sphere(name,loc,scale,key='paint',parent=None,n=20,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=n,ring_count=rings,radius=1,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,key,parent,True)
def panel(name,pts,depth,key='paint',parent=None):
    n=len(pts);vs=pts+[tuple(Vector(p)+Vector(depth)) for p in pts]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,key,parent)
def catmull(rows,t):
    i=min(len(rows)-2,max(0,int(t)));u=max(0,min(1,t-i))
    a,b,c,d=rows[max(0,i-1)],rows[i],rows[i+1],rows[min(len(rows)-1,i+2)]
    return [.5*((2*b[j])+(-a[j]+c[j])*u+(2*a[j]-5*b[j]+4*c[j]-d[j])*u*u+(-a[j]+3*b[j]-3*c[j]+d[j])*u**3) for j in range(len(b))]
def loft(name,rows,key='paint',parent=None,around=16,steps=2,exp=.8,vertical=False):
    vs=[];fs=[];count=(len(rows)-1)*steps+1
    for i in range(count):
        axis,w,b,t=catmull(rows,i/steps);w=max(.003,w)
        for j in range(around):
            a=2*pi*j/around;x=w*math.copysign(abs(cos(a))**exp,cos(a));v=(b+t)/2+(t-b)/2*math.copysign(abs(sin(a))**exp,sin(a))
            vs.append((x,v,axis) if vertical else (x,axis,v))
    for i in range(count-1):
        for j in range(around):fs.append((i*around+j,i*around+(j+1)%around,(i+1)*around+(j+1)%around,(i+1)*around+j))
    fs.extend([tuple(range(around-1,-1,-1)),tuple((count-1)*around+j for j in range(around))])
    return mesh(name,vs,fs,key,parent,True)
def ring(name,c,profile,key,parent=None,n=28):
    cx,cy,cz=c;vs=[];fs=[];m=len(profile)
    for j in range(n):
        a=j*2*pi/n
        for x,r in profile:vs.append((cx+x,cy+r*sin(a),cz+r*cos(a)))
    for j in range(n):
        for k in range(m):fs.append((j*m+k,((j+1)%n)*m+k,((j+1)%n)*m+(k+1)%m,j*m+(k+1)%m))
    return mesh(name,vs,fs,key,parent,True)
def text(name,content,loc,size,rot,key='white',parent=None):
    d=bpy.data.curves.new(name,'FONT');d.body=content;d.size=size;d.align_x='CENTER';d.align_y='CENTER';d.resolution_u=2
    o=bpy.data.objects.new(name,d);MODEL.objects.link(o);o.location=loc;o.rotation_euler=rot
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH');return finish(o,key,parent)
def patch(name,coords,project,key,parent=None,cuts=2):
    bm=bmesh.new();v=[bm.verts.new((x,y,0)) for x,y in coords];bm.faces.new(v)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    if cuts:bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=cuts,use_grid_fill=True)
    bm.verts.index_update();vs=[project(v.co.x,v.co.y) for v in bm.verts];fs=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
    return mesh(name,vs,fs,key,parent,True)

def wheel(name,center,r,width,parent,car=False,retro=False):
    x,y,z=center;w=width/2;rr=r*.67
    ring(name+'_Tire',center,[(-w*.75,rr),(-w,rr*1.10),(-w,r*.87),(-w*.70,r),(.70*w,r),(w,r*.87),(w,rr*1.1),(.75*w,rr)],'rubber',parent,n=24)
    rim='cream' if retro else 'alloy'
    ring(name+'_Rim',center,[(-w*.87,rr*.88),(-w*.94,rr), (w*.94,rr),(w*.87,rr*.88)],rim,parent,n=24)
    cyl(name+'_Hub',(x-w*.85,y,z),(x+w*.85,y,z),rr*.24,rim,parent,n=12)
    for s in [-1,1]:
        xx=x+s*w*.82
        for i in range(5 if not car else 6):
            a=2*pi*i/(5 if not car else 6)
            pts=[(xx,y+rr*.19*sin(a),z+rr*.19*cos(a)),(xx,y+rr*.89*sin(a+.13),z+rr*.89*cos(a+.13))]
            cyl(name+'_Spoke',*pts,rr*.07,rim,parent,n=5)
        if car:
            cyl(name+'_Center_Cap',(xx,y,z),(xx+s*.004,y,z),rr*.27,'chrome',parent,n=12)
    # Sparse raised grooves read as tread at traffic distance.
    if not car:
        for i in range(18):
            a=2*pi*i/18
            tube(name+'_Tread',[(x-w*.55,y+r*sin(a-.04),z+r*cos(a-.04)),(x,y+(r+.001)*sin(a),z+(r+.001)*cos(a)),(x+w*.55,y+r*sin(a-.04),z+r*cos(a-.04))],.0016,'black',parent,n=3)
    if not car:
        cyl(name+'_Rotor',(x-w*1.03,y,z),(x-w*.95,y,z),rr*.78,'chrome',parent,n=20)
        cyl(name+'_Rotor_Center',(x-w*1.05,y,z),(x-w*1.04,y,z),rr*.32,'trim',parent,n=12)

def mudguard(cy,cz,r,width,parent,key='paint'):
    vs=[];fs=[];steps=13
    for i in range(steps):
        a=-.85+1.95*i/(steps-1)
        for j in range(5):
            u=-1+j/2;rr=r+.035-.014*u*u;vs.append((u*width/2,cy+rr*sin(a),cz+rr*cos(a)))
    for i in range(steps-1):
        for j in range(4):fs.append((i*5+j,i*5+j+1,(i+1)*5+j+1,(i+1)*5+j))
    o=mesh('Front_Fender',vs,fs,key,parent,True)
    mod=o.modifiers.new('Fender edge','SOLIDIFY');mod.thickness=.006;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)

def scooter(cfg):
    kind=cfg['style'];retro=kind=='retro';maxi=kind=='maxi';r=cfg['radius'];wb=cfg['wb'];fy=wb/2;ry=-wb/2
    steering=empty('Steering_Pivot',(0,fy-.18,.88),ROOT);fw=empty('Wheel_Front',(0,fy,r),steering);rw=empty('Wheel_Rear',(0,ry,r),ROOT)
    wheel('Front',(0,fy,r),r,.105 if maxi else .095,fw,retro=retro)
    wheel('Rear',(0,ry,r),r,.115 if maxi else .10,rw,retro=retro)
    for s in [-1,1]:
        cyl('Fork_Lower',(s*.065,fy,r),(s*.065,fy-.04,.53),.022,'alloy',steering)
        cyl('Fork_Upper',(s*.065,fy-.04,.49),(s*.065,fy-.18,.90),.014,'chrome',steering)
    mudguard(fy,r,r,.16 if retro else .15,steering)
    loft('Lower_Frame',[(-.60,.12,.22,.35),(-.3,.15,.18,.30),(.14,.17,.17,.31),(.43,.13,.22,.39)],'trim',around=12)
    loft('Floorboard',[(-.28,.13,.285,.303),(-.10,.17,.275,.300),(.18,.17,.275,.30),(.35,.13,.30,.34)],'brown' if retro else 'rubber',around=12)
    for s in [-1,1]:
        for i in range(8):box('Foot_Grip',(s*.1,-.12+i*.046,.305),(.065,.007,.003),'trim',bevel=.001,segments=1)
        tube('Floor_Trim',[(s*.13,-.24,.305),(s*.174,-.07,.302),(s*.174,.20,.30),(s*.13,.36,.33)],.005,'chrome' if retro else 'alloy')
    back=-.94 if maxi else -.88
    loft('Seat_Undertray',[(-.185,.048,.39,.68),(-.13,.066,.38,.67),(-.075,.027,.43,.62)],'trim',around=12)
    if retro:
        rear_shell=loft('Rounded_Rear_Body',[(-.89,.05,.60,.74),(-.77,.155,.44,.73),(-.56,.19,.36,.71),(-.32,.15,.34,.66),(-.16,.075,.40,.63)],around=20,steps=3,exp=1)
        loft('Saddle',[(-.90,.055,.75,.78),(-.76,.15,.75,.80),(-.49,.16,.71,.78),(-.22,.13,.69,.755),(-.14,.06,.70,.74)],'brown',around=16,steps=2)
        ar=[(.31,.10,.32,.47),(.52,.19,.25,.63),(.78,.20,.23,.66),(.92,.14,.25,.57),(.98,.06,.29,.46)]
    elif maxi:
        rear_shell=loft('Swept_Rear_Panels',[(-.98,.055,.64,.76),(-.82,.18,.51,.78),(-.55,.21,.39,.73),(-.30,.16,.33,.67),(-.13,.08,.38,.64)],around=12,steps=2,exp=.7)
        loft('Stepped_Saddle',[(-.96,.06,.78,.82),(-.82,.17,.77,.835),(-.60,.185,.72,.83),(-.45,.18,.70,.78),(-.25,.14,.69,.77),(-.13,.055,.69,.74)],'seat',around=16)
        loft('Center_Tunnel',[(-.20,.09,.29,.67),(0,.08,.27,.47),(.28,.11,.28,.56),(.45,.14,.34,.75)],'trim',around=12)
        ar=[(.30,.13,.28,.53),(.52,.24,.25,.76),(.75,.28,.27,.79),(.95,.23,.27,.64),(1.03,.17,.27,.54)]
    else:
        rear_shell=loft('Angular_Rear_Panels',[(-.93,.035,.67,.76),(-.80,.15,.55,.77),(-.57,.19,.43,.71),(-.30,.14,.35,.67),(-.15,.07,.38,.63)],around=12,steps=2,exp=.6)
        loft('Stepped_Saddle',[(-.90,.04,.77,.80),(-.76,.15,.76,.82),(-.53,.16,.72,.79),(-.32,.14,.69,.77),(-.15,.06,.68,.74)],'seat',around=16)
        ar=[(.30,.09,.32,.50),(.50,.18,.25,.69),(.73,.23,.25,.70),(.91,.21,.25,.58),(.98,.13,.30,.48)]
    apron=loft('Front_Apron',ar,around=20 if retro else 12,steps=2,vertical=True,exp=1 if retro else .65)
    bpy.context.view_layer.update()
    def on_side(s,y,z,offset=.005):
        hit,p,_,_=rear_shell.ray_cast(Vector((s*1,y,z)),Vector((-s,0,0)))
        return (p.x+s*offset,y,z) if hit else (s*.17,y,z)
    def on_front(x,z,offset=.007):
        hit,p,_,_=apron.ray_cast(Vector((x,2,z)),Vector((0,-1,0)))
        return (x,p.y+offset,z) if hit else (x,.60,z)
    # Inner apron is inset on the rider-facing side.
    loft('Inner_Legshield',[(.31,.08,.29,.33),(.52,.17,.23,.28),(.76,.18,.21,.26),(.94,.12,.27,.32)],'brown' if retro else 'trim',around=12,vertical=True)
    for s in [-1,1]:
        if not retro:
            patch('Side_Accent',[(-.73,.64),(-.56,.57),(-.32,.48),(-.51,.60)],lambda y,z,s=s:on_side(s,y,z),'gold' if maxi else 'chrome',cuts=3)
        else:
            tube('Rear_Chrome_Trim',[(s*.11,-.82,.59),(s*.186,-.59,.49),(s*.16,-.35,.46)],.006,'chrome')
        tube('Grab_Rail',[(s*.12,-.63,.79),(s*.14,back-.015,.815),(s*.08,back-.06,.81)],.013,'chrome' if retro else 'trim')
    loft('Engine_Case',[(ry-.22,.04,.28,.38),(ry-.08,.09,.25,.41),(ry+.22,.08,.25,.40),(ry+.31,.02,.29,.35)],'trim',around=12)
    # The exhaust sits on the right, transmission cover on the left.
    o=sphere('Exhaust_Silencer',(.17,ry+.03,.31),(.07,.30,.075),'trim',n=16,rings=8)
    o=sphere('Exhaust_Heat_Shield',(.225,ry+.03,.33),(.016,.24,.045),'alloy',n=12,rings=6)
    cyl('Exhaust_Tip',(.17,ry-.27,.31),(.17,ry-.32,.325),.027,'black')
    o=sphere('CVT_Cover',(-.13,ry+.11,.315),(.045,.27,.095),'vent',n=16,rings=8)
    for s in [-1,1] if maxi else [-1]:
        cyl('Shock_Rod',(s*.10,ry+.05,.30),(s*.10,ry+.16,.62),.011,'chrome')
        tube('Shock_Spring',[(s*.10+.026*cos(j*2*pi/7),ry+.05+.11*j/42+.026*sin(j*2*pi/7),.32+.27*j/42) for j in range(43)],.006,'gold' if maxi else 'alloy',n=5)
    pod_y=fy-.23;pod_z=1.005
    cyl('Handlebar',(-.28,pod_y,pod_z),(.28,pod_y,pod_z),.016,'alloy',steering)
    sphere('Handlebar_Cowl',(0,pod_y,pod_z),(.20,.10,.075),'paint',steering,n=16,rings=8)
    box('Instrument_Panel',(0,pod_y-.071,pod_z+.048),(.115,.052,.011),'black',steering,bevel=.009)
    box('Digital_Display',(0,pod_y-.072,pod_z+.055),(.085,.034,.005),'screen',steering,bevel=.004)
    for s in [-1,1]:
        cyl('Grip',(s*.22,pod_y,pod_z),(s*.32,pod_y,pod_z),.018,'rubber',steering)
        tube('Brake_Lever',[(s*.21,pod_y+.035,pod_z),(s*.29,pod_y+.045,pod_z),(s*.33,pod_y+.025,pod_z)],.005,'chrome',steering,n=5)
        tube('Mirror_Stem',[(s*.19,pod_y,pod_z+.045),(s*.23,pod_y+.025,1.13),(s*.30,pod_y+.025,1.19)],.006,'chrome' if retro else 'trim',steering)
        sphere('Mirror_Housing',(s*.30,pod_y+.025,1.19),(.047 if retro else .060,.021,.047 if retro else .038),'trim',steering,n=12,rings=6)
        sphere('Mirror_Face',(s*.30,pod_y+.005,1.19),(.042 if retro else .054,.005,.042 if retro else .032),'chrome',steering,n=12,rings=6)
        empty('Socket_Grip_'+('L' if s<0 else 'R'),(s*.27,pod_y,pod_z),steering)
    if retro:
        sphere('Oval_Headlamp_Bezel',(0,.645,.81),(.105,.035,.13),'chrome',LIGHTS['Headlights'],n=24,rings=12)
        sphere('Oval_Headlamp_Lens',(0,.674,.81),(.087,.015,.111),'headlamp',LIGHTS['Headlights'],n=20,rings=10)
        tube('Lamp_Divider',[(-.080,.687,.81),(.080,.687,.81)],.004,'chrome',LIGHTS['Headlights'])
        for s in [-1,1]:sphere('Front_Indicator',(s*.15,.612,.65),(.025,.019,.065),'amber',LIGHTS['Indicators'],n=12,rings=6)
    else:
        for s in [-1,1]:
            zz=.81 if maxi else .80
            coords=[(s*.035,zz-.045),(s*.18,zz+.035),(s*.18,zz+.075),(s*.065,zz+.025)]
            patch('Angular_Headlight',coords,on_front,'headlamp',LIGHTS['Headlights'],cuts=2)
            tube('DRL_Strip',[on_front(s*.04,zz+.05),on_front(s*.12,zz+.08),on_front(s*.19,zz+.10)],.006,'white',LIGHTS['Headlights'])
            patch('Front_Indicator',[(s*.14,zz-.06),(s*.18,zz-.03),(s*.18,zz-.015)],lambda x,z:on_front(x,z,.009),'amber',LIGHTS['Indicators'],cuts=1)
        if maxi:
            panel('Tinted_Windscreen',[(-.17,.54,.99),(.17,.54,.99),(.14,.37,1.21),(-.14,.37,1.21)],(0,-.008,0),'glass')
            for s in [-1,1]:cyl('Screen_Bolt',(s*.13,.55,1.02),(s*.13,.555,1.02),.006,'alloy',n=6)
        else:
            patch('Nose_Insert',[(-.045,.88),(0,.70),(.045,.88)],lambda x,z:on_front(x,z,.010),'black',cuts=3)
    box('Tail_Lamp',(0,back-.012,.72),(.17,.025,.045),'tail',LIGHTS['TailLights'],bevel=.01)
    box('Rear_Mudflap',(0,back+.025,.47),(.14,.025,.31),'trim',bevel=.006)
    box('Rear_Plate',(0,back-.01,.50),(.185,.014,.062),'black',bevel=.003)
    text('Registration','B 160 JLN',(0,back-.019,.50),.019,(pi/2,0,0))
    for s in [-1,1]:box('Rear_Indicator',(s*.105,back-.008,.68),(.035,.028,.026),'amber',LIGHTS['Indicators'],bevel=.006)
    empty('Socket_Rider',(0,-.28,.77 if not retro else .75),ROOT)
    empty('Socket_Passenger',(0,-.64,.82 if not retro else .79),ROOT)
    return [{'node':fw.name,'position':(0,fy,r),'radius':r},{'node':rw.name,'position':(0,ry,r),'radius':r}]

def car(cfg):
    pickup=cfg['style']=='pickup';mpv=cfg['style']=='mpv';L,W,H,wb,r=[cfg[k] for k in ['length','width','height','wb','radius']]
    hw=W/2;front=L/2;rear=-L/2;axles=[wb/2,-wb/2];wheelinfo=[]
    for yi,cy in enumerate(axles):
        for s in [-1,1]:
            pos=(s*(hw-.055),cy,r);tag=('F' if yi==0 else 'R')+('L' if s<0 else 'R')
            steer=empty('Steering_'+tag,pos,ROOT) if yi==0 else ROOT
            wp=empty('Wheel_'+tag,pos,steer);wheel('Wheel_'+tag,pos,r,.17 if pickup else .185,wp,car=True)
            wheelinfo.append({'node':wp.name,'position':pos,'radius':r})
    box('Underbody',(0,0,.28),(W*.72,L*.85,.18),'trim',bevel=.04)
    for cy in axles:cyl('Axle',(-hw+.1,cy,r),(hw-.1,cy,r),.035,'trim',n=10)
    bodyrear=.27 if pickup else rear
    belt=1.04 if pickup else H*.60
    def width(y):
        end=min((front-y)/.35,(y-bodyrear)/.24,1);return hw*(.91+.09*max(0,end))
    def top(y):
        if pickup:return belt
        return belt-(.14 if mpv else .09)*max(0,(y-.66)/(front-.66))
    ys=sorted(set([bodyrear+(front-bodyrear)*i/72 for i in range(73)]+[cy+d for cy in axles for d in [-r-.05,0,r+.05] if bodyrear<cy+d<front]))
    for s in [-1,1]:
        vs=[];fs=[]
        for y in ys:
            bottom=.24
            for cy in axles:
                d=y-cy;arch=r+.045
                if abs(d)<arch:bottom=max(bottom,r+sqrt(max(0,arch*arch-d*d)))
            ztop=top(y);ww=width(y)
            for u,f in [(0,.96),(.3,1),(.85,1),(1,.94)]:vs.append((s*ww*f,y,bottom+(ztop-bottom)*u))
        for i in range(len(ys)-1):
            for j in range(3):fs.append((i*4+j,i*4+j+1,(i+1)*4+j+1,(i+1)*4+j))
        mesh('Body_Side_Shell',vs,fs,'paint',smooth=True)
        for cy in axles:
            if cy<bodyrear:continue
            pts=[]
            for j in range(17):
                a=-pi/2+j*pi/16;y=cy+(r+.045)*sin(a);z=r+(r+.045)*cos(a)
                if bodyrear<y<front:pts.append((s*(width(y)+.003),y,z))
            tube('Wheel_Arch_Lip',pts,.012,'trim' if pickup else 'paint',n=5)
        if not pickup:
            tube('Rocker_Trim',[(s*(hw*.98),axles[1]+r+.06,.265),(s*hw,axles[0]-r-.07,.265)],.026,'trim',n=5)
    # Hood and rear deck close the top of the lower shell.
    vs=[];fs=[]
    for y in ys:
        for u in [-1,-.92,0,.92,1]:vs.append((width(y)*.94*u,y,top(y)+.022*(1-u*u)))
    for i in range(len(ys)-1):
        for j in range(4):fs.append((i*5+j,i*5+j+1,(i+1)*5+j+1,(i+1)*5+j))
    mesh('Hood_And_Deck',vs,fs,'paint',smooth=True)
    for y,isfront in [(front,True),(bodyrear,False)]:
        w=width(y);zt=top(y)
        contour=[(-w*f,y,.24+(zt-.24)*u) for u,f in [(0,.96),(.3,1),(.85,1),(1,.94)]]
        contour += [(w*f,y,.24+(zt-.24)*u) for u,f in [(1,.94),(.85,1),(.3,1),(0,.96)]]
        panel('Lower_Fascia',contour,(0,-.03 if isfront else .03,0))
    # Cabin: actual sloping roof and windows, with opaque dark glass for cheap traffic rendering.
    if pickup:rows=[(.28,hw*.94,belt+.01),(.40,hw*.88,H-.025),(1.60,hw*.88,H-.025),(front-.025,hw*.94,belt+.015)]
    elif mpv:rows=[(rear+.12,hw*.94,belt+.025),(rear+.68,hw*.79,H-.015),(.04,hw*.79,H),(.75,hw*.94,belt+.025)]
    else:rows=[(rear+.10,hw*.94,belt+.03),(rear+.78,hw*.77,H-.025),(-.02,hw*.78,H),(.65,hw*.94,belt+.02)]
    def roof(y):
        y=max(rows[0][0],min(rows[-1][0],y))
        for a,b in zip(rows,rows[1:]):
            if a[0]<=y<=b[0]:
                t=(y-a[0])/(b[0]-a[0]);return a[1]*(1-t)+b[1]*t,a[2]*(1-t)+b[2]*t
        return rows[-1][1:]
    def side(s,y,t,offset=.004):
        w,z=roof(y);return (s*(hw*.94*(1-t)+w*t+offset),y,belt+(z-belt)*t)
    vs=[];fs=[]
    for y,w,z in rows:
        vs.extend([(-hw*.94,y,belt),(-w,y,z-.035),(-w*.9,y,z),(w*.9,y,z),(w,y,z-.035),(hw*.94,y,belt)])
    for i in range(len(rows)-1):
        for j in range(5):fs.append((i*6+j,i*6+j+1,(i+1)*6+j+1,(i+1)*6+j))
    mesh('Cabin_Structure',vs,fs,'paint' if (mpv or pickup) else 'black',smooth=False)
    if pickup:
        windows=[[(.39,.13),(.47,.88),(1.52,.88),(front-.19,.13)]]
    elif mpv:
        windows=[[(.63,.12),(.00,.88),(-.45,.88),(-.45,.12)],[(-.52,.12),(-.52,.88),(-1.22,.88),(-1.22,.12)],[(-1.30,.12),(-1.30,.88),(-1.57,.85),(rear+.22,.12)]]
    else:
        windows=[[(.54,.12),(-.06,.87),(-.39,.87),(-.39,.12)],[(-.46,.12),(-.46,.87),(-1.01,.87),(rear+.20,.12)]]
    for s in [-1,1]:
        for coords in windows:patch('Side_Window',coords,lambda y,t,s=s:side(s,y,t),'glass',cuts=1)
        tube('Window_Sill',[side(s,rows[0][0]+.10,.08,.007),side(s,rows[-1][0]-.08,.08,.007)],.012,'chrome' if mpv else 'trim',n=5)
        for y in ([.35] if pickup else [-.43,-1.37 if mpv else -1.15]):
            z=top(y);tube('Door_Seam',[(s*(width(y)+.003),y,.32),(s*(width(y)+.008),y,z-.08),(s*(hw*.94+.009),y,belt+.025)],.004,'trim',n=4)
        for y in ([.61] if pickup else [-.29,-1.20 if mpv else -1.13]):
            box('Door_Handle',(s*(width(y)+.010),y,belt-.105),(.021,.16,.025),'trim' if not mpv else 'chrome',bevel=.008,segments=1)
        my=1.70 if pickup else .51;mz=belt+.11
        tube('Mirror_Arm',[(s*hw*.94,my,mz),(s*(hw+.095),my+.015,mz)],.025,'trim',n=6)
        box('Door_Mirror',(s*(hw+.11),my,mz+.035),(.20,.12,.095),'trim' if pickup else 'paint',bevel=.035)
        box('Mirror_Glass',(s*(hw+.11),my-.063,mz+.035),(.155,.006,.060),'chrome',bevel=.018)
    def topglass(x,y):
        w,z=roof(y);return (x*w,y,z+.008)
    a,b=rows[-2],rows[-1];ya=a[0]+.04;yb=b[0]-.075
    patch('Windshield',[(-.86,ya),(.86,ya),(.90,yb),(-.90,yb)],topglass,'glass',cuts=3)
    for s in [-1,1]:
        x=s*.33;y=yb-.09;w,z=roof(y)
        tube('Wiper',[(x,y,z+.018),(x+s*.16,y-.22,roof(y-.22)[1]+.018)],.008,'trim',n=5)
    if not pickup:
        ya=rows[0][0]+.08;yb=rows[1][0]-.055
        patch('Rear_Window',[(-.84,ya),(.84,ya),(.84,yb),(-.84,yb)],topglass,'glass',cuts=3)
        tube('Rear_Wiper',[(0,ya+.06,roof(ya+.06)[1]+.015),(.32,ya+.18,roof(ya+.18)[1]+.015)],.009,'trim',n=5)
        box('Roof_Spoiler',(0,rows[1][0]-.02,H-.02),(W*.79,.18,.045),'paint' if mpv else 'black',bevel=.018)
        box('High_Brake_Light',(0,rows[1][0]-.107,H-.03),(.27,.014,.023),'tail',LIGHTS['TailLights'],bevel=.005)
    if pickup:
        # Open cargo deck, stamped side gates, hinges and load hooks.
        bedfront=.28;bedrear=rear;bedlen=bedfront-bedrear
        box('Cargo_Floor',(0,(bedfront+bedrear)/2,.725),(W,bedlen,.07),'alloy',bevel=.02)
        for i in range(11):box('Bed_Corrugation',(-W*.43+i*W*.086,(bedfront+bedrear)/2,.769),(.018,bedlen-.12,.015),'chrome',bevel=.004,segments=1)
        for s in [-1,1]:
            box('Cargo_Side_Gate',(s*(hw-.025),(bedfront+bedrear)/2,.94),(.05,bedlen,.35),'paint',bevel=.015)
            tube('Bed_Top_Rail',[(s*hw,bedrear,1.13),(s*hw,bedfront,1.13)],.025,'alloy',n=6)
            for i in range(4):
                y=bedrear+.22+i*(bedlen-.44)/3
                box('Gate_Stamp',(s*(hw+.004),y,.95),(.010,.39,.20),'alloy',bevel=.015)
                tube('Tie_Down_Hook',[(s*(hw+.015),y,.80),(s*(hw+.045),y,.76),(s*(hw+.05),y+.045,.79)],.008,'trim',n=5)
            for cy in axles:
                if cy<0:box('Rear_Mudflap',(s*(hw-.05),cy-r-.065,.23),(.22,.025,.24),'trim',bevel=.005)
        for y in [bedfront,bedrear]:box('Cargo_End_Gate',(0,y,.94),(W,.045,.35),'paint',bevel=.015)
        for s in [-1,1]:
            cyl('Tailgate_Hinge',(s*.48-.06,bedrear-.04,.80),(s*.48+.06,bedrear-.04,.80),.018,'chrome')
        tube('Cab_Load_Rack',[(-hw*.85,.32,1.14),(-hw*.85,.32,H),(hw*.85,.32,H),(hw*.85,.32,1.14)],.027,'alloy',n=6)
        box('Rear_Bumper',(0,rear-.015,.38),(W,.12,.12),'trim',bevel=.025)
        text('Tailgate_Badge','NIAGA',(0,rear-.027,.99),.085,(pi/2,0,0),'white')
    # Front grille, contrasting lower intake and modeled horizontal slats.
    fy=front+.025;fw=width(front)*.94
    gz=.68 if pickup else top(front)-.22
    panel('Front_Grille',[(-fw*.58,fy,gz-.10),(fw*.58,fy,gz-.10),(fw*.72,fy,gz+.10),(-fw*.72,fy,gz+.10)],(0,.018,0),'black')
    for i in range(3):box('Grille_Slat',(0,fy+.022,gz-.06+i*.058),(fw*1.20,.018,.018),'chrome' if (mpv or pickup) else 'alloy',bevel=.005,segments=1)
    box('Lower_Intake',(0,front+.018,.38),(W*.60,.035,.095),'black',bevel=.025)
    for s in [-1,1]:
        z=top(front)-(.13 if pickup else .085)
        pts=[(s*fw*.45,fy+.014,z-.045),(s*fw*.97,fy-.016,z-.065),(s*fw*.95,fy-.018,z+.065),(s*fw*.42,fy+.016,z+.045)]
        panel('Headlamp_Housing',pts,(0,.018,0),'chrome',LIGHTS['Headlights'])
        pts=[(s*fw*.48,fy+.037,z-.027),(s*fw*.91,fy+.009,z-.042),(s*fw*.91,fy+.009,z+.040),(s*fw*.46,fy+.037,z+.027)]
        panel('Headlamp_Lens',pts,(0,.003,0),'headlamp',LIGHTS['Headlights'])
        box('Front_Indicator',(s*fw*.93,fy+.022,z-.05),(.045,.015,.025),'amber',LIGHTS['Indicators'],bevel=.004)
        if not pickup:
            box('Fog_Recess',(s*fw*.80,front+.018,.41),(.15,.02,.12),'trim',bevel=.025)
            cyl('Fog_Lamp',(s*fw*.80,front+.025,.41),(s*fw*.80,front+.040,.41),.037,'headlamp',LIGHTS['Headlights'],n=12)
    if not pickup:
        for s in [-1,1]:
            box('Tail_Housing',(s*W*.38,rear-.019,belt-.13),(.20,.038,.20 if mpv else .26),'tail',LIGHTS['TailLights'],bevel=.025)
            box('Tail_White_Strip',(s*W*.38,rear-.041,belt-.13),(.17,.008,.035),'white',LIGHTS['TailLights'],bevel=.004)
            box('Rear_Indicator',(s*W*.38,rear-.046,belt-.075),(.15,.012,.033),'amber',LIGHTS['Indicators'],bevel=.004)
        box('Rear_Bumper_Trim',(0,rear-.016,.34),(W*.87,.06,.09),'trim',bevel=.03)
        text('Rear_Badge','KELUARGA' if mpv else 'KOTA',(0,rear-.037,belt-.16),.065,(pi/2,0,0),'chrome')
    else:
        for s in [-1,1]:
            box('Rear_Truck_Lamp',(s*.62,rear-.043,.58),(.20,.042,.10),'tail',LIGHTS['TailLights'],bevel=.007)
            box('Rear_Truck_Indicator',(s*.69,rear-.069,.58),(.055,.008,.075),'amber',LIGHTS['Indicators'],bevel=.003)
    for y,flip in [(front+.055,False),(rear-.07,True)]:
        box('Number_Plate',(0,y,.49 if not pickup else .43),(.40,.016,.115),'black',bevel=.007)
        text('Registration','B 2048 JLN',(0,y+(-.011 if flip else .011),.49 if not pickup else .43),.037,(pi/2,0,0 if flip else pi))
    empty('Socket_Driver',(.32,.2 if not pickup else 1.1,.93 if not pickup else 1.12),ROOT)
    empty('Socket_Passenger',(-.32,.2 if not pickup else 1.1,.93 if not pickup else 1.12),ROOT)
    return wheelinfo

def materials(cfg,folder):
    global ATLAS,PAINT
    ATLAS=bpy.data.materials.new('Traffic_Atlas_PBR');ATLAS.use_nodes=True
    bs=ATLAS.node_tree.nodes.get('Principled BSDF');bs.inputs['Coat Weight'].default_value=0
    for channel,socket in [('base_color','Base Color'),('roughness','Roughness'),('metallic','Metallic'),('emission','Emission Color')]:
        im=bpy.data.images.new('Traffic_'+channel,width=128,height=128,alpha=True)
        if channel in ['roughness','metallic']:im.colorspace_settings.name='Non-Color'
        data=[]
        for y in range(128):
            for x in range(128):
                hx,rough,metal,em=PALETTE[KEYS[y//32*4+x//32]]
                cc=color(hx)
                if channel=='roughness':cc=(rough,)*3
                elif channel=='metallic':cc=(metal,)*3
                elif channel=='emission':cc=tuple(v*em for v in cc)
                data.extend((*cc,1))
        im.pixels.foreach_set(data);im.file_format='PNG';im.filepath_raw=str(folder/'textures'/('traffic_'+channel+'.png'));im.save();im.pack()
        n=ATLAS.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;n.interpolation='Closest';ATLAS.node_tree.links.new(n.outputs['Color'],bs.inputs[socket])
    bs.inputs['Emission Strength'].default_value=1
    PAINT=bpy.data.materials.new('Body_Paint');PAINT.use_nodes=True
    p=PAINT.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.32;p.inputs['Metallic'].default_value=.22;p.inputs['Coat Weight'].default_value=.18
    set_paint(cfg['colors'][0][1])
def set_paint(h):
    c=(*color(h),1);PAINT.diffuse_color=c;PAINT.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=c

def triangles(objs):
    n=0
    for o in objs:
        if o.type=='MESH':o.data.calc_loop_triangles();n+=len(o.data.loop_triangles)
    return n
def consolidate():
    col=bpy.data.collections.new('EXPORT_TEMP');bpy.context.scene.collection.children.link(col);copies={}
    for old in MODEL.objects:
        cp=old.copy()
        if old.data:cp.data=old.data.copy()
        col.objects.link(cp);copies[old]=cp
    for old,cp in copies.items():cp.parent=copies.get(old.parent);cp.matrix_parent_inverse=old.matrix_parent_inverse.copy();cp.name=old.name+'_Export'
    for parent in [o for o in col.objects if o.type=='EMPTY']:
        kids=[o for o in col.objects if o.type=='MESH' and o.parent==parent]
        if not kids:continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in kids:o.select_set(True)
        bpy.context.view_layer.objects.active=kids[0];bpy.ops.object.join();o=bpy.context.object;o.name=parent.name.replace('_Export','')+'_Mesh'
        mats=list(o.data.materials);indices=[mats[p.material_index] for p in o.data.polygons]
        o.data.materials.clear();o.data.materials.append(ATLAS);o.data.materials.append(PAINT)
        for p,m in zip(o.data.polygons,indices):p.material_index=int(m==PAINT)
        bpy.context.scene.cursor.location=parent.matrix_world.translation;bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location=(0,0,0)
    return col
def export(cfg,folder):
    col=consolidate();stats={}
    for lod in [0,1]:
        if lod:
            for o in col.objects:
                if o.type=='MESH' and len(o.data.polygons)>40:
                    d=o.modifiers.new('Traffic_LOD','DECIMATE');d.ratio=.43;d.use_collapse_triangulate=True
                    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=d.name);o.data.validate()
        stats['lod'+str(lod)+'_triangles']=triangles(col.objects)
        for ci,(label,hx) in enumerate(cfg['colors']):
            set_paint(hx);bpy.ops.object.select_all(action='DESELECT')
            for o in col.objects:o.select_set(True)
            bpy.context.view_layer.objects.active=next(o for o in col.objects if o.parent is None)
            fn=cfg['id']+'_'+label+('_lod1' if lod else '')
            bpy.ops.export_scene.gltf(filepath=str(folder/(fn+'.glb')),export_format='GLB',use_selection=True,export_yup=True,export_extras=True,export_animations=False)
            if not lod and ci==0:
                bpy.ops.export_scene.fbx(filepath=str(folder/(cfg['id']+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,add_leaf_bones=False,bake_anim=False)
    stats['export_meshes']=sum(o.type=='MESH' for o in col.objects)
    for o in list(col.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(col);set_paint(cfg['colors'][0][1])
    return stats

def studio(cfg):
    stage=bpy.data.collections.new('STUDIO_excluded_from_exports');bpy.context.scene.collection.children.link(stage)
    sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=True
    sc.render.resolution_x=1100;sc.render.resolution_y=850;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
    sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1;sc.view_settings.view_transform='AgX'
    sc.world=bpy.data.worlds.new('StudioWorld');sc.world.use_nodes=True
    sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.65,.77,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.45
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));ground=bpy.context.object;move(ground,stage)
    mat=bpy.data.materials.new('StudioFloor');mat.diffuse_color=(.69,.72,.74,1);mat.use_nodes=True;mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.69,.72,.74,1);mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8;ground.data.materials.append(mat)
    carlike=cfg['kind']=='car';scale=2 if carlike else 1
    for name,loc,powr,size in [('Key',(-3,3.5,5),500,4),('Fill',(4,2,3),340,3),('Rim',(0,-4,4),650,3)]:
        d=bpy.data.lights.new(name,'AREA');d.energy=powr*scale*scale;d.shape='DISK';d.size=size*scale
        o=bpy.data.objects.new(name,d);stage.objects.link(o);o.location=Vector(loc)*scale;o.rotation_euler=(Vector((0,0,.65))-o.location).to_track_quat('-Z','Y').to_euler()
    d=bpy.data.cameras.new('Preview_Camera');cam=bpy.data.objects.new('Preview_Camera',d);stage.objects.link(cam);sc.camera=cam
    cam.location=(5.7,7.0,4.1) if carlike else (2.5,3.1,1.82);target=Vector((0,0,.75 if carlike else .60));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=5.8 if carlike else 2.65
    return sc,cam,target
def build(cfg):
    global MODEL,ROOT,BODY,LIGHTS
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
    folder=OUT/cfg['id'];(folder/'textures').mkdir(parents=True,exist_ok=True);(folder/'previews').mkdir(exist_ok=True)
    MODEL=bpy.data.collections.new('VEHICLE_authoring');bpy.context.scene.collection.children.link(MODEL)
    ROOT=empty(cfg['id']);BODY=empty('Body',parent=ROOT)
    ROOT['display_name']=cfg['name'];ROOT['units']='meters';ROOT['blender_forward']='+Y';ROOT['gltf_forward']='-Z';ROOT['paint_options']=json.dumps(cfg['colors'])
    ROOT['role']='Lightweight background traffic; fictional reference-inspired design'
    LIGHTS={name:empty(name,parent=ROOT) for name in ['Headlights','TailLights','Indicators']}
    materials(cfg,folder)
    wheels=scooter(cfg) if cfg['kind']=='scooter' else car(cfg)
    bpy.context.view_layer.update()
    pts=[o.matrix_world @ v.co for o in MODEL.objects if o.type=='MESH' for v in o.data.vertices]
    bounds={'min':[min(p[i] for p in pts) for i in range(3)],'max':[max(p[i] for p in pts) for i in range(3)]}
    stats=export(cfg,folder);stats.update(id=cfg['id'],name=cfg['name'],kind=cfg['kind'],colors=cfg['colors'],materials=2,atlas_size=128,bounds_m=bounds,wheels=wheels,blender_forward='+Y',glb_forward='-Z')
    sc,cam,target=studio(cfg)
    bpy.ops.object.select_all(action='DESELECT');ROOT.select_set(True);bpy.context.view_layer.objects.active=ROOT
    for area in bpy.context.screen.areas if bpy.context.screen else []:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=6 if cfg['kind']=='car' else 2.8;area.spaces.active.region_3d.view_location=target;area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();area.spaces.active.shading.type='MATERIAL'
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(cfg['id']+'.blend')))
    (folder/'asset_stats.json').write_text(json.dumps(stats,indent=2)+'\n')
    print('MODEL_EXPORTED',cfg['id'],json.dumps(stats),flush=True)
    for label,hx in cfg['colors']:
        set_paint(hx);sc.render.filepath=str(folder/'previews'/(label+'.png'));bpy.ops.render.render(write_still=True)
    set_paint(cfg['colors'][0][1]);cam.location.x*=-1;cam.location.y*=-1;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=str(folder/'previews'/'rear.png');bpy.ops.render.render(write_still=True)
    print('MODEL_COMPLETE',cfg['id'],flush=True)

if __name__=='__main__':
    ids=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    for cfg in CONFIGS:
        if not ids or cfg['id'] in ids:build(cfg)
    print('BUILD_COMPLETE',flush=True)
