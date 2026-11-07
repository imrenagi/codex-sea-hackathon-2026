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
        public bool Headlight {get;private set;}=true;
        public int Indicator {get;private set;}
        public int CameraMode {get;private set;}
        public VehicleMode Mode {get;private set;}
        public bool TrafficEnabled=true;
        readonly PlayerVehicleView player;
        readonly RoadVehicleMotion motion=new();
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
        readonly StreetPerson[] people;
        int seed=1975;
        bool snap=true;
        public LandmarkDrive(Transform parent,JakartaPlace place)
        {
            Environment=new LandmarkEnvironment(parent,place);
            var actors=new GameObject("Landmark vehicles and people").transform;actors.SetParent(Environment.Root.transform,false);
            player=new PlayerVehicleView(actors);
            Traffic=new Actor[place==JakartaPlace.BundaranHI?16:28];for(int i=0;i<Traffic.Length;i++)Traffic[i]=new Actor(actors,i);
            people=new StreetPerson[place==JakartaPlace.BundaranHI?20:44];
            for(int i=0;i<people.Length;i++)people[i]=new StreetPerson(actors,"Pedestrian "+i,new[]{"d2c4a6","4d718a","bb8166","697a61"}[i%4],i%7==0?.8f:1,false,i);
            Camera=new GameObject("Landmark camera").AddComponent<Camera>();Camera.transform.SetParent(Environment.Root.transform,false);Camera.tag="MainCamera";
            Camera.clearFlags=CameraClearFlags.Skybox;Camera.fieldOfView=65;Camera.nearClipPlane=.1f;Camera.farClipPlane=1500;Camera.allowHDR=true;Camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            Camera.gameObject.AddComponent<AudioListener>();
            Mirrors=new DrivingMirrors(Environment.Root.transform);
            Reset(VehicleMode.Car);
        }
        public void Reset(VehicleMode mode)
        {
            Mode=mode;motion.Reset();Speed=Distance=Elapsed=0;CurbContact=TrafficContact=false;Headlight=true;Indicator=0;CameraMode=0;
            Position=Environment.Route.At(0,-1.75f);Heading=Environment.Route.Heading(0);snap=true;Environment.Activate();
            var roster=new TrafficRoster(seed++);
            for(int i=0;i<Traffic.Length;i++)
            {
                var a=Traffic[i];a.Choice=roster.Choices[i%6];a.Lane=Environment.Place==JakartaPlace.BundaranHI?new[]{-5.15f,-1.75f,1.75f,5.15f}[i%4]:new[]{-1.75f,1.75f}[i%2];
                a.S=32+i*Environment.Route.Length/Traffic.Length;a.Target=Environment.Place==JakartaPlace.BundaranHI?6+i%3:8+i%3;a.Speed=a.Target;
                a.Position=Environment.Route.At(a.S,a.Lane);a.Heading=Environment.Route.Heading(a.S);a.View.Select(a.Choice);
            }
            Present(0);
        }
        public void ToggleCamera(){CameraMode=(CameraMode+1)%3;snap=true;}
        public void ToggleIndicator(int direction){Indicator=Indicator==direction?0:direction;}
        public void ToggleHeadlight(){Headlight=!Headlight;}
        public void Step(float dt,bool throttle,bool brake,float steer)
        {
            if(dt<=0)return;dt=Mathf.Min(dt,.04f);Elapsed+=dt;
            if(TrafficEnabled)StepTraffic(dt);
            float target=throttle&&!brake?11.1f:0;
            var forward=new Vector2(Mathf.Sin(Heading*Mathf.Deg2Rad),Mathf.Cos(Heading*Mathf.Deg2Rad));var right=new Vector2(forward.y,-forward.x);
            if(TrafficEnabled)foreach(var a in Traffic)
            {
                var delta=a.Position-Position;float ahead=Vector2.Dot(delta,forward);
                if(ahead<=0||Mathf.Abs(Vector2.Dot(delta,right))>a.Choice.Vehicle.HalfWidth+TrafficFootprint.PlayerHalfWidth(Mode)+.15f)continue;
                float gap=ahead-a.Choice.Vehicle.HalfLength-TrafficFootprint.PlayerHalfLength(Mode);
                target=Mathf.Min(target,Mathf.Sqrt(2*4*Mathf.Max(0,gap-2)));
            }
            TrafficContact=throttle&&!brake&&target<.5f;
            float nextSpeed=Speed,nextHeading=Heading;
            motion.Step(dt,Mode,throttle,brake,steer,target,ref nextSpeed,ref nextHeading);Speed=nextSpeed;
            float angle=nextHeading*Mathf.Deg2Rad;Vector2 next=Position+new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*Speed*dt;
            float s=Environment.Route.Nearest(next,out float lateral),relative=Mathf.DeltaAngle(Environment.Route.Heading(s),nextHeading)*Mathf.Deg2Rad;
            float extent=TrafficFootprint.PlayerHalfWidth(Mode)*Mathf.Abs(Mathf.Cos(relative))+TrafficFootprint.PlayerHalfLength(Mode)*Mathf.Abs(Mathf.Sin(relative));
            float limit=Environment.Map.roadWidth*.5f-extent-.18f;
            CurbContact=Mathf.Abs(lateral)>limit;
            if(CurbContact){next=Environment.Route.At(s,Mathf.Clamp(lateral,-limit,limit));Speed=Mathf.MoveTowards(Speed,0,20*dt);}
            bool blocked=false;
            if(TrafficEnabled)foreach(var a in Traffic)
                if(TrafficFootprint.Overlaps(next,nextHeading,TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode),a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,.13f)){blocked=true;break;}
            if(blocked){Speed=0;TrafficContact=true;}else{Distance+=Vector2.Distance(Position,next);Position=next;Heading=nextHeading;}
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
                float target=Mathf.Min(a.Target,Mathf.Sqrt(2*3.7f*Mathf.Max(0,nearest-2.5f)));a.Speed=Mathf.MoveTowards(a.Speed,target,(target<a.Speed?6:1.8f)*dt);
                float nextS=Mathf.Repeat(a.S+Mathf.Min(a.Speed*dt,Mathf.Max(0,nearest-.5f)),route.Length);var next=route.At(nextS,a.Lane);float heading=route.Heading(nextS);
                bool blocked=TrafficFootprint.Overlaps(next,heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,Position,Heading,TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode),.15f);
                foreach(var b in Traffic)if(a!=b&&TrafficFootprint.Overlaps(next,heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,b.Position,b.Heading,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,.1f)){blocked=true;break;}
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
            for(int i=0;i<people.Length;i++)
            {
                float start=i*Environment.Route.Length/people.Length,walk=Mathf.Sin(Elapsed*.075f+i)*7;
                float s=start+walk;var q=Environment.Route.At(s,i%3==0?9.6f:-9.6f);
                people[i].Root.SetActive(Vector2.Distance(Position,q)<240);
                people[i].Draw(new Vector3(q.x,.25f,q.y),Environment.Route.Heading(s)+(Mathf.Cos(Elapsed*.075f+i)<0?180:0),Elapsed+i,.7f,i%4==0);
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
