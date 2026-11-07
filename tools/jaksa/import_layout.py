"""Refresh the ODbL map extract. Explicitly run when source updates are wanted; not at game startup."""
from pathlib import Path
import json, math, urllib.request, xml.etree.ElementTree as ET

SOURCE = 'https://api.openstreetmap.org/api/0.6/map?bbox=106.8275,-6.1880,106.8302,-6.1851'
ROOT = Path(__file__).resolve().parents[2]
LAT, LON, BEARING, CENTER = -6.1865309, 106.8289926, 194, 175

def local(lat, lon):
    east = (lon-LON)*111319.49*math.cos(math.radians(LAT))
    north = (lat-LAT)*111319.49
    s, c = math.sin(math.radians(BEARING)), math.cos(math.radians(BEARING))
    return dict(x=round(east*c-north*s,3), y=round(east*s+north*c+CENTER,3))

def main():
    req = urllib.request.Request(SOURCE,headers={'User-Agent':'JalanKitaMapPreparation/1.0'})
    with urllib.request.urlopen(req,timeout=45) as response:
        root = ET.fromstring(response.read())
    nodes = {n.attrib['id']:local(float(n.attrib['lat']),float(n.attrib['lon'])) for n in root.findall('node')}
    buildings, road = [], []
    for way in root.findall('way'):
        tags = {t.attrib['k']:t.attrib['v'] for t in way.findall('tag')}
        points = [nodes[n.attrib['ref']] for n in way.findall('nd') if n.attrib['ref'] in nodes]
        if way.attrib['id']=='68558044':road=points
        if 'building' not in tags or len(points)<4:continue
        if points[0]==points[-1]:points=points[:-1]
        x=sum(p['x'] for p in points)/len(points); z=sum(p['y'] for p in points)/len(points)
        if abs(x)>65 or not -30<=z<=280:continue
        buildings.append(dict(id=way.attrib['id'],name=tags.get('name',''),
            levels=float(tags.get('building:levels',0)),roof=tags.get('building:roof',''),points=points))
    if not road or not buildings:raise RuntimeError('Incomplete map response; existing layout left intact.')
    data=dict(originLatitude=LAT,originLongitude=LON,bearingDegrees=BEARING,length=250,
        centerAtMetres=CENTER,roadOsmId='68558044',oneway=True,road=road,buildings=buildings)
    (ROOT/'Unity/Assets/Resources/Jaksa/layout.json').write_text(json.dumps(data,indent=2)+'\n')
    print('Imported',len(buildings),'building footprints. Check NOTICE.txt and update provenance date.')

if __name__=='__main__':main()
