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
            public Vector2 PreviousPosition;
            public float Offset = -5.4f, SignalAge;
            public int Indicator;
            public float HalfWidth => Motorcycle ? .4f : .95f;
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
            if (dt <= 0) return;
            Clock+=dt;
            foreach (var vehicle in vehicles) vehicle.PreviousPosition = vehicle.Position;
            foreach(var n in vehicles)
            {
                var road=Roads[n.Road];
                if(!n.Active)
                {
                    float entry=Vector2.Dot(n.Direction,road.Direction)>0?0:road.Length;
                    Vector2 pos=road.Start+road.Direction*entry+new Vector2(n.Direction.y,-n.Direction.x)*-5.4f;
                    if(Vector2.Distance(pos,run.Position)<35 || Occupied(pos,n)) continue;
                    n.Position=pos; n.PreviousPosition=pos; n.Offset=-5.4f; n.Along=entry; n.Speed=0; n.Active=true;
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
                    if (toLine < 0 || toLine > 60) continue;
                    int arm=Arm(stage,n.Direction), phase=run.CitySignal(stage,arm);
                    bool stop=phase==0 || phase==1 && toLine>=n.Speed*n.Speed/12;
                    if(toLine>=0) foreach(var other in vehicles)
                        if(other!=n && other.Active && Mathf.Abs(Vector2.Dot(n.Direction,other.Direction))<.5f
                            && Mathf.Abs(other.Position.x-center.x)<18 && Mathf.Abs(other.Position.y-center.y)<18) stop=true;
                    if (toLine >= 0)
                    {
                        var playerDelta = run.Position - center;
                        var playerForward = new Vector2(Mathf.Sin(run.Heading*Mathf.Deg2Rad), Mathf.Cos(run.Heading*Mathf.Deg2Rad));
                        if (Mathf.Abs(playerDelta.x) < 18 && Mathf.Abs(playerDelta.y) < 18
                            && Mathf.Abs(Vector2.Dot(n.Direction, playerForward)) < .8f) stop = true;
                        // Do not enter a box junction unless there is room beyond it.
                        var exit = center + n.Direction * (19 + n.HalfLength)
                            + new Vector2(n.Direction.y, -n.Direction.x) * n.Offset;
                        if (!ClearFor(exit,n.Heading,n.HalfWidth,n.HalfLength)
                            || GapAhead(exit, n.Direction, n.HalfLength, n) < n.HalfLength * 2 + 2
                            || TrafficFootprint.Overlaps(exit,n.Heading,n.HalfWidth,n.HalfLength,run.Position,run.Heading,
                                TrafficFootprint.PlayerHalfWidth(run.Mode),TrafficFootprint.PlayerHalfLength(run.Mode),2)) stop = true;
                    }
                    if(toLine>=0 && stop) gap=Mathf.Min(gap,toLine+1.85f);
                }
                if(run.BoulevardPedestrian.HasValue)
                {
                    var pedestrian = run.BoulevardPedestrian.Value;
                    var walking = run.Stage == 5 ? Boulevard.Right(run.Stage) : Boulevard.Forward(run.Stage);
                    var laneCrossing = n.Position + n.Direction * Vector2.Dot(pedestrian - n.Position, n.Direction);
                    float ahead = Vector2.Dot(pedestrian - n.Position, n.Direction) - n.HalfLength;
                    if (ahead >= 0 && Mathf.Abs(Vector2.Dot(walking, n.Direction)) < .5f
                        && PedestrianClearance.Blocks(pedestrian, walking, laneCrossing, walking, Boulevard.LaneWidth))
                        gap = Mathf.Min(gap, ahead); // Includes the normal two metre stopping margin.
                }
                if(n.Road==3 && run.RailWarning)
                {
                    float line=n.Direction.y>0?790:810;
                    float toLine=(line-n.Position.y)*n.Direction.y-n.HalfLength;
                    if(toLine>=0) gap=Mathf.Min(gap,toLine+1.85f);
                }
                float safe=Mathf.Sqrt(Mathf.Max(0,2*5*(gap-2)));
                float target=Mathf.Min(Mathf.Min(n.Cruise, 50 / 3.6f),safe);
                float desiredOffset = -5.4f;
                if (n.Road == 3 && n.Direction.y > 0 && run.Ambulance.HasValue && n.Position.y > 845)
                {
                    float ambulanceBehind = n.Position.y - run.Ambulance.Value.y;
                    if (ambulanceBehind > -22 && ambulanceBehind < 65) desiredOffset = -9;
                }
                int signal = Mathf.Abs(n.Offset - desiredOffset) > .05f ? desiredOffset < n.Offset ? -1 : 1 : 0;
                n.SignalAge = signal == n.Indicator ? n.SignalAge + dt : 0;
                n.Indicator = signal;
                float offset = n.SignalAge > .6f ? Mathf.MoveTowards(n.Offset, desiredOffset, dt * 1.2f) : n.Offset;
                if (desiredOffset == -9) target = Mathf.Min(target, 3);
                n.Speed=Mathf.MoveTowards(n.Speed,target,dt*(target<n.Speed?8:2.5f));
                // Never overlap a stopped vehicle, even if the player cuts in late.
                float travel=Mathf.Min(n.Speed*dt,Mathf.Max(0,gap-2));
                if(travel<n.Speed*dt) n.Speed=travel/dt;
                var next = n.Position + n.Direction*travel + new Vector2(n.Direction.y,-n.Direction.x)*(offset-n.Offset);
                if (Blocked(n, next, run))
                {
                    // Wait for room to change lanes while continuing safely in the current lane.
                    offset=n.Offset; next=n.Position+n.Direction*travel;
                    if (Blocked(n,next,run)) { n.Speed=0; continue; }
                }
                n.Position=next; n.Offset=offset;
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
            float side=(half>1.5f?.95f:.4f)+(otherHalf>1.5f?.95f:.4f)+.05f;
            if(ahead>0 && ahead<60 && Mathf.Abs(Vector2.Dot(delta,new Vector2(direction.y,-direction.x)))<side)
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
        public bool ClearFor(Vector2 position, float heading, float width, float length)
        {
            foreach (var n in vehicles)
                if (n.Active && TrafficFootprint.Overlaps(position,heading,width,length,n.Position,n.Heading,n.HalfWidth,n.HalfLength,2)) return false;
            return true;
        }

        bool Blocked(Vehicle n, Vector2 next, Journey run)
        {
            bool Touch(Vector2 point, float heading, float width, float length) =>
                TrafficFootprint.Sweeps(n.Position,next,n.Heading,n.Heading,n.HalfWidth,n.HalfLength,
                    point,point,heading,heading,width,length,.12f);
            if (Touch(run.Position,run.Heading,TrafficFootprint.PlayerHalfWidth(run.Mode),TrafficFootprint.PlayerHalfLength(run.Mode))) return true;
            foreach (var other in vehicles)
                if (other != n && other.Active && (other.Position-next).sqrMagnitude < 100
                    && Touch(other.Position,other.Heading,other.HalfWidth,other.HalfLength)) return true;
            if (Touch(new Vector2(-189,914),0,.95f,2.3f)) return true;
            return run.Ambulance.HasValue && Touch(run.Ambulance.Value,0,.95f,2.2f);
        }
        public bool Collides(Journey run) => Collides(run,run.Position,run.Heading,false);
        public bool Collides(Journey run,Vector2 from,float fromHeading,bool moved)
        {
            foreach(var n in vehicles)
            {
                if(!n.Active) continue;
                if (TrafficFootprint.Sweeps(from,run.Position,fromHeading,run.Heading,
                    TrafficFootprint.PlayerHalfWidth(run.Mode),TrafficFootprint.PlayerHalfLength(run.Mode),
                    moved?n.PreviousPosition:n.Position,n.Position,n.Heading,n.Heading,n.HalfWidth,n.HalfLength)) return true;
            }
            return false;
        }

    }

    public sealed partial class Journey
    {
        public BoulevardTraffic CityTraffic { get; private set; }
        int trafficAttempt;
        Vector2 trafficMotionStart;
        float trafficHeadingStart;
        void ResetCityTraffic() => CityTraffic=Stage>=5 && !Finished?new BoulevardTraffic(trafficSeed+trafficAttempt++,Position):null;
        void StepCityTraffic(float dt)
        {
            CityTraffic ??= new BoulevardTraffic(trafficSeed+trafficAttempt++,Position);
            if(CityTraffic.Collides(this,dt > 0 ? trafficMotionStart : Position,dt > 0 ? trafficHeadingStart : Heading,false)) Fail(Rule.Collision);
            CityTraffic.Step(dt,this);
            if(CityTraffic.Collides(this,dt > 0 ? trafficMotionStart : Position,dt > 0 ? trafficHeadingStart : Heading,dt > 0)) Fail(Rule.Collision);
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
