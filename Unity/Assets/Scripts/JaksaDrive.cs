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
            foreach(var spot in new[]{new Vector3(-4.7f,0,34),new Vector3(4.8f,0,87),new Vector3(-4.9f,0,164),new Vector3(4.6f,0,219)})
            {
                var person=Actor("Person");person.name="Sidewalk pedestrian";
                person.transform.SetPositionAndRotation(new Vector3(Street.RoadX(spot.z)+spot.x,.25f,spot.z),Quaternion.Euler(0,spot.x<0?90:270,0));
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
            motion.Reset();
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
            if(dt<=0)return;
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
            bool vehicleContact=Traffic.BlocksPlayer(Position,next,Heading,Mode,previousHeading);
            if(!contained || vehicleContact)
            {
                next=Position;Heading=previousHeading;Speed=0;
                CurbContact|=!contained;TrafficContact|=vehicleContact;
                if(vehicleContact)collisionRemaining=.8f;
            }
            Position=next;
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
