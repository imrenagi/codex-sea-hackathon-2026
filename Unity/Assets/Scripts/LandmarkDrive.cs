using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace JalanKita
{
    public sealed class LandmarkDrive
    {
        public readonly LandmarkEnvironment Environment;
        public readonly Camera Camera;
        public readonly DrivingMirrors Mirrors;
        float lookAngle;
        public Vector2 Position {get;private set;}
        public float Heading {get;private set;}
        public float Speed {get;private set;}
        public float Distance {get;private set;}
        public float Elapsed {get;private set;}
        public bool CurbContact {get;private set;}
        public bool TrafficContact {get;private set;}
        public readonly DriveSafetyState Safety = new();
        public float PostedLimitKph => 40;
        readonly DrivePedestrian[] pedestrians;
        public bool Headlight {get;private set;}=true;
        public int Indicator {get;private set;}
        public int CameraMode {get;private set;}
        public VehicleMode Mode {get;private set;}
        public bool TrafficEnabled=true;
        readonly PlayerVehicleView player;
        readonly RoadVehicleMotion motion=new();
        readonly Func<Vector2,RoadBoundary.Frame> roadFrame;
        public sealed class Actor
        {
            public readonly TrafficVehicleView View;
            public TrafficChoice Choice;
            public float S,Lane,Speed,Target;
            public Vector2 Position;
            public float Heading;
            public Actor(Transform parent,int i){View=new TrafficVehicleView(parent,"Landmark traffic "+i);}
        }
        public readonly Actor[] Traffic;
        public readonly LandmarkAccessTraffic AccessTraffic;
        readonly StreetPerson[] people;
        int seed=1975;
        bool snap=true;
        public LandmarkDrive(Transform parent,JakartaPlace place,bool forwardMissionTraffic=false)
        {
            Environment=new LandmarkEnvironment(parent,place);
            roadFrame=RoadFrame;
            var actors=new GameObject("Landmark vehicles and people").transform;actors.SetParent(Environment.Root.transform,false);
            player=new PlayerVehicleView(actors);
            Traffic=new Actor[place==JakartaPlace.BundaranHI?16:28];for(int i=0;i<Traffic.Length;i++)Traffic[i]=new Actor(actors,i);
            AccessTraffic=new LandmarkAccessTraffic(actors,Environment.Network,forwardMissionTraffic);
            people=new StreetPerson[place==JakartaPlace.BundaranHI?20:44];
            pedestrians=new DrivePedestrian[people.Length];
            for(int i=0;i<people.Length;i++)people[i]=new StreetPerson(actors,"Pedestrian "+i,new[]{"d2c4a6","4d718a","bb8166","697a61"}[i%4],i%7==0?.8f:1,false,i);
            Camera=new GameObject("Landmark camera").AddComponent<Camera>();Camera.transform.SetParent(Environment.Root.transform,false);Camera.tag="MainCamera";
            Camera.clearFlags=CameraClearFlags.Skybox;Camera.fieldOfView=65;Camera.nearClipPlane=.1f;Camera.farClipPlane=1500;Camera.allowHDR=true;Camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            Camera.gameObject.AddComponent<AudioListener>();
            Mirrors=new DrivingMirrors(Environment.Root.transform);
            foreach(var branch in Environment.Network.Branches)
            {
                float s=branch.Incoming?20:40;var p=branch.Route.At(s,-branch.Width*.5f-1.1f);
                RoadSpeedSign.Create(Environment.Root.transform,new Vector3(p.x,.12f,p.y),branch.Route.Heading(s),40);
            }
            Reset(VehicleMode.Car);
        }
        public void Reset(VehicleMode mode)
        {
            Mode=mode;motion.Reset();Safety.Reset();Speed=Distance=Elapsed=0;CurbContact=TrafficContact=false;Headlight=true;Indicator=0;CameraMode=0;
            Heading=Environment.Network.SpawnHeading;Position=RoadBoundary.Fit(roadFrame,mode,Environment.Network.Spawn,Heading);snap=true;Environment.Activate();
            int rosterSeed=seed++;var roster=new TrafficRoster(rosterSeed);
            AccessTraffic.Reset(rosterSeed);
            for(int i=0;i<Traffic.Length;i++)
            {
                var a=Traffic[i];a.Choice=roster.Choices[i%6];a.Lane=Environment.Place==JakartaPlace.BundaranHI?new[]{-5.15f,-1.75f,1.75f,5.15f}[i%4]:new[]{-1.75f,1.75f}[i%2];
                a.S=32+i*Environment.Route.Length/Traffic.Length;a.Target=Environment.Place==JakartaPlace.BundaranHI?6+i%3:8+i%3;a.Speed=a.Target;
                a.Position=Environment.Route.At(a.S,a.Lane);a.Heading=Environment.Route.Heading(a.S);a.View.Select(a.Choice);
            }
            PedestrianPaths.FillHiRing(Environment.Route,pedestrians,0,0);
            Present(0);
        }
        public void ToggleCamera(){CameraMode=(CameraMode+1)%3;snap=true;}
        public void ToggleIndicator(int direction){Indicator=Indicator==direction?0:direction;}
        public void ToggleHeadlight(){Headlight=!Headlight;}
        RoadBoundary.Frame RoadFrame(Vector2 position)
        {
            Environment.Network.Sample(position,out var center,out var forward,out float halfWidth);
            return new RoadBoundary.Frame(center,forward,halfWidth);
        }
        bool OnFiniteRoad(Vector2 position,float heading)
        {
            float angle=heading*Mathf.Deg2Rad;
            Vector2 forward=new(Mathf.Sin(angle),Mathf.Cos(angle)),right=new(forward.y,-forward.x);
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                if(!Environment.Network.ContainsPoint(position+right*(x*RoadBoundary.HalfWidth(Mode))+forward*(z*RoadBoundary.HalfLength(Mode)),.1f))return false;
            return true;
        }
        bool TrafficBlocks(Vector2 next,float nextHeading)
        {
            float turn=Mathf.DeltaAngle(Heading,nextHeading);
            float radius=new Vector2(TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode)).magnitude;
            int samples=Mathf.Max(1,Mathf.CeilToInt((Vector2.Distance(Position,next)+Mathf.Abs(turn)*Mathf.Deg2Rad*radius)/.15f));
            for(int i=1;i<=samples;i++)
                foreach(var a in Traffic)
                    if(TrafficFootprint.Overlaps(Vector2.Lerp(Position,next,(float)i/samples),Heading+turn*i/samples,
                        TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode),a.Position,a.Heading,
                        a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,.13f))return true;
            return false;
        }
        public void Step(float dt,bool throttle,bool brake,float steer,bool reverse=false)
        {
            if(dt<=0||Safety.Failure.HasValue)return;dt=Mathf.Min(dt,.04f);float previousTime=Elapsed;Elapsed+=dt;
            PedestrianPaths.FillHiRing(Environment.Route,pedestrians,previousTime,Elapsed);
            var previousPosition=Position;float previousHeading=Heading;
            if(TrafficEnabled){StepTraffic(dt);AccessTraffic.Step(dt,Position,Heading,Mode,Traffic);}
            float target=RoadVehicleMotion.FreeSpeed(Mode);
            float travelHeading=Heading+(Speed<0||reverse&&Speed<=0?180:0);
            var forward=new Vector2(Mathf.Sin(travelHeading*Mathf.Deg2Rad),Mathf.Cos(travelHeading*Mathf.Deg2Rad));var right=new Vector2(forward.y,-forward.x);
            if(TrafficEnabled)foreach(var a in Traffic)
            {
                var delta=a.Position-Position;float ahead=Vector2.Dot(delta,forward);
                if(ahead<=0||Mathf.Abs(Vector2.Dot(delta,right))>a.Choice.Vehicle.HalfWidth+TrafficFootprint.PlayerHalfWidth(Mode)+.15f)continue;
                float gap=ahead-a.Choice.Vehicle.HalfLength-TrafficFootprint.PlayerHalfLength(Mode);
                target=Mathf.Min(target,Mathf.Sqrt(2*4*Mathf.Max(0,gap-2)));
            }
            if(TrafficEnabled)target=AccessTraffic.SafeSpeed(Position,travelHeading,TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode),target);
            TrafficContact=(throttle||reverse)&&!brake&&target<.5f;
            float nextSpeed=Speed,nextHeading=Heading;
            motion.Step(dt,Mode,throttle,brake,steer,target,ref nextSpeed,ref nextHeading,reverse);
            Vector2 next=RoadBoundary.Move(roadFrame,Mode,Position,dt,ref nextSpeed,ref nextHeading,out bool curb);
            next=Environment.Network.ClampEndpoint(Position,next,out bool endpoint);
            if(endpoint)nextSpeed=0;
            Speed=nextSpeed;CurbContact=curb||endpoint;
            bool outside=Vector2.Distance(Position,next)>Mathf.Abs(nextSpeed)*dt+.5f
                ||!RoadBoundary.Contains(roadFrame,Mode,next,nextHeading)||!OnFiniteRoad(next,nextHeading);
            bool actorBlocked=TrafficEnabled&&(TrafficBlocks(next,nextHeading)||AccessTraffic.BlocksPath(Position,next,Heading,nextHeading,TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode)));
            bool personContact=DriveSafetyState.PedestrianContact(Position,next,Heading,nextHeading,Mode,pedestrians)
                ||DriveSafetyState.PedestrianContact(Position,next,Heading,nextHeading,Mode,AccessTraffic.Pedestrians);
            Environment.Network.AllowedDirections(next,out var allowed,out var alternate);
            Safety.Step(dt,new DriveSafetySample{SignedSpeed=Speed,Heading=nextHeading,AllowedForward=allowed,AlternateForward=alternate,
                DirectionKnown=true,ReverseRequested=reverse,PostedLimitKph=PostedLimitKph,VehicleContact=actorBlocked,PedestrianContact=personContact});
            if(outside||actorBlocked||personContact){Speed=0;CurbContact|=outside;TrafficContact|=actorBlocked;}
            else{Distance+=Vector2.Distance(Position,next);Position=next;Heading=nextHeading;}
        }
        void StepTraffic(float dt)
        {
            var route=Environment.Route;float playerS=route.Nearest(Position,out float playerLane);
            foreach(var a in Traffic)
            {
                float nearest=float.PositiveInfinity;
                foreach(var b in Traffic)
                {if(a==b||Mathf.Abs(a.Lane-b.Lane)>2)continue;float ahead=Mathf.Repeat(b.S-a.S,route.Length);nearest=Mathf.Min(nearest,ahead-a.Choice.Vehicle.HalfLength-b.Choice.Vehicle.HalfLength);}
                if(Mathf.Abs(playerLane-a.Lane)<TrafficFootprint.PlayerHalfWidth(Mode)+a.Choice.Vehicle.HalfWidth+.2f)
                    nearest=Mathf.Min(nearest,Mathf.Repeat(playerS-a.S,route.Length)-a.Choice.Vehicle.HalfLength-TrafficFootprint.PlayerHalfLength(Mode));
                float target=Mathf.Min(a.Target,Mathf.Sqrt(2*3.7f*Mathf.Max(0,nearest-2.5f)));
                target=AccessTraffic.SafeSpeed(a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,target);a.Speed=Mathf.MoveTowards(a.Speed,target,(target<a.Speed?6:1.8f)*dt);
                float nextS=Mathf.Repeat(a.S+Mathf.Min(a.Speed*dt,Mathf.Max(0,nearest-.5f)),route.Length);var next=route.At(nextS,a.Lane);float heading=route.Heading(nextS);
                bool blocked=TrafficFootprint.Overlaps(next,heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,Position,Heading,TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode),.15f);
                foreach(var b in Traffic)if(a!=b&&TrafficFootprint.Overlaps(next,heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,b.Position,b.Heading,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,.1f)){blocked=true;break;}
                if(AccessTraffic.BlocksPath(a.Position,next,a.Heading,heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength))blocked=true;
                if(blocked)a.Speed=0;else{a.S=nextS;a.Position=next;a.Heading=heading;}
            }
        }
        public void Present(float dt,bool driving=true,float lookInput=0)
        {
            var p=new Vector3(Position.x,.055f,Position.y);var rotation=Quaternion.Euler(0,Heading,0);
            player.Present(Mode,p,Heading,Speed,dt,Headlight,Indicator,true,CameraMode!=1);
            foreach(var a in Traffic)
            {
                if(!TrafficEnabled||Vector2.Distance(Position,a.Position)>230){a.View.Hide();continue;}
                a.View.Present(new Vector3(a.Position.x,.055f,a.Position.y),a.Heading,a.Speed,dt,true,0,a.Speed<.2f);
            }
            AccessTraffic.Present(Position,dt,TrafficEnabled);
            for(int i=0;i<people.Length;i++)
            {
                var q=PedestrianPaths.HiRingPosition(Environment.Route,i,people.Length,Elapsed);
                people[i].Root.SetActive(Vector2.Distance(Position,q)<240);
                people[i].Draw(new Vector3(q.x,.25f,q.y),PedestrianPaths.HiRingHeading(Environment.Route,i,people.Length,Elapsed),Elapsed+i,.7f,i%4==0);
            }
            lookAngle=Mathf.Lerp(lookAngle,driving?lookInput*65:0,1-Mathf.Exp(-8*dt));
            Vector3 eye,look;
            if(CameraMode==2){eye=p+rotation*new Vector3(-2,4,-7);look=Environment.LandmarkTarget;}
            else{eye=p+rotation*(CameraMode==1?new Vector3(0,Mode==VehicleMode.Car?1.4f:1.6f,.7f):new Vector3(0,3,-6));look=p+rotation*new Vector3(0,1.5f,18);}
            if(CameraMode!=2)look=eye+Quaternion.AngleAxis(lookAngle,Vector3.up)*(look-eye);
            Mirrors.Present(Mode,Position,Heading,driving,snap);
            float blend=snap?1:1-Mathf.Exp(-7*Mathf.Max(0,dt));Camera.transform.SetPositionAndRotation(Vector3.Lerp(Camera.transform.position,eye,blend),Quaternion.Slerp(Camera.transform.rotation,Quaternion.LookRotation(look-eye),blend));Environment.Water.Present(Camera,Elapsed,snap);snap=false;
        }
    }
}
