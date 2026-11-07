"""Author the Avanza-inspired MPV visual replacement, without touching Unity.
Blender --background --factory-startup --python tools/vehicles/build_avanza.py
Uses the original project's mesh/atlas helpers. +Y forward, metres, four .32m wheels.
All delivered meshes are newly authored; no downloaded geometry/photo textures.
"""
import bpy, bmesh, importlib.util, json, math, hashlib
from pathlib import Path
from mathutils import Vector
from math import sin, cos, pi, sqrt
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/vehicles/avanza_inspired'
spec = importlib.util.spec_from_file_location('traffic_author', ROOT / 'assets/vehicles/indonesia_traffic_pack/build.py')
b = importlib.util.module_from_spec(spec); spec.loader.exec_module(b)
CFG = dict(id='family_mpv', name='Avanza-inspired MPV', kind='car', style='mpv', colors=[('silver','C5CACD'),('graphite','454E56')], length=4.395, width=1.73, height=1.70, wb=2.75, radius=.32)
# Retain the original traffic atlas layout exactly: existing shared URP materials work.

def make_wheel(tag, x, y, parent):
    c=(x,y,.32); w=.095; rr=.208
    b.ring(tag+'_Tyre',c,[(-w*.75,rr),(-w,rr*1.09),(-w,.285),(-.073,.316),(-.055,.32),(.055,.32),(.073,.316),(w,.285),(w,rr*1.09),(.75*w,rr)],'rubber',parent,n=36)
    b.ring(tag+'_Rim_Barrel',c,[(-w*.9,rr*.91),(-w*.92,rr),(w*.92,rr),(w*.9,rr*.91)],'alloy',parent,n=36)
    for s in [-1,1]:
        xx=x+s*.088
        b.cyl(tag+'_Brake_Disc',(xx-s*.026,y,.32),(xx-s*.020,y,.32),.168,'alloy',parent,n=24)
        b.cyl(tag+'_Dark_Wheel_Well',(xx-s*.012,y,.32),(xx-s*.010,y,.32),.193,'black',parent,n=32)
        # Five split machined spokes with a broad, swept face.
        for i in range(5):
            for split in [-1,1]:
                a=2*pi*i/5+split*.085
                yz=[(.052,a-.085),(.183,a+.035),(.199,a+.105),(.198,a+.18),(.166,a+.14),(.055,a+.075)]
                pts=[(xx,y+r*sin(t),.32+r*cos(t)) for r,t in yz]
                b.panel(tag+'_Machined_Spoke',pts,(s*.007,0,0),'chrome',parent)
        b.cyl(tag+'_Hub',(xx,y,.32),(xx+s*.009,y,.32),.053,'alloy',parent,n=20)
        b.cyl(tag+'_Hub_Cap',(xx+s*.010,y,.32),(xx+s*.013,y,.32),.026,'chrome',parent,n=16)
        for i in range(5):
            a=i*2*pi/5
            b.cyl(tag+'_Lug',(xx+s*.010,y+.039*sin(a),.32+.039*cos(a)),(xx+s*.014,y+.039*sin(a),.32+.039*cos(a)),.006,'black',parent,n=6)

def nose_y(x): return 2.1975-.12*(abs(x)/.865)**2

def front_panel(name, coords, key, parent=None, offset=0):
    return b.patch(name,coords,lambda x,z:(x,nose_y(x)+offset,z),key,parent,cuts=5)

def rear_y(x,z): return -2.1975+.065*(abs(x)/.865)**2+max(0,z-1.12)*.27

def rear_panel(name, coords, key, parent=None, offset=0):
    return b.patch(name,coords,lambda x,z:(x,rear_y(x,z)-offset,z),key,parent,cuts=4)

def make_car():
    wheels=[]
    for y,axle in [(1.375,'F'),(-1.375,'R')]:
        for s in [-1,1]:
            tag=axle+('L' if s<0 else 'R');pos=(s*.810,y,.32)
            steer=b.empty('Steering_'+tag,pos,b.ROOT) if axle=='F' else b.ROOT
            wp=b.empty('Wheel_'+tag,pos,steer);make_wheel(tag,*pos[:2],wp)
            wheels.append(dict(node=wp.name,position=pos,radius=.32))
    b.box('Recessed_Underbody',(0,0,.245),(1.30,3.90,.11),'black',bevel=.035)
    # Sculpted lower shell: subtly convex doors, raised shoulder and clean arch cuts.
    def width(y):
        return .865-.055*max(0,(y-1.73)/.4675)**2-.030*max(0,(-y-1.82)/.3775)**2
    def belt(y):return 1.115-.085*max(0,(y-1.05)/1.1475)
    ys=sorted(set([-2.1975+4.395*i/108 for i in range(109)]+[a+d for a in [-1.375,1.375] for d in [-.36,0,.36]]))
    for s in [-1,1]:
        vs=[];fs=[]
        for y in ys:
            low=.245
            for a in [-1.375,1.375]:
                d=y-a
                if abs(d)<.363:low=max(low,.32+sqrt(.363**2-d*d))
            top=belt(y)
            for t,f in [(0,.955),(.10,.983),(.40,1),(.73,1),(.90,.985),(1,.945)]:
                x=s*width(y)*f
                yy=y-.12*(abs(x)/.865)**2*max(0,(y-1.73)/.4675)+.065*(abs(x)/.865)**2*max(0,(-y-1.82)/.3775)
                vs.append((x,yy,low+(top-low)*t))
        for i in range(len(ys)-1):
            for j in range(5):fs.append((i*6+j,(i+1)*6+j,(i+1)*6+j+1,i*6+j+1))
        b.mesh('Sculpted_Door_And_Fender',vs,[tuple(reversed(f)) for f in fs] if s<0 else fs,smooth=True)
        for a in [-1.375,1.375]:
            # A thin painted return lip instead of chunky black SUV cladding.
            pts=[(s*(width(a+.363*sin(t))+.002),a+.363*sin(t),.32+.363*cos(t)) for t in [-pi/2+i*pi/28 for i in range(29)]]
            b.tube('Painted_Arch_Return',pts,.009,'paint',n=5)
        b.tube('Rocker_Shadow',[(s*.833,-1.02,.275),(s*.847,-.65,.257),(s*.847,.73,.257),(s*.833,1.01,.275)],.018,'trim',n=5)
        b.tube('Lower_Door_Crease',[(s*.858,-1.05,.405),(s*.866,-.8,.38),(s*.866,.65,.38),(s*.858,1.0,.405)],.003,'alloy',n=4)
        # Raised belt crease, body coloured: creates readable highlights under game light.
        b.tube('Shoulder_Highlight',[(s*width(y)*.987,y,belt(y)-.065) for y in [-2.05,-1.5,-.8,0,.8,1.5,1.94]],.005,'paint',n=5)
        for y in [-1.13,-.20,.98]:
            pts=[(s*.851,y,.34),(s*.867,y,.66),(s*.859,y,.93),(s*.819,y,1.105)]
            if y==-1.13:pts[0]=(s*.860,y,.625)
            b.tube('Door_Shutline',pts,.0028,'trim',n=4)
        for y in [-.88,.07]:
            b.box('Handle_Recess',(s*.858,y,.990),(.014,.171,.043),'trim',bevel=.013,segments=3)
            b.box('Body_Colour_Handle',(s*.872,y,.996),(.025,.153,.025),'paint',bevel=.010,segments=3)
        if s<0:
            b.panel('Fuel_Door',[(s*.863,-1.71,.87),(s*.861,-1.51,.87),(s*.857,-1.51,1.01),(s*.857,-1.71,1.01)],(.002,0,0),'paint')
            b.tube('Fuel_Door_Seam',[(s*.865,-1.71,.87),(s*.863,-1.51,.87),(s*.859,-1.51,1.01),(s*.859,-1.71,1.01),(s*.865,-1.71,.87)],.002,'trim',n=4)
    # Hood surface and subtle twin stamped creases.
    vs=[];fs=[]
    for i in range(19):
        t=i/18;y=1.03+t*1.1675
        for u in [-1,-.96,-.83,-.66,-.40,0,.40,.66,.83,.96,1]:
            x=width(y)*.945*u; yy=y-.12*(abs(x)/.865)**2*t
            z=belt(y)+.028*(1-u*u)+.010*math.exp(-((abs(u)-.66)/.12)**2)
            vs.append((x,yy,z))
    for i in range(18):
        for j in range(10):fs.append((i*11+j,(i+1)*11+j,(i+1)*11+j+1,i*11+j+1))
    b.mesh('Sculpted_Short_Hood',vs,[tuple(reversed(f)) for f in fs],smooth=True)
    for s in [-1,1]:
        b.tube('Hood_Shutline',[(s*.777,1.03,1.117),(s*.774,1.50,1.084),(s*.747,1.96,1.052)],.0026,'trim',n=4)
    # Rounded nose and tail close the shell; face coordinates follow the same bulge.
    for rear in [False,True]:
        pts=[]
        sections=[(0,.955),(.10,.983),(.40,1),(.73,1),(.90,.985),(1,.945)]
        for i in range(25):
            u=-1+i*2/24
            for t,f in sections:
                z=.245+t*((1.115 if rear else 1.030)-.245)
                x=u*(.835 if rear else .810)*f
                pts.append((x,rear_y(x,z) if rear else nose_y(x),z))
        faces=[(i*6+j,(i+1)*6+j,(i+1)*6+j+1,i*6+j+1) for i in range(24) for j in range(5)]
        b.mesh('Tailgate_Lower' if rear else 'Front_Bumper',pts,faces if rear else [tuple(reversed(f)) for f in faces],smooth=True)
    # Cabin stations: low cowl, long MPV roof, near-upright tailgate.
    rows=[(-2.175,.796,1.115),(-1.92,.735,1.58),(-1.69,.705,1.665),(-1.25,.704,1.687),(-.65,.704,1.69),(.12,.705,1.67),(.34,.718,1.615),(1.07,.817,1.117)]
    def roof(y):
        y=max(rows[0][0],min(rows[-1][0],y))
        for a,c in zip(rows,rows[1:]):
            if a[0]<=y<=c[0]:
                t=(y-a[0])/(c[0]-a[0]);return a[1]*(1-t)+c[1]*t,a[2]*(1-t)+c[2]*t
        return rows[-1][1:]
    def side(s,y,t,offset=.004):
        w,z=roof(y);return (s*(.817*(1-t)+w*t+offset),y,1.112+(z-1.112)*t)
    def topglass(x,y):
        w,z=roof(y);return (x*w,y,z+.027*(1-x*x)+.007)
    # Chamfered corners and slightly crowned roof avoid a flat slab silhouette.
    vs=[];fs=[]
    for y,w,z in rows:
        vs.extend([(-.817,y,1.112),(-w,y,z-.024),(-w*.94,y,z+.003),(-w*.65,y,z+.020),(0,y,z+.026),(w*.65,y,z+.020),(w*.94,y,z+.003),(w,y,z-.024),(.817,y,1.112)])
    for i in range(len(rows)-1):
        for j in range(8):fs.append((i*9+j,(i+1)*9+j,(i+1)*9+j+1,i*9+j+1))
    b.mesh('Continuous_Cabin_And_Crowned_Roof',vs,[tuple(reversed(f)) for f in fs],smooth=True)
    windows=[[(.96,.10),(.30,.94),(-.18,.94),(-.18,.08)],[(-.26,.08),(-.26,.94),(-1.10,.94),(-1.10,.08)],[(-1.19,.08),(-1.19,.94),(-1.70,.92),(-1.96,.16)]]
    for s in [-1,1]:
        # Black pillars make the three separate panes read as one long glasshouse.
        for y in [-.22,-1.145]:
            b.patch('Gloss_Black_Window_Pillar',[(y-.043,.035),(y-.043,.98),(y+.043,.98),(y+.043,.035)],lambda y,t,s=s:side(s,y,t,.005),'black',cuts=1)
        for coords in windows:
            b.patch('Flush_Tinted_Side_Glass',coords,lambda y,t,s=s:side(s,y,t,.009),'glass',cuts=2)
            b.tube('Window_Rubber_Seal',[side(s,y,t,.011) for y,t in coords+[coords[0]]],.006,'black',n=5)
        b.tube('Window_Sill_Trim',[side(s,y,.04,.010) for y in [-1.94,-1.65,-1.2,-.6,.1,.90]],.008,'chrome',n=5)
        b.tube('Roof_Drain_Channel',[(s*roof(y)[0]*.96,y,roof(y)[1]+.008) for y in [-1.78,-1.25,-.65,.12,.28]],.003,'trim',n=4)
        # Angled mirror pedestal, painted cap and dark lower housing.
        b.panel('Mirror_Sail',[side(s,.78,.07,.016),side(s,.75,.32,.016),side(s,1.0,.07,.016)],(.008*s,0,0),'black')
        b.tube('Mirror_Stalk',[(s*.828,.79,1.19),(s*.95,.79,1.20)],.023,'trim',n=8)
        b.box('Mirror_Lower',(s*.979,.80,1.226),(.190,.16,.074),'black',bevel=.031,segments=3)
        b.box('Mirror_Painted_Cap',(s*.979,.808,1.263),(.190,.169,.074),'paint',bevel=.033,segments=3)
        b.box('Mirror_Reflector',(s*.979,.719,1.253),(.163,.008,.062),'chrome',bevel=.023,segments=3)
        b.box('Mirror_Signal',(s*1.014,.885,1.244),(.115,.012,.012),'amber',b.LIGHTS['Indicator_Front_'+('L' if s<0 else 'R')],bevel=.003)
    wind=[(-.93,.345),(.93,.345),(.94,1.006),(-.94,1.006)]
    b.patch('Windscreen',wind,topglass,'glass',cuts=8)
    b.tube('Windscreen_Seal',[topglass(x,y) for x,y in wind+[wind[0]]],.008,'black',n=5)
    for s in [-1,1]:
        b.tube('Parked_Front_Wiper',[(s*.14,.96,roof(.96)[1]+.038),(s*.53,.885,roof(.885)[1]+.036)],.006,'trim',n=5)
    rearwind=[(-.90,-2.11),(.90,-2.11),(.91,-1.87),(-.91,-1.87)]
    b.patch('Rear_Glass',rearwind,topglass,'glass',cuts=6)
    b.tube('Rear_Glass_Seal',[topglass(x,y) for x,y in rearwind+[rearwind[0]]],.008,'black',n=5)
    b.tube('Rear_Wiper',[(0,-2.09,roof(-2.09)[1]+.027),(.34,-2.05,roof(-2.05)[1]+.027)],.007,'trim',n=5)
    b.box('Integrated_Roof_Spoiler',(0,-1.885,1.604),(1.46,.20,.057),'paint',bevel=.023,segments=3)
    b.box('Spoiler_Stop_Lamp',(0,-1.990,1.600),(.34,.014,.021),'tail',b.LIGHTS['TailLights'],bevel=.005)
    b.panel('Shark_Fin_Antenna',[(-.025,-1.27,1.712),(-.018,-1.44,1.712),(-.013,-1.39,1.775),(-.010,-1.34,1.748)],(.035,0,0),'black')
    # Current Avanza-inspired face: wide trapezoid lower grille, slim upper lamps.
    front_panel('Upper_Grille',[(-.48,1.035),(.48,1.035),(.44,.948),(-.44,.948)],'black',offset=.012)
    front_panel('Lower_Grille_Surround',[(-.43,.930),(.43,.930),(.687,.425),(.59,.359),(-.59,.359),(-.687,.425)],'trim',offset=.013)
    front_panel('Deep_Lower_Grille',[(-.408,.907),(.408,.907),(.647,.439),(.574,.394),(-.574,.394),(-.647,.439)],'black',offset=.022)
    for i in range(6):
        z=.445+i*.076;ww=.616-(z-.445)*.47
        front_panel('Horizontal_Grille_Blade',[(-ww,z-.012),(ww,z-.012),(ww-.008,z+.012),(-ww+.008,z+.012)],'trim',offset=.040)
        # Restrained highlights make the slats visible without a chrome wall.
        b.tube('Grille_Blade_Edge',[(-ww,nose_y(-ww)+.046,z+.012),(0,nose_y(0)+.046,z+.012),(ww,nose_y(ww)+.046,z+.012)],.003,'alloy',n=4)
    for x in [-.28,0,.28]:
        front_panel('Grille_Vertical_Web',[(x-.008,.43),(x+.008,.43),(x+.008,.90),(x-.008,.90)],'trim',offset=.027)
    front_panel('Upper_Grille_Accent',[(-.43,.947),(.43,.947),(.43,.958),(-.43,.958)],'chrome',offset=.030)
    for s in [-1,1]:
        lamp=b.LIGHTS['Headlights'];ind=b.LIGHTS['Indicator_Front_'+('L' if s<0 else 'R')]
        coords=[(s*.405,1.032),(s*.815,1.048),(s*.831,.988),(s*.457,.968)]
        front_panel('Slim_Headlamp_Housing',coords,'black',offset=.018)
        front_panel('Slim_Headlamp_Lens',[(s*.434,1.019),(s*.805,1.033),(s*.809,.999),(s*.468,.983)],'headlamp',lamp,offset=.030)
        for x in [.53,.65,.75]:
            front_panel('LED_Reflector_Divider',[(s*(x-.004),.989),(s*(x+.004),.989),(s*(x+.004),1.029),(s*(x-.004),1.029)],'alloy',offset=.037)
        front_panel('Front_Indicator',[(s*.786,.988),(s*.831,.994),(s*.831,.974),(s*.786,.969)],'amber',ind,offset=.035)
        front_panel('Angular_Fog_Recess',[(s*.697,.813),(s*.808,.865),(s*.815,.443),(s*.699,.414),(s*.625,.464)],'trim',offset=.025)
        b.cyl('Fog_Lamp_Bezel',(s*.752,nose_y(.752)+.023,.652),(s*.752,nose_y(.752)+.039,.652),.045,'black',n=20)
        b.cyl('Fog_Lamp',(s*.752,nose_y(.752)+.040,.652),(s*.752,nose_y(.752)+.045,.652),.029,'headlamp',lamp,n=16)
        front_panel('Bumper_Corner_Accent',[(s*.698,.398),(s*.823,.435),(s*.820,.416),(s*.705,.373)],'alloy',offset=.031)
    front_panel('Lower_Bumper_Valance',[(-.66,.28),(.66,.28),(.70,.326),(-.70,.326)],'trim',offset=.015)
    # Horizontal rear lamps wrap into the rear quarter; all real lens groups survive LOD.
    for s in [-1,1]:
        rear_panel('Tail_Lamp_Housing',[(s*.31,1.085),(s*.79,1.110),(s*.805,1.002),(s*.50,.985),(s*.37,1.021)],'black',offset=.014)
        rear_panel('Tail_Lamp_Lens',[(s*.34,1.077),(s*.78,1.095),(s*.792,1.023),(s*.50,1.006)],'tail',b.LIGHTS['TailLights'],offset=.025)
        rear_panel('Rear_Indicator',[(s*.57,1.037),(s*.78,1.055),(s*.78,1.035),(s*.57,1.018)],'amber',b.LIGHTS['Indicator_Rear_'+('L' if s<0 else 'R')],offset=.031)
        b.panel('Wrapped_Tail_Lens',[(s*.818,-2.115,1.095),(s*.837,-1.94,1.087),(s*.842,-1.94,1.042),(s*.823,-2.115,1.018)],(.005*s,0,0),'tail',b.LIGHTS['TailLights'])
        rear_panel('Rear_Reflector',[(s*.61,.377),(s*.754,.391),(s*.755,.366),(s*.61,.352)],'tail',b.LIGHTS['TailLights'],offset=.019)
    rear_panel('Tailgate_Handle', [(-.11,.929),(.11,.929),(.11,.909),(-.11,.909)],'chrome',offset=.017)
    rear_panel('Rear_Lower_Valance',[(-.69,.26),(.69,.26),(.76,.32),(-.76,.32)],'trim',offset=.015)
    for s in [-1,1]:
        b.tube('Tailgate_Shutline',[(s*.65,rear_y(s*.65,z)-.006,z) for z in [.42,.75,.94]],.0025,'trim',n=4)
    for front in [True,False]:
        z=.635 if front else .80;y=nose_y(0)+.052 if front else rear_y(0,z)-.028
        b.box('Registration_Plate',(0,y,z),(.38,.013,.105),'black',bevel=.006)
        b.text('Registration','B 2048 JLN',(0,y+(.009 if front else -.009),z),.033,(pi/2,0,pi if front else 0),'white')
    b.empty('Socket_Driver',(.32,.20,.93),b.ROOT);b.empty('Socket_Passenger',(-.32,.20,.93),b.ROOT)
    return wheels

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    for p in ['textures','previews','unity']: (OUT/p).mkdir(exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
    b.MODEL=bpy.data.collections.new('AVANZA_INSPIRED_AUTHORING');bpy.context.scene.collection.children.link(b.MODEL)
    b.ROOT=b.empty('family_mpv');b.BODY=b.empty('Body',parent=b.ROOT)
    b.ROOT['design']='Original Avanza-inspired MPV; not an official Toyota asset'
    b.ROOT['units']='meters';b.ROOT['blender_forward']='+Y'
    b.LIGHTS={n:b.empty(n,parent=b.ROOT) for n in ['Headlights','TailLights','Indicator_Front_L','Indicator_Front_R','Indicator_Rear_L','Indicator_Rear_R']}
    b.materials(CFG,OUT);wheels=make_car();bpy.context.view_layer.update()
    pts=[o.matrix_world@v.co for o in b.MODEL.objects if o.type=='MESH' for v in o.data.vertices]
    stats=dict(name=CFG['name'],id='family_mpv',wheels=wheels,materials=2,atlas_size=128,bounds_m=dict(min=[min(p[i] for p in pts) for i in range(3)],max=[max(p[i] for p in pts) for i in range(3)]),lods=[])
    # Consolidate per animation group, preserving existing runtime node/material contract.
    col=b.consolidate()
    for lod in [0,1]:
        if lod:
            for o in col.objects:
                if o.type=='MESH' and len(o.data.polygons)>50:
                    mod=o.modifiers.new('Distance_LOD','DECIMATE');mod.ratio=.42;mod.use_collapse_triangulate=True
                    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
        for o in col.objects:
            if o.type=='MESH':o.data.validate();o.data.update()
        bpy.ops.object.select_all(action='DESELECT')
        for o in col.objects:o.select_set(True)
        bpy.context.view_layer.objects.active=next(o for o in col.objects if o.parent is None)
        fn='Body_LOD1' if lod else 'Body'
        bpy.ops.export_scene.fbx(filepath=str(OUT/'unity'/(fn+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='AUTO',add_leaf_bones=False)
        bpy.ops.export_scene.gltf(filepath=str(OUT/('avanza_inspired'+('_lod1' if lod else '')+'.glb')),export_format='GLB',use_selection=True,export_yup=True,export_extras=True,export_animations=False)
        stats['lods'].append(dict(level=lod,triangles=b.triangles(col.objects),meshes=sum(o.type=='MESH' for o in col.objects)))
    for o in list(col.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.collections.remove(col)
    sc,cam,target=b.studio(CFG);sc.cycles.samples=32;sc.render.resolution_x=1280;sc.render.resolution_y=960
    cam.location=(5.7,8.1,3.4);cam.rotation_euler=(Vector((0,0,.82))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=5.85
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'avanza_inspired.blend'))
    (OUT/'asset_stats.json').write_text(json.dumps(stats,indent=2)+'\n')
    for label,loc in [('front',(5.7,8.1,3.4)),('rear',(-5.6,-8.0,3.1)),('side',(9,0,2.1))]:
        cam.location=loc;cam.rotation_euler=(Vector((0,0,.82))-cam.location).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=str(OUT/'previews'/(label+'.png'));bpy.ops.render.render(write_still=True)
    print('AVANZA_ASSETS_READY',json.dumps(stats),flush=True)
if __name__=='__main__':main()
