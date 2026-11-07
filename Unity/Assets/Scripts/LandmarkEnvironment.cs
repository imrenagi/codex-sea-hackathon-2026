using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace JalanKita
{
    public sealed class LandmarkEnvironment
    {
        public readonly GameObject Root;
        public readonly JakartaPlace Place;
        public readonly LandmarkMap Map;
        public readonly LandmarkRoute Route;
        public readonly LandmarkRoadNetwork Network;
        public readonly LandmarkWater Water;
        public readonly List<Material> Materials=new();
        public string Name=>Place==JakartaPlace.BundaranHI?"Bundaran HI":"Monas · Medan Merdeka";
        public int BuildingCount {get;private set;}
        public Vector3 LandmarkTarget=>new(0,Place==JakartaPlace.BundaranHI?10:65,0);
        readonly StreetMesh mesh=new();
        readonly System.Random rng;
        readonly List<Rect> lots=new();
        readonly Material asphalt,paving,stone,cream,dark,silver,gold,bronze,water,grass,bark,leaves,paint,red;
        readonly Material[] wall,glass;
        readonly Material[] grassBlades;
        readonly Material drySoil;
        readonly Light sun;
        readonly Material sky;
        float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());
        public LandmarkEnvironment(Transform parent,JakartaPlace place)
        {
            Place=place;rng=new System.Random(place==JakartaPlace.BundaranHI?1962:1975);
            Map=JsonUtility.FromJson<LandmarkMap>(Resources.Load<TextAsset>("JakartaLandmarks/"+(place==JakartaPlace.BundaranHI?"hi":"monas")).text);
            Route=new LandmarkRoute(Map.route);Network=new LandmarkRoadNetwork(Map,Route);Root=new GameObject(Name);Root.transform.SetParent(parent,false);
            asphalt=Mat("asphalt","aeb1b0","Jaksa/asphalt_02");paving=Mat("paving","b8b8ae","Jaksa/concrete_floor_02");
            stone=Mat("stone","a5a59c","Jaksa/concrete_floor_02");cream=Mat("marble","e7e3d8",null,.3f);
            dark=Mat("dark","303a3e");silver=Mat("aluminium","bfc7c9",null,.6f,.5f);gold=Mat("gold","dfad36",null,.65f,.8f);
            bronze=Mat("bronze","647369",null,.35f,.55f);water=Mat("pool water","418b91",null,.92f,.45f);
            grass=Mat("lawn","8eac72","SopoDel/grass_ground");bark=Mat("bark","aaa18b","Jaksa/bark_brown_02");
            leaves=Mat("foliage","b4c7a1");leaves.SetTexture("_BaseMap",Resources.Load<Texture2D>("StreetLook/tropical_foliage")??Resources.Load<Texture2D>("Jaksa/leaves"));
            leaves.SetFloat("_AlphaClip",1);leaves.SetFloat("_Cutoff",.45f);leaves.SetFloat("_Cull",0);leaves.EnableKeyword("_ALPHATEST_ON");leaves.renderQueue=2450;
            paint=Mat("markings","e4e2d6");red=Mat("red","b73634");
            wall=new[]{"d5d0bd","bebbb0","b5c0bf","9caaa9"}.Select((c,i)=>Mat("plaster "+i,c,"Jaksa/plastered_wall")).ToArray();
            glass=new[]{"557989","6b8e9c","7d9caa","526d7c","8aadb6"}.Select((c,i)=>Mat("glass "+i,c,null,.75f,.4f)).ToArray();
            grassBlades=new[]{"516b3d","71864d","8a905b"}.Select((c,i)=>Mat("grass blades "+i,c)).ToArray();
            foreach(var blade in grassBlades)blade.SetFloat("_Cull",0);
            drySoil=Mat("dry planted soil","b8b495","SopoDel/withered_grass");
            mesh.Box(grass,new Vector3(0,-.2f,0),Vector3.Scale(Vector3.one,new Vector3(2200,.3f,2200)),repeat:2);
            ContextRoads();MainRoad();
            foreach(var building in Map.buildings)
            {
                // OSM also contains stacked building:part outlines of Monas; the authored monument replaces them.
                if(place==JakartaPlace.Monas && building.points.All(p=>p.magnitude<110))continue;
                Building(building);
            }
            AccessRoads();
            Water=new GameObject("Animated water and fountains").AddComponent<LandmarkWater>();Water.transform.SetParent(Root.transform,false);
            if(place==JakartaPlace.BundaranHI)BundaranHI();else Monas();
            Water.Complete();
            Landscape();GroundDetails();mesh.Build(Root.transform);
            sun=new GameObject("Landmark daylight").AddComponent<Light>();sun.transform.SetParent(Root.transform,false);
            sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(54,-28,0);sun.intensity=1.5f;sun.color=new Color(1,.97f,.91f);sun.shadows=LightShadows.Soft;sun.shadowBias=.08f;sun.shadowNormalBias=.45f;
            sky=new Material(Shader.Find("Skybox/Procedural")){name="Landmark sky"};sky.SetColor("_SkyTint",new Color(.56f,.66f,.76f));sky.SetColor("_GroundColor",new Color(.47f,.52f,.49f));sky.SetFloat("_AtmosphereThickness",.7f);sky.SetFloat("_Exposure",1.1f);
            var volume=new GameObject("Landmark atmosphere").AddComponent<Volume>();volume.transform.SetParent(Root.transform,false);volume.isGlobal=true;volume.priority=3;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);volume.profile.Add<ColorAdjustments>().saturation.Override(-5);
            Activate();
        }
        Material Mat(string name,string hex,string texture=null,float smooth=.12f,float metal=0)
        {
            ColorUtility.TryParseHtmlString("#"+hex,out var color);var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Landmark "+name};
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);
            if(texture!=null){m.SetTexture("_BaseMap",Resources.Load<Texture2D>(texture+"_albedo"));m.SetTexture("_BumpMap",Resources.Load<Texture2D>(texture+"_normal"));m.SetFloat("_BumpScale",.45f);m.EnableKeyword("_NORMALMAP");}
            Materials.Add(m);return m;
        }
        public void Activate()
        {
            Root.SetActive(true);RenderSettings.skybox=sky;RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.7f,.77f,.82f);RenderSettings.ambientEquatorColor=new Color(.47f,.5f,.49f);RenderSettings.ambientGroundColor=new Color(.27f,.29f,.24f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.71f,.78f,.81f);RenderSettings.fogStartDistance=Place==JakartaPlace.Monas?650:320;RenderSettings.fogEndDistance=Place==JakartaPlace.Monas?1400:850;
        }
        static Vector3 V(Vector2 p,float y=0)=>new(p.x,y,p.y);
        void Strip(Material material,Vector2 a,Vector2 b,float width,float y)
        {
            var d=(b-a).normalized;var n=new Vector2(d.y,-d.x)*width*.5f;
            mesh.Quad(material,V(a-n,y),V(b-n,y),V(b+n,y),V(a+n,y),3);
        }
        void ContextRoads()
        {
            foreach(var road in Map.roads)
                for(int i=1;i<road.points.Length;i++)
                    Strip(road.kind=="footway"||road.kind=="path"||road.kind=="pedestrian"?paving:asphalt,road.points[i-1],road.points[i],Mathf.Clamp(road.width,1.4f,16),.015f);
        }
        void MainRoad()
        {
            float width=Map.roadWidth;
            for(float s=0;s<Route.Length;s+=2)
            {
                float next=Mathf.Min(s+2,Route.Length);
                mesh.Quad(asphalt,V(Route.At(s,-width*.5f),.055f),V(Route.At(next,-width*.5f),.055f),V(Route.At(next,width*.5f),.055f),V(Route.At(s,width*.5f),.055f),3);
                foreach(int side in new[]{-1,1})
                {
                    float edge=side*width*.5f;
                    if(side<0&&Network.OnBranch(Route.At((s+next)*.5f,edge),.8f))continue;
                    mesh.Quad(paving,V(Route.At(s,edge),.22f),V(Route.At(next,edge),.22f),V(Route.At(next,edge+side*4),.22f),V(Route.At(s,edge+side*4),.22f),2);
                    var a=V(Route.At(s,edge),.15f);var b=V(Route.At(next,edge),.15f);
                    mesh.Box(((int)(s/4)%2)==0?cream:dark,(a+b)*.5f,new Vector3(.18f,.3f,Vector3.Distance(a,b)+.015f),Quaternion.LookRotation(b-a));
                    Strip(paint,Route.At(s,edge-side*.35f),Route.At(next,edge-side*.35f),.09f,.072f);
                }
                if((int)(s/4)%2==0)foreach(float offset in width>10?new[]{-3.5f,0,3.5f}:new[]{0f})Strip(paint,Route.At(s,offset),Route.At(next,offset),.11f,.073f);
            }
        }
        void AccessRoads()
        {
            foreach(var branch in Network.Branches)
            {
                var road=branch.Route;float width=branch.Width;
                for(float s=0;s<road.Length;s+=2)
                {
                    float next=Mathf.Min(s+2,road.Length),mid=(s+next)*.5f;
                    mesh.Quad(asphalt,V(road.At(s,-width*.5f),.057f),V(road.At(next,-width*.5f),.057f),V(road.At(next,width*.5f),.057f),V(road.At(s,width*.5f),.057f),3);
                    foreach(int side in new[]{-1,1})
                    {
                        float edge=side*width*.5f;var point=road.At(mid,edge);
                        Route.Nearest(point,out float ringOffset);
                        if(Mathf.Abs(ringOffset)<Map.roadWidth*.5f+.6f||Network.OnBranch(point,.6f,branch))continue;
                        mesh.Quad(paving,V(road.At(s,edge),.22f),V(road.At(next,edge),.22f),V(road.At(next,edge+side*2.6f),.22f),V(road.At(s,edge+side*2.6f),.22f),2);
                        var a=V(road.At(s,edge),.15f);var b=V(road.At(next,edge),.15f);
                        mesh.Box(((int)(s/4)%2)==0?cream:dark,(a+b)*.5f,new Vector3(.18f,.3f,Vector3.Distance(a,b)+.015f),Quaternion.LookRotation(b-a));
                        Strip(paint,road.At(s,edge-side*.28f),road.At(next,edge-side*.28f),.09f,.076f);
                    }
                    if((int)(s/4)%2==0&&road.At(mid).magnitude>76)
                        foreach(float lane in width>9?new[]{-1.7f,1.7f}:new[]{0f})Strip(paint,road.At(s,lane),road.At(next,lane),.1f,.077f);
                }
                for(float s=35;s<road.Length-22;s+=55)
                {
                    RoadArrow(road.At(s,branch.Width>9?-3.4f:-1.5f),road.Heading(s));
                    var p=V(road.At(s,-width*.5f-1.5f),.22f);
                    if(Network.OnRoad(new Vector2(p.x,p.z),.5f))continue;
                    mesh.Tube(dark,p,p+Vector3.up*8,.07f,.045f);mesh.Box(silver,p+Vector3.up*8,new Vector3(.5f,.14f,1.2f));
                    var tree=road.At(s+12,-width*.5f-4.7f);
                    if(!Network.OnRoad(tree,3)&&!lots.Any(b=>b.Contains(tree)))Tree(V(tree,.1f),Rand(7,10));
                }
                if(branch.Incoming)
                {
                    float yieldS=road.Length-46;
                    for(float lane=-width*.5f+.6f;lane<width*.5f-.5f;lane+=.9f)
                    {
                        var f=Quaternion.Euler(0,road.Heading(yieldS),0);var p=V(road.At(yieldS,lane),.08f);
                        mesh.Quad(paint,p+f*new Vector3(-.28f,0,-.5f),p+f*new Vector3(0,0,.35f),p+f*new Vector3(.28f,0,-.5f),p+f*new Vector3(.28f,0,-.5f));
                    }
                    RoadSign(road,road.Length-90,"BUNDARAN HI\nKURANGI KECEPATAN\nLIHAT KANAN · BERI JALAN",false);
                    Strip(paint,road.At(3.5f,-width*.5f+.2f),road.At(3.5f,width*.5f-.2f),.4f,.08f);
                }
                else
                {
                    RoadSign(road,25,branch.Name.StartsWith("Thamrin")?"KELUAR BUNDARAN\nTHAMRIN":"KELUAR BUNDARAN\nSUDIRMAN",true);
                    RoadSign(road,road.Length-35,"UJUNG RUAS DEMO  30 M\nBERHENTI · R UNTUK ULANG",false);
                    Strip(paint,road.At(road.Length-3.5f,-width*.5f+.2f),road.At(road.Length-3.5f,width*.5f-.2f),.4f,.08f);
                }
            }
        }
        void RoadArrow(Vector2 p,float heading)
        {
            var r=Quaternion.Euler(0,heading,0);var c=V(p,.08f);
            mesh.Box(paint,c+r*new Vector3(0,0,-.6f),new Vector3(.18f,.012f,2.3f),r);
            foreach(int side in new[]{-1,1})mesh.Box(paint,c+r*new Vector3(side*.35f,0,.55f),new Vector3(.16f,.014f,1.05f),r*Quaternion.Euler(0,-side*42,0));
        }
        void RoadSign(LandmarkRoute route,float s,string text,bool green)
        {
            float width=Network.Branches.First(b=>b.Route==route).Width;
            var p=V(route.At(s,-width*.5f-1.3f),2.8f);var rot=Quaternion.Euler(0,route.Heading(s),0);
            mesh.Tube(silver,p-Vector3.up*2.6f,p+Vector3.up*.9f,.05f,.045f);
            mesh.Box(green?glass[3]:dark,p+Vector3.up*.6f,new Vector3(5,1.65f,.14f),rot);
            Label(text,p+Vector3.up*.6f-rot*Vector3.forward*.09f,-(rot*Vector3.forward),.025f);
        }
        void Building(JaksaStreet.Building b)
        {
            if(b.points.Length<3||b.kind=="roof")return;
            var ps=b.points.ToList();float area=0;for(int i=0;i<ps.Count;i++){var p=ps[i];var q=ps[(i+1)%ps.Count];area+=p.x*q.y-q.x*p.y;}if(area<0)ps.Reverse();
            var bounds=Rect.MinMaxRect(ps.Min(p=>p.x),ps.Min(p=>p.y),ps.Max(p=>p.x),ps.Max(p=>p.y));lots.Add(bounds);
            float h=b.height>0?b.height:b.levels>0?b.levels*3.5f:Rand(6,14),bottom=b.minLevel*3.5f;
            if(h<=bottom)h=bottom+3.5f;
            Route.Nearest(bounds.center,out float roadDistance);bool detail=Mathf.Abs(roadDistance)<160;
            bool curtain=h>65 && !(Place==JakartaPlace.BundaranHI && bounds.center.x>25 && bounds.center.x<130 && bounds.center.y< -65 && bounds.center.y> -165);
            Material body=curtain?glass[rng.Next(glass.Length)]:wall[rng.Next(wall.Length)];
            var tris=JaksaStreet.Triangulate(ps);
            for(int i=0;i<tris.Count;i+=3)mesh.Quad(stone,V(ps[tris[i]],h),V(ps[tris[i+2]],h),V(ps[tris[i+1]],h),V(ps[tris[i+1]],h),4);
            for(int i=0;i<ps.Count;i++)
            {
                var a=V(ps[i],bottom);var b2=V(ps[(i+1)%ps.Count],bottom);var d=(b2-a).normalized;var n=new Vector3(d.z,0,-d.x);float w=Vector3.Distance(a,b2),height=h-bottom;
                mesh.Quad(body,b2,a,a+Vector3.up*height,b2+Vector3.up*height,4);
                if(!detail||w<2)continue;
                int bays=Mathf.Max(1,Mathf.RoundToInt(w/(curtain?4:3.8f))),floors=Mathf.Clamp(Mathf.RoundToInt(height/3.6f),1,55);float bw=w/bays,fh=height/floors;
                for(int f=0;f<floors;f++)
                {
                    for(int j=0;j<bays;j++)
                    {
                        Vector3 c=a+d*bw*(j+.5f)+Vector3.up*(fh*(f+.56f))+n*.07f;
                        Vector3 u=d*(curtain?bw*.45f:bw*.32f),v=Vector3.up*(curtain?fh*.42f:fh*.26f);
                        mesh.Quad(glass[(j+f)%glass.Length],c+u-v,c-u-v,c-u+v,c+u+v);
                        if(h<65&&f<5)mesh.Box(cream,c-v+n*.1f,new Vector3(u.magnitude*2+.15f,.12f,.26f),Quaternion.LookRotation(n));
                    }
                    if(curtain)mesh.Tube(silver,a+Vector3.up*(f+1)*fh+n*.12f,b2+Vector3.up*(f+1)*fh+n*.12f,.045f,sides:4);
                }
            }
            BuildingCount++;
        }
        void Ring(Material material,Vector3 center,float inner,float outer,float height=0,int count=128)
        {
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
                Vector3 D(float angle,float radius,float y)=>center+new Vector3(Mathf.Sin(angle)*radius,y,Mathf.Cos(angle)*radius);
                mesh.Quad(material,D(a,inner,height),D(a,outer,height),D(b,outer,height),D(b,inner,height),3);
                if(height>0)mesh.Quad(material,D(a,outer,0),D(b,outer,0),D(b,outer,height),D(a,outer,height),2);
            }
        }
        void Taper(Material material,Vector3 center,float y0,float y1,float bottom,float top)
        {
            var ps=new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1)};
            for(int i=0;i<4;i++)
            {var a=ps[i];var b=ps[(i+1)%4];mesh.Quad(material,center+new Vector3(a.x*bottom/2,y0,a.y*bottom/2),center+new Vector3(b.x*bottom/2,y0,b.y*bottom/2),center+new Vector3(b.x*top/2,y1,b.y*top/2),center+new Vector3(a.x*top/2,y1,a.y*top/2),3);}
            mesh.Box(material,center+Vector3.up*y1,new Vector3(top,.1f,top),repeat:3);
        }
        void Ellipsoid(Material material,Vector3 center,Vector3 radii)
        {
            for(int j=0;j<8;j++)for(int i=0;i<12;i++)
            {
                Vector3 P(int a,int b){float u=a*Mathf.PI*2/12,v=-Mathf.PI/2+b*Mathf.PI/8;return center+Vector3.Scale(radii,new Vector3(Mathf.Cos(v)*Mathf.Sin(u),Mathf.Sin(v),Mathf.Cos(v)*Mathf.Cos(u)));}
                mesh.Quad(material,P(i,j),P(i+1,j),P(i+1,j+1),P(i,j+1));
            }
        }
        void BundaranHI()
        {
            Ring(stone,Vector3.zero,0,49,.3f);Water.AddPool(new Vector3(0,.34f,0),44,true);Ring(cream,Vector3.zero,44,45,.55f);
            Ring(grass,Vector3.zero,46,50,.28f);Ring(paving,Vector3.zero,50,52,.26f);
            Ring(stone,Vector3.zero,0,6,.7f);mesh.Box(stone,new Vector3(0,1.1f,0),new Vector3(7,.8f,5));
            // The documented 10 m pedestal supports two approximately 7 m waving bronze figures.
            foreach(int side in new[]{-1,1})Taper(stone,new Vector3(side*1.5f,0,0),1.5f,10,1.25f,.9f);
            mesh.Box(stone,new Vector3(0,10,.15f),new Vector3(5,.55f,3.8f));
            WelcomeFigure(new Vector3(-1.15f,10.3f,0),false);WelcomeFigure(new Vector3(1.15f,10.3f,0),true);
            Label("BUNDARAN HI",new Vector3(0,.7f,47),Vector3.forward,.043f);
            // Authored transit entrance, a recognizable street detail beside the north approach.
            mesh.Box(dark,new Vector3(-19,1.2f,112),new Vector3(4,2.4f,18));mesh.Box(glass[2],new Vector3(-19,2.6f,112),new Vector3(4.6f,.25f,19));
            Label("MRT  BUNDARAN HI",new Vector3(-19,2.2f,102.9f),Vector3.back,.027f);
        }
        void WelcomeFigure(Vector3 p,bool woman)
        {
            Vector3 P(float x,float y,float z)=>p+new Vector3(x,y,z);
            mesh.Tube(bronze,P(-.35f,.2f,.25f),P(-.3f,2.25f,0),.22f,.32f,10);
            mesh.Tube(bronze,P(.4f,.1f,-.15f),P(.35f,2.3f,0),.23f,.32f,10);
            Ellipsoid(bronze,P(-.35f,.12f,.42f),new Vector3(.25f,.14f,.48f));Ellipsoid(bronze,P(.4f,.12f,.05f),new Vector3(.25f,.14f,.48f));
            if(woman)Taper(bronze,p,1.7f,3.1f,1.6f,.85f);
            Ellipsoid(bronze,P(0,3.15f,0),new Vector3(.65f,1.1f,.38f));
            mesh.Tube(bronze,P(0,3.9f,0),P(0,4.45f,0),.21f,.2f,10);
            Ellipsoid(bronze,P(0,4.62f,.04f),new Vector3(.37f,.48f,.34f));
            if(woman)Ellipsoid(bronze,P(.12f,4.7f,-.24f),new Vector3(.43f,.37f,.27f));
            Ellipsoid(bronze,P(0,4.57f,.36f),new Vector3(.09f,.13f,.13f));
            mesh.Tube(bronze,P(-.5f,3.9f,0),P(-1.15f,5.2f,.05f),.24f,.19f,10);
            mesh.Tube(bronze,P(-1.15f,5.2f,.05f),P(-1.65f,6.3f,.12f),.19f,.12f,10);
            Ellipsoid(bronze,P(-1.68f,6.45f,.14f),new Vector3(.26f,.32f,.1f));
            for(int f=0;f<5;f++)mesh.Tube(bronze,P(-1.9f+f*.1f,6.52f,.15f),P(-2.05f+f*.16f,6.9f-Mathf.Abs(f-2)*.08f,.15f),.045f,.025f,6);
            mesh.Tube(bronze,P(.5f,3.9f,0),P(1,3.25f,.1f),.23f,.16f,10);mesh.Tube(bronze,P(1,3.25f,.1f),P(.85f,3.7f,.45f),.16f,.12f,10);
            if(woman)for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;var flower=P(.85f+Mathf.Sin(a)*.32f,4.05f+Mathf.Cos(a)*.25f,.5f);mesh.Tube(bronze,P(.85f,3.65f,.45f),flower,.025f);Ellipsoid(bronze,flower,Vector3.one*.15f);}
        }
        void Monas()
        {
            mesh.Box(paving,new Vector3(0,.12f,0),new Vector3(170,.24f,170),repeat:4);
            for(int i=0;i<4;i++)mesh.Box(cream,new Vector3(0,.3f+i*.3f,0),new Vector3(94-i*4,.6f,94-i*4),repeat:5);
            Taper(cream,Vector3.zero,1.5f,8,25,20);Taper(cream,Vector3.zero,8,16.3f,20,45);mesh.Box(cream,new Vector3(0,16.7f,0),new Vector3(46,.8f,46),repeat:5);
            Taper(cream,Vector3.zero,17,111,8,5.5f);Taper(cream,Vector3.zero,111,114,5.5f,16);
            mesh.Box(dark,new Vector3(0,115,0),new Vector3(12,1.6f,12));mesh.Box(cream,new Vector3(0,116.1f,0),new Vector3(17,.6f,17));
            for(int i=-6;i<=6;i+=2)foreach(int side in new[]{-1,1})
            {mesh.Box(cream,new Vector3(i,115,side*6.1f),new Vector3(.16f,1.6f,.2f));mesh.Box(cream,new Vector3(side*6.1f,115,i),new Vector3(.2f,1.6f,.16f));}
            Taper(cream,Vector3.zero,116.4f,118,17,5.5f);
            Flame(new Vector3(0,118,0));
            for(int side=0;side<4;side++)
            {
                var rot=Quaternion.Euler(0,side*90,0);
                for(int i=-5;i<=5;i++)
                {
                    var p=rot*new Vector3(i*6,2,43);mesh.Box(stone,p,new Vector3(5,.9f,.18f),rot);
                    for(int j=0;j<4;j++)mesh.Box(bronze,p+rot*new Vector3(-1.8f+j*1.2f,.1f,.12f),new Vector3(.4f,.65f,.2f),rot);
                }
                mesh.Box(paving,rot*new Vector3(0,.09f,230),new Vector3(18,.18f,300),rot,3);
            }
            // Formal lawns and planted paths retain intentional park space around the monument.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                mesh.Box(paving,new Vector3(sx*160,.08f,sz*160),new Vector3(180,.16f,9),Quaternion.Euler(0,-sx*sz*45,0));
                Ring(paving,new Vector3(sx*215,.1f,sz*200),26,30);
            }
            foreach(int side in new[]{-1,1}) { var pool=new Vector3(side*250,.34f,0);Ring(stone,pool,22,23,.22f);Water.AddPool(pool,22,false); }
            Label("MONUMEN NASIONAL",new Vector3(0,1.8f,48),Vector3.forward,.06f);
            for(float x=-330;x<340;x+=22)foreach(float z in new[]{-315f,315f})Tree(new Vector3(x,.15f,z),Rand(8,13));
            for(float z=-300;z<310;z+=24)foreach(float x in new[]{-315f,315f})Tree(new Vector3(x,.15f,z),Rand(8,13));
        }
        void Flame(Vector3 p)
        {
            float[] ys={0,2,5,8,11,14},r={1.5f,3,2.3f,1.7f,1,0};
            for(int k=0;k<ys.Length-1;k++)for(int i=0;i<24;i++)
            {
                Vector3 P(int ring,int n){float a=n*Mathf.PI/12+ring*.28f;return p+new Vector3(Mathf.Sin(a)*r[ring]+ring*ring*.065f,ys[ring],Mathf.Cos(a)*r[ring]);}
                mesh.Quad(gold,P(k,i),P(k,i+1),P(k+1,i+1),P(k+1,i));
            }
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;mesh.Tube(gold,p+new Vector3(Mathf.Sin(a)*2,2,Mathf.Cos(a)*2),p+new Vector3(Mathf.Sin(a)*1.3f,9+i*.5f,Mathf.Cos(a)*1.3f),.7f,.03f,9);}
        }
        void Landscape()
        {
            for(float s=0;s<Route.Length;s+=23)
            {
                foreach(int side in new[]{-1,1})
                {
                    var p=V(Route.At(s,side*10.5f),.22f);if(Network.OnBranch(new Vector2(p.x,p.z),2))continue;mesh.Tube(dark,p,p+Vector3.up*8,.075f,.05f);
                    var t=Route.At(s+1)-Route.At(s-1);var inward=new Vector3(t.y,0,-t.x).normalized*-side;
                    mesh.Tube(dark,p+Vector3.up*7.7f,p+Vector3.up*8+inward*1.5f,.045f);mesh.Box(silver,p+Vector3.up*8+inward*1.5f,new Vector3(.5f,.12f,.8f));
                    var q=V(Route.At(s+8,side*12.5f),.22f);
                    if(!Network.OnBranch(new Vector2(q.x,q.z),3)&&!lots.Any(b=>b.Contains(new Vector2(q.x,q.z)))&&(Place!=JakartaPlace.BundaranHI||side<0))Tree(q,Rand(7,11));
                }
                if(Place==JakartaPlace.Monas)
                {
                    var p=V(Route.At(s,-11.5f),.2f);var q=V(Route.At(s+23,-11.5f),.2f);
                    mesh.Tube(dark,p+Vector3.up*.5f,q+Vector3.up*.5f,.04f);mesh.Tube(dark,p+Vector3.up*1.7f,q+Vector3.up*1.7f,.04f);
                    for(float t=0;t<23;t+=1.2f){var a=V(Route.At(s+t,-11.5f),.2f);mesh.Tube(dark,a,a+Vector3.up*1.8f,.028f,sides:4);}
                }
            }
            int count=Place==JakartaPlace.BundaranHI?180:1400;
            for(int i=0;i<count;i++)
            {
                var p=new Vector2(Rand(-430,430),Rand(-490,390));Route.Nearest(p,out float offset);
                if(lots.Any(b=>b.Contains(p))||Mathf.Abs(offset)<13||Network.OnBranch(p,4)||(Place==JakartaPlace.BundaranHI&&p.magnitude<78)||(Place==JakartaPlace.Monas&&(Mathf.Abs(p.x)<18||Mathf.Abs(p.y)<18||p.magnitude<95)))continue;
                Tree(V(p,.1f),Rand(5,12));
            }
            for(float s=0;s<Route.Length;s+=54)
            {
                var p=V(Route.At(s,-9.3f),.25f);if(Network.OnBranch(new Vector2(p.x,p.z),2))continue;var rot=Quaternion.Euler(0,Route.Heading(s),0);
                mesh.Box(stone,p+Vector3.up*.25f,new Vector3(.6f,.5f,2.4f),rot);mesh.Box(wall[0],p+Vector3.up*.55f,new Vector3(.7f,.12f,2.6f),rot);
                mesh.Box(dark,p+rot*new Vector3(0,.5f,2.2f),new Vector3(.5f,1,.5f));
            }
        }
        void GroundDetails()
        {
            // The close roadside has planted soil, shrubs, wild grass and drain grilles.
            // Monument lawns and the drivable asphalt remain clear.
            for(float s=0;s<Route.Length;s+=13)
            {
                foreach(int side in new[]{-1,1})
                {
                    if(Place==JakartaPlace.BundaranHI&&side>0)continue;
                    var p=Route.At(s,side*(Map.roadWidth*.5f+6.3f));
                    if(lots.Any(b=>b.Contains(p))||Network.OnBranch(p,4))continue;
                    float heading=Route.Heading(s);var rot=Quaternion.Euler(0,heading,0);
                    mesh.Box(drySoil,V(p,.025f),new Vector3(3,.035f,7),rot,2);
                    for(int i=0;i<18;i++)GrassClump(V(p,.06f)+rot*new Vector3(Rand(-1.4f,1.4f),0,Rand(-3.3f,3.3f)),Rand(.18f,.5f));
                    if(((int)s)%3==0)Shrub(V(p,.1f),Rand(.6f,1));
                    var drain=V(Route.At(s,side*(Map.roadWidth*.5f-.26f)),.064f);
                    mesh.Box(dark,drain,new Vector3(.36f,.015f,.8f),rot);
                    for(int bar=0;bar<7;bar++)mesh.Box(silver,drain+rot*new Vector3(0,.012f,-.32f+bar*.105f),new Vector3(.32f,.014f,.018f),rot);
                }
            }
            if(Place==JakartaPlace.BundaranHI)
                for(int i=0;i<90;i++){float angle=i*Mathf.PI*2/90;Shrub(new Vector3(Mathf.Sin(angle)*48,.3f,Mathf.Cos(angle)*48),.65f);}
            else
                for(int i=0;i<2800;i++)
                {
                    var p=new Vector2(Rand(-310,310),Rand(-300,300));
                    if(Mathf.Abs(p.x)<25||Mathf.Abs(p.y)<25||p.magnitude<105||lots.Any(b=>b.Contains(p)))continue;
                    // Keep the authored diagonal park promenades and round gardens legible.
                    if(Mathf.Abs(Mathf.Abs(p.x)+Mathf.Abs(p.y)-320)<12)continue;
                    GrassClump(V(p,.02f),Rand(.15f,.38f));
                }
        }
        void GrassClump(Vector3 p,float h)
        {
            for(int i=0;i<6;i++)
            {
                float a=Rand(0,Mathf.PI*2);var across=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Rand(.025f,.045f);
                var bottom=p+new Vector3(Rand(-.25f,.25f),0,Rand(-.25f,.25f));var tip=bottom+new Vector3(Mathf.Sin(a)*h*.3f,h*Rand(.6f,1),Mathf.Cos(a)*h*.3f);
                mesh.Quad(grassBlades[i%3],bottom-across,bottom+across,tip,tip);
            }
        }
        void Shrub(Vector3 p,float size)
        {
            for(int i=0;i<5;i++)
            {
                var rotation=Quaternion.Euler(0,i*72,0);var u=rotation*Vector3.right*size*.65f;var v=Vector3.up*size*.5f;var c=p+v;
                mesh.Quad(leaves,c-u-v,c+u-v,c+u+v,c-u+v,1,new Rect(0,0,1,1));
            }
        }
        void Tree(Vector3 p,float height)
        {
            mesh.Tube(bark,p,p+Vector3.up*height*.8f,.2f,.07f,7);
            for(int b=0;b<5;b++)
            {
                float a=b*Mathf.PI*2/5;var tip=p+new Vector3(Mathf.Sin(a)*height*.26f,height*.75f,Mathf.Cos(a)*height*.26f);
                mesh.Tube(bark,p+Vector3.up*height*.4f,tip,.09f,.035f,5);
                for(int k=0;k<8;k++)
                {
                    var c=tip+new Vector3(Rand(-1,1),Rand(-.3f,1),Rand(-1,1));float size=height*.23f;
                    var rot=Quaternion.Euler(Rand(-35,35),Rand(0,180),0);var u=rot*Vector3.right*size;var v=rot*Vector3.up*size*.58f;
                    mesh.Quad(leaves,c-u-v,c+u-v,c+u+v,c-u+v,1,new Rect(0,0,1,1));
                }
            }
        }
        void Label(string text,Vector3 p,Vector3 normal,float size)
        {
            var obj=new GameObject(text);obj.transform.SetParent(Root.transform,false);obj.transform.SetPositionAndRotation(p,Quaternion.LookRotation(-normal));
            var tm=obj.AddComponent<TextMesh>();tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.text=text;tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.color=Color.white;obj.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
        }
    }
}
