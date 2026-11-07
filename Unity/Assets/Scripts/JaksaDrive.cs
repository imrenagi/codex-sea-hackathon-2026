using UnityEngine;
using UnityEngine.Rendering.Universal;
using UjianSim.Game;

namespace JalanKita
{
    // An independent drive leaves lesson saves and the existing five-checkpoint route intact.
    public sealed class JaksaDrive
    {
        public readonly JaksaStreet Street;
        public readonly Camera Camera;
        public Vector2 Position { get; private set; }
        public float Speed { get; private set; }
        public float Heading { get; private set; }
        public bool AtEnd => Position.y >= 246;
        public bool CurbContact { get; private set; }
        public bool Hood { get; set; }
        public VehicleMode Mode { get; private set; }
        public bool Headlight { get; private set; } = true;
        public int Indicator { get; private set; }
        public bool TrafficContact { get; private set; }
        float collisionRemaining;
        public bool VehicleCollision => collisionRemaining > 0;
        public readonly DriveSafetyState Safety = new();
        public float PostedLimitKph => 30;
        readonly DrivePedestrian[] pedestrians = new DrivePedestrian[PedestrianPaths.SopoCount];
        public readonly TrafficSimulation Traffic;
        readonly PlayerVehicleView player;
        readonly Transform actors;
        readonly DrivingCamera followCamera = new();
        public readonly DrivingMirrors Mirrors;
        readonly RoadVehicleMotion motion = new();
        readonly System.Func<Vector2, RoadBoundary.Frame> roadFrame;
        bool snap = true;
        int trafficSeed = 731;

        public JaksaDrive(Transform parent)
        {
            Street = new JaksaStreet(parent);
            roadFrame = RoadFrame;
            actors = new GameObject("Jaksa vehicles").transform;actors.SetParent(Street.Root.transform,false);
            player = new PlayerVehicleView(actors);
            Traffic = new TrafficSimulation(actors, Street);
            PedestrianPaths.FillSopo(Street,pedestrians);
            for(int i=0;i<pedestrians.Length;i++)
            {
                var person=Actor("Person");person.name="Sidewalk pedestrian";
                var spot=pedestrians[i].Position;
                person.transform.SetPositionAndRotation(new Vector3(spot.x,.25f,spot.y),Quaternion.Euler(0,PedestrianPaths.SopoHeading(i),0));
            }
            foreach(float z in new[]{14f,135f})
            {
                float yaw=RoadFrame(new Vector2(0,z)).Heading;
                RoadSpeedSign.Create(Street.Root.transform,new Vector3(Street.RoadX(z)-4.15f,.12f,z),yaw,30);
                RoadSpeedSign.Mark(Street.Root.transform,new Vector3(Street.RoadX(z+4)-1.5f,.08f,z+4),yaw,30);
                if(!Street.Data.oneway)RoadSpeedSign.Create(Street.Root.transform,new Vector3(Street.RoadX(z)+4.15f,.12f,z+12),yaw+180,30);
            }
            Camera = new GameObject("Jaksa camera").AddComponent<Camera>();
            Camera.gameObject.AddComponent<AudioListener>();
            Camera.transform.SetParent(Street.Root.transform,false);Camera.tag="MainCamera";
            Camera.clearFlags=CameraClearFlags.Skybox;Camera.backgroundColor=new Color(.72f,.78f,.79f);
            Camera.fieldOfView=64;Camera.nearClipPlane=.08f;Camera.farClipPlane=330;
            Camera.allowHDR=true;Camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            DrivingCamera.Configure(Camera);
            Mirrors = new DrivingMirrors(Street.Root.transform);
            Reset(VehicleMode.Car);
        }

        GameObject Actor(string model)
        {
            // Keep the imported FBX basis rotation on the child, as the training district does.
            var root=new GameObject(model+" actor");root.transform.SetParent(actors,false);
            Models.Spawn(model,root.transform);
            return root;
        }

        public void Reset(VehicleMode mode)
        {
            Mode=mode;
            var frame=RoadFrame(new Vector2(0,8));
            Heading=frame.Heading;
            Position=RoadBoundary.Fit(roadFrame,mode,new Vector2(Street.RoadX(8)-1.48f/frame.Forward.y,8),Heading);
            Speed=0;
            motion.Reset();Safety.Reset();PedestrianPaths.FillSopo(Street,pedestrians);
            Traffic.Reset(trafficSeed++);TrafficContact=false;collisionRemaining=0;
            Hood=false;CurbContact=false;Headlight=true;Indicator=0;snap=true;Street.Activate();Present(0);
        }
        public void ToggleCamera(){Hood=!Hood;snap=true;}
        public void ToggleIndicator(int direction) => Indicator = Indicator == direction ? 0 : direction;
        public void ToggleHeadlight() => Headlight = !Headlight;

        RoadBoundary.Frame RoadFrame(Vector2 position)
        {
            float slope=Street.RoadX(position.y+.1f)-Street.RoadX(position.y-.1f);
            var forward=new Vector2(slope,.2f).normalized;
            // This scene's mesh is a horizontal X strip, not a normal-offset strip.
            return new RoadBoundary.Frame(new Vector2(Street.RoadX(position.y),position.y),forward,
                JaksaStreet.RoadHalfWidth*forward.y);
        }

        public void Step(float dt,bool throttle,bool brake,float steer,bool reverse=false)
        {
            if(dt<=0||Safety.Failure.HasValue)return;
            dt=Mathf.Min(dt,.05f);
            collisionRemaining=Mathf.Max(0,collisionRemaining-dt);
            Traffic.Step(dt,Position,Heading,Mode);
            float safeSpeed=Traffic.SafePlayerSpeed(Position,Heading+(Speed<0||reverse&&Speed<=0?180:0),Mode);
            TrafficContact=(throttle||reverse)&&!brake&&safeSpeed<2;
            float previousHeading=Heading;
            float speed=Speed,heading=Heading;
            motion.Step(dt,Mode,throttle&&!AtEnd,brake||AtEnd&&!reverse&&Speed>=0,steer,safeSpeed,ref speed,ref heading,reverse);
            Vector2 next=RoadBoundary.Move(roadFrame,Mode,Position,dt,ref speed,ref heading,out bool curb);
            Speed=speed;Heading=heading;CurbContact=curb;
            // The rendered street begins at -24; leave room to reverse from the
            // initial pose at 8, while keeping the whole car inside this slice.
            bool terminal=next.y < -20 || next.y > 246;
            if(terminal)
            {
                float end=Mathf.Clamp(next.y,-20,246);
                float fraction=Mathf.Clamp01((end-Position.y)/(next.y-Position.y));
                next=Vector2.Lerp(Position,next,fraction);next.y=end;
                Heading=Mathf.LerpAngle(previousHeading,Heading,fraction);Speed=0;CurbContact=true;
            }
            next=RoadBoundary.Fit(roadFrame,Mode,next,Heading);
            bool contained=RoadBoundary.Contains(roadFrame,Mode,next,Heading);
            if(!contained){next=Position;Heading=previousHeading;Speed=0;}
            bool vehicleContact=Traffic.BlocksPlayer(Position,next,Heading,Mode,previousHeading);
            if(!contained || vehicleContact)
            {
                next=Position;Heading=previousHeading;Speed=0;
                CurbContact|=!contained;TrafficContact|=vehicleContact;
                if(vehicleContact)collisionRemaining=.8f;
            }
            bool personContact=DriveSafetyState.PedestrianContact(Position,next,previousHeading,Heading,Mode,pedestrians);
            var road=RoadFrame(next);
            var allowed=road.Forward;
            if(!Street.Data.oneway&&Vector2.Dot(next-road.Center,road.Right)>0)allowed=-allowed;
            Safety.Step(dt,new DriveSafetySample{SignedSpeed=Speed,Heading=Heading,AllowedForward=allowed,DirectionKnown=true,
                ReverseRequested=reverse,PostedLimitKph=PostedLimitKph,VehicleContact=vehicleContact,PedestrianContact=personContact});
            if(personContact){next=Position;Heading=previousHeading;Speed=0;}
            Position=next;
            if(Safety.Failure.HasValue)Speed=0;
        }

        public void Present(float dt, bool driving = true, float lookInput = 0)
        {
            bool motorcycle=Mode==VehicleMode.Motorcycle;
            Vector3 p=new(Position.x,.035f,Position.y);Quaternion rot=Quaternion.Euler(0,Heading,0);
            player.Present(Mode,p,Heading,Speed,dt,Headlight,Indicator,true,!Hood);
            Traffic.Present(dt,Position);
            followCamera.Present(Camera,p,Heading,Speed,motorcycle,Hood,dt,snap,driving ? lookInput : 0);
            Mirrors.Present(Mode,Position,Heading,driving,snap);snap=false;
        }
    }
}
