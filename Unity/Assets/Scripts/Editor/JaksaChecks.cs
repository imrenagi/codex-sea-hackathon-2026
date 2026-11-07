using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JalanKita.EditorTools
{
    public static class JaksaChecks
    {
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Verify(new JaksaDrive(new GameObject("Exploration verification").transform));
        }
        [MenuItem("Jalan Kita/Build Sea office preview macOS")]
        public static void BuildMac()
        {
            const string destination="Builds/SeaOffice/Jalan Kita.app";
            Directory.CreateDirectory("Builds/SeaOffice");
            string previousId=PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone);
            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                // Let the dedicated preview coexist with the main training app during local review.
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"id.jalankita.sopodel.preview");
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes=new[]{ProjectSetup.ScenePath},locationPathName=destination,target=BuildTarget.StandaloneOSX
                });
            }
            finally { PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,previousId); }
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Jaksa preview build: "+report.summary.result);
            File.Copy("Assets/Resources/SopoDel/NOTICE.txt","Builds/SeaOffice/ASSET_NOTICES.txt",true);
            Debug.Log("Sea office preview build ready: "+Path.GetFullPath(destination));
        }

        [MenuItem("Jalan Kita/Prepare and verify Sea office")]
        public static void Prepare()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            foreach(string file in Directory.GetFiles("Assets/Resources/Jaksa").Concat(Directory.GetFiles("Assets/Resources/SopoDel")).Where(f=>f.EndsWith(".jpg")||f.EndsWith(".png")))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType=file.Contains("_normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.wrapMode=file.Contains("signs")||file.Contains("leaves")||file.Contains("sea-logo")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
                importer.maxTextureSize=file.Contains("signs")?2048:1024;
                importer.mipmapEnabled=true;importer.anisoLevel=4;
                importer.alphaIsTransparency=file.Contains("leaves");importer.SaveAndReimport();
            }
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/DistrictPipeline.asset");
            pipeline.supportsHDR=true;pipeline.shadowDistance=85;pipeline.msaaSampleCount=4;
            pipeline.mainLightShadowmapResolution=4096;
            EditorUtility.SetDirty(pipeline);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var drive=new JaksaDrive(new GameObject("Jaksa verification").transform);
            Directory.CreateDirectory("Assets/Resources/Jaksa/Materials");
            Directory.CreateDirectory("Assets/Resources/SopoDel/Materials");AssetDatabase.Refresh();
            const string skyPath="Assets/Resources/Jaksa/Materials/Jaksa sky.mat";
            var oldSky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(oldSky)EditorUtility.CopySerialized(RenderSettings.skybox,oldSky);
            else AssetDatabase.CreateAsset(new Material(RenderSettings.skybox),skyPath);
            foreach(var m in drive.Street.Root.GetComponentsInChildren<MeshRenderer>().Select(r=>r.sharedMaterial).Distinct())
            {
                // Preserve runtime material shader variants (normal maps and alpha clipping) in player builds.
                if(!m || !new[]{"Asphalt","Concrete","Sidewalk paving","Terracotta roofs","Painted steel","Faded white paint",
                    "Tactile paving","Recess shadow","Smoked glazing","Bark","Leaf canopy","Original shop signs",
                    "Sea office cladding","Sea office glass","Sea official logo"}.Contains(m.name)&&!m.name.StartsWith("Plaster ")&&!m.name.StartsWith("Sopo "))continue;
                string path="Assets/Resources/"+(m.name.StartsWith("Sopo ")?"SopoDel":"Jaksa")+"/Materials/"+m.name+".mat";
                var old=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(old)EditorUtility.CopySerialized(m,old);else AssetDatabase.CreateAsset(new Material(m),path);
            }
            AssetDatabase.SaveAssets();
            Capture(drive);Verify(drive);
            Debug.Log("Jaksa verification and captures complete.");
        }

        static void Verify(JaksaDrive drive)
        {
            if(drive.Street.Data.buildings.Length<20)throw new Exception("Building footprint data missing");
            var dressing=drive.Street.Root.GetComponent<SopoDelDressingStats>();
            if(!dressing||dressing.InfillBuildings<1||dressing.GrassClumps<1)throw new Exception("Sopo Del land dressing missing");
            foreach(var lot in dressing.InfillLots)
                for(float z=8;z<=246;z+=2)
                    if(lot.Overlaps(new Rect(drive.Street.RoadX(z)-4,z-1,8,2)))
                        throw new Exception("Authored building intrudes into the road at "+z);
            var savedCar=VehicleCatalogue.Selected(VehicleMode.Car);
            var savedMotor=VehicleCatalogue.Selected(VehicleMode.Motorcycle);
            try
            {
            foreach(var choice in VehicleCatalogue.All)
            {
                VehicleCatalogue.Select(choice);var mode=choice.Mode;
                drive.Reset(mode);
                if(!drive.Street.Root.GetComponentsInChildren<Transform>().Any(t=>t.name==choice.Label))
                    throw new Exception("Sea office vehicle presentation missing: "+choice.Id);
                for(int i=0;i<4000&&!drive.AtEnd;i++)
                {
                    float steer=TrafficChecks.ExplorationSteer(drive);
                    drive.Step(.02f,true,false,steer);
                }
                if(!drive.AtEnd||drive.Speed!=0)throw new Exception("Jaksa end-of-street stop failed: "+mode);
                drive.Reset(mode);
                for(int i=0;i<900;i++)drive.Step(.02f,true,false,-1);
                float offset=Mathf.Abs(drive.Position.x-drive.Street.RoadX(drive.Position.y));
                if(offset>JaksaStreet.RoadHalfWidth-.4f||!drive.CurbContact)throw new Exception("Jaksa curb containment failed: "+mode);
                drive.Reset(mode);
                for(int i=0;i<150;i++)drive.Step(.02f,true,false,0);
                for(int i=0;i<150;i++)drive.Step(.02f,false,true,0);
                if(drive.Speed>.001f)throw new Exception("Jaksa braking failed");
                drive.ToggleCamera();drive.Present(.02f);
                if(!drive.Hood)throw new Exception("Camera toggle failed: "+choice.Id);
                drive.Reset(mode);
                if(drive.Hood||drive.AtEnd||drive.Speed!=0)throw new Exception("Reset failed: "+choice.Id);
                Debug.Log("SOPO_DEL_VEHICLE_PASS "+choice.Id);
            }
            }
            finally { VehicleCatalogue.Select(savedCar);VehicleCatalogue.Select(savedMotor); }
            Debug.Log($"Sea office checks passed: all {VehicleCatalogue.All.Length} vehicles, curb containment, braking, end stop, camera and reset.");
            JourneyChecks.Run();
        }

        static void Capture(JaksaDrive drive)
        {
            string output=Path.GetFullPath("Builds/SeaOfficePreview");Directory.CreateDirectory(output);
            var camera=drive.Camera;
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32){antiAliasing=4};
            camera.targetTexture=rt;
            bool asyncCompilation=ShaderUtil.allowAsyncCompilation;
            foreach(float z in new[]{20f,80f,95f,175f,228f,-1f,-2f,-3f})
            {
                drive.Reset(VehicleMode.Car);
                drive.Hood=true;drive.Present(0);
                Vector3 eye=new(drive.Street.RoadX(z)-.7f,1.65f,z);
                camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(new Vector3((drive.Street.RoadX(z+5)-drive.Street.RoadX(z))/5,-.015f,1)));
                string filename="sopodel-"+(int)z+"m.png";
                if(z==-1)
                {
                    // Review the whole authored landmark, including both signs and its open entrance.
                    eye=new Vector3(-45,48,345);camera.farClipPlane=650;
                    camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(new Vector3(99,75,160)-eye));
                    filename="sopodel-overview.png";
                }
                else if(z==-2)
                {
                    drive.Reset(VehicleMode.Car);drive.Present(0);
                    filename="sopodel-car-chase.png";
                }
                else if(z==-3)
                {
                    drive.Reset(VehicleMode.Car);drive.ToggleCamera();drive.Present(0);
                    filename="sopodel-car-driver.png";
                }
                // The editor compiles material variants lazily; warm them before reading the target.
                ShaderUtil.allowAsyncCompilation=false;
                for(int warmup=0;warmup<4;warmup++)camera.Render();
                var old=RenderTexture.active;RenderTexture.active=rt;
                var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();RenderTexture.active=old;
                File.WriteAllBytes(Path.Combine(output,filename),texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
            ShaderUtil.allowAsyncCompilation=asyncCompilation;
            Debug.Log("Jaksa previews: "+output);
        }
    }
}
