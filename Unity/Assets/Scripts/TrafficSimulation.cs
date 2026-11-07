using System;
using UnityEngine;

namespace JalanKita
{
    // A fixed pool follows the actual scene centerline. Longitudinal headways plus
    // oriented footprints prevent invisible hazards and vehicle interpenetration.
    public sealed class TrafficSimulation
    {
        public sealed class Actor
        {
            public TrafficChoice Choice;
            public readonly TrafficVehicleView View;
            public float Z,Speed,Heading,DesiredSpeed;
            public int Direction;
            public Vector2 Position;
            public bool Braking;
            public Actor(Transform parent,int index){View=new TrafficVehicleView(parent,"Road traffic "+index);}
        }
        public readonly Actor[] Actors=new Actor[8];
        readonly TrafficVehicleView[] parked=new TrafficVehicleView[3];
        readonly JaksaStreet street;
        readonly Vector3[] parking={new(-7.5f,0,61),new(7.3f,0,142),new(-8.4f,0,201)};
        public TrafficSimulation(Transform parent,JaksaStreet street)
        {
            this.street=street;
            for(int i=0;i<Actors.Length;i++)Actors[i]=new Actor(parent,i);
            for(int i=0;i<parked.Length;i++)parked[i]=new TrafficVehicleView(parent,"Parked traffic "+i);
        }
        public void Reset(int seed)
        {
            var roster=new TrafficRoster(seed);var rng=new System.Random(seed);
            for(int i=0;i<Actors.Length;i++)
            {
                var a=Actors[i];a.Choice=roster.Choices[i%6];a.Direction=i<4||street.Data.oneway?1:-1;
                a.Z=44+(i%4)*57+(i>=4?20:0);a.DesiredSpeed=(a.Choice.Vehicle.Motorcycle?7.1f:6.3f)+(float)rng.NextDouble()*.8f;
                a.Speed=a.DesiredSpeed;a.Braking=false;a.Position=OnRoad(a.Z,a.Direction);a.Heading=RoadHeading(a.Z,a.Direction);
                a.View.Select(a.Choice);a.View.Hide();
            }
            for(int i=0;i<parked.Length;i++)parked[i].Select(new TrafficChoice(TrafficCatalogue.All[3+(i+Math.Abs(seed%3))%3],rng.Next(2)));
        }
        Vector2 OnRoad(float z,int direction)
        {
            // Keep left in each travel direction; road tangents come from the loaded map.
            float slope=(street.RoadX(z+.5f)-street.RoadX(z-.5f));
            float offset=1.48f*Mathf.Sqrt(1+slope*slope);
            return new Vector2(street.RoadX(z)-direction*offset,z);
        }
        float RoadHeading(float z,int direction)=>Mathf.Atan2(street.RoadX(z+1)-street.RoadX(z-1),2)*Mathf.Rad2Deg+(direction<0?180:0);
        public void Step(float dt,Vector2 player,float playerYaw,VehicleMode mode)
        {
            if(dt<=0)return;dt=Mathf.Min(dt,.05f);
            foreach(var a in Actors)
            {
                float nearest=float.PositiveInfinity;
                foreach(var b in Actors)
                {
                    if(a==b||a.Direction!=b.Direction)continue;
                    float ahead=(b.Z-a.Z)*a.Direction;
                    if(ahead>0)nearest=Mathf.Min(nearest,ahead-a.Choice.Vehicle.HalfLength-b.Choice.Vehicle.HalfLength);
                }
                float playerAhead=(player.y-a.Z)*a.Direction;
                var lane=OnRoad(player.y,a.Direction);
                if(playerAhead>0&&Mathf.Abs(player.x-lane.x)<a.Choice.Vehicle.HalfWidth+TrafficFootprint.PlayerHalfWidth(mode)+.2f)
                    nearest=Mathf.Min(nearest,playerAhead-a.Choice.Vehicle.HalfLength-TrafficFootprint.PlayerHalfLength(mode));
                float safe=Mathf.Sqrt(2*3.7f*Mathf.Max(0,nearest-2.4f));
                float target=Mathf.Min(a.DesiredSpeed,safe);
                a.Braking=a.Speed>target+.1f||target<.1f;
                a.Speed=Mathf.MoveTowards(a.Speed,target,(target<a.Speed?6:1.6f)*dt);
                float slope=street.RoadX(a.Z+.5f)-street.RoadX(a.Z-.5f);
                float move=Mathf.Min(a.Speed*dt/Mathf.Sqrt(1+slope*slope),Mathf.Max(0,nearest-.5f));
                float nextZ=a.Z+move*a.Direction;
                if(nextZ>282)nextZ=-28;
                else if(nextZ< -32)nextZ=278;
                var next=OnRoad(nextZ,a.Direction);float yaw=RoadHeading(nextZ,a.Direction);
                bool blocked=TrafficFootprint.Overlaps(next,yaw,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,player,playerYaw,
                    TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode),.15f);
                foreach(var b in Actors)
                    if(b!=a&&TrafficFootprint.Overlaps(next,yaw,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,b.Position,b.Heading,
                        b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,.12f)){blocked=true;break;}
                if(blocked){a.Speed=0;a.Braking=true;}
                else{a.Z=nextZ;a.Position=next;a.Heading=yaw;}
            }
        }
        public float SafePlayerSpeed(Vector2 player,float heading,VehicleMode mode)
        {
            float safe=8.33f;float angle=heading*Mathf.Deg2Rad;
            Vector2 forward=new(Mathf.Sin(angle),Mathf.Cos(angle)),right=new(forward.y,-forward.x);
            foreach(var a in Actors)
            {
                Vector2 delta=a.Position-player;float ahead=Vector2.Dot(delta,forward);
                if(ahead<=0||Mathf.Abs(Vector2.Dot(delta,right))>a.Choice.Vehicle.HalfWidth+TrafficFootprint.PlayerHalfWidth(mode)+.12f)continue;
                float gap=ahead-a.Choice.Vehicle.HalfLength-TrafficFootprint.PlayerHalfLength(mode);
                safe=Mathf.Min(safe,Mathf.Sqrt(2*4*Mathf.Max(0,gap-2.1f)));
            }
            return safe;
        }
        public bool BlocksPlayer(Vector2 from,Vector2 to,float heading,VehicleMode mode,float? previousHeading=null)
        {
            float startHeading=previousHeading??heading;
            bool Hits(Vector2 point,float yaw,TrafficDefinition definition) =>
                TrafficFootprint.Sweeps(from,to,startHeading,heading,TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode),
                    point,point,yaw,yaw,definition.HalfWidth,definition.HalfLength,.12f);
            foreach(var a in Actors) if(Hits(a.Position,a.Heading,a.Choice.Vehicle)) return true;
            for(int i=0;i<parked.Length;i++)
            {
                var p=parking[i];
                if(Hits(new Vector2(street.RoadX(p.z)+p.x,p.z),p.x<0?185:175,parked[i].Choice.Vehicle)) return true;
            }
            return false;
        }

        public void Present(float dt,Vector2 player)
        {
            foreach(var a in Actors)
            {
                if(Mathf.Abs(a.Z-player.y)>115||a.Z< -10||a.Z>270){a.View.Hide();continue;}
                a.View.Present(new Vector3(a.Position.x,.035f,a.Position.y),a.Heading,a.Speed,dt,true,0,a.Braking);
            }
            for(int i=0;i<parked.Length;i++)
            {
                var p=parking[i];bool loading=parked[i].Choice.Vehicle.Id=="utility_pickup";
                parked[i].Present(new Vector3(street.RoadX(p.z)+p.x,.1f,p.z),p.x<0?185:175,0,dt,false,loading?2:0);
            }
        }
    }
}
