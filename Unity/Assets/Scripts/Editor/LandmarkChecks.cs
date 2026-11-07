using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace JalanKita.EditorTools
{
    public static class LandmarkChecks
    {
        [MenuItem("Jalan Kita/Verify Jakarta landmarks")]
        public static void Run()
        {
            var savedCar=VehicleCatalogue.Selected(VehicleMode.Car);var savedMotor=VehicleCatalogue.Selected(VehicleMode.Motorcycle);
            try
            {
                foreach(JakartaPlace place in Enum.GetValues(typeof(JakartaPlace)))
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                    var drive=new LandmarkDrive(new GameObject("Landmark verification").transform,place);
                    string folder="Assets/Resources/JakartaLandmarks/Materials";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                    foreach(var m in drive.Environment.Materials)
                    {
                        string path=folder+"/"+m.name+".mat";var prior=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(prior)EditorUtility.CopySerialized(m,prior);else AssetDatabase.CreateAsset(new Material(m),path);
                    }
                    AssetDatabase.SaveAssets();
                    if(place==JakartaPlace.Monas)
                    {
                        float height=drive.Environment.Root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name=="Landmark gold").Max(r=>r.bounds.max.y);
                        Require(Mathf.Abs(height-132)<.15f,"Monas height "+height);
                    }
                    Capture(drive);
                    Require(drive.Environment.Water.PoolCount==(place==JakartaPlace.BundaranHI?1:2),"Pool count");
                    Require(drive.Environment.Water.ReflectionFrames>0&&drive.Environment.Water.DropletCount>300,"Rendered water and fountain spray");
                    foreach(var choice in VehicleCatalogue.All)
                    {
                        VehicleCatalogue.Select(choice);drive.Reset(choice.Mode);drive.TrafficEnabled=false;
                        Require(drive.Environment.Root.GetComponentsInChildren<Transform>(true).Any(t=>t.name==choice.Label),"Selected model "+choice.Id);
                        float goal=choice.Id==VehicleCatalogue.All[0].Id?drive.Environment.Route.Length+10:280;
                        for(int i=0;i<24000&&drive.Distance<goal;i++)Coach(drive,.04f);
                        Require(drive.Distance>=goal,"Road traversal "+place+" "+choice.Id+" at "+drive.Distance+" of "+goal);
                        for(int i=0;i<200;i++)drive.Step(.04f,false,true,0);
                        Require(drive.Speed<.001f,"Braking "+choice.Id);
                        drive.ToggleCamera();drive.Present(0);Require(drive.CameraMode==1,"Driver camera");drive.ToggleCamera();drive.Present(0);Require(drive.CameraMode==2,"Landmark camera");
                        drive.Reset(choice.Mode);Require(drive.Distance==0&&drive.Speed==0&&drive.CameraMode==0,"Reset");
                        for(int i=0;i<400;i++)drive.Step(.04f,true,false,-1);
                        drive.Environment.Route.Nearest(drive.Position,out float lateral);Require(Mathf.Abs(lateral)<drive.Environment.Map.roadWidth*.5f,"Road containment");
                        Debug.Log("LANDMARK_VEHICLE_PASS "+place+" "+choice.Id);
                    }
                    drive.TrafficEnabled=true;drive.Reset(VehicleMode.Car);
                    var initial=drive.Traffic.Select(a=>a.Position).ToArray();
                    for(int i=0;i<1500;i++)Coach(drive,.04f);
                    Require(drive.Traffic.Where((a,i)=>Vector2.Distance(initial[i],a.Position)>10).Count()>3,"Moving traffic");
                    foreach(var a in drive.Traffic)Require(!TrafficFootprint.Overlaps(drive.Position,drive.Heading,TrafficFootprint.PlayerHalfWidth(drive.Mode),TrafficFootprint.PlayerHalfLength(drive.Mode),a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,0),"Player/traffic overlap");
                    float time=drive.Elapsed;var positions=drive.Traffic.Select(a=>a.Position).ToArray();drive.Step(0,true,false,1);drive.Present(0);
                    Require(drive.Environment.Water.SimulationTime==time,"Paused water time");
                    Require(time==drive.Elapsed&&drive.Traffic.Select((a,i)=>a.Position==positions[i]).All(x=>x),"Paused simulation");
                    Debug.Log("LANDMARK_TRAFFIC_PASS "+place+" route_m="+drive.Environment.Route.Length+" buildings="+drive.Environment.BuildingCount);
                }
            }
            finally{VehicleCatalogue.Select(savedCar);VehicleCatalogue.Select(savedMotor);}
            Debug.Log("ALL_LANDMARK_CHECKS_PASS");
        }
        static void Require(bool ok,string message){if(!ok)throw new Exception("Landmarks: "+message);}
        static void Coach(LandmarkDrive drive,float dt)
        {
            var route=drive.Environment.Route;float s=route.Nearest(drive.Position,out _);var target=route.At(s+10,-1.75f)-drive.Position;
            float desired=Mathf.Atan2(target.x,target.y)*Mathf.Rad2Deg,error=Mathf.DeltaAngle(drive.Heading,desired);
            drive.Step(dt,true,false,RoadVehicleMotion.TrackLane(drive.Mode,drive.Speed,drive.Heading,0,0,desired));
        }
        static void Capture(LandmarkDrive drive)
        {
            string folder="Builds/LandmarkPreview";Directory.CreateDirectory(folder);string id=drive.Environment.Map.id;
            var cam=drive.Camera;cam.aspect=1600f/900;var rt=new RenderTexture(1600,900,24){antiAliasing=4};cam.targetTexture=rt;
            bool old=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            for(int view=0;view<4;view++)
            {
                drive.Reset(VehicleMode.Car);drive.Present(0);string name=id+"-chase";
                if(view==1){drive.ToggleCamera();drive.Present(0);name=id+"-driver";}
                if(view==2){drive.ToggleCamera();drive.ToggleCamera();drive.Present(0);name=id+"-landmark";}
                if(view==3)
                {
                    var eye=id=="hi"?new Vector3(0,55,120):new Vector3(-190,70,240);
                    cam.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(drive.Environment.LandmarkTarget-eye));name=id+"-overview";
                }
                drive.Environment.Water.Present(cam,3.5f,true);
                if(view==2)
                {
                    var reflection=(RenderTexture)typeof(LandmarkWater).GetField("reflectionTexture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(drive.Environment.Water);
                    SaveTexture(reflection,folder+"/"+id+"-reflection.png");
                }
                for(int i=0;i<5;i++)cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);
            }
            cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);ShaderUtil.allowAsyncCompilation=old;
        }
        public static void RenderReview()
        {
            foreach(JakartaPlace place in Enum.GetValues(typeof(JakartaPlace)))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var drive=new LandmarkDrive(new GameObject("Landmark render review").transform,place);
                Capture(drive);
                var cam=drive.Camera;var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;
                var sample=new Texture2D(16,16,TextureFormat.RGB24,false);
                var timing=new System.Diagnostics.Stopwatch();var ms=new double[10];
                for(int i=0;i<12;i++)
                {
                    timing.Restart();drive.Environment.Water.Present(cam,i*.1f,true);cam.Render();
                    var prior=RenderTexture.active;RenderTexture.active=rt;sample.ReadPixels(new Rect(0,0,16,16),0,0);sample.Apply();RenderTexture.active=prior;
                    timing.Stop();if(i>=2)ms[i-2]=timing.Elapsed.TotalMilliseconds;
                }
                Array.Sort(ms);
                var meshes=drive.Environment.Root.GetComponentsInChildren<MeshFilter>();
                string note=$"{place}: synchronous 1600x900 main camera + forced 640x360 reflection + GPU readback median {ms[5]:F2} ms, p90 {ms[9]:F2} ms; meshes={meshes.Length}; vertices={meshes.Sum(m=>(long)m.sharedMesh.vertexCount)}; droplets={drive.Environment.Water.DropletCount}. Batch render benchmark, not live game FPS. Runtime reflections limited to changed viewpoints at most 11.1 Hz.\n";
                File.AppendAllText("Builds/LandmarkPreview/performance.txt",note);Debug.Log(note);
                cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(sample);
            }
        }
        public static void RunAndBuild(){Run();BuildMac();}
        static void SaveTexture(RenderTexture texture,string path)
        {
            var prior=RenderTexture.active;RenderTexture.active=texture;
            var image=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=prior;
        }
        public static void BuildMac()=>ProjectSetup.BuildMac();
    }
}
