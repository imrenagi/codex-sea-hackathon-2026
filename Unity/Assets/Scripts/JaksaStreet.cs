using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JalanKita
{
    /// <summary>OSM footprint/centerline based vertical slice. Elevations, facades and props are authored estimates.</summary>
    public sealed class JaksaStreet
    {
        [Serializable] public sealed class Building
        { public string id, name, roof, kind; public float levels, height, minLevel; public Vector2[] points; }
        [Serializable] public sealed class Road
        { public string id, name, kind; public float width; public Vector2[] points; }
        [Serializable] public sealed class Layout
        { public double originLatitude, originLongitude; public float bearingDegrees, length, centerAtMetres;
          public string roadOsmId; public bool oneway; public Vector2[] road; public Building[] buildings; public Road[] surroundingRoads; }

        public const float Length = 250, RoadHalfWidth = 3.25f;
        public readonly Layout Data;
        public readonly GameObject Root;
        public readonly bool IsSopoDel;
        readonly StreetMesh mesh = new();
        readonly Dictionary<string, Material> materials = new();
        readonly System.Random rng = new(4961);
        readonly List<Rect> occupied = new();
        readonly Material sky;
        readonly Light sun;
        Material asphalt, concrete, paving, roof, metal, white, yellow, dark, glass, bark, leaf, signs;
        readonly Material[] plaster = new Material[6];
        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        public float RoadX(float z)
        {
            var road = Data.road;
            for (int i = 1; i < road.Length; i++)
                if (z <= road[i].y) return Mathf.Lerp(road[i-1].x, road[i].x, Mathf.InverseLerp(road[i-1].y, road[i].y,z));
            return road[^1].x;
        }

        public JaksaStreet(Transform parent, bool sopoDel = true)
        {
            IsSopoDel=sopoDel;
            Data = JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>(sopoDel?"SopoDel/layout":"Jaksa/layout").text);
            Root = new GameObject(sopoDel?"Sopo Del · Mega Kuningan":"Jl. Jaksa · 250 m"); Root.transform.SetParent(parent, false);
            asphalt = Mat("Asphalt", "asphalt_02", "b9b6b1");
            concrete = Mat("Concrete", "concrete_floor_02", "b0b1ad");
            paving = Mat("Sidewalk paving", "brick_pavement", "b0aba1");
            roof = Mat("Terracotta roofs", "clay_roof_tiles", "a18973");
            metal = Mat("Painted steel", null, "414a48", .28f, .6f);
            white = Mat("Faded white paint", null, "dedbd0");
            yellow = Mat("Tactile paving", null, "bda349");
            dark = Mat("Recess shadow", null, "272c2a");
            glass = Mat("Smoked glazing", null, "53676a", .72f, .25f);
            bark = Mat("Bark", "bark_brown_02", "9d9588");
            string[] colors = { "d6d1c3", "aea99b", "b8c3b9", "d1b8a2", "d9d6c7", "919c9b" };
            for (int i=0;i<plaster.Length;i++) plaster[i] = Mat("Plaster " + i, "plastered_wall", colors[i]);
            leaf = Mat("Leaf canopy", null, "c5cfad");
            leaf.SetTexture("_BaseMap", Resources.Load<Texture2D>("StreetLook/tropical_foliage") ?? Resources.Load<Texture2D>("Jaksa/leaves"));
            leaf.SetFloat("_AlphaClip",1); leaf.SetFloat("_Cutoff",.45f); leaf.EnableKeyword("_ALPHATEST_ON");
            leaf.SetFloat("_Cull", 0); leaf.renderQueue = 2450;
            signs = Mat("Original shop signs", null,"ffffff");
            signs.SetTexture("_BaseMap", Resources.Load<Texture2D>("Jaksa/signs"));
            if(sopoDel)
                new SopoDelEnvironment(Root.transform,Data,mesh,Mat,RoadX,Tree).Build();
            else
            {
                BuildStreet();
                foreach (var building in Data.buildings) BuildBuilding(building);
                BuildSeaOffice();
                BuildStreetLife();
                BuildDistantBackdrop();
            }
            mesh.Build(Root.transform);
            sun = new GameObject("Jakarta daylight").AddComponent<Light>();
            sun.transform.SetParent(Root.transform,false); sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(48,-36,0); sun.intensity=1.4f;
            sun.color=new Color(1,.96f,.89f); sun.shadows=LightShadows.Soft;
            sun.shadowBias=.08f; sun.shadowNormalBias=.45f;
            sky = new Material(Shader.Find("Skybox/Procedural")) { name="Jaksa sky" };
            sky.SetColor("_SkyTint",new Color(.57f,.66f,.72f));
            sky.SetColor("_GroundColor",new Color(.42f,.43f,.4f));
            sky.SetFloat("_AtmosphereThickness",.9f); sky.SetFloat("_Exposure",1.05f);
            sky.SetFloat("_SunSize",.025f);
            var volume = new GameObject("Jaksa atmosphere").AddComponent<Volume>();
            volume.transform.SetParent(Root.transform,false); volume.isGlobal=true; volume.priority=2;
            volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var tone=volume.profile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            var color=volume.profile.Add<ColorAdjustments>(); color.postExposure.Override(.15f); color.saturation.Override(-8);
            var vignette=volume.profile.Add<Vignette>(); vignette.intensity.Override(.12f); vignette.smoothness.Override(.45f);
            Activate();
        }

        public void Activate()
        {
            Root.SetActive(true);
            RenderSettings.skybox=sky;RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.69f,.75f,.8f);
            RenderSettings.ambientEquatorColor=new Color(.42f,.44f,.43f);
            RenderSettings.ambientGroundColor=new Color(.24f,.23f,.2f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.72f,.78f,.79f);
            RenderSettings.fogStartDistance=IsSopoDel?160:80; RenderSettings.fogEndDistance=IsSopoDel?420:280;
        }

        Material Mat(string name,string texture,string hex,float smooth=.1f,float metallic=0)
        {
            ColorUtility.TryParseHtmlString("#"+hex,out var color);
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")) { name=name };
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smooth); m.SetFloat("_Metallic",metallic);
            if(texture!=null)
            {
                string texturePath=texture.Contains("/")?texture:"Jaksa/"+texture;
                m.SetTexture("_BaseMap",Resources.Load<Texture2D>(texturePath+"_albedo"));
                m.SetTexture("_BumpMap",Resources.Load<Texture2D>(texturePath+"_normal"));
                m.SetFloat("_BumpScale",.55f); m.EnableKeyword("_NORMALMAP");
            }
            materials[name]=m; return m;
        }

        void BuildStreet()
        {
            mesh.Box(concrete,new Vector3(7,-.25f,125),new Vector3(160,.3f,340),repeat:4);
            for(float z=-24;z<250;z+=2)
            {
                float x=RoadX(z),x2=RoadX(z+2);
                mesh.Quad(asphalt,new Vector3(x-3.25f,.015f,z),new Vector3(x2-3.25f,.015f,z+2),
                    new Vector3(x2+3.25f,.015f,z+2),new Vector3(x+3.25f,.015f,z),3,
                    new Rect(z/3,0,2f/3,6.5f/3));
                foreach(int side in new[]{-1,1})
                {
                    float edge=x+side*3.35f;
                    mesh.Box(concrete,new Vector3(edge,.1f,z+1),new Vector3(.22f,.2f,1.97f));
                    mesh.Box(paving,new Vector3(x+side*4.25f,.13f,z+1),new Vector3(1.6f,.22f,2),repeat:2);
                    mesh.Box(yellow,new Vector3(x+side*4.28f,.248f,z+1),new Vector3(.3f,.018f,1.96f));
                    for(int rib=0;rib<3;rib++)
                        mesh.Box(yellow,new Vector3(x+side*4.28f+(rib-1)*.085f,.263f,z+1),new Vector3(.025f,.022f,1.94f));
                    mesh.Box(white,new Vector3(x+side*3.02f,.026f,z+1),new Vector3(.085f,.012f,1.96f));
                    if(Mathf.RoundToInt(z)%14==0)
                    {
                        mesh.Box(dark,new Vector3(x+side*3.16f,.028f,z+.5f),new Vector3(.24f,.025f,.64f));
                        for(int j=0;j<6;j++) mesh.Box(metal,new Vector3(x+side*3.16f,.046f,z+.23f+j*.1f),new Vector3(.26f,.025f,.025f));
                    }
                }
            }
            // The south end meets Wahid Hasyim; it is a visual boundary for this initial slice.
            mesh.Box(asphalt,new Vector3(7,.018f,257),new Vector3(160,.025f,12),repeat:3);
            for(int i=0;i<8;i++) mesh.Box(white,new Vector3(RoadX(244)-2.8f+i*.8f,.035f,244),new Vector3(.4f,.02f,2.8f));
            for(int i=0;i<5;i++)
            {
                float z=28+i*48,x=RoadX(z);
                mesh.Box(white,new Vector3(x,.035f,z),new Vector3(.14f,.015f,2.3f));
                for(int k=0;k<5;k++)mesh.Box(white,new Vector3(x,.035f,z+1+k*.15f),new Vector3(.9f-k*.16f,.015f,.16f));
            }
        }

        void BuildBuilding(Building b)
        {
            if(b.points.Length<3)return;
            var ps=new List<Vector2>(b.points);
            float area=0;for(int i=0;i<ps.Count;i++){Vector2 p=ps[i],q=ps[(i+1)%ps.Count];area+=p.x*q.y-q.x*p.y;}
            if(area<0)ps.Reverse();
            float minx=float.MaxValue,minz=float.MaxValue,maxx=float.MinValue,maxz=float.MinValue;
            foreach(var p in ps){minx=Mathf.Min(minx,p.x);maxx=Mathf.Max(maxx,p.x);minz=Mathf.Min(minz,p.y);maxz=Mathf.Max(maxz,p.y);}
            occupied.Add(Rect.MinMaxRect(minx,minz,maxx,maxz));
            int floors=b.levels>0?Mathf.RoundToInt(b.levels):b.name.Contains("Morrissey")?9:b.name.Contains("Dafam")?6:rng.Next(1,4);
            float h=floors*3.25f; Material wall=plaster[rng.Next(plaster.Length)];
            var cap=Triangulate(ps);
            for(int i=0;i<cap.Count;i+=3)
            {
                Vector3 P(int n)=>new(ps[n].x,h,ps[n].y);
                Vector3 a=P(cap[i]),c=P(cap[i+1]),d=P(cap[i+2]);
                mesh.Quad(concrete,a,d,c,c,3);
            }
            for(int e=0;e<ps.Count;e++)
            {
                Vector2 a=ps[e],b2=ps[(e+1)%ps.Count];
                Vector3 p=new(a.x,0,a.y),q=new(b2.x,0,b2.y);
                Vector3 edge=q-p,along=edge.normalized,normal=new(along.z,0,-along.x);
                float width=edge.magnitude;
                mesh.Quad(wall,q,p,p+Vector3.up*h,q+Vector3.up*h,3);
                Quaternion rot=Quaternion.LookRotation(normal);
                Vector3 center=(p+q)*.5f;
                void Detail(Material m,float u,float y,float outwards,Vector3 size)=>
                    mesh.Box(m,center+along*u+Vector3.up*y+normal*outwards,size,rot,2);
                Detail(concrete,0,.25f,.06f,new Vector3(width,.5f,.12f));
                Detail(concrete,0,h+.12f,0,new Vector3(width+.16f,.24f,.32f));
                bool frontage=Mathf.Abs(center.x-RoadX(center.z))<25 && Vector3.Dot(normal,new Vector3(RoadX(center.z)-center.x,0,0).normalized)>.45f;
                if(!frontage && Mathf.Abs(center.x-RoadX(center.z))>45)continue;
                int bays=Mathf.Max(1,Mathf.FloorToInt(width/3.1f));float bay=width/bays;
                for(int f=0;f<floors;f++)
                {
                    if(f>0)Detail(concrete,0,f*3.25f,.12f,new Vector3(width,.18f,.25f));
                    for(int j=0;j<bays;j++)
                    {
                        float u=-width*.5f+bay*(j+.5f),y=f*3.25f+1.9f,w=Mathf.Min(1.65f,bay*.65f);
                        bool shop=f==0&&frontage&&width>3&&floors<5;
                        if(shop)
                        {
                            Detail(dark,u,1.42f,.025f,new Vector3(bay*.82f,2.64f,.07f));
                            Detail(metal,u,2.6f,.14f,new Vector3(bay*.85f,.32f,.17f));
                            for(int slat=0;slat<7;slat++)Detail(metal,u,2.45f-slat*.07f,.18f,new Vector3(bay*.82f,.025f,.04f));
                            Detail(glass,u+.28f,1.3f,.12f,new Vector3(Mathf.Max(.3f,bay*.49f),2.3f,.035f));
                            Detail(metal,u-bay*.41f,1.4f,.13f,new Vector3(.07f,2.8f,.1f));
                            Detail(metal,u,1.4f,.16f,new Vector3(.055f,2.7f,.1f));
                            Detail(concrete,u,.15f,.7f,new Vector3(bay*.9f,.25f,1.4f));
                            if(j==0)Sign(center+along*u+Vector3.up*3+normal*.18f,normal,Mathf.Min(width*.85f,4.4f),.65f,rng.Next(0,12));
                        }
                        else
                        {
                            Detail(dark,u,y,.025f,new Vector3(w+.18f,1.55f,.06f));
                            Detail(glass,u,y,.075f,new Vector3(w,1.4f,.07f));
                            Detail(white,u,y-.79f,.18f,new Vector3(w+.3f,.13f,.38f));
                            Detail(metal,u,y,.13f,new Vector3(.05f,1.43f,.09f));
                            Detail(metal,u,y+.12f,.13f,new Vector3(w,.045f,.09f));
                            if(frontage&&f>0&&j%2==0)
                            {
                                Detail(concrete,u,y+.99f,.36f,new Vector3(w+.45f,.14f,.85f));
                                Detail(white,u+.76f,y-.8f,.45f,new Vector3(.75f,.48f,.5f));
                                for(int slat=0;slat<5;slat++)Detail(metal,u+.76f,y-.95f+slat*.07f,.71f,new Vector3(.6f,.017f,.02f));
                            }
                        }
                    }
                }
                if(frontage && width>4 && floors<=3)
                {
                    Material canopy=plaster[rng.Next(plaster.Length)];
                    mesh.Box(canopy,center+Vector3.up*2.85f+normal*1.05f,new Vector3(width+.25f,.1f,1.9f),rot*Quaternion.Euler(-9,0,0));
                    foreach(float u in new[]{-width*.48f,width*.48f})
                        mesh.Tube(metal,center+along*u+normal*1.8f+Vector3.up*.2f,center+along*u+normal*1.8f+Vector3.up*2.7f,.035f);
                }
                if(frontage && width>5 && floors<=3)
                {
                    mesh.Tube(metal,p+normal*.17f+Vector3.up*.1f,p+normal*.17f+Vector3.up*h,.065f);
                    if(floors>=2)
                    {
                        Detail(concrete,0,3.25f,.55f,new Vector3(width,.15f,1.2f));
                        Detail(metal,0,4.28f,1.05f,new Vector3(width,.055f,.055f));
                        for(float u=-width*.48f;u<width*.5f;u+=.32f)
                            Detail(metal,u,3.78f,1.05f,new Vector3(.035f,1.0f,.035f));
                    }
                }
            }
            if(floors<=2 && maxx-minx<24 && maxz-minz<35)
            {
                float mid=(minx+maxx)/2,rise=Mathf.Min(2.3f,(maxx-minx)*.2f);
                mesh.Quad(roof,new Vector3(minx-.3f,h,minz-.4f),new Vector3(minx-.3f,h,maxz+.4f),
                    new Vector3(mid,h+rise,maxz+.4f),new Vector3(mid,h+rise,minz-.4f),2.5f);
                mesh.Quad(roof,new Vector3(mid,h+rise,minz-.4f),new Vector3(mid,h+rise,maxz+.4f),
                    new Vector3(maxx+.3f,h,maxz+.4f),new Vector3(maxx+.3f,h,minz-.4f),2.5f);
            }
        }

        void Sign(Vector3 p,Vector3 normal,float w,float h,int index)
        {
            var right=Vector3.Cross(Vector3.up,normal)*w*.5f;var up=Vector3.up*h*.5f;
            mesh.Box(metal,p-normal*.035f,new Vector3(w+.07f,h+.07f,.065f),Quaternion.LookRotation(normal));
            int col=index%4,row=index/4;
            mesh.Quad(signs,p-right-up,p+right-up,p+right+up,p-right+up,1,
                new Rect(col*.25f,1-(row+1)*.25f,.25f,.25f),flipU:true);
        }

        void BuildSeaOffice()
        {
            // Authored landmark requested for the game; this is not Sea's actual office location.
            const float z=127, depth=28, width=16, height=16;
            float front=RoadX(z)-8.4f, x=front-width*.5f;
            occupied.Add(Rect.MinMaxRect(x-width*.5f,z-depth*.5f,front,z+depth*.5f));
            // Reserve an open forecourt so generic fences and trees do not block the entry signs.
            occupied.Add(Rect.MinMaxRect(front,107,RoadX(z)-4.9f,145));
            Material cladding=Mat("Sea office cladding",null,"edece8",.3f);
            Material glazing=Mat("Sea office glass",null,"547b8b",.8f,.35f);
            Material logo=Mat("Sea official logo",null,"ffffff");
            logo.SetTexture("_BaseMap",Resources.Load<Texture2D>("Jaksa/sea-logo"));
            logo.SetFloat("_AlphaClip",1);logo.SetFloat("_Cutoff",.1f);logo.EnableKeyword("_ALPHATEST_ON");
            logo.renderQueue=2450;
            mesh.Box(paving,new Vector3(front+1.7f,.12f,z),new Vector3(3.4f,.24f,36));
            mesh.Box(cladding,new Vector3(x,.15f,z),new Vector3(width+1,.3f,depth+1));
            mesh.Box(glazing,new Vector3(x,height*.5f,z),new Vector3(width,height,depth));
            // Horizontal floor plates and light vertical fins form a distinct modern office frontage.
            for(int floor=0;floor<=4;floor++)
                mesh.Box(cladding,new Vector3(x,floor*4+.2f,z),new Vector3(width+.35f,.36f,depth+.35f));
            for(float bay=-depth*.5f;bay<=depth*.5f;bay+=3.5f)
                mesh.Box(cladding,new Vector3(front+.19f,height*.5f,z+bay),new Vector3(.4f,height,.18f));
            for(float bay=-width*.5f;bay<=width*.5f;bay+=2)
                mesh.Box(cladding,new Vector3(x+bay,height*.5f,z-depth*.5f-.19f),new Vector3(.16f,height,.4f));
            mesh.Box(cladding,new Vector3(x,height+1,z),new Vector3(width+.4f,1.7f,depth+.4f));
            // Entrance canopy, steps and a glazed double door face the street.
            mesh.Box(cladding,new Vector3(front+1.6f,3.8f,z),new Vector3(3.3f,.22f,9));
            mesh.Box(concrete,new Vector3(front+1.35f,.28f,z),new Vector3(2.8f,.3f,8));
            foreach(float offset in new[]{-3.9f,3.9f})
                mesh.Box(metal,new Vector3(front+3,1.95f,z+offset),new Vector3(.1f,3.6f,.1f));
            mesh.Box(metal,new Vector3(front+.23f,1.75f,z),new Vector3(.08f,3.15f,.09f));
            foreach(float offset in new[]{-.2f,.2f})
                mesh.Box(white,new Vector3(front+.29f,1.65f,z+offset),new Vector3(.06f,.6f,.045f));
            void Logo(Vector3 center,Vector3 normal,float w)
            {
                // Frame the mark inside the official 600 x 400 source without distorting it.
                float h=w*180f/400f;
                var right=Vector3.Cross(Vector3.up,normal)*w*.5f;var up=Vector3.up*h*.5f;
                mesh.Box(cladding,center-normal*.055f,new Vector3(w+.8f,h+.65f,.1f),Quaternion.LookRotation(normal));
                mesh.Quad(logo,center-right-up,center+right-up,center+right+up,center-right+up,1,
                    new Rect(100f/600f,.25f,400f/600f,.45f),flipU:true);
            }
            Logo(new Vector3(x,12.4f,z-depth*.5f-.45f),Vector3.back,8);
            Logo(new Vector3(front+.45f,12.4f,z),Vector3.right,8);
            // A low entry pylon is readable from the driver's approach.
            float pylonX=RoadX(110)-5.7f;
            mesh.Box(cladding,new Vector3(pylonX,1.3f,110),new Vector3(2.7f,2.6f,.4f));
            Logo(new Vector3(pylonX,1.85f,109.78f),Vector3.back,2.15f);
        }

        void BuildStreetLife()
        {
            // Original, approximate props fill the unmapped verges, never the drivable lane.
            for(int side=-1;side<=1;side+=2)
            {
                for(float z=4;z<248;z+=1.75f)
                {
                    float x=RoadX(z)+side*5.5f;
                    if(InsideBuilding(x,z,1))continue;
                    bool gate=Mathf.FloorToInt(z/8)%4==0;
                    bool railing=Mathf.FloorToInt(z/24)%3!=1;
                    float height=gate?.24f:railing?.6f:1.25f;
                    mesh.Box(plaster[Mathf.FloorToInt(z/28)%4],new Vector3(x,.24f+height*.5f,z),new Vector3(.18f,height,1.74f));
                    mesh.Box(concrete,new Vector3(x,.26f+height,z),new Vector3(.25f,.07f,1.74f));
                    if(Mathf.RoundToInt((z-4)/1.75f)%4==0)mesh.Box(concrete,new Vector3(x,1.02f,z),new Vector3(.26f,1.55f,.26f));
                    if(gate||railing)
                    {
                        for(float k=-.8f;k<.9f;k+=.18f)mesh.Box(metal,new Vector3(x,1.22f,z+k),new Vector3(.035f,.85f,.025f));
                        mesh.Box(metal,new Vector3(x,1.66f,z),new Vector3(.045f,.045f,1.74f));
                    }
                }
                for(float z=9;z<242;z+=Rand(13,22))
                {
                    float x=RoadX(z)+side*Rand(5,6.8f);
                    if(!InsideBuilding(x,z,1))Tree(new Vector3(x,.15f,z),Rand(6.5f,10.5f));
                }
                for(float z=13;z<244;z+=27)
                {
                    Vector3 p=new(RoadX(z)+side*3.8f,.24f,z);
                    mesh.Tube(metal,p,p+Vector3.up*7,.095f,.06f);
                    mesh.Tube(metal,p+Vector3.up*6.6f,p+new Vector3(-side*1.35f,7.1f,.5f),.055f,.035f);
                    mesh.Box(metal,p+new Vector3(-side*1.4f,7.08f,.5f),new Vector3(.32f,.12f,.75f),Quaternion.Euler(0,20,0));
                    mesh.Box(white,p+new Vector3(-side*1.4f,7.01f,.5f),new Vector3(.25f,.025f,.6f));
                    if(z+27<244)
                        for(int cable=0;cable<5;cable++)
                        {
                            Vector3 prev=p+new Vector3(cable*.09f,5.7f+cable*.15f,0);
                            for(int k=1;k<=12;k++)
                            {
                                float t=k/12f;
                                Vector3 next=new(RoadX(z+27*t)+side*3.8f+cable*.09f,5.94f+cable*.15f-Mathf.Sin(t*Mathf.PI)*.65f,z+27*t);
                                mesh.Tube(metal,prev,next,.012f,sides:4);prev=next;
                            }
                        }
                }
            }
            for(int i=0;i<10;i++)
            {
                float z=16+i*23.4f,side=i%2==0?-1:1;
                Vector3 p=new(RoadX(z)+side*4.95f,.24f,z);
                if(InsideBuilding(p.x,p.z,.7f))continue;
                mesh.Box(plaster[3],p+Vector3.up*.32f,new Vector3(.65f,.64f,.7f));
                for(int k=0;k<12;k++)LeafCard(p+new Vector3(Rand(-.3f,.3f),Rand(.6f,1.2f),Rand(-.3f,.3f)),Rand(.4f,.8f));
            }
            // Food carts, stools, umbrellas, and an entrance sign are authored local details.
            foreach(float z in new[]{32f,91f,166f,213f})
            {
                Vector3 p=new(RoadX(z)-4.9f,.26f,z);
                mesh.Box(plaster[2],p+Vector3.up*.68f,new Vector3(.8f,1.1f,1.65f));
                mesh.Box(glass,p+Vector3.up*1.3f,new Vector3(.78f,.45f,1.55f));
                mesh.Box(metal,p+Vector3.up*1.57f,new Vector3(.88f,.07f,1.75f));
                for(int s=-1;s<=1;s+=2)mesh.Tube(dark,p+new Vector3(-.45f,.1f,s*.53f),p+new Vector3(.45f,.1f,s*.53f),.17f,sides:12);
                Sign(p+new Vector3(.415f,.85f,0),Vector3.right,1.4f,.35f,12);
                mesh.Tube(metal,p+new Vector3(-.6f,0,0),p+new Vector3(-.6f,2.7f,0),.025f);
                Vector3 peak=p+new Vector3(-.6f,2.8f,0);
                for(int k=0;k<10;k++)
                {
                    float a=k*Mathf.PI/5,b=(k+1)*Mathf.PI/5;
                    mesh.Quad(k%2==0?plaster[3]:plaster[4],peak,peak+new Vector3(Mathf.Cos(a)*1.4f,-.48f,Mathf.Sin(a)*1.4f),
                        peak+new Vector3(Mathf.Cos(b)*1.4f,-.48f,Mathf.Sin(b)*1.4f),peak,2);
                }
                mesh.Box(plaster[1],p+new Vector3(0,.22f,1.6f),new Vector3(.42f,.45f,.42f));
            }
            Sign(new Vector3(RoadX(8)-4.15f,2.5f,8),Vector3.back,2.3f,.6f,13);
            mesh.Tube(metal,new Vector3(RoadX(8)-4.15f,.2f,8),new Vector3(RoadX(8)-4.15f,2.9f,8),.04f);
        }

        bool InsideBuilding(float x,float z,float margin)
        {
            foreach(var rect in occupied)if(x>rect.xMin-margin&&x<rect.xMax+margin&&z>rect.yMin-margin&&z<rect.yMax+margin)return true;
            return false;
        }

        void BuildDistantBackdrop()
        {
            // Approximate scenery beyond the playable boundary closes the Wahid Hasyim view.
            // These authored blocks are deliberately separate from the OSM footprint dataset.
            mesh.Box(paving,new Vector3(7,.13f,265),new Vector3(160,.22f,3));
            mesh.Box(concrete,new Vector3(7,.11f,263.4f),new Vector3(160,.22f,.2f));
            for(int i=0;i<5;i++)
            {
                float x=4+i*20,z=272+i%2*2;
                BuildBuilding(new Building
                {
                    id="authored-background-"+i,name="",levels=2+i%3,
                    points=new[]{new Vector2(x,z),new Vector2(x+19,z),new Vector2(x+19,z+22),new Vector2(x,z+22)}
                });
            }
            foreach(float x in new[]{19f,43f,67f})Tree(new Vector3(x,.24f,266),7.5f);
        }
        void Tree(Vector3 p,float h)
        {
            mesh.Tube(bark,p,p+new Vector3(.25f,h*.7f,.15f),.19f,.1f,9);
            for(int k=0;k<9;k++)
            {
                float a=k*Mathf.PI*2/9; Vector3 end=p+new Vector3(Mathf.Cos(a)*2.7f,h*.8f+Rand(-.9f,.9f),Mathf.Sin(a)*2.7f);
                mesh.Tube(bark,p+new Vector3(.15f,h*.48f,0),end,.095f,.015f);
                Vector3 twig=end+new Vector3(Mathf.Cos(a+.5f)*.8f,.4f,Mathf.Sin(a+.5f)*.8f);
                mesh.Tube(bark,end-Vector3.up*.3f,twig,.032f,.009f,5);
            }
            for(int k=0;k<165;k++)
            {
                float a=Rand(0,Mathf.PI*2),radius=Mathf.Sqrt(Rand(0,1))*3.9f;
                LeafCard(p+new Vector3(Mathf.Cos(a)*radius,h*.8f+Rand(-1.6f,1.6f)*(1-radius/5),Mathf.Sin(a)*radius),Rand(1.1f,2.15f));
            }
        }
        void LeafCard(Vector3 p,float size)
        {
            Quaternion rot=Quaternion.Euler(Rand(25,150),Rand(0,360),Rand(0,360));
            Vector3 u=rot*Vector3.right*size*.5f,v=rot*Vector3.up*size*.5f;
            mesh.Quad(leaf,p-u-v,p+u-v,p+u+v,p-u+v,1,new Rect(0,0,1,1));
        }

        internal static List<int> Triangulate(List<Vector2> p)
        {
            var remaining=new List<int>();var triangles=new List<int>();for(int i=0;i<p.Count;i++)remaining.Add(i);
            float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
            int guard=p.Count*p.Count;
            while(remaining.Count>2&&guard-->0)
            {
                bool found=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                    if(Cross(p[b]-p[a],p[c]-p[b])<=.0001f)continue;
                    bool inside=false;
                    foreach(int n in remaining)
                        if(n!=a&&n!=b&&n!=c&&Cross(p[b]-p[a],p[n]-p[a])>=0&&Cross(p[c]-p[b],p[n]-p[b])>=0&&Cross(p[a]-p[c],p[n]-p[c])>=0){inside=true;break;}
                    if(inside)continue;triangles.AddRange(new[]{a,b,c});remaining.RemoveAt(i);found=true;break;
                }
                if(!found)break;
            }
            return triangles;
        }
    }
}
