using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JalanKita
{
    // The real Sea Labs Indonesia address: Sopo Del, The Sky, Mega Kuningan.
    // Road and footprint geometry are OSM data. Architectural detail is modeled from
    // developer/architect/facade-supplier references; small dimensions are estimates.
    public sealed partial class SopoDelEnvironment
    {
        readonly Transform root;
        readonly JaksaStreet.Layout data;
        readonly StreetMesh mesh;
        readonly Func<float,float> roadX;
        readonly Action<Vector3,float> tree;
        readonly Material road,pavement,stone,silver,dark,white,paint,soil,green,red;
        readonly Material[] glazing,contextWalls;
        readonly System.Random random=new(71126);
        float Rand(float a,float b)=>a+(float)random.NextDouble()*(b-a);

        public SopoDelEnvironment(Transform root,JaksaStreet.Layout data,StreetMesh mesh,
            Func<string,string,string,float,float,Material> mat,Func<float,float> roadX,Action<Vector3,float> tree)
        {
            this.root=root;this.data=data;this.mesh=mesh;this.roadX=roadX;this.tree=tree;
            road=mat("Sopo asphalt","StreetLook/Surfaces/asphalt","dededb",.12f,0);
            pavement=mat("Sopo granite pavement","concrete_floor_02","a7aaa7",.2f,0);
            stone=mat("Sopo charcoal stone","concrete_floor_02","8c9295",.28f,.08f);
            silver=mat("Sopo aluminium fins",null,"c2c8c9",.55f,.45f);
            dark=mat("Sopo recess",null,"273239",.2f,0);
            white=mat("Sopo pale concrete",null,"c5c7c4",.16f,0);
            paint=mat("Sopo road markings",null,"e6e1cf",.12f,0);
            soil=mat("Sopo landscaped ground","SopoDel/grass_ground","9cba86",.1f,0);
            green=mat("Sopo trimmed hedges",null,"4f6b39",.08f,0);
            red=mat("Sopo red identity",null,"bb3029",.2f,0);
            string[] tones={"577585","6b8795","8099a4","526b7b","7894a1","607d8a"};
            glazing=tones.Select((c,i)=>mat("Sopo glazing "+i,null,c,.78f,.35f)).ToArray();
            contextWalls=new[]{"b8b1a3","aebfc3","b3b9ad"}.Select((c,i)=>mat("Sopo surrounding plaster "+i,"plastered_wall",c,.12f,0)).ToArray();
            PrepareDressing(mat);
        }

        public void Build()
        {
            mesh.Box(soil,new Vector3(20,-.18f,140),new Vector3(900,.3f,1000),repeat:4);
            BuildRoads();
            foreach(var b in data.buildings.Where(b=>!b.name.StartsWith("Sopo Del")))ContextBuilding(b);
            var podium=data.buildings.Single(b=>b.id=="483602812");
            Facade(podium.points,0,7,1,true,false);
            Facade(podium.points,7,31,6,false,false);
            Tower(data.buildings.Single(b=>b.id=="483610514"),167,41,true);
            // The OSM level count for B conflicts with the developer's published 28-floor layout.
            // 116 m is an authored height estimate; the mapped footprint is retained unchanged.
            Tower(data.buildings.Single(b=>b.id=="483602802"),116,28,false);
            var roundVolume=new Vector2[32];
            for(int i=0;i<roundVolume.Length;i++)
            {
                float angle=i*Mathf.PI*2/roundVolume.Length;
                roundVolume[i]=new Vector2(89.15f+Mathf.Cos(angle)*16.5f,191.6f+Mathf.Sin(angle)*16.5f);
            }
            Facade(roundVolume,31,42,3,true,false);
            FillVacantLand();Forecourt();StreetFurniture();ForecourtDetails();
        }

        static List<Vector2> Polygon(Vector2[] source)
        {
            var ps=new List<Vector2>(source);float area=0;
            for(int i=0;i<ps.Count;i++){var a=ps[i];var b=ps[(i+1)%ps.Count];area+=a.x*b.y-b.x*a.y;}
            if(area<0)ps.Reverse();return ps;
        }

        void Segment(Material m,Vector2 a,Vector2 b,float half,float y,float repeat=3)
        {
            Vector2 delta=b-a;if(delta.sqrMagnitude<.001f)return;
            Vector2 side=new(delta.y,-delta.x);side=side.normalized*half;
            Vector3 V(Vector2 p)=>new(p.x,y,p.y);
            mesh.Quad(m,V(a-side),V(b-side),V(b+side),V(a+side),repeat);
        }

        void BuildRoads()
        {
            foreach(var r in data.surroundingRoads)
            {
                if(data.roadOsmId.Split(',').Contains(r.id))continue;
                for(int i=1;i<r.points.Length;i++)
                {
                    var a=r.points[i-1];var b=r.points[i];var mid=(a+b)*.5f;
                    if(mid.x < -220||mid.x>250||mid.y < -100||mid.y>430)continue;
                    Segment(r.kind=="footway"?pavement:road,a,b,Mathf.Clamp(r.width*.5f,1,7),.025f);
                }
            }
            // The published mapped width is 7 m, with two-way traffic. The initial drive proceeds south.
            for(float z=-25;z<355;z+=2)
            {
                float x=roadX(z),x2=roadX(z+2);
                Vector3 direction=new(x2-x,0,2);Quaternion rotation=Quaternion.LookRotation(direction);
                float segmentLength=direction.magnitude+.02f,midX=(x+x2)*.5f;
                mesh.Quad(road,new Vector3(x-3.5f,.035f,z),new Vector3(x2-3.5f,.035f,z+2),
                    new Vector3(x2+3.5f,.035f,z+2),new Vector3(x+3.5f,.035f,z),3,
                    new Rect(z/3,0,2f/3,7f/3));
                foreach(int s in new[]{-1,1})
                {
                    mesh.Box(pavement,new Vector3(midX+s*4.45f,.12f,z+1),new Vector3(1.65f,.24f,segmentLength),rotation,2);
                    mesh.Box(((int)(z/2)&1)==0?white:dark,new Vector3(midX+s*3.57f,.13f,z+1),new Vector3(.19f,.26f,segmentLength),rotation);
                    Segment(paint,new Vector2(x+s*3.2f,z),new Vector2(x2+s*3.2f,z+2),.035f,.052f);
                }
                // Centre dashes are an approximate road-detail treatment, not a traffic survey.
                if((int)z%8<4)Segment(paint,new Vector2(x,z),new Vector2(x2,z+2),.05f,.054f);
            }
        }

        void ContextBuilding(JaksaStreet.Building b)
        {
            if(b.points.Length<3)return;
            float h=b.height>0?b.height:b.levels>0?b.levels*3.5f:Rand(5,13);
            int floors=Mathf.Max(1,b.levels>0?(int)b.levels:Mathf.RoundToInt(h/3.5f));
            bool office=h>35;
            Facade(b.points,0,h,Mathf.Min(floors,42),office,false,contextWalls[random.Next(contextWalls.Length)]);
        }

        void Facade(Vector2[] source,float bottom,float top,int floors,bool allGlass,bool motif,Material wall=null)
        {
            var ps=Polygon(source);var tris=JaksaStreet.Triangulate(ps);
            Vector3 P(int i,float y)=>new(ps[i].x,y,ps[i].y);
            for(int i=0;i<tris.Count;i+=3)
                mesh.Quad(stone,P(tris[i],top),P(tris[i+2],top),P(tris[i+1],top),P(tris[i+1],top),5);
            for(int i=0;i<ps.Count;i++)
            {
                Vector3 a=P(i,bottom),b=P((i+1)%ps.Count,bottom),along=(b-a).normalized;
                Vector3 n=new(along.z,0,-along.x);float width=Vector3.Distance(a,b),height=top-bottom;
                mesh.Quad(allGlass?glazing[0]:wall?wall:stone,b,a,a+Vector3.up*height,b+Vector3.up*height,4);
                if(width<1)continue;
                int bays=Mathf.Max(1,Mathf.RoundToInt(width/(allGlass?1.8f:3.5f)));
                float bw=width/bays,fh=height/floors;
                for(int f=0;f<floors;f++)
                {
                    for(int j=0;j<bays;j++)
                    {
                        if(!allGlass&&(f+j)%3==0)continue;
                        float w=allGlass?bw-.16f:bw*.55f,h=allGlass?fh-.35f:Mathf.Min(1.75f,fh*.6f);
                        Vector3 c=a+along*bw*(j+.5f)+Vector3.up*(f*fh+fh*.52f)+n*.055f;
                        Vector3 u=along*w*.5f,v=Vector3.up*h*.5f;
                        mesh.Quad(glazing[(f*7+j*3+i)%glazing.Length],c+u-v,c-u-v,c-u+v,c+u+v,4);
                        if(!allGlass&&wall)
                        {
                            Quaternion rotation=Quaternion.LookRotation(n);
                            mesh.Box(white,c-Vector3.up*(h*.5f+.055f)+n*.09f,new Vector3(w+.22f,.11f,.28f),rotation);
                            mesh.Box(silver,c+n*.06f,new Vector3(.045f,h,.08f),rotation);
                        }
                    }
                    if(allGlass)
                    {
                        Vector3 q=a+Vector3.up*((f+1)*fh)+n*.11f;
                        mesh.Quad(silver,q+along*width,q,q+Vector3.up*.11f,q+along*width+Vector3.up*.11f,4);
                    }
                }
                if(allGlass)
                    for(int j=0;j<=bays;j++)
                    {
                        var q=a+along*j*bw+n*.14f;
                        mesh.Quad(silver,q+along*.1f,q,q+Vector3.up*height,q+along*.1f+Vector3.up*height,4);
                    }
                if(motif&&width>35)
                {
                    Vector3 mid=(a+b)*.5f+n*.35f;
                    // Repeating woven diamonds interpret the facade's documented ulos spine.
                    for(float y=0;y<height-6;y+=6)
                    {
                        Vector3 p=mid+Vector3.up*y;
                        for(int column=-1;column<=1;column++)
                        {
                            Vector3 c=p+along*column*1.6f;
                            mesh.Tube(silver,c,c+along*1.6f+Vector3.up*3,.10f,sides:4);
                            mesh.Tube(silver,c+along*1.6f+Vector3.up*3,c+Vector3.up*6,.10f,sides:4);
                            mesh.Tube(silver,c,c-along*1.6f+Vector3.up*3,.10f,sides:4);
                            mesh.Tube(silver,c-along*1.6f+Vector3.up*3,c+Vector3.up*6,.10f,sides:4);
                        }
                    }
                }
            }
        }

        void Tower(JaksaStreet.Building b,float height,int floors,bool main)
        {
            float baseY=main?31:22;
            Facade(b.points,baseY,height-5,floors-(main?8:6),true,main);
            if(!main)Facade(b.points,0,baseY,6,true,false);
            var ps=Polygon(b.points);
            // Upright silver fins flare slightly at the crown, a recognizable Sopo Del silhouette.
            for(int i=0;i<ps.Count;i++)
            {
                Vector3 a=new(ps[i].x,height-10,ps[i].y),d=new(ps[(i+1)%ps.Count].x,height-10,ps[(i+1)%ps.Count].y);
                Vector3 edge=d-a,n=new(edge.normalized.z,0,-edge.normalized.x);
                for(float u=0;u<edge.magnitude;u+=1.7f)
                {
                    Vector3 p=a+edge.normalized*u;
                    mesh.Tube(silver,p,p+Vector3.up*10+n*1.5f,.13f,.16f,4);
                }
            }
        }

        float Front(float z)=>67.65f-(z-109.91f)*.101f;
        void Forecourt()
        {
            for(float z=107;z<213;z+=2)
            {
                float left=roadX(z)+5.25f,right=Front(z);
                if(right<=left)continue;
                mesh.Box(pavement,new Vector3((left+right)*.5f,.1f,z+1),new Vector3(right-left,.2f,2.05f),repeat:3);
                // A planted buffer separates the public pavement from the lobby drop-off.
                if(z<128||z>153)
                {
                    mesh.Box(stone,new Vector3(left+2,.36f,z+1),new Vector3(2.8f,.7f,1.98f));
                    mesh.Box(green,new Vector3(left+2,.85f,z+1),new Vector3(2.55f,.5f,1.9f));
                }
            }
            Vector3 face=new(Front(138.8f),0,138.8f),n=new Vector3(-1,0,-.101f).normalized;
            Vector3 along=Vector3.Cross(Vector3.up,n);Quaternion rot=Quaternion.LookRotation(n);
            // Broad metal canopy and its diamond bracing, modeled from the built entrance photos.
            Vector3 center=face+n*5+Vector3.up*7;
            mesh.Box(silver,center,new Vector3(32,.35f,10),rot);
            mesh.Box(glazing[2],center+Vector3.up*.2f,new Vector3(31,.08f,9),rot);
            for(float u=-15;u<15;u+=3)
            {
                mesh.Tube(silver,center+along*u-n*4.7f-Vector3.up*.25f,center+along*(u+3)+n*4.7f-Vector3.up*.25f,.13f,sides:4);
                mesh.Tube(silver,center+along*(u+3)-n*4.7f-Vector3.up*.25f,center+along*u+n*4.7f-Vector3.up*.25f,.13f,sides:4);
            }
            foreach(float u in new[]{-14f,14f})mesh.Tube(silver,face+along*u+n*9.5f,face+along*u+n*9.5f+Vector3.up*6.9f,.28f,sides:12);
            for(int step=0;step<5;step++)
                mesh.Box(stone,face+n*(1.2f+step*.45f)+Vector3.up*(.13f*(5-step)),new Vector3(28,.26f*(5-step),.5f),rot);
            mesh.Box(dark,face+Vector3.up*4+n*.35f,new Vector3(12,7.5f,.18f),rot);
            mesh.Box(glazing[4],face+Vector3.up*3.5f+n*.49f,new Vector3(10,6.5f,.12f),rot);
            for(int u=-2;u<=2;u++)mesh.Box(silver,face+along*u*2+Vector3.up*3.5f+n*.58f,new Vector3(.09f,6.5f,.15f),rot);
            Text("LOBBY IRISANNA",face+Vector3.up*6.2f+n*.7f,n,.2f,Color.white);
            mesh.Box(stone,face+Vector3.up*20+n*.18f,new Vector3(15,22,.45f),rot);
            Text("del",face+Vector3.up*28+n*.55f,n,.65f,Color.white);
            Vector3 crest=face+Vector3.up*31+n*.6f;
            mesh.Tube(red,crest-along*2,crest-along*.8f+Vector3.up*1,.13f,sides:4);
            mesh.Tube(red,crest-along*.8f+Vector3.up*1,crest+along*.3f,.13f,sides:4);
            mesh.Tube(red,crest+along*.3f,crest+along*1.5f+Vector3.up*1.2f,.13f,sides:4);
            // The pedestrian plaza and round lifestyle volume are schematic architectural details.
            Vector3 plaza=new(69.85f,.2f,224.65f);
            for(int ring=0;ring<4;ring++)
                for(int k=0;k<48;k++)
                {
                    float a=k*Mathf.PI/24,b=(k+1)*Mathf.PI/24,r=3+ring*1.3f;
                    mesh.Tube(white,plaza+new Vector3(Mathf.Cos(a)*r,.06f,Mathf.Sin(a)*r),
                        plaza+new Vector3(Mathf.Cos(b)*r,.06f,Mathf.Sin(b)*r),.1f,sides:4);
                }
            foreach(float z in new[]{117f,169f,195f,213f})Palm(new Vector3(roadX(z)+9,.25f,z),9);
        }

        void StreetFurniture()
        {
            for(float z=35;z<216;z+=3)
            {
                float x=roadX(z)-6.5f;
                mesh.Box(white,new Vector3(x,.3f,z+1.5f),new Vector3(.5f,.6f,3));
                mesh.Box(green,new Vector3(x-.2f,.85f,z+1.5f),new Vector3(.9f,.6f,2.9f));
            }
            for(float z=-10;z<360;z+=24)
            {
                foreach(int s in new[]{-1,1})
                {
                    Vector3 p=new(roadX(z)+s*5.1f,.25f,z);
                    mesh.Tube(dark,p,p+Vector3.up*8,.09f,.055f);
                    mesh.Tube(dark,p+Vector3.up*7.6f,p+new Vector3(-s*1.3f,8.2f,0),.05f);
                    mesh.Box(silver,p+new Vector3(-s*1.4f,8.15f,0),new Vector3(.45f,.15f,.8f));
                    if(s<0||z<104||z>230)tree(p+new Vector3(s*2,0,8),Rand(6.5f,9));
                }
            }
            Vector3 sign=new(roadX(18)-5.6f,2.6f,18);
            mesh.Box(dark,sign,new Vector3(3.7f,.65f,.09f));
            Text("JL. MEGA KUNINGAN BARAT III",sign+Vector3.back*.06f,Vector3.back,.027f,Color.white);
            mesh.Tube(silver,new Vector3(sign.x,.25f,sign.z),sign+Vector3.up*.4f,.05f);
        }

        void Palm(Vector3 p,float h)
        {
            mesh.Tube(stone,p,p+Vector3.up*h,.18f,.1f,8);
            for(int k=0;k<10;k++)
            {
                float a=k*Mathf.PI/5;
                Vector3 direction=new(Mathf.Cos(a),0,Mathf.Sin(a));
                for(int j=0;j<5;j++)
                {
                    float t=j/5f,t2=(j+1)/5f;
                    Vector3 A=p+Vector3.up*(h+Mathf.Sin(t*Mathf.PI)*1.2f-t*1.4f)+direction*t*4;
                    Vector3 B=p+Vector3.up*(h+Mathf.Sin(t2*Mathf.PI)*1.2f-t2*1.4f)+direction*t2*4;
                    Vector3 side=Vector3.Cross(Vector3.up,direction)*(.4f*(1-t));
                    mesh.Quad(green,A-side,B-side*.7f,B+side*.7f,A+side,2);
                    mesh.Quad(green,A+side,B+side*.7f,B-side*.7f,A-side,2);
                }
            }
        }

        void Text(string value,Vector3 p,Vector3 normal,float size,Color color)
        {
            var obj=new GameObject(value);obj.transform.SetParent(root,false);
            obj.transform.SetPositionAndRotation(p,Quaternion.LookRotation(-normal));
            var text=obj.AddComponent<TextMesh>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
            obj.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        }
    }
}
