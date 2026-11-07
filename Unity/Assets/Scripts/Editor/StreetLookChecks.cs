using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JalanKita.EditorTools
{
    public static class StreetLookChecks
    {
        [MenuItem("RoadSense/Verify grounded street and capture")]
        public static void Run()
        {
            Prepare();
            MotionChecks();
            MissionChecks.Run();
            VehicleChecks.Run();
            Capture();
            Debug.Log("GROUNDED_STREET_CHECKS_PASSED");
        }

        public static void Prepare()
        {
            var texture = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/StreetLook/tropical_foliage.png");
            if (texture)
            {
                texture.textureType = TextureImporterType.Default;
                texture.alphaIsTransparency = true; texture.mipmapEnabled = true;
                texture.wrapMode = TextureWrapMode.Clamp; texture.maxTextureSize = 1024;
                texture.anisoLevel = 4; texture.SaveAndReimport();
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/DistrictPipeline.asset");
            pipeline.supportsHDR = true;
            var settings = new SerializedObject(pipeline);
            settings.FindProperty("m_SoftShadowsSupported").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.shadowCascadeCount = 2; pipeline.cascade2Split = .3f;
            pipeline.shadowDistance = 95; pipeline.msaaSampleCount = 4;
            pipeline.mainLightShadowmapResolution = 4096;
            EditorUtility.SetDirty(pipeline);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var host = new GameObject("Street material preparation");
            new District(host.transform);
            Directory.CreateDirectory("Assets/Resources/StreetLook/Materials"); AssetDatabase.Refresh();
            // Keep runtime post-processing variants available in player builds.
            const string profilePath = "Assets/Resources/StreetLook/StreetColour.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            foreach (var component in profile.components) UnityEngine.Object.DestroyImmediate(component, true);
            profile.components.Clear();
            foreach (var component in host.GetComponentInChildren<Volume>().profile.components)
            {
                var copy = UnityEngine.Object.Instantiate(component);
                AssetDatabase.AddObjectToAsset(copy, profile); profile.components.Add(copy);
            }
            EditorUtility.SetDirty(profile);
            foreach (var material in host.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
                .Where(m => m && m.name.StartsWith("Kenanga ")).Append(RenderSettings.skybox).Distinct())
            {
                string path = "Assets/Resources/StreetLook/Materials/" + material.name + ".mat";
                var old = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (old) EditorUtility.CopySerialized(material, old);
                else AssetDatabase.CreateAsset(new Material(material), path);
            }
            AssetDatabase.SaveAssets();
        }

        public static void MotionChecks()
        {
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                var motion = new RoadVehicleMotion(); float speed = 0, yaw = 17;
                for (int i = 0; i < 60; i++) motion.Step(.02f, mode, false, true, 1, 17, ref speed, ref yaw);
                Require(Mathf.Abs(yaw - 17) < .001f, "A stationary vehicle must not pivot");
                motion.Reset(); speed = 10; yaw = 0;
                for (int i = 0; i < 30; i++) motion.Step(.02f, mode, false, false, .3f, 17, ref speed, ref yaw);
                Require(yaw > 3, "Moving steering must change heading");
                for (int i = 0; i < 50; i++) motion.Step(.02f, mode, false, false, 0, 17, ref speed, ref yaw);
                float held = yaw;
                for (int i = 0; i < 20; i++) motion.Step(.02f, mode, false, false, 0, 17, ref speed, ref yaw);
                Require(Mathf.Abs(yaw - held) < .001f, "Centred wheels must retain the new heading");
                motion.Reset(); speed = 13.89f; yaw = 0; float distance = 0;
                for (int i = 0; i < 200; i++) { motion.Step(.02f, mode, false, true, 0, 17, ref speed, ref yaw); distance += speed * .02f; }
                Require(speed == 0 && distance > 8 && distance < 15, "Emergency braking distance at 50 km/h");
                Debug.Log("ROAD_MOTION_PASS " + mode + " stopping distance " + distance);
            }
        }

        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Grounded street review"); var district = new District(root.transform);
            var camera = root.GetComponentInChildren<Camera>();
            string output = Path.GetFullPath("Builds/GroundedPreview"); Directory.CreateDirectory(output);
            var car = VehicleCatalogue.Selected(VehicleMode.Car); var motor = VehicleCatalogue.Selected(VehicleMode.Motorcycle);
            try
            {
                VehicleCatalogue.Select(VehicleCatalogue.All.First(v => v.Id == "mpv"));
                VehicleCatalogue.Select(VehicleCatalogue.All.First(v => v.Id == "beat-blue"));
                for (int i = 0; i < Mission.Scenarios.Length; i++)
                {
                    var mission = Mission.Scenarios[i];
                    var run = new Journey(i == 2 ? VehicleMode.Motorcycle : VehicleMode.Car, mission.NewSave(), false, mission);
                    run.DriverGear = run.PassengerGear = run.Headlight = true;
                    if (run.Stage == 0) run.Depart();
                    if (i == 0)
                    {
                        for (int step = 0; step < 2000 && run.Position.y < -4; step++) DrivingCoach.Step(run, .02f);
                        district.Snap(); district.Present(run, false, 0);
                        Save(camera, Path.Combine(output, "village-frontages.png"));
                    }
                    float z = new[] { 15f, 310f, 251f }[i];
                    for (int step = 0; step < 6000 && run.Position.y < z && !run.Failure.HasValue; step++) DrivingCoach.Step(run, .02f);
                    Require(!run.Failure.HasValue, "Preview route " + mission.Id + ": " + run.Failure);
                    district.Snap(); district.Present(run, false, 0);
                    Save(camera, Path.Combine(output, mission.Id + "-chase.png"));
                    if (i == 1) { district.Snap(); district.Present(run, true, 0); Save(camera, Path.Combine(output, "school-driver.png")); }
                }
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var exploration = new JaksaDrive(new GameObject("Exploration review").transform);
                exploration.Reset(VehicleMode.Car); exploration.Present(0);
                Save(exploration.Camera, Path.Combine(output, "sopodel-chase.png"));
            }
            finally { VehicleCatalogue.Select(car); VehicleCatalogue.Select(motor); }
            Debug.Log("GROUNDED_PREVIEWS " + output);
        }

        static void Save(Camera camera, string path)
        {
            var rt = new RenderTexture(1440, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var old = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
            camera.targetTexture = rt; ShaderUtil.allowAsyncCompilation = false;
            for (int i = 0; i < 4; i++) camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1440, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old; ShaderUtil.allowAsyncCompilation = async;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
        }

        public static void BuildMac()
        {
            string previous = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone);
            try
            {
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "id.jalankita.grounded.preview");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ProjectSetup.ScenePath },
                    locationPathName = "Builds/Grounded/Jalan Kita.app", target = BuildTarget.StandaloneOSX });
                Require(report.summary.result == BuildResult.Succeeded, "Grounded preview build failed");
            }
            finally { PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, previous); }
            Debug.Log("GROUNDED_BUILD_PASSED");
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
