using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JalanKita.EditorTools
{
    public static class FidelityChecks
    {
        public static void Preview()
        {
            Prepare(); StreetLookChecks.Capture(); CaptureCars();
            Debug.Log("FIDELITY_PREVIEW_READY");
        }
        public static void Run()
        {
            Prepare();
            IntegrationChecks.RunSimulation();
            LightingChecks();
            StreetLookChecks.Run();
            CaptureCars();
            Debug.Log("FIDELITY_CHECKS_PASSED");
        }
        public static void Flows()
        {
            LightingChecks();
            ResultFlowChecks.Run();
        }
        public static void Combined()
        {
            Run();
            LandmarkChecks.Run();
            LandmarkChecks.RenderReview();
            Debug.Log("FIDELITY_COMBINED_PASSED core simulation, original locations and both landmark routes/render benchmarks");
        }
        public static void LightingChecks()
        {
            using var preferences = new IntegrationChecks.SavedPreferences();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var root = new GameObject("Daylight switching verification");
            var district = new District(root.transform);
            RequireSingleSun("local menu");
            var daytime = RenderSettings.sun.transform.rotation;
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                var route = new Journey(mode, new JourneySave(), persistent: false, trafficSeed: 1);
                district.Present(route, false, .02f);
                RequireSingleSun("full route " + mode);
                if (Quaternion.Angle(daytime, RenderSettings.sun.transform.rotation) > .1f)
                    throw new InvalidOperationException("Daylight direction differs between drivable areas");
                var local = new Journey(mode, Mission.Scenarios[1].NewSave(), persistent: false, mission: Mission.Scenarios[1]);
                district.Present(local, false, .02f);
                RequireSingleSun("local scenario " + mode);
            }
            Directory.CreateDirectory("Builds/FidelityPreview");
            var main = root.GetComponentsInChildren<Camera>().Single(c => c.CompareTag("MainCamera"));
            var full = new Journey(VehicleMode.Car, new JourneySave { next = 7 }, persistent: false, trafficSeed: 1);
            district.Snap(); district.Present(full, false, 0);
            Save(main, "Builds/FidelityPreview/full-route-daylight.png");
            foreach (int phase in new[] { 0, 2 })
            {
                var signalRun = new Journey(VehicleMode.Car, new JourneySave { next = 1 }, persistent: false, trafficSeed: 1);
                typeof(Journey).GetProperty("Position").SetValue(signalRun, new Vector2(-2, 72));
                typeof(Journey).GetField("lightTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(signalRun, phase == 0 ? 0f : 10f);
                district.Present(signalRun, false, .02f);
                var cam = district.Signals.Camera; cam.Render();
                var texture = district.Signals.Texture; var old = RenderTexture.active;
                RenderTexture.active = texture;
                var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply();
                int colored = image.GetPixels().Count(c => phase == 0 ? c.r > .6f && c.r > c.g * 1.8f : c.g > .6f && c.g > c.r * 1.8f);
                File.WriteAllBytes("Builds/FidelityPreview/daylight-signal-" + phase + ".png", image.EncodeToPNG());
                RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(image);
                if (colored < 40) throw new InvalidOperationException("Daylight washed out signal " + phase);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var exploration = new JaksaDrive(new GameObject("Exploration sunlight verification").transform);
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            { exploration.Reset(mode); exploration.Present(0); RequireSingleSun("Sopo Del " + mode); }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            new JaksaStreet(new GameObject("Retained Jaksa verification").transform, false);
            RequireSingleSun("retained Jaksa street");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var weatherRoot = new GameObject("Retained weather verification");
            var weather = new UjianSim.Game.Atmosphere(weatherRoot.transform, weatherRoot.AddComponent<Camera>());
            foreach (UjianSim.Game.Atmosphere.TimeOfDay time in Enum.GetValues(typeof(UjianSim.Game.Atmosphere.TimeOfDay)))
            foreach (bool rain in new[] { false, true })
            { weather.Apply(time, rain); RequireSingleSun(time + " rain=" + rain); }
            foreach (string id in new[] { "city_hatchback", "family_mpv", "utility_pickup" })
            {
                int Triangles(string suffix) => Resources.Load<GameObject>("StreetLook/Vehicles/" + id + "/Body" + suffix)
                    .GetComponentsInChildren<MeshFilter>().Sum(m => Enumerable.Range(0, m.sharedMesh.subMeshCount)
                        .Sum(s => (int)m.sharedMesh.GetIndexCount(s) / 3));
                int near = Triangles(""), far = Triangles("_LOD1");
                if (far >= near * .5f) throw new InvalidOperationException("Vehicle distance LOD is ineffective: " + id);
                Debug.Log("FIDELITY_LOD " + id + " near=" + near + " far=" + far);
            }
            Debug.Log("FIDELITY_LIGHTING_PASSED single sun across modes/weather; red and green signal pixels readable; distance LODs retained");
        }
        static void RequireSingleSun(string area)
        {
            var suns = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l => l.isActiveAndEnabled && l.type == LightType.Directional).ToArray();
            if (suns.Length != 1 || RenderSettings.sun != suns[0] || suns[0].intensity <= 0)
                throw new InvalidOperationException("Expected exactly one active sun: " + area + ", found " + suns.Length);
        }
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles("Assets/Resources/StreetLook/Vehicles", "*.fbx", SearchOption.AllDirectories))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation = false; importer.importLights = false; importer.importCameras = false;
                importer.isReadable = false; importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.SaveAndReimport();
            }
            foreach (string path in Directory.GetFiles("Assets/Resources/StreetLook/Surfaces", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = path.Contains("_normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = path.Contains("_albedo"); importer.alphaIsTransparency = path.Contains("marking");
                importer.mipmapEnabled = true; importer.anisoLevel = 8; importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear; importer.maxTextureSize = 1024; importer.SaveAndReimport();
            }
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/DistrictRenderer.asset");
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            var ao = renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if (!ao)
            {
                ao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>(); ao.name = "Street contact shading";
                AssetDatabase.AddObjectToAsset(ao, renderer); renderer.rendererFeatures.Add(ao);
            }
            var data = new SerializedObject(ao);
            var settings = data.FindProperty("m_Settings");
            settings.FindPropertyRelative("Downsample").boolValue = true;
            settings.FindPropertyRelative("Intensity").floatValue = .45f;
            settings.FindPropertyRelative("Radius").floatValue = .32f;
            settings.FindPropertyRelative("DirectLightingStrength").floatValue = .08f;
            settings.FindPropertyRelative("Falloff").floatValue = 65;
            settings.FindPropertyRelative("Samples").enumValueIndex = 1;
            data.ApplyModifiedPropertiesWithoutUndo(); ao.SetActive(true); ao.Create();
            EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(ao);
            // Body materials keep their established paint and lamp states.
            foreach (var definition in TrafficCatalogue.All)
                for (int i = 0; i < 2; i++)
                {
                    var m = Resources.Load<Material>("Traffic/Materials/" + definition.Id + "_" + i);
                    m.SetFloat("_Metallic", .28f); m.SetFloat("_Smoothness", .62f); EditorUtility.SetDirty(m);
                }
            AssetDatabase.SaveAssets();
            StreetLookChecks.Prepare();
            BakeReflection();
            AssetDatabase.SaveAssets();
            Debug.Log("FIDELITY_PREPARED");
        }
        static void BakeReflection()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var root = new GameObject("Own street reflection source"); new District(root.transform);
            root.transform.Find("Traffic and player").gameObject.SetActive(false);
            var go = new GameObject("Reflection capture"); var camera = go.AddComponent<Camera>();
            DrivingCamera.Configure(camera); camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.transform.position = new Vector3(0, 2.3f, 3); camera.farClipPlane = 95;
            // Capture six ordinary views after shader warmup. A one-shot cube render
            // could leave the first face uninitialized during import (negative HDR
            // values then made otherwise healthy building materials render black).
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.customReflectionTexture = null;
            camera.fieldOfView = 90; camera.aspect = 1;
            var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBHalf);
            var cube = new Cubemap(128, TextureFormat.RGBAHalf, true);
            var face = new Texture2D(128, 128, TextureFormat.RGBAHalf, false, true);
            var old = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
            var directions = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            var ups = new[] { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };
            try
            {
                rt.Create(); camera.targetTexture = rt; ShaderUtil.allowAsyncCompilation = false;
                for (int i = 0; i < 6; i++)
                {
                    camera.transform.rotation = Quaternion.LookRotation(directions[i], ups[i]);
                    for (int warmup = 0; warmup < 3; warmup++) camera.Render();
                    RenderTexture.active = rt;
                    face.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); face.Apply();
                    var pixels = face.GetPixels();
                    foreach (var pixel in pixels)
                        foreach (float channel in new[] { pixel.r, pixel.g, pixel.b })
                            if (float.IsNaN(channel) || float.IsInfinity(channel) || channel < 0 || channel > 64)
                                throw new InvalidOperationException("Invalid street reflection radiance on face " + i);
                    cube.SetPixels(pixels, (CubemapFace)i);
                }
                cube.Apply(true); cube.name = "Original Kenanga street reflection";
                const string path = "Assets/Resources/StreetLook/StreetReflection.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                if (existing) EditorUtility.CopySerialized(cube, existing); else AssetDatabase.CreateAsset(cube, path);
                Debug.Log("FIDELITY_REFLECTION_PASSED six finite nonnegative HDR faces");
            }
            finally
            {
                RenderTexture.active = old; camera.targetTexture = null; ShaderUtil.allowAsyncCompilation = async;
                UnityEngine.Object.DestroyImmediate(face); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void CaptureCars()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var root = new GameObject("Vehicle close review");
            var district = new District(root.transform);
            root.transform.Find("Traffic and player").gameObject.SetActive(false);
            var camera = root.GetComponentsInChildren<Camera>().Single(c => c.CompareTag("MainCamera"));
            var view = new TrafficVehicleView(root.transform, "Detailed vehicle review");
            Directory.CreateDirectory("Builds/FidelityPreview");
            foreach (var definition in TrafficCatalogue.All.Where(v => !v.Motorcycle))
            {
                view.Select(new TrafficChoice(definition, definition.Id == "family_mpv" ? 1 : 0));
                view.Present(new Vector3(-2.15f, .03f, 3), 0, 0, 0, false);
                foreach (bool rear in new[] { false, true })
                {
                    camera.transform.position = new Vector3(rear ? 1.4f : -5.7f, 1.7f, rear ? -2.5f : 8.5f);
                    camera.transform.LookAt(new Vector3(-2.15f, .82f, 3)); camera.fieldOfView = 44;
                    Save(camera, "Builds/FidelityPreview/" + definition.Id + (rear ? "-rear.png" : "-front.png"));
                }
            }
        }
        internal static void Save(Camera camera, string path)
        {
            var rt = new RenderTexture(1600, 1000, 24) { antiAliasing = 4 };
            var previous = RenderTexture.active; bool async = ShaderUtil.allowAsyncCompilation;
            camera.targetTexture = rt; ShaderUtil.allowAsyncCompilation = false;
            for (int i = 0; i < 5; i++) camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; ShaderUtil.allowAsyncCompilation = async;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
