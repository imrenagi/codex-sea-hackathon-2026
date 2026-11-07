"""Import the real Sopo Del frontage from OSM; no runtime network access."""
from pathlib import Path
import argparse, json, math, urllib.request, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
SOURCE = 'https://api.openstreetmap.org/api/0.6/map?bbox=106.8215,-6.2330,106.8260,-6.2280'
LAT, LON, BEARING = -6.2290, 106.823910, 157
ROAD_IDS = ['1467161547', '28190488', '1408773795', '1281427724']

def local(lat, lon):
    east = (lon-LON)*111319.49*math.cos(math.radians(LAT))
    north = (lat-LAT)*111319.49
    s,c = math.sin(math.radians(BEARING)),math.cos(math.radians(BEARING))
    return dict(x=round(east*c-north*s,3),y=round(east*s+north*c,3))

def number(value, default=0):
    try: return float(value)
    except (ValueError,TypeError): return default

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--source-file');args=parser.parse_args()
    if args.source_file: raw=Path(args.source_file).read_bytes()
    else:
        request=urllib.request.Request(SOURCE,headers={'User-Agent':'JalanKitaMapPreparation/1.0'})
        with urllib.request.urlopen(request,timeout=45) as response: raw=response.read()
    root=ET.fromstring(raw)
    nodes={n.get('id'):local(float(n.get('lat')),float(n.get('lon'))) for n in root.findall('node')}
    buildings=[];roads=[];main_road={}
    for way in root.findall('way'):
        tags={t.get('k'):t.get('v') for t in way.findall('tag')};oid=way.get('id')
        points=[nodes[n.get('ref')] for n in way.findall('nd') if n.get('ref') in nodes]
        if not points:continue
        if oid in ROAD_IDS:
            for p in points:main_road[(p['x'],p['y'])]=p
        x=sum(p['x'] for p in points)/len(points);z=sum(p['y'] for p in points)/len(points)
        if 'highway' in tags and not tags['highway'].startswith('motorway'):
            if any(-200<p['x']<240 and -100<p['y']<420 for p in points):
                roads.append(dict(id=oid,name=tags.get('name',''),kind=tags['highway'],
                    width=number(tags.get('width'),3 if tags['highway']=='service' else 6),points=points))
        if 'building' not in tags or not -180<x<240 or not -100<z<420:continue
        if points[0]==points[-1]:points=points[:-1]
        buildings.append(dict(id=oid,name=tags.get('name',''),roof=tags.get('building:roof',''),
            levels=number(tags.get('building:levels')),height=number(tags.get('height')),
            minLevel=number(tags.get('building:min_level')),kind=tags.get('building',''),points=points))
    assert len(main_road)>8 and any(b['id']=='483610514' for b in buildings)
    data=dict(originLatitude=LAT,originLongitude=LON,bearingDegrees=BEARING,length=250,centerAtMetres=0,
        roadOsmId=','.join(ROAD_IDS),oneway=False,road=sorted(main_road.values(),key=lambda p:p['y']),
        buildings=buildings,surroundingRoads=roads)
    folder=ROOT/'Unity/Assets/Resources/SopoDel';folder.mkdir(exist_ok=True)
    (folder/'layout.json').write_text(json.dumps(data,indent=2)+'\n')
    print(f'Imported {len(buildings)} footprints and {len(roads)} road segments around Sopo Del.')

if __name__=='__main__':main()
