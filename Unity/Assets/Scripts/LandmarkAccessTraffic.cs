using System;
using UnityEngine;
namespace JalanKita
{
    // Sparse through traffic actually enters from one mapped carriageway, yields
    // to circulating vehicles, and leaves via the opposite carriageway.
    public sealed class LandmarkAccessTraffic
    {
        public sealed class Actor
        {
            public readonly TrafficVehicleView View;
            public readonly LandmarkRoute Path;
            public readonly int PathIndex;
            public TrafficChoice Choice;
            public float S,Speed,YieldS,JoinS,ExitS;
            public Vector2 Position;
            public float Heading;
            public Actor(Transform parent,LandmarkRoute path,int pathIndex,int index)
            {Path=path;PathIndex=pathIndex;View=new TrafficVehicleView(parent,"HI approach traffic "+index);}
        }
        public readonly Actor[] Actors;
        readonly LandmarkRoadNetwork network;
        readonly bool forwardMissionTraffic;
        readonly StreetPerson[] people;
        float elapsed;
        public readonly DrivePedestrian[] Pedestrians;
        public LandmarkAccessTraffic(Transform parent,LandmarkRoadNetwork roads,bool forwardMissionTraffic=false)
        {
            network=roads;this.forwardMissionTraffic=forwardMissionTraffic;Actors=new Actor[roads.HasAccess?(forwardMissionTraffic?4:8):0];
            for(int i=0;i<Actors.Length;i++)Actors[i]=new Actor(parent,roads.TrafficPaths[forwardMissionTraffic?0:i%2],forwardMissionTraffic?0:i%2,i);
            people=new StreetPerson[roads.HasAccess?16:0];
            Pedestrians=new DrivePedestrian[people.Length];
            for(int i=0;i<people.Length;i++)people[i]=new StreetPerson(parent,"HI sidewalk walker "+i,new[]{"6587a0","b58869","7b9167","d6c9ad"}[i%4],1,false,i+47);
        }
        public void Reset(int seed)
        {
            var roster=new TrafficRoster(seed);elapsed=0;PedestrianPaths.FillHiAccess(network,Pedestrians,0,0);
            for(int i=0;i<Actors.Length;i++)
            {
                var a=Actors[i];a.Choice=roster.Choices[i%6];a.S=55+(forwardMissionTraffic?i:i/2)*44;a.Speed=5;
                var entry=network.Branches[a.PathIndex*2];
                var join=entry.Route.At(entry.Route.Length-8);
                a.JoinS=network.Ring.Nearest(join,out _);a.YieldS=a.Path.Nearest(entry.Route.At(entry.Route.Length-46),out _);
                a.ExitS=a.Path.Nearest(network.Branches[a.PathIndex*2+1].Route.At(16),out _);
                UpdatePose(a);a.View.Select(a.Choice);
            }
        }
        static void UpdatePose(Actor a){a.Position=a.Path.At(a.S);a.Heading=a.Path.Heading(a.S);}
        public static float AheadLimit(Vector2 position,float heading,float halfWidth,float halfLength,Vector2 other,float otherWidth,float otherLength,float maximum)
        {
            float angle=heading*Mathf.Deg2Rad;var forward=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle));var right=new Vector2(forward.y,-forward.x);var d=other-position;
            float ahead=Vector2.Dot(d,forward);
            if(ahead<=0||Mathf.Abs(Vector2.Dot(d,right))>halfWidth+otherWidth+.25f)return maximum;
            return Mathf.Min(maximum,Mathf.Sqrt(2*4*Mathf.Max(0,ahead-halfLength-otherLength-2.7f)));
        }
        public float SafeSpeed(Vector2 position,float heading,float halfWidth,float halfLength,float maximum)
        {
            foreach(var a in Actors)maximum=AheadLimit(position,heading,halfWidth,halfLength,a.Position,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,maximum);
            return maximum;
        }
        public bool Blocks(Vector2 position,float heading,float halfWidth,float halfLength)
        {
            foreach(var a in Actors)if(TrafficFootprint.Overlaps(position,heading,halfWidth,halfLength,a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,.16f))return true;
            return false;
        }
        public bool BlocksPath(Vector2 from,Vector2 to,float fromHeading,float toHeading,float halfWidth,float halfLength)
        {
            float turn=Mathf.DeltaAngle(fromHeading,toHeading);
            int samples=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/.25f),Mathf.CeilToInt(Mathf.Abs(turn)/5));
            for(int i=1;i<=samples;i++)if(Blocks(Vector2.Lerp(from,to,i/(float)samples),fromHeading+turn*i/samples,halfWidth,halfLength))return true;
            return false;
        }
        public void Step(float dt,Vector2 player,float playerHeading,VehicleMode mode,LandmarkDrive.Actor[] circulating)
        {
            float previous=elapsed;elapsed+=dt;
            PedestrianPaths.FillHiAccess(network,Pedestrians,previous,elapsed);
            foreach(var a in Actors)
            {
                var def=a.Choice.Vehicle;float target=7;
                foreach(var b in Actors)if(a!=b)target=AheadLimit(a.Position,a.Heading,def.HalfWidth,def.HalfLength,b.Position,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,target);
                target=AheadLimit(a.Position,a.Heading,def.HalfWidth,def.HalfLength,player,TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode),target);
                foreach(var b in circulating)
                {
                    target=AheadLimit(a.Position,a.Heading,def.HalfWidth,def.HalfLength,b.Position,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,target);
                    float upstream=Mathf.Repeat(a.JoinS-b.S,network.Ring.Length);
                    if(a.S<a.YieldS+1&&a.S>a.YieldS-32&&upstream<Mathf.Max(15,b.Speed*3.5f)&&Mathf.Abs(b.Lane+4.9f)<3.6f)
                        target=Mathf.Min(target,Mathf.Sqrt(2*4*Mathf.Max(0,a.YieldS-a.S-1.5f)));
                }
                a.Speed=Mathf.MoveTowards(a.Speed,target,(target<a.Speed?7:1.5f)*dt);
                float nextS=a.S+a.Speed*dt;
                if(nextS>a.Path.Length-1)
                {
                    // The finite slice despawns/recycles traffic only at distant ends,
                    // never next to the player or inside the merge.
                    if(Vector2.Distance(player,a.Position)<90){a.Speed=0;continue;}
                    nextS=0;
                }
                var next=a.Path.At(nextS);float heading=a.Path.Heading(nextS);
                bool blocked=TrafficFootprint.Overlaps(next,heading,def.HalfWidth,def.HalfLength,player,playerHeading,TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode),.2f);
                foreach(var b in circulating)if(TrafficFootprint.Overlaps(next,heading,def.HalfWidth,def.HalfLength,b.Position,b.Heading,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,.16f))blocked=true;
                foreach(var b in Actors)if(a!=b&&TrafficFootprint.Overlaps(next,heading,def.HalfWidth,def.HalfLength,b.Position,b.Heading,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,.16f))blocked=true;
                if(blocked)a.Speed=0;else{a.S=nextS;UpdatePose(a);}
            }
        }
        public void Present(Vector2 player,float dt,bool enabled)
        {
            foreach(var a in Actors)
            {
                if(!enabled||Vector2.Distance(player,a.Position)>280){a.View.Hide();continue;}
                a.View.Present(new Vector3(a.Position.x,.06f,a.Position.y),a.Heading,a.Speed,dt,true,a.S>a.ExitS-40&&a.S<a.ExitS+20?-1:0,a.Speed<.3f);
            }
            for(int i=0;i<people.Length;i++)
            {
                var p=PedestrianPaths.HiAccessPosition(network,i,elapsed);people[i].Root.SetActive(Vector2.Distance(player,p)<250);
                people[i].Draw(new Vector3(p.x,.25f,p.y),PedestrianPaths.HiAccessHeading(network,i,elapsed),elapsed+i,.65f,false);
            }
        }
    }
}
