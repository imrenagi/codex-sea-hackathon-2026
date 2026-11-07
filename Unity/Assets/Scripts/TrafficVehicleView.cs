using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UjianSim.Game;

namespace JalanKita
{
    // No per-vehicle Light, Rigidbody or material instance. Shared materials only change
    // on lamp-state transitions; wheels and real lens meshes survive both LODs.
    public sealed class TrafficVehicleView
    {
        public readonly GameObject Root;
        public TrafficChoice Choice {get;private set;}
        readonly List<Pivot> wheels=new(),steering=new();
        readonly List<Renderer> left=new(),right=new(),head=new(),tail=new();
        readonly List<PaintSlot> paintSlots=new();
        readonly Dictionary<Renderer,Material> lampState=new();
        GameObject content;
        bool positioned;
        float travelled,previousHeading,previousSpeed,blinkClock,brakeHold;
        int previousIndicator;
        Vector3 previousPosition;
        static readonly Dictionary<string,Material> materials=new();
        sealed class Pivot {public Transform Part;public Quaternion Rest;public Vector3 Axis;}
        sealed class PaintSlot {public Renderer Renderer;public int Index;}
        public TrafficVehicleView(Transform parent,string name)
        {Root=new GameObject(name);Root.transform.SetParent(parent,false);Root.SetActive(false);}
        static Material Material(string name)
        {
            if(!materials.TryGetValue(name,out var m)||!m)
                materials[name]=m=Resources.Load<Material>("Traffic/Materials/"+name);
            if(!m)throw new InvalidOperationException("Missing prepared traffic material "+name);
            return m;
        }
        public void Select(TrafficChoice choice)
        {
            if(Choice?.Vehicle==choice.Vehicle)
            {if(Choice.Color!=choice.Color){Choice=choice;ApplyPaint();}return;}
            Choice=choice;
            if(content){content.SetActive(false);Models.Kill(content);}
            wheels.Clear();steering.Clear();left.Clear();right.Clear();head.Clear();tail.Clear();paintSlots.Clear();lampState.Clear();positioned=false;
            content=new GameObject(choice.Vehicle.Id);content.transform.SetParent(Root.transform,false);
            content.transform.localScale=Vector3.one*choice.Vehicle.Scale;
            var levels=new List<Renderer[]>();
            for(int level=0;level<2;level++)
            {
                string path="Traffic/"+choice.Vehicle.Id+"/"+(level==0?"Body":"Body_LOD1");
                var prefab=Resources.Load<GameObject>("StreetLook/Vehicles/"+choice.Vehicle.Id+"/"+(level==0?"Body":"Body_LOD1"))
                    ?? Resources.Load<GameObject>(path);if(!prefab)throw new InvalidOperationException("Missing traffic "+path);
                var adapter=new GameObject("Traffic forward adapter").transform;adapter.SetParent(content.transform,false);
                var model=UnityEngine.Object.Instantiate(prefab,adapter);
                // Infer forward from the physical axles, preserving the FBX import basis.
                var front=Find(model.transform,choice.Vehicle.Motorcycle?"Wheel_Front":"Wheel_FL");
                var rear=Find(model.transform,choice.Vehicle.Motorcycle?"Wheel_Rear":"Wheel_RL");
                var forward=content.transform.InverseTransformDirection(front.position-rear.position);forward.y=0;
                // A 180-degree FromToRotation can choose the X axis and invert the model.
                // Constrain the conversion to yaw so imported +Y remains upright.
                adapter.localRotation=Quaternion.Euler(0,-Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,0);
                foreach(var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    var slots=r.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {
                        bool paint=slots[i]&&slots[i].name.StartsWith("Body_Paint");
                        slots[i]=Material(paint?PaintName():"Atlas");
                        if(paint)paintSlots.Add(new PaintSlot{Renderer=r,Index=i});
                    }
                    r.sharedMaterials=slots;r.shadowCastingMode=level==0?ShadowCastingMode.On:ShadowCastingMode.Off;
                    r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                }
                string[] wheelNames=choice.Vehicle.Motorcycle?new[]{"Wheel_Front","Wheel_Rear"}:new[]{"Wheel_FL","Wheel_FR","Wheel_RL","Wheel_RR"};
                foreach(string n in wheelNames)AddPivot(model.transform,n,wheels,Vector3.right);
                foreach(string n in choice.Vehicle.Motorcycle?new[]{"Steering_Pivot"}:new[]{"Steering_FL","Steering_FR"})AddPivot(model.transform,n,steering,Vector3.up);
                head.AddRange(Find(model.transform,"Headlights").GetComponentsInChildren<Renderer>(true));
                tail.AddRange(Find(model.transform,"TailLights").GetComponentsInChildren<Renderer>(true));
                foreach(string n in new[]{"Indicator_Front_L","Indicator_Front_R","Indicator_Rear_L","Indicator_Rear_R"})
                    foreach(var r in Find(model.transform,n).GetComponentsInChildren<Renderer>(true))
                        (Root.transform.InverseTransformPoint(r.transform.TransformPoint(r.GetComponent<MeshFilter>().sharedMesh.bounds.center)).x<0?left:right).Add(r);
                levels.Add(model.GetComponentsInChildren<Renderer>(true));
            }
            var group=content.AddComponent<LODGroup>();group.animateCrossFading=false;
            group.SetLODs(new[]{new LOD(.13f,levels[0]),new LOD(.012f,levels[1])});group.RecalculateBounds();
            SetLamps(head,"Atlas");SetLamps(tail,"Atlas");SetLamps(left,"Atlas");SetLamps(right,"Atlas");
        }
        string PaintName()=>Choice.Vehicle.Id+"_"+Choice.Color;
        void ApplyPaint()
        {foreach(var slot in paintSlots){var mats=slot.Renderer.sharedMaterials;mats[slot.Index]=Material(PaintName());slot.Renderer.sharedMaterials=mats;}}
        static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true)
            .First(t=>t.name==name||t.name==name+"_Export");
        void AddPivot(Transform root,string name,List<Pivot> list,Vector3 axis)
        {
            var part=Find(root,name);list.Add(new Pivot{Part=part,Rest=part.localRotation,
                Axis=part.parent.InverseTransformDirection(Root.transform.TransformDirection(axis))});
        }
        public void Hide(){Root.SetActive(false);positioned=false;}
        public void Present(Vector3 position,float yaw,float speed,float dt,bool headlights=true,int indicator=0,bool braking=false,float steeringAngle=float.NaN,bool signedSpeed=false)
        {
            if(Choice==null)throw new InvalidOperationException("Select traffic before presenting");
            Root.SetActive(true);dt=Mathf.Clamp(dt,0,.1f);
            float distance=positioned?Vector3.Distance(position,previousPosition):0;
            bool teleport=!positioned||distance>3;
            // NPCs retain their negative auto-speed sentinel; only player callers
            // opt into signed metres/sec for reverse wheel and steering motion.
            if(!signedSpeed&&speed<0)speed=!teleport&&dt>0?distance/dt:0;
            if(!teleport){travelled+=signedSpeed&&speed<0?-distance:distance;if(Mathf.Abs(previousSpeed)-Mathf.Abs(speed)>2*dt&&dt>0)brakeHold=.4f;}
            else{travelled=0;brakeHold=0;}
            brakeHold=Mathf.Max(0,brakeHold-dt);
            float turn=0;
            if(!teleport&&dt>0&&Mathf.Abs(speed)>.2f)
                turn=Mathf.Atan(Choice.Vehicle.Wheelbase*Choice.Vehicle.Scale*Mathf.DeltaAngle(previousHeading,yaw)*Mathf.Deg2Rad/dt/speed)*Mathf.Rad2Deg;
            if(!float.IsNaN(steeringAngle))turn=steeringAngle;
            Root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            foreach(var p in steering)p.Part.localRotation=Quaternion.AngleAxis(Mathf.Clamp(turn,-32,32),p.Axis)*p.Rest;
            float roll=travelled/(Choice.Vehicle.Radius*Choice.Vehicle.Scale)*Mathf.Rad2Deg;
            foreach(var p in wheels)p.Part.localRotation=Quaternion.AngleAxis(roll,p.Axis)*p.Rest;
            blinkClock=indicator!=previousIndicator?0:blinkClock+dt;
            bool blink=blinkClock%.72f<.36f;
            SetLamps(left,blink&&(indicator==-1||indicator==2)?"IndicatorOn":"Atlas");
            SetLamps(right,blink&&(indicator==1||indicator==2)?"IndicatorOn":"Atlas");
            SetLamps(head,headlights?"HeadOn":"Atlas");
            SetLamps(tail,braking||brakeHold>0?"BrakeOn":headlights?"TailOn":"Atlas");
            previousPosition=position;previousHeading=yaw;previousSpeed=speed;previousIndicator=indicator;positioned=true;
        }
        void SetLamps(List<Renderer> renderers,string name)
        {
            var material=Material(name);
            foreach(var r in renderers)
            {
                if(lampState.TryGetValue(r,out var old)&&old==material)continue;
                var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=material;
                r.sharedMaterials=mats;lampState[r]=material;
            }
        }
    }
}
