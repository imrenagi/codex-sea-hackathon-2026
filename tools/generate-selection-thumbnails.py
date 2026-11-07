#!/usr/bin/env python3
"""Offline selection art. Requires Pillow; never launches Unity.
City images use bundled OSM geometry (© OpenStreetMap contributors, ODbL).
Encounter diagrams are schematic, derived from Mission/RouteStage/District landmarks.
They are not gameplay captures; decorative blocks and route emphasis are illustrative.
Usage: python tools/generate-selection-thumbnails.py [repository-root]
"""
from pathlib import Path
import json, math, sys
from PIL import Image, ImageDraw
ROOT = Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
OUT = Path(sys.argv[2]) if len(sys.argv)>2 else ROOT/'Unity/Assets/Resources/UI/Thumbnails'
OUT.mkdir(parents=True,exist_ok=True)
S=2; W=640; H=360
INK='#192126'; GROUND='#243236'; ROAD='#465357'; EDGE='#697579'; LINE='#d3c8a5'; GOLD='#d5b679'; SAGE='#94b8a4'; RED='#d48c7c'; ROOF='#60736e'
def canvas():
    im=Image.new('RGB',(W*S,H*S),INK)
    return im,ImageDraw.Draw(im)
def poly(d,points,fill): d.polygon([(int(x*S),int(y*S)) for x,y in points],fill=fill)
def line(d,points,fill,width=1): d.line([(int(x*S),int(y*S)) for x,y in points],fill=fill,width=max(1,int(width*S)),joint='curve')
def box(d,b,fill,r=0,outline=None,width=1): d.rounded_rectangle(tuple(int(v*S) for v in b),radius=int(r*S),fill=fill,outline=outline,width=int(width*S))
def circle(d,x,y,r,fill,outline=None,width=1): d.ellipse(tuple(int(v*S) for v in (x-r,y-r,x+r,y+r)),fill=fill,outline=outline,width=int(width*S))
def save(im,name): im.resize((W,H),Image.Resampling.LANCZOS).save(OUT/(name+'.png'),optimize=True)
def pin(d,x,y):
    circle(d,x,y,13,INK); circle(d,x,y,9,GOLD); circle(d,x,y,3,INK)
def route(d,points):
    line(d,points,INK,10); line(d,points,GOLD,4)
    x,y=points[-1]; poly(d,[(x,y-9),(x-6,y+3),(x+6,y+3)],GOLD)
def building(d,x,y,w,h,color=ROOF):
    box(d,(x+5,y+7,x+w+5,y+h+7),'#182125',3)
    box(d,(x,y,x+w,y+h),color,3)
    line(d,[(x+5,y+5),(x+w-5,y+5)],'#90a098',2)
def tree(d,x,y):
    circle(d,x+3,y+3,12,'#1b292b');circle(d,x,y,12,'#3d5a50');circle(d,x-3,y-3,6,'#526e5c')
def car(d,x,y,color=GOLD):
    box(d,(x-10,y-19,x+10,y+19),color,5)
    box(d,(x-7,y-12,x+7,y-3),INK,2);box(d,(x-7,y+8,x+7,y+14),INK,2)
def zebra(d,y,left=258,right=382):
    for x in range(left,right,16): box(d,(x,y-11,x+9,y+11),LINE,1)
def signal(d,x,y):
    box(d,(x-13,y-31,x+13,y+31),INK,8,EDGE,2)
    for dy,c in [(-19,RED),(0,'#786f4e'),(19,SAGE)]: circle(d,x,y+dy,6,c)
def speed(d,x,y):
    circle(d,x,y,30,RED);circle(d,x,y,24,'#e4ded0')
    # Vector numerals remain language independent traffic-sign symbols.
    line(d,[(x-14,y-12),(x-3,y-12),(x-3,y),(x-12,y),(x-3,y),(x-3,y+12),(x-14,y+12)],INK,3)
    box(d,(x+3,y-12,x+16,y+12),None,4,INK,3)
def base():
    im,d=canvas();box(d,(20,20,620,340),GROUND,14)
    for x,y in [(56,45),(84,286),(587,76),(560,302)]: tree(d,x,y)
    box(d,(246,-10,394,370),EDGE);box(d,(253,-10,387,370),ROAD)
    for y in range(0,360,34): box(d,(318,y,322,y+18),LINE)
    return im,d
def cross(d,y):
    box(d,(-10,y-39,650,y+39),EDGE);box(d,(-10,y-32,650,y+32),ROAD)
    for x in range(0,640,34):
        if x<240 or x>390: box(d,(x,y-2,x+18,y+2),LINE)
def schematic(name):
    im,d=base()
    if name=='preparation':
        box(d,(162,68,478,298),'#566361',12)
        building(d,85,35,172,100);box(d,(117,79,225,135),INK,2)
        for x in [184,260,336,412]: line(d,[(x,190),(x,276)],LINE,2)
        car(d,296,231);car(d,371,231,SAGE)
        route(d,[(296,209),(296,158),(321,158),(321,40)])
        circle(d,485,105,35,SAGE);line(d,[(467,105),(480,117),(505,88)],INK,6)
    elif name in ('signals','village'):
        cross(d,105 if name=='village' else 83)
        building(d,80,176,120,61);building(d,425,213,126,70)
        if name=='signals': cross(d,260);signal(d,423,45);box(d,(262,297,313,302),LINE);route(d,[(286,330),(286,281),(286,145),(286,25)])
        else:
            building(d,86,34,137,51,SAGE)
            box(d,(88,74,221,88),GOLD)
            route(d,[(285,326),(285,145),(285,45)]);pin(d,167,58)
        poly(d,[(218,128),(229,139),(229,155),(218,166),(202,166),(191,155),(191,139),(202,128)],RED)
        box(d,(199,145,220,149),LINE)
    elif name in ('priority','tutorial'):
        cross(d,90);zebra(d,231)
        building(d,79,140,121,60);building(d,426,180,131,92)
        route(d,[(286,329),(286,257),(286,144),(286,25)])
        for x,y in [(223,226),(241,238),(410,237)]:circle(d,x,y,6,GOLD)
        car(d,477,86,SAGE)
        if name=='tutorial':
            for y in [135,178,221]:
                building(d,70,y,133,28);box(d,(70,y,203,y+9),GOLD)
            for x in [269,295,326]:circle(d,x,231,6,SAGE)
            circle(d,489,272,31,GOLD);poly(d,[(480,255),(480,289),(507,272)],INK)
    elif name in ('focus','school'):
        building(d,61,53,153,146,SAGE);box(d,(73,105,202,117),LINE)
        for x in range(80,193,28):box(d,(x,73,x+12,88),INK)
        speed(d,464,259)
        if name=='school':
            zebra(d,140)
            for x in [221,241,411]:circle(d,x,140,6,GOLD)
            route(d,[(286,326),(286,177),(286,33)])
        else:
            box(d,(458,58,511,151),INK,8,EDGE,2)
            box(d,(465,71,504,128),ROOF,2);line(d,[(442,55),(526,157)],RED,6)
            route(d,[(286,326),(286,33)])
    elif name in ('lanes','market'):
        poly(d,[(387,35),(463,87),(463,199),(387,250)],ROAD)
        line(d,[(393,42),(453,87),(453,196),(393,244)],EDGE,3)
        building(d,484,100,85,73)
        if name=='market':
            box(d,(185,100,252,270),ROAD,5)
            for y in range(110,265,22):box(d,(249,y,253,y+12),LINE)
            for y in [78,127,176]:
                building(d,80,y,83,35);box(d,(80,y,163,y+12),GOLD if y!=127 else SAGE)
            route(d,[(286,330),(286,284),(220,240),(220,142),(286,86),(286,30)])
            car(d,220,201,SAGE);pin(d,177,202)
        else:
            box(d,(262,170,308,215),RED,3)
            for y in [179,196]:line(d,[(264,y),(306,y)],LINE,3)
            route(d,[(286,330),(286,272),(354,227),(354,143),(286,89),(286,30)])
    elif name in ('straight','left_turn','right_turn'):
        cross(d,155);signal(d,432,218);building(d,75,44,122,62);building(d,449,266,119,58)
        zebra(d,209);zebra(d,100)
        if name=='straight': route(d,[(286,330),(286,31)])
        elif name=='left_turn':route(d,[(286,330),(286,155),(74,155)])
        else:
            route(d,[(354,330),(354,155),(563,155)])
            poly(d,[(357,235),(376,201),(338,201)],SAGE)
    elif name=='railway':
        for x in range(0,640,20):line(d,[(x,121),(x+19,179)],ROOF,6)
        line(d,[(0,130),(640,130)],LINE,3);line(d,[(0,169),(640,169)],LINE,3)
        box(d,(69,127,219,171),SAGE,7)
        for x in range(84,210,24):box(d,(x,137,x+15,156),INK,2)
        line(d,[(237,208),(318,208)],RED,8)
        for x in range(242,310,18):line(d,[(x,206),(x+8,210)],LINE,5)
        route(d,[(286,330),(286,237)]);signal(d,429,214)
    elif name=='ambulance':
        building(d,60,46,139,94,SAGE)
        box(d,(112,65,135,120),LINE);box(d,(96,81,151,104),LINE)
        car(d,351,261,'#e4ded0');box(d,(344,253,358,267),RED);box(d,(337,240,365,246),RED)
        car(d,279,110,SAGE)
        route(d,[(350,335),(350,305),(280,240),(280,183)])
        for x in [333,369]:line(d,[(x,211),(x,194)],RED,3)
    car(d,286,311)
    save(im,'mission_'+name)
def city(identifier):
    data=json.loads((ROOT/'Unity/Assets/Resources/CityMaps'/f'{identifier}.json').read_text())
    im,d=canvas()
    def p(x,z,h=0): return 320+x*.235+z*.12,180+z*.13-x*.06-min(h,180)*.28
    poly(d,[p(-864,-735),p(864,-735),p(864,735),p(-864,735)],GROUND)
    for road in data['roads']:
        if road.get('tunnel')=='yes': continue
        pts=[p(q['x'],-q['z']) for q in road.get('points',[])]
        if len(pts)<2:continue
        kind=road.get('highway','');major=kind in ['motorway','trunk','primary','secondary']
        width={'motorway':4,'trunk':4,'primary':3,'secondary':2.5}.get(kind,1.3)
        line(d,pts,'#566464' if major else '#39494c',width+2)
        line(d,pts,GOLD if major else '#8b9790',width)
    for b in sorted(data['buildings'],key=lambda b:-sum(q['z'] for q in b['points'])/len(b['points'])):
        pts=b.get('points',[])
        if len(pts)<3:continue
        cx=sum(q['x'] for q in pts)/len(pts);cz=sum(q['z'] for q in pts)/len(pts)
        if abs(cx)>864 or abs(cz)>735:continue
        h=b.get('height',8); roof=[p(q['x'],-q['z'],h) for q in pts];ground=[p(q['x'],-q['z']) for q in pts]
        for i in range(len(pts)-1):poly(d,[ground[i],ground[i+1],roof[i+1],roof[i]],'#354844')
        poly(d,roof,'#8c9e8e' if h>35 else '#627d70')
    # Center pin identifies the bundled map's configured center; no fictional route.
    pin(d,320,180)
    # A small north arrow follows the same projection as the geometry.
    line(d,[(574,67),(563,40)],LINE,2);poly(d,[(563,35),(560,49),(570,44)],LINE)
    save(im,'city_'+identifier)
for identifier in ['morrissey','bundaran_hi','sea_labs','semanggi','monas','bundaran_senayan']:city(identifier)
for identifier in ['tutorial','preparation','signals','priority','lanes','focus','village','school','market','straight','left_turn','right_turn','railway','ambulance']:schematic(identifier)
print(f'Wrote 20 source-derived selection thumbnails to {OUT}')
