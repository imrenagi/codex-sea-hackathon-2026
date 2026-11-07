using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UjianSim.Game;

namespace JalanKita.EditorTools
{
    public static class TrafficChecks
    {
        const string Base="Assets/Resources/Traffic/";
        static readonly string Output=Path.GetFullPath("Builds/TrafficEvidence");
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("TRAFFIC: "+message);}
        [MenuItem("Jalan Kita/Prepare traffic vehicles")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Base,"*.fbx",SearchOption.AllDirectories))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation=false;importer.importLights=false;importer.importCameras=false;
                importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles(Base+"Textures","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=path.Contains("BaseColor");
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=128;importer.SaveAndReimport();
            }
            Directory.CreateDirectory(Base+"Materials");AssetDatabase.Refresh();
            var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"Textures/BaseColor.png");
            var gloss=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"Textures/MetallicSmoothness.png");
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(Base+"Textures/LampMask.png");
            foreach(string name in new[]{"Atlas","HeadOn","TailOn","BrakeOn","IndicatorOn"})
            {
                var m=Material(name);m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",albedo);
                m.SetTexture("_MetallicGlossMap",gloss);m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",mask);
                m.SetColor("_EmissionColor",name switch{
                    "HeadOn"=>new Color(.8f,.94f,1)*3.2f,"TailOn"=>new Color(1,.025f,.01f)*.3f,
                    "BrakeOn"=>new Color(1,.025f,.01f)*4,"IndicatorOn"=>new Color(1,.34f,.015f)*5,_=>Color.black});
                // URP validates _EMISSION from AnyEmissive; None silently removes the keyword.
                // BakedEmissive keeps visible self-illumination, with no runtime GI/light cost.
                m.globalIlluminationFlags=name=="Atlas"?MaterialGlobalIlluminationFlags.EmissiveIsBlack:MaterialGlobalIlluminationFlags.BakedEmissive;
                EditorUtility.SetDirty(m);
            }
            foreach(var def in TrafficCatalogue.All)
                for(int i=0;i<2;i++)
                {
                    var m=Material(def.Id+"_"+i);ColorUtility.TryParseHtmlString("#"+def.Colors[i],out var color);
                    m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.22f);m.SetFloat("_Smoothness",.68f);EditorUtility.SetDirty(m);
                }
            AssetDatabase.SaveAssets();Run();Capture();
            Debug.Log("TRAFFIC_PREPARE_VERIFY_CAPTURE_PASSED");
        }
        static Material Material(string name)
        {
            string path=Base+"Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};AssetDatabase.CreateAsset(m,path);}
            return m;
        }
        static Transform Node(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name+"_Export");
        static Vector3 Center(Renderer r)=>r.transform.TransformPoint(r.GetComponent<MeshFilter>().sharedMesh.bounds.center);
        static Renderer[] Signals(GameObject root)=>root.GetComponentsInChildren<Renderer>(true).Where(r=>r.transform.parent.name.StartsWith("Indicator_")).ToArray();
        static void CheckSignals(TrafficVehicleView view,int side,bool on)
        {
            var signals=Signals(view.Root);Require(signals.Length==8,"Four physical lenses in each LOD: "+view.Choice.Vehicle.Id);
            foreach(var r in signals)
            {
                var p=view.Root.transform.InverseTransformPoint(Center(r));
                bool expected=on&&(side==2||(side<0?p.x<0:p.x>0));
                Require(r.sharedMaterial.name==(expected?"IndicatorOn":"Atlas"),view.Choice.Vehicle.Id+" wrong indicator side/phase at "+p);
                if(expected)Require(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),"Indicator shader emission variant is disabled");
                Require(Mathf.Abs(p.x)>.06f&&Mathf.Abs(p.z)>.35f,"Indicator detached from a vehicle corner");
            }
            Require(signals.Count(r=>view.Root.transform.InverseTransformPoint(Center(r)).x<0)==4,"Left/right lamp geometry balance");
        }
        [MenuItem("Jalan Kita/Verify traffic integration")]
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            foreach(var def in TrafficCatalogue.All)
            {
                var host=new GameObject("Check "+def.Id);var view=new TrafficVehicleView(host.transform,"Traffic check");
                for(int paint=0;paint<2;paint++)
                {
                    view.Select(new TrafficChoice(def,paint));view.Present(Vector3.zero,0,0,0,true,-1);CheckSignals(view,-1,true);
                    Require(view.Root.GetComponentsInChildren<Light>(true).Length==0,"NPC realtime lights add avoidable cost");
                    var front=Node(view.Root,def.Motorcycle?"Wheel_Front":"Wheel_FL");var rear=Node(view.Root,def.Motorcycle?"Wheel_Rear":"Wheel_RL");
                    Require(front.position.z>rear.position.z+.9f,"Wrong forward axis "+def.Id);
                    Require(Mathf.Abs(front.position.y-def.Radius*def.Scale)<.015f,"Wheel grounding "+def.Id+" front="+front.position+" rear="+rear.position+" scale="+front.lossyScale);
                    var body=view.Root.GetComponentsInChildren<Renderer>(true).First(r=>r.sharedMaterials.Any(m=>m.name==def.Id+"_"+paint));
                    Require(body.sharedMaterials.All(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"),"Missing URP materials");
                    var rest=front.localRotation;
                    for(int i=0;i<4;i++)view.Present(new Vector3(0,0,(i+1)*.1f),0,1,.1f,true,-1);
                    CheckSignals(view,-1,false);Require(Quaternion.Angle(rest,front.localRotation)>2,"Wheel does not roll");
                    view.Present(new Vector3(0,0,.4f),0,0,.02f,true,1,true);CheckSignals(view,1,true);
                    Require(Node(view.Root,"TailLights").GetComponentInChildren<Renderer>().sharedMaterial.name=="BrakeOn","Brake lens does not light");
                    view.Present(new Vector3(0,0,.4f),0,0,.02f,false,0);CheckSignals(view,1,false);
                    Require(Node(view.Root,"Headlights").GetComponentInChildren<Renderer>().sharedMaterial.name=="Atlas","Headlights off");
                    view.Present(new Vector3(0,0,.4f),0,0,.02f,true,2);CheckSignals(view,2,true);
                    var lods=view.Root.GetComponentInChildren<LODGroup>().GetLODs();Require(lods.Length==2,"LOD missing");
                    long Count(LOD l)=>l.renderers.Sum(r=>{var m=r.GetComponent<MeshFilter>().sharedMesh;return Enumerable.Range(0,m.subMeshCount).Sum(i=>(long)m.GetIndexCount(i)/3);});
                    Require(Count(lods[1])<Count(lods[0])*.65f,"LOD ineffective");
                    Debug.Log($"TRAFFIC_MODEL {def.Id}/{paint} {Count(lods[0])}->{Count(lods[1])} triangles; actual left/right lamps PASS");
                }
                UnityEngine.Object.DestroyImmediate(host);
            }
            // Existing player lamps must also agree with physical left/right after FBX basis conversion.
            foreach(var def in VehicleCatalogue.All)
            {
                var host=new GameObject("Player indicators");var view=new PlayerVehicleView(host.transform,def);
                view.Present(def.Mode,Vector3.zero,0,0,0,true,-1,true,true);
                var lamps=host.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Lamp_Ind_")||r.transform.parent.name.StartsWith("Indicator_")).ToArray();
                Require(lamps.Length>=2,"Player indicator meshes missing "+def.Id);
                foreach(var r in lamps)
                {
                    var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);
                    bool lit=(block.isEmpty?r.sharedMaterial.GetColor("_EmissionColor"):block.GetColor("_EmissionColor")).maxColorComponent>0;
                    Require(lit==(Center(r).x<0),"Player left indicator on wrong side: "+def.Id+" "+r.name);
                    if(lit)Require(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),"Player lamp shader emission disabled: "+def.Id);
                }
                UnityEngine.Object.DestroyImmediate(host);
            }
            VehicleChecks.Run();
            MissionChecks.Run();
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
                foreach(var mission in Mission.Tests.Concat(Mission.Scenarios))
                {
                    var run=new Journey(mode,mission.NewSave(),false,mission);run.Retry();
                    var saved=JsonUtility.FromJson<JourneySave>(JsonUtility.ToJson(run.Save));
                    var resumed=new Journey(mode,saved,false,mission);
                    var demo=new Journey(mode,new JourneySave{next=run.Stage,checkpoint=saved.checkpoint,variant=saved.variant,trafficSeed=saved.trafficSeed},false,mission);
                    string Fleet(Journey j)=>string.Join(",",j.Traffic.Choices.Select(c=>c.Vehicle.Id+":"+c.Color));
                    Require(saved.trafficSeed!=0&&Fleet(run)==Fleet(resumed)&&Fleet(run)==Fleet(demo),"Gameplay retry/reload/demo roster differs "+mission.Id);
                }
            Debug.Log("TRAFFIC_ROSTER_PERSISTENCE_PASSED 16 gameplay retry/reload/demo cases without explicit seeds");
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
                foreach(int seed in new[]{17,203,601,992})
                    foreach(var mission in Mission.Tests.Concat(Mission.Scenarios))
                    {
                        var run=new Journey(mode,mission.NewSave(),false,mission,seed);
                        var same=new Journey(mode,mission.NewSave(),false,mission,seed);
                        Require(run.Traffic.Choices.Select(c=>c.Vehicle.Id+":"+c.Color).SequenceEqual(same.Traffic.Choices.Select(c=>c.Vehicle.Id+":"+c.Color)),"Seed is not stable");
                        if(run.Stage==0){run.DriverGear=run.PassengerGear=run.Headlight=true;Require(run.Depart(),"Preflight");}
                        for(int i=0;i<10000&&!run.Finished&&!run.Failure.HasValue;i++)DrivingCoach.Step(run,.02f);
                        Require(run.Finished&&!run.Failure.HasValue,$"Seeded lesson {mission.Id}/{mode}/{seed}: {run.Failure} at {run.Position}");
                        foreach(var def in TrafficCatalogue.All)
                        {
                            var choice=new TrafficChoice(def,0);
                            Require(run.TrafficCollision(choice,run.Position,0),"Coincident traffic collision not detected");
                            Require(!run.TrafficCollision(choice,run.Position+Vector2.right*6,0),"Invisible distant collision");
                        }
                    }
            Debug.Log("TRAFFIC_SEEDED_MISSIONS_PASSED 64 completions plus baseline failure/retry/save checks");
            CheckExploration();
            JaksaChecks.Run();
            Debug.Log("ALL_TRAFFIC_CHECKS_PASSED");
        }
        public static void CheckExploration()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var root=new GameObject("Sopo Del traffic tests");var drive=new JaksaDrive(root.transform);
            Require(drive.Street.IsSopoDel,"Exploration must use current Sopo Del scene");
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
                foreach(int seed in new[]{17,203,601})
                {
                    drive.Reset(mode);drive.Traffic.Reset(seed);
                    Require(drive.Traffic.Actors.Select(a=>a.Choice.Vehicle.Id).Distinct().Count()==6,"Fleet omits models");
                    for(int i=0;i<7000&&!drive.AtEnd;i++)
                    {
                        float steer=ExplorationSteer(drive);
                        drive.Step(.02f,true,false,steer);
                        foreach(var a in drive.Traffic.Actors)
                            Require(!TrafficFootprint.Overlaps(drive.Position,drive.Heading,TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode),
                                a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,-.015f),"Player/NPC overlap "+mode+" "+seed+" at "+drive.Position);
                        if(i%120==0)
                            foreach(var a in drive.Traffic.Actors)
                                foreach(var b in drive.Traffic.Actors)
                                    if(a!=b)Require(!TrafficFootprint.Overlaps(a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,
                                        b.Position,b.Heading,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,-.015f),"Traffic vehicles overlap");
                    }
                    Require(drive.AtEnd,"Traffic deadlock / road traversal "+mode+" "+seed+" at "+drive.Position+" yaw="+drive.Heading+" speed="+drive.Speed+" safe="+drive.Traffic.SafePlayerSpeed(drive.Position,drive.Heading,mode)+" curb="+drive.CurbContact+" traffic="+drive.TrafficContact+" actors="+string.Join(";",drive.Traffic.Actors.Select(a=>$"{a.Choice.Vehicle.Id} {a.Position} yaw={a.Heading} speed={a.Speed}")));
                    var actor=drive.Traffic.Actors[0];
                    Require(drive.Traffic.BlocksPlayer(actor.Position-Vector2.up*8,actor.Position+Vector2.up*8,0,mode),"Swept player path tunnels through traffic");
                    // Place the obstruction on the actual curved lane, not straight ahead in world Z.
                    drive.Reset(mode);drive.Traffic.Reset(seed);
                    for(int j=0;j<drive.Traffic.Actors.Length;j++)
                    {var other=drive.Traffic.Actors[j];other.Speed=other.DesiredSpeed=0;}
                    actor.Z=0;actor.Speed=actor.DesiredSpeed=7;
                    drive.Traffic.Step(.02f,new Vector2(100,100),0,mode);
                    float stopZ=actor.Z+actor.Choice.Vehicle.HalfLength+TrafficFootprint.PlayerHalfLength(mode)+8;
                    float slope=drive.Street.RoadX(stopZ+.5f)-drive.Street.RoadX(stopZ-.5f);
                    var stop=new Vector2(drive.Street.RoadX(stopZ)-1.48f*Mathf.Sqrt(1+slope*slope),stopZ);
                    for(int i=0;i<200;i++)drive.Traffic.Step(.02f,stop,0,mode);
                    Require(actor.Speed<.1f,"Traffic fails to stop for stationary player");
                    for(int i=0;i<150;i++)drive.Traffic.Step(.02f,stop+Vector2.right*15,0,mode);
                    Require(actor.Speed>.2f,"Traffic fails to recover after obstruction clears "+mode+" seed="+seed+" actor="+actor.Position+" speed="+actor.Speed+" stop="+stop+" others="+string.Join(";",drive.Traffic.Actors.Select(a=>$"{a.Position} {a.Choice.Vehicle.Id} speed={a.Speed} desired={a.DesiredSpeed}")));
                    Debug.Log($"TRAFFIC_EXPLORATION_PASS {mode} seed {seed}");
                }
            // Steering at rest beside every model must reject rotation as well as movement.
            foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
                foreach(var def in TrafficCatalogue.All)
                    foreach(int steer in new[]{-1,1})
                    {
                        drive.Reset(mode);
                        for(int i=0;i<drive.Traffic.Actors.Length;i++)
                        {var other=drive.Traffic.Actors[i];other.Speed=other.DesiredSpeed=0;}
                        var a=drive.Traffic.Actors[0];a.Z=20;a.Choice=new TrafficChoice(def,0);
                        drive.Traffic.Step(.02f,new Vector2(100,100),0,mode);
                        float yaw=a.Heading*Mathf.Deg2Rad;
                        var beside=a.Position+new Vector2(Mathf.Cos(yaw),-Mathf.Sin(yaw))*(def.HalfWidth+TrafficFootprint.PlayerHalfWidth(mode)+.14f);
                        typeof(JaksaDrive).GetProperty(nameof(JaksaDrive.Position)).SetValue(drive,beside);
                        typeof(JaksaDrive).GetProperty(nameof(JaksaDrive.Heading)).SetValue(drive,a.Heading);
                        float initialHeading=drive.Heading;
                        for(int i=0;i<100;i++)
                        {
                            drive.Step(.02f,false,false,steer);
                            Require(!TrafficFootprint.Overlaps(drive.Position,drive.Heading,TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode),
                                a.Position,a.Heading,def.HalfWidth,def.HalfLength),"Stationary steering intersects "+mode+" / "+def.Id);
                        }
                        Require(Mathf.Abs(Mathf.DeltaAngle(initialHeading,drive.Heading))<.001f,"Stationary bicycle controller pivots the body");
                        Require(drive.Traffic.BlocksPlayer(beside,beside,initialHeading+steer*30,mode,initialHeading),"Rotation sweep misses adjacent body");
                        bool blocked=false;
                        for(int i=0;i<100;i++)
                        {
                            drive.Step(.02f,true,false,steer);blocked|=drive.TrafficContact;
                            Require(!TrafficFootprint.Overlaps(drive.Position,drive.Heading,TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode),
                                a.Position,a.Heading,def.HalfWidth,def.HalfLength),"Creeping turn intersects "+mode+" / "+def.Id);
                        }
                        Require(blocked,"Creeping turn never rejected the adjacent body");
                        Require(drive.Traffic.BlocksPlayer(a.Position+Vector2.up*8,a.Position-Vector2.up*8,180,mode),"Reverse swept path tunnels through traffic");
                    }
            Debug.Log("TRAFFIC_STATIONARY_STEERING_PASSED 24 adjacent-body cases plus reverse path sweeps");
            UnityEngine.Object.DestroyImmediate(root);
        }
        public static void Capture()
        {
            Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.8f,.83f,.89f);RenderSettings.ambientEquatorColor=new Color(.45f,.48f,.52f);RenderSettings.ambientGroundColor=new Color(.3f,.3f,.32f);
            var sun=new GameObject("Review sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.transform.rotation=Quaternion.Euler(40,-30,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.localScale=Vector3.one*3;floor.GetComponent<Renderer>().sharedMaterial=MaterialLibrary.Create("Review floor",new Color(.27f,.31f,.34f));
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.5f,.59f,.65f);camera.fieldOfView=38;camera.nearClipPlane=.05f;
            foreach(var def in TrafficCatalogue.All)
            {
                var host=new GameObject("Traffic preview");var view=new TrafficVehicleView(host.transform,"Traffic");view.Select(new TrafficChoice(def,0));
                view.Present(Vector3.zero,0,0,0,true,-1);
                camera.transform.position=def.Motorcycle?new Vector3(-3,2.1f,3.6f):new Vector3(-5,3,6);camera.transform.LookAt(new Vector3(0,def.Motorcycle?.85f:.75f,0));
                Save(camera,def.Id+"-unity.png");
                foreach(bool frontView in new[]{true,false})
                    foreach(int side in new[]{-1,1})
                    {
                        view.Present(Vector3.zero,0,0,0,true,side);
                        camera.transform.position=new Vector3(0,def.Motorcycle?1.05f:1.25f,(frontView?1:-1)*(def.Motorcycle?3.1f:5.5f));camera.transform.LookAt(new Vector3(0,.7f,0));
                        string prefix=def.Id+(frontView?"":"-rear")+(side<0?"-left":"-right");
                        var lit=Save(camera,prefix+"-on.png");
                        for(int i=0;i<4;i++)view.Present(Vector3.zero,0,0,.1f,true,side);
                        var dark=Save(camera,prefix+"-off.png");
                        CheckBlinkPixels(lit,dark,frontView?side<0:side>0,prefix);
                    }
                view.Present(Vector3.zero,0,0,0,true,0);
                var tail=Save(camera,def.Id+"-tail.png");
                view.Present(Vector3.zero,0,0,0,true,0,true);
                var brake=Save(camera,def.Id+"-brake.png");
                Require(ChangedPixels(brake,tail)>8,"Brake brightness not visible: "+def.Id);
                Debug.Log("TRAFFIC_RENDERED_BRAKE_PASS "+def.Id);
                UnityEngine.Object.DestroyImmediate(host);
            }
            foreach(var def in VehicleCatalogue.All)
                foreach(int side in new[]{-1,1})
                {
                    var host=new GameObject("Player lamp render");var view=new PlayerVehicleView(host.transform,def);
                    view.Present(def.Mode,Vector3.zero,0,0,0,true,side,true,true);
                    bool bike=def.Mode==VehicleMode.Motorcycle;
                    camera.transform.position=new Vector3(0,bike?1.05f:1.25f,bike?3.1f:5.5f);camera.transform.LookAt(new Vector3(0,.7f,0));
                    string prefix="player-"+def.Id+(side<0?"-left":"-right");
                    var lit=Save(camera,prefix+"-on.png");
                    for(int i=0;i<6;i++)view.Present(def.Mode,Vector3.zero,0,0,.1f,true,side,true,true);
                    var dark=Save(camera,prefix+"-off.png");
                    CheckBlinkPixels(lit,dark,side<0,prefix);
                    UnityEngine.Object.DestroyImmediate(host);
                }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var trainingRoot=new GameObject("Training traffic capture");var district=new District(trainingRoot.transform);
            var run=new Journey(VehicleMode.Motorcycle,new JourneySave{next=1},false,trafficSeed:601);
            for(int i=0;i<3000&&!run.Failure.HasValue;i++)
            {DrivingCoach.Step(run,.02f);district.Present(run,false,.02f);if(run.CrossTraffic.HasValue&&Mathf.Abs(run.CrossTraffic.Value.x)<6)break;}
            Save(trainingRoot.GetComponentInChildren<Camera>(),"training-traffic.png");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var explore=new JaksaDrive(new GameObject("Exploration traffic capture").transform);
            for(int i=0;i<1800&&explore.Position.y<70;i++)
            {
                float steer=ExplorationSteer(explore);
                explore.Step(.02f,true,false,steer);explore.Present(.02f);
            }
            Save(explore.Camera,"sopodel-traffic.png");
            Debug.Log("TRAFFIC_CAPTURES_READY "+Output);
        }
        public static float ExplorationSteer(JaksaDrive drive)
        {
            float z=drive.Position.y;
            float tangent=Mathf.Atan2(drive.Street.RoadX(z+2)-drive.Street.RoadX(z),2)*Mathf.Rad2Deg;
            return RoadVehicleMotion.TrackLane(drive.Mode,drive.Speed,drive.Heading,drive.Position.x,drive.Street.RoadX(z)-1.2f,tangent);
        }
        public static void CaptureFleet()
        {
            Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.8f,.84f,.9f);RenderSettings.ambientEquatorColor=new Color(.45f,.49f,.54f);RenderSettings.ambientGroundColor=new Color(.25f,.28f,.3f);
            var sun=new GameObject("Fleet daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.4f;sun.transform.rotation=Quaternion.Euler(45,-25,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.localScale=Vector3.one*4;floor.GetComponent<Renderer>().sharedMaterial=MaterialLibrary.Create("Fleet floor",new Color(.23f,.28f,.33f));
            var host=new GameObject("Six traffic options");
            for(int i=0;i<6;i++)
            {
                var view=new TrafficVehicleView(host.transform,TrafficCatalogue.All[i].Id);view.Select(new TrafficChoice(TrafficCatalogue.All[i],i%2));
                view.Present(new Vector3((i%3-1)*4.3f,0,i<3?2.8f:-2.8f),-12,0,0,true,0);
                view.Root.GetComponentInChildren<LODGroup>().ForceLOD(0);
            }
            var camera=new GameObject("Fleet camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.43f,.52f,.59f);
            camera.orthographic=true;camera.orthographicSize=6;camera.transform.position=new Vector3(7,10,15);camera.transform.LookAt(new Vector3(0,.6f,0));
            Save(camera,"traffic-fleet.png");Debug.Log("TRAFFIC_FLEET_CAPTURE_READY");
        }
        static void CheckBlinkPixels(Color32[] lit,Color32[] dark,bool screenRight,string label)
        {
            int expected=0,wrong=0;
            for(int i=0;i<lit.Length;i++)
                if(Math.Abs(lit[i].r-dark[i].r)+Math.Abs(lit[i].g-dark[i].g)+Math.Abs(lit[i].b-dark[i].b)>25)
                {if((i%1100>=550)==screenRight)expected++;else wrong++;}
            Require(expected>8&&wrong<Mathf.Max(2,expected/20),$"Rendered blink missing/wrong side {label}: correct={expected}, wrong={wrong}");
            Debug.Log($"TRAFFIC_RENDERED_BLINK_PASS {label}: {expected} lens pixels changed, {wrong} on opposite side");
        }
        static int ChangedPixels(Color32[] a,Color32[] b)
        {
            int count=0;
            for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>25)count++;
            return count;
        }
        static Color32[] Save(Camera camera,string file)
        {
            var target=new RenderTexture(1100,760,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            bool previous=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            for(int i=0;i<4;i++)camera.Render();
            RenderTexture.active=target;var pixels=new Texture2D(1100,760,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1100,760),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(Output,file),pixels.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;ShaderUtil.allowAsyncCompilation=previous;
            var colors=pixels.GetPixels32();
            UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);
            return colors;
        }
        public static void BuildMac()
        {
            Directory.CreateDirectory("Builds/TrafficPreview");
            string previous=PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone);
            try
            {
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"id.jalankita.traffic.preview");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectSetup.ScenePath},locationPathName="Builds/TrafficPreview/Jalan Kita Traffic.app",target=BuildTarget.StandaloneOSX});
                Require(report.summary.result==BuildResult.Succeeded,"macOS build "+report.summary.result);
            }
            finally{PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,previous);}
            Debug.Log("TRAFFIC_MAC_BUILD_PASSED");
        }
    }
}
