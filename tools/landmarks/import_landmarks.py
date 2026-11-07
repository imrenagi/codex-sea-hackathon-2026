"""Prepare offline landmark map extracts. Source footprints stay in local east/north metres."""
from pathlib import Path
import xml.etree.ElementTree as ET
import json,math,argparse,urllib.request,heapq
SETTINGS={'hi':(-6.19498,106.823045,'106.8188,-6.1992,106.8276,-6.1908'),
          'monas':(-6.1753924,106.8271528,'106.8205,-6.1822,106.8337,-6.1695')}
def number(x,default=0):
 try:return float(x)
 except (ValueError,TypeError):return default

def monas_route(root,nodes):
 def xy(n):return nodes[n]['x'],nodes[n]['y']
 graph={};ways=[]
 for way in root.findall('way'):
  tags={t.get('k'):t.get('v') for t in way.findall('tag')}
  if tags.get('highway') not in ('primary','secondary','tertiary','primary_link','secondary_link','tertiary_link','residential','unclassified') or tags.get('access')=='private' or tags.get('motor_vehicle')=='no':continue
  ids=[n.get('ref') for n in way.findall('nd') if n.get('ref') in nodes];ways.append((tags,ids))
  for a,b in zip(ids,ids[1:]):
   if max(math.hypot(*xy(a)),math.hypot(*xy(b)))>1200:continue
   length=math.dist(xy(a),xy(b))
   if tags.get('oneway')!='-1':graph.setdefault(a,[]).append((b,length))
   if tags.get('oneway')!='yes':graph.setdefault(b,[]).append((a,length))
 targets=[]
 for point,direction,name in [((-450,-150),(0,-1),'Barat'),((0,-555),(1,0),'Selatan'),((390,30),(0,1),'Timur'),((0,438),(-1,0),'Utara')]:
  candidates=set()
  for tags,ids in ways:
   if tags.get('name')!='Jalan Medan Merdeka '+name:continue
   a,b=xy(ids[0]),xy(ids[-1])
   if (b[0]-a[0])*direction[0]+(b[1]-a[1])*direction[1]>0:candidates.update(ids)
  targets.append(min(candidates,key=lambda n:math.dist(xy(n),point)))
 result=[]
 for start,end in zip(targets,targets[1:]+targets[:1]):
  queue=[(0,start)];distance={start:0};previous={}
  while queue:
   cost,n=heapq.heappop(queue)
   if n==end:break
   if cost>distance[n]:continue
   for nxt,length in graph.get(n,[]):
    if cost+length<distance.get(nxt,float('inf')):
     distance[nxt]=cost+length;previous[nxt]=n;heapq.heappush(queue,(cost+length,nxt))
  assert end in distance,'Monas public-road chain disconnected'
  path=[end]
  while path[-1]!=start:path.append(previous[path[-1]])
  result+=path[::-1][:-1]
 assert len(set(result))==len(result),'Unexpected repeated road in Monas loop'
 return [nodes[n] for n in result]

def prepare(name,source,destination):
 lat,lon,bbox=SETTINGS[name]
 root=ET.parse(source).getroot()
 nodes={n.get('id'):{'x':round((float(n.get('lon'))-lon)*111319.49*math.cos(math.radians(lat)),3),'y':round((float(n.get('lat'))-lat)*111319.49,3)} for n in root.findall('node')}
 buildings=[];roads=[];circle={}
 for w in root.findall('way'):
  t={a.get('k'):a.get('v') for a in w.findall('tag')};points=[nodes[a.get('ref')] for a in w.findall('nd') if a.get('ref') in nodes]
  if len(points)<2:continue
  x=sum(p['x'] for p in points)/len(points);z=sum(p['y'] for p in points)/len(points)
  if math.hypot(x,z)>(510 if name=='hi' else 900):continue
  if t.get('junction')=='roundabout' and name=='hi':
   for p in points:circle[(p['x'],p['y'])]=p
  if 'highway' in t and t['highway'] not in ('steps','construction','proposed'):
   roads.append(dict(id=w.get('id'),name=t.get('name',''),kind=t['highway'],width=number(t.get('width'),2 if t['highway'] in ('footway','path','pedestrian') else 6),points=points))
  if ('building' not in t and 'building:part' not in t) or t.get('name')=='Monumen Nasional':continue
  if points[0]==points[-1]:points=points[:-1]
  if len(points)<3:continue
  levels=number(t.get('building:levels'))
  # The mall has no height tag. Its podium estimate does not replace the mapped tower parts.
  if t.get('name')=='Grand Indonesia Shopping Town' and not levels:levels=6
  buildings.append(dict(id=w.get('id'),name=t.get('name',''),kind=t.get('building','part'),height=number(t.get('height')),levels=levels,minLevel=number(t.get('building:min_level')),points=points))
 if name=='hi':
  route=sorted(circle.values(),key=lambda p:math.atan2(p['x'],p['y'])%(2*math.pi))
 else:
  route=monas_route(root,nodes)
 data=dict(id=name,originLatitude=lat,originLongitude=lon,source='https://api.openstreetmap.org/api/0.6/map?bbox='+bbox,roadWidth=14 if name=='hi' else 8,route=route,buildings=buildings,roads=roads)
 destination.mkdir(parents=True,exist_ok=True);(destination/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n')
 print(name,len(buildings),'buildings',len(roads),'roads',len(route),'route points')
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--source-dir',type=Path,default=Path('/tmp'));a=p.parse_args()
 for n in SETTINGS:prepare(n,a.source_dir/(n+'-osm.xml'),a.output)
