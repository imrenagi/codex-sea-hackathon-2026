using System;
using System.Collections.Generic;
using UnityEngine;

namespace JalanKita
{
    // Adapted from Ujian SIM Traffic.cs: fixed lanes, seeded mixtures/gaps, obstacle
    // following, 2.5 m/s² acceleration and 8 m/s² braking. Coordinates use +Z north.
    public sealed class BoulevardTraffic
    {
        public sealed class Vehicle
        {
            public int Id, Road;
            public bool Motorcycle, Active = true;
            public string Color;
            public Vector2 Position, Direction;
            public float Speed, Cruise, Along;
            public float Radius => Motorcycle ? .8f : 1.3f;
            public float HalfLength => Motorcycle ? 1 : 2.1f;
            public float Heading => Mathf.Atan2(Direction.x,Direction.y)*Mathf.Rad2Deg;
        }
        readonly struct Road
        {
            public readonly Vector2 Start, End;
            public Vector2 Direction => (End-Start).normalized;
            public float Length => Vector2.Distance(Start,End);
            public Road(float x1,float z1,float x2,float z2) { Start=new(x1,z1); End=new(x2,z2); }
        }
        static readonly Road[] Roads = {
            new(0,390,0,710), new(-85,480,85,480), new(-260,640,85,640), new(-180,565,-180,1160)
        };
        static readonly string[] CarColors = { "f4f4f4","1f1f1f","9aa3ad","2b4c7e","7a1f1f","c9b18a" };
        static readonly string[] MotoColors = { "c62828","111111","1565c0","eeeeee" };
        readonly List<Vehicle> vehicles = new();
        public IReadOnlyList<Vehicle> Vehicles => vehicles;
        public float Clock { get; private set; }

        public BoulevardTraffic(int seed, Vector2 player)
        {
            var rng = new System.Random(seed);
            for(int road=0;road<Roads.Length;road++) foreach(int sign in new[] { 1,-1 })
            {
                var r=Roads[road];
                float scale=.8f+(float)rng.NextDouble()*.6f;
                for(float along=18+(float)rng.NextDouble()*25;along<r.Length-18;along+=(28+(float)rng.NextDouble()*47)*scale)
                {
                    Vector2 direction=r.Direction*sign;
                    Vector2 pos=r.Start+r.Direction*along+new Vector2(direction.y,-direction.x)*-5.4f;
                    bool junction=false;
                    for(int stage=5;stage<=7;stage++) if(Vector2.Distance(pos,Boulevard.Center(stage))<38) junction=true;
                    if(junction || Vector2.Distance(pos,player)<18 || road==3 && Mathf.Abs(pos.y-800)<28) continue;
                    bool moto=rng.NextDouble()<.4;
                    var colors=moto?MotoColors:CarColors;
                    vehicles.Add(new Vehicle { Id=vehicles.Count,Road=road,Motorcycle=moto,Color=colors[rng.Next(colors.Length)],
                        Position=pos,Direction=direction,Along=along,Cruise=(28+(float)rng.NextDouble()*14)/3.6f });
                }
            }
        }

        public void Step(float dt, Journey run)
        {
            Clock+=dt;
            foreach(var n in vehicles)
            {
                var road=Roads[n.Road];
                if(!n.Active)
                {
                    float entry=Vector2.Dot(n.Direction,road.Direction)>0?0:road.Length;
                    Vector2 pos=road.Start+road.Direction*entry+new Vector2(n.Direction.y,-n.Direction.x)*-5.4f;
                    if(Vector2.Distance(pos,run.Position)<35 || Occupied(pos,n)) continue;
                    n.Position=pos; n.Along=entry; n.Speed=0; n.Active=true;
                }
                float gap=GapAhead(n.Position,n.Direction,n.HalfLength,n);
                gap=ObstacleGap(n.Position,n.Direction,n.HalfLength,run.Position,run.Mode==VehicleMode.Car?2.1f:1,gap);
                if(run.Ambulance.HasValue) gap=ObstacleGap(n.Position,n.Direction,n.HalfLength,run.Ambulance.Value,2.2f,gap);
                gap=ObstacleGap(n.Position,n.Direction,n.HalfLength,new Vector2(-189,914),2.3f,gap);
                for(int stage=5;stage<=7;stage++)
                {
                    Vector2 center=Boulevard.Center(stage);
                    if(Mathf.Abs(Vector2.Dot(center-road.Start,new Vector2(road.Direction.y,-road.Direction.x)))>.1f) continue;
                    float front=Vector2.Dot(n.Position-center,n.Direction)+n.HalfLength;
                    float line=n.Motorcycle?-18:-26;
                    float toLine=line-front;
                    int arm=Arm(stage,n.Direction), phase=run.CitySignal(stage,arm);
                    bool stop=phase==0 || phase==1 && toLine>=n.Speed*n.Speed/12;
                    if(toLine>=0) foreach(var other in vehicles)
                        if(other!=n && other.Active && Mathf.Abs(Vector2.Dot(n.Direction,other.Direction))<.5f
                            && Mathf.Abs(other.Position.x-center.x)<18 && Mathf.Abs(other.Position.y-center.y)<18) stop=true;
                    if(toLine>=0 && stop) gap=Mathf.Min(gap,toLine+2);
                }
                if(run.BoulevardPedestrian.HasValue)
                    gap=ObstacleGap(n.Position,n.Direction,n.HalfLength,run.BoulevardPedestrian.Value,1,gap);
                if(n.Road==3 && run.RailWarning)
                {
                    float line=n.Direction.y>0?790:810;
                    float toLine=(line-n.Position.y)*n.Direction.y-n.HalfLength;
                    if(toLine>=0) gap=Mathf.Min(gap,toLine+2);
                }
                float safe=Mathf.Sqrt(Mathf.Max(0,2*5*(gap-2)));
                float target=Mathf.Min(n.Cruise,safe);
                n.Speed=Mathf.MoveTowards(n.Speed,target,dt*(target<n.Speed?8:2.5f));
                // Never overlap a stopped vehicle, even if the player cuts in late.
                float travel=Mathf.Min(n.Speed*dt,Mathf.Max(0,gap-2));
                if(travel<n.Speed*dt) n.Speed=travel/dt;
                n.Position+=n.Direction*travel;
                n.Along+=Vector2.Dot(n.Direction,road.Direction)*travel;
                if(n.Along<0 || n.Along>road.Length) n.Active=false;
            }
        }

        bool Occupied(Vector2 point,Vehicle except)
        {
            foreach(var n in vehicles) if(n!=except && n.Active && Vector2.Distance(n.Position,point)<16) return true;
            return false;
        }
        public static int Arm(int stage,Vector2 direction) =>
            (Mathf.RoundToInt((Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg-(stage==7?270:0))/90)+4)%4;

        static float ObstacleGap(Vector2 position,Vector2 direction,float half,Vector2 other,float otherHalf,float gap)
        {
            Vector2 delta=other-position;
            float ahead=Vector2.Dot(delta,direction);
            if(ahead>0 && ahead<60 && Mathf.Abs(Vector2.Dot(delta,new Vector2(direction.y,-direction.x)))<1.8f)
                return Mathf.Min(gap,ahead-half-otherHalf);
            return gap;
        }
        public float GapAhead(Vector2 position,Vector2 direction,float half,Vehicle except=null)
        {
            float gap=float.PositiveInfinity;
            foreach(var n in vehicles) if(n.Active && n!=except)
                gap=ObstacleGap(position,direction,half,n.Position,n.HalfLength,gap);
            return gap;
        }
        public bool Collides(Journey run)
        {
            Vector2 forward=new(Mathf.Sin(run.Heading*Mathf.Deg2Rad),Mathf.Cos(run.Heading*Mathf.Deg2Rad));
            Vector2 right=new(forward.y,-forward.x);
            float half=run.Mode==VehicleMode.Car?1.95f:.95f, width=run.Mode==VehicleMode.Car?.82f:.35f;
            foreach(var n in vehicles)
            {
                if(!n.Active || (n.Position-run.Position).sqrMagnitude>36) continue;
                var nr=new Vector2(n.Direction.y,-n.Direction.x);
                bool separate=false;
                foreach(var axis in new[] { forward,right,n.Direction,nr })
                {
                    float a=Mathf.Abs(Vector2.Dot(axis,forward))*half+Mathf.Abs(Vector2.Dot(axis,right))*width;
                    float b=Mathf.Abs(Vector2.Dot(axis,n.Direction))*n.HalfLength+Mathf.Abs(Vector2.Dot(axis,nr))*(n.Motorcycle?.35f:.82f);
                    if(Mathf.Abs(Vector2.Dot(n.Position-run.Position,axis))>=a+b) { separate=true; break; }
                }
                if(!separate) return true;
            }
            return false;
        }
    }

    public sealed partial class Journey
    {
        public BoulevardTraffic CityTraffic { get; private set; }
        int trafficAttempt;
        void ResetCityTraffic() => CityTraffic=Stage>=5 && !Finished?new BoulevardTraffic(trafficSeed+trafficAttempt++,Position):null;
        void StepCityTraffic(float dt)
        {
            CityTraffic ??= new BoulevardTraffic(trafficSeed+trafficAttempt++,Position);
            CityTraffic.Step(dt,this);
            bool collision=CityTraffic.Collides(this);
            if(reverseSweep)foreach(var actor in CityTraffic.Vehicles)
                if(actor.Active&&TrafficFootprint.SweptOverlaps(motionFrom,motionHeading,Position,Heading,
                    TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode),
                    actor.Position,actor.Heading,actor.Motorcycle?.35f:.82f,actor.HalfLength))collision=true;
            if(collision) Fail(Rule.Collision);
        }
        public int CitySignal(int stage,int arm)
        {
            if(stage>Stage) return 0;
            if(stage<Stage)
            {
                float t=(CityTraffic?.Clock??0)%52;
                if(arm%2==0) return t>=4&&t<22?2:t>=22&&t<25?1:0;
                return t>=30&&t<48?2:t>=48&&t<51?1:0;
            }
            if(arm%2==0) return MainPhase;
            if(CrossingClock<0) return 0;
            float time=CrossingClock%42;
            // Cross traffic clears before the player's green / protected-right arrow.
            if(stage==5 && time<7) return 2;
            if(stage==5 && time<10) return 1;
            if(stage==6 && time>=33&&time<35 || stage==7 && time>=35&&time<39) return 2;
            if(stage==6 && time>=35&&time<37 || stage==7 && time>=39&&time<41) return 1;
            return 0;
        }
    }
}
