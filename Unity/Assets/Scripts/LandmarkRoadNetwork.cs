using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace JalanKita
{
    // Mapped carriageways connect to the existing clockwise ring. The road union
    // is shared by rendering and containment so an asphalt junction is traversable.
    public sealed class LandmarkRoadNetwork
    {
        public sealed class Branch
        {
            public readonly string Name;
            public readonly LandmarkRoute Route;
            public readonly float Width;
            public readonly bool Incoming;
            public Branch(string name,List<Vector2> points,float width,bool incoming)
            {Name=name;Route=new LandmarkRoute(points.ToArray(),false);Width=width;Incoming=incoming;}
        }
        public readonly LandmarkRoute Ring;
        public readonly float RingWidth;
        public readonly Branch[] Branches;
        public readonly LandmarkRoute[] TrafficPaths;
        public bool HasAccess=>Branches.Length>0;
        public Vector2 Spawn=>HasAccess?Branches[0].Route.At(16,-3.4f):Ring.At(0,-1.75f);
        public float SpawnHeading=>HasAccess?Branches[0].Route.Heading(16):Ring.Heading(0);
        public LandmarkRoadNetwork(LandmarkMap map,LandmarkRoute ring)
        {
            Ring=ring;RingWidth=map.roadWidth;
            if(map.id!="hi"){Branches=System.Array.Empty<Branch>();TrafficPaths=System.Array.Empty<LandmarkRoute>();return;}
            List<Vector2> Roads(params string[] ids)
            {
                var points=new List<Vector2>();
                foreach(string id in ids)foreach(var p in map.roads.First(r=>r.id==id).points)
                    if(points.Count==0||Vector2.Distance(points[^1],p)>.05f)points.Add(p);
                return points;
            }
            var southIn=Roads("1263586498","449551295","1378151562","1263586499");
            float southJoin=ring.Nearest(southIn[^1],out _);southIn.Add(ring.At(southJoin+8));
            var northOut=Roads("453439709","453439715");
            float northLeave=ring.Nearest(northOut[0],out _);northOut.Insert(0,ring.At(northLeave-8));
            var northIn=Roads("449551309","759477372","449551312");
            northIn.RemoveAll(p=>p.y>315);northIn.Insert(0,new Vector2(6.72f,315));
            float northJoin=ring.Nearest(northIn[^1],out _);northIn.Add(ring.At(northJoin+8));
            var southOut=Roads("449551298","1263586497","449551306");
            southOut.RemoveAll(p=>p.y< -315);southOut.Add(new Vector2(21.1f,-315));
            float southLeave=ring.Nearest(southOut[0],out _);southOut.Insert(0,ring.At(southLeave-8));
            Branches=new[]{new Branch("Sudirman · masuk HI",southIn,10.2f,true),new Branch("Thamrin · keluar HI",northOut,10.5f,false),
                new Branch("Thamrin · masuk HI",northIn,6,true),new Branch("Sudirman · keluar HI",southOut,6,false)};
            TrafficPaths=new[]{Through(Branches[0],southJoin,Branches[1],northLeave),Through(Branches[2],northJoin,Branches[3],southLeave)};
        }
        LandmarkRoute Through(Branch entry,float join,Branch exit,float leave)
        {
            var points=new List<Vector2>();
            // Traffic uses the outside lane, with mapped centerlines merging gently.
            for(float s=0;s<entry.Route.Length-11;s+=4)points.Add(entry.Route.At(s));
            float length=Mathf.Repeat(leave-join,Ring.Length);
            // The branch's first knot lies BEFORE its mouth on the ring centreline.
            // Connecting to s=10 doubled back across circulating lanes. Leave the
            // outer lane early and join a forward point beyond the shared road area.
            float exitStart=10;
            for(float s=10;s<Mathf.Min(80,exit.Route.Length);s+=1)
            {
                Ring.Nearest(exit.Route.At(s),out float lateral);
                if(Mathf.Abs(lateral)>RingWidth*.5f+2){exitStart=s;break;}
            }
            float ringEnd=Mathf.Max(0,length-12);
            for(float s=0;s<ringEnd;s+=3)points.Add(Ring.At(join+s,-4.1f));
            points.Add(Ring.At(join+ringEnd,-4.1f));
            for(float s=exitStart;s<exit.Route.Length;s+=4)points.Add(exit.Route.At(s));
            points.Add(exit.Route.At(exit.Route.Length));return new LandmarkRoute(points.ToArray(),false);
        }
        static float Clearance(LandmarkRoute route,float width,Vector2 p,out Vector2 center,out Vector2 forward,bool finite=true)
        {
            // Use the exact winning segment for both projection and permission.
            // At a vertex an averaged heading disagrees with Nearest's normal,
            // making Fit accept a pose that the finite-road check then rejects.
            float s=route.Nearest(p,out float lateral,out center,out forward);
            float clearance=width*.5f-Mathf.Abs(lateral);
            if(!route.Closed)
            {
                float longitudinal=Vector2.Dot(p-center,forward);
                // Finite strips have real end planes, not rounded half-width caps.
                // Another connected corridor may still cover the same point.
                if((s<.01f&&longitudinal<-.001f)||(s>route.Length-.01f&&longitudinal>.001f))
                {if(finite)return float.NegativeInfinity;clearance-=Mathf.Abs(longitudinal);}
            }
            return clearance;
        }
        public void Sample(Vector2 p,out Vector2 center,out Vector2 forward,out float halfWidth)
        {
            float best=Clearance(Ring,RingWidth,p,out center,out forward,false);halfWidth=RingWidth*.5f;
            foreach(var b in Branches)
            {
                float score=Clearance(b.Route,b.Width,p,out var c,out var f,false);
                if(score<=best)continue;best=score;center=c;forward=f;halfWidth=b.Width*.5f;
            }
        }
        public void AllowedDirections(Vector2 position,out Vector2 primary,out Vector2 alternate)
        {
            Sample(position,out _,out primary,out _);alternate=Vector2.zero;
            if(Clearance(Ring,RingWidth,position,out _,out var ringForward)>=-.2f
                &&Vector2.Dot(primary,ringForward)<.999f)alternate=ringForward;
            foreach(var branch in Branches)
                if(Clearance(branch.Route,branch.Width,position,out _,out var forward)>=-.2f
                    &&Vector2.Dot(primary,forward)<.999f)alternate=forward;
        }
        public bool OnBranch(Vector2 p,float margin=0,Branch except=null)
        {
            foreach(var b in Branches)if(b!=except&&Clearance(b.Route,b.Width,p,out _,out _)>=-margin)return true;
            return false;
        }
        public bool OnRoad(Vector2 p,float margin=0)=>Clearance(Ring,RingWidth,p,out _,out _)>=-margin||OnBranch(p,margin);
        public bool ContainsPoint(Vector2 p,float inset=0)
        {
            if(Clearance(Ring,RingWidth,p,out _,out _)>=inset)return true;
            foreach(var b in Branches)
            {
                float s=b.Route.Nearest(p,out _);float score=Clearance(b.Route,b.Width,p,out var center,out var forward);
                float along=Vector2.Dot(p-center,forward);
                if((s<.01f&&along<0)||(s>b.Route.Length-.01f&&along>0))continue;
                if(score>=inset)return true;
            }
            return false;
        }
        public bool AtEnd(Vector2 p)
        {
            if(!HasAccess)return false;
            foreach(var b in Branches)
            {
                float s=b.Route.Nearest(p,out float lane);
                if(Mathf.Abs(lane)>b.Width*.5f+1)continue;
                if((!b.Incoming&&s>b.Route.Length-7)||(b.Incoming&&s<7))return true;
            }
            return false;
        }
        public Vector2 ClampEndpoint(Vector2 from,Vector2 proposed,out bool contact)
        {
            contact=false;
            foreach(var b in Branches)
            {
                float capS=b.Incoming?6:b.Route.Length-6;
                var cap=b.Route.At(capS);float angle=b.Route.Heading(capS)*Mathf.Deg2Rad;
                var outward=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*(b.Incoming?-1:1);
                var right=new Vector2(outward.y,-outward.x);
                if(Mathf.Abs(Vector2.Dot(from-cap,right))>b.Width*.5f+2||Vector2.Distance(from,cap)>15)continue;
                float excess=Vector2.Dot(proposed-cap,outward);
                // Only an outward crossing is clamped. Backing away from a cap
                // remains possible, even when the vehicle was already at its end.
                if(excess<=0||Vector2.Dot(proposed-from,outward)<=0)continue;
                proposed-=outward*excess;contact=true;
            }
            return proposed;
        }
        public string Guidance(Vector2 p)
        {
            if(!HasAccess)return "Ikuti arah jalan melingkar · C untuk kamera pengemudi atau landmark";
            if(AtEnd(p))return "Ujung ruas demo · mundur dengan S/↓, R mengulang pendekatan, Esc memilih lokasi";
            foreach(var b in Branches)
            {
                float s=b.Route.Nearest(p,out float lane);
                if(Mathf.Abs(lane)>b.Width*.5f||p.magnitude<78)continue;
                if(!b.Incoming)return "Keluar bundaran · lanjutkan di lajur kiri menuju "+(b==Branches[1]?"Thamrin":"Sudirman");
                return b.Route.Length-s<70?"Kurangi kecepatan · lihat arus dari kanan (X), tunggu celah aman sebelum masuk":"Mendekati Bundaran HI · ikuti marka, bersiap memberi jalan saat masuk";
            }
            return "Arus searah jarum jam · ambil lajur luar dan sein kiri sebelum keluar ke Thamrin atau Sudirman";
        }
    }
}
