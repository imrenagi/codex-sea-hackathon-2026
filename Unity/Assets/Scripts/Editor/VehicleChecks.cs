using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UjianSim.Game;

namespace JalanKita.EditorTools
{
    public static class VehicleChecks
    {
        [MenuItem("Jalan Kita/Prepare and verify vehicles")]
        public static void Prepare()
        {
            foreach (string folder in new[] { "BeatRed", "BeatBlue" })
            {
                string root = "Assets/Resources/Vehicles/" + folder + "/";
                foreach (string path in Directory.GetFiles(root, "*.fbx"))
                {
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
                    importer.isReadable = true; importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    importer.SaveAndReimport();
                }
                foreach (string path in Directory.GetFiles(root, "*.png"))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    bool normal = path.Contains("normal");
                    importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    importer.sRGBTexture = path.Contains("base_color") || path.Contains("emission");
                    importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.maxTextureSize = 512; importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
                var atlas = Material(root + "Atlas.mat");
                atlas.SetColor("_BaseColor", Color.white);
                atlas.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "scooter_base_color.png"));
                atlas.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "scooter_metallic_smoothness.png"));
                atlas.SetFloat("_Smoothness", 1); atlas.EnableKeyword("_METALLICSPECGLOSSMAP");
                atlas.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "scooter_emission.png"));
                atlas.SetColor("_EmissionColor", Color.black); atlas.EnableKeyword("_EMISSION");
                var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "scooter_surface_normal.png");
                if (normalMap) { atlas.SetTexture("_BumpMap", normalMap); atlas.EnableKeyword("_NORMALMAP"); atlas.SetFloat("_BumpScale", .7f); }
                EditorUtility.SetDirty(atlas);
                var lens = Material(root + "Lens.mat");
                lens.SetColor("_BaseColor", new Color(.82f,.91f,.96f,.18f)); lens.SetFloat("_Smoothness", .92f);
                lens.SetFloat("_Surface", 1); lens.SetFloat("_Blend", 0); lens.SetFloat("_ZWrite", 0);
                lens.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); lens.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                lens.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); lens.renderQueue = (int)RenderQueue.Transparent;
                EditorUtility.SetDirty(lens);
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Run(); Capture();
        }

        static Material Material(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            return material;
        }

        [MenuItem("Jalan Kita/Verify vehicles")]
        public static void Run()
        {
            foreach (var definition in VehicleCatalogue.All)
            {
                var host = new GameObject("Vehicle verification " + definition.Id);
                var view = new PlayerVehicleView(host.transform, definition);
                view.Present(definition.Mode, Vector3.zero, 0, 0, 0, true, -1, true, true);
                var renderers = view.Root.GetComponentsInChildren<Renderer>();
                Require(renderers.Length > 0, definition.Id + " has no visible meshes");
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    Require(renderer.sharedMaterials.All(m => m && m.shader && m.shader.name != "Hidden/InternalErrorShader"), definition.Id + " material missing");
                }
                Debug.Log($"VEHICLE {definition.Id}: bounds {bounds.min} to {bounds.max}, size {bounds.size}");
                Require(bounds.size.y > .8f && bounds.size.y < 2.6f && bounds.size.z > 1.6f && bounds.size.z < 5.5f, definition.Id + " invalid orientation or meter scale");
                Require(bounds.min.y > -.2f && bounds.min.y < .15f, definition.Id + " wheels not grounded");
                if (definition.Mode == VehicleMode.Car)
                    Require(bounds.size.z <= 3.91f && bounds.size.x <= 1.96f, definition.Id + " exceeds shared car collision envelope");
                if (definition.Beat)
                {
                    var nodes = view.Root.GetComponentsInChildren<Transform>(true);
                    var front = nodes.First(t => t.name == "Front_Wheel_Pivot_Export");
                    var rear = nodes.First(t => t.name == "Rear_Wheel_Pivot_Export");
                    Debug.Log($"BEAT_AXLES {definition.Id}: front {front.position}, rear {rear.position}");
                    Require(front.position.z > rear.position.z + 1.2f, definition.Id + " does not face forward");
                    Require(Mathf.Abs(front.position.y - .2498f) < .02f, definition.Id + " axle height");
                    Require(view.Root.GetComponentInChildren<LODGroup>().GetLODs().Length == 2, definition.Id + " missing LODs");
                    var lods = view.Root.GetComponentInChildren<LODGroup>().GetLODs();
                    int triangles0 = lods[0].renderers.Sum(r => r.GetComponent<MeshFilter>()?.sharedMesh.triangles.Length / 3 ?? 0);
                    int triangles1 = lods[1].renderers.Sum(r => r.GetComponent<MeshFilter>()?.sharedMesh.triangles.Length / 3 ?? 0);
                    Require(triangles1 < triangles0, definition.Id + " LOD1 does not reduce geometry");
                    Debug.Log($"VEHICLE_LOD {definition.Id}: {triangles0} -> {triangles1} triangles including occupants");
                    var rest = front.localRotation;
                    view.Present(definition.Mode, Vector3.forward, 12, 4, .1f, false, 1, false, true, false, false);
                    Require(Quaternion.Angle(rest, front.localRotation) > 1, definition.Id + " wheels do not rotate");
                    Require(!view.Root.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Pillion_") && t.gameObject.activeInHierarchy), definition.Id + " passenger failed to hide");
                    Require(!view.Root.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Rider_Helmet_SNI") && t.gameObject.activeInHierarchy), definition.Id + " helmet state");
                    var head = nodes.First(t => t.name == "Headlight_Export").GetComponentInChildren<Renderer>();
                    var brake = nodes.First(t => t.name == "Brake_Light_Export").GetComponentInChildren<Renderer>();
                    var block = new MaterialPropertyBlock(); head.GetPropertyBlock(block);
                    Require(block.GetColor("_EmissionColor").maxColorComponent == 0, definition.Id + " headlight did not switch off");
                    view.Present(definition.Mode, Vector3.forward * 1.1f, 0, 1, .1f, true, -1, true, true);
                    head.GetPropertyBlock(block); Require(block.GetColor("_EmissionColor").maxColorComponent > 1, definition.Id + " headlight did not switch on");
                    brake.GetPropertyBlock(block); Require(block.GetColor("_EmissionColor").r > 2, definition.Id + " brake lamp did not respond to deceleration");
                    var lamp = nodes.Where(t => t.name.StartsWith("Indicator_Front_"))
                        .SelectMany(t => t.GetComponentsInChildren<Renderer>())
                        .First(r => view.Root.transform.InverseTransformPoint(r.transform.TransformPoint(r.GetComponent<MeshFilter>().sharedMesh.bounds.center)).x < 0);
                    lamp.GetPropertyBlock(block); Require(block.GetColor("_EmissionColor").r > 1, definition.Id + " indicator did not respond");
                }
                UnityEngine.Object.DestroyImmediate(host);
            }
            Debug.Log($"Vehicle checks passed: all {VehicleCatalogue.All.Length} selectable models, category, materials, meter scale, BeAT forward axes, wheel motion, LODs and occupant states.");
        }

        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.75f,.8f,.87f); RenderSettings.ambientEquatorColor = new Color(.5f,.55f,.6f);
            RenderSettings.ambientGroundColor = new Color(.3f,.3f,.32f); RenderSettings.fog = false;
            var sun = new GameObject("Vehicle review light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.6f; sun.transform.rotation = Quaternion.Euler(40,-30,0);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane); plane.transform.localScale = Vector3.one * 3;
            plane.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Create("Review ground", new Color(.32f,.35f,.39f));
            var camera = new GameObject("Vehicle review camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.55f,.62f,.68f);
            camera.fieldOfView = 37; camera.nearClipPlane = .05f;
            string output = Path.GetFullPath("Builds/VehiclePreview"); Directory.CreateDirectory(output);
            foreach (var definition in VehicleCatalogue.All)
            {
                var host = new GameObject("Capture " + definition.Id);
                var view = new PlayerVehicleView(host.transform, definition);
                view.Present(definition.Mode, Vector3.zero, 0, 0, 0, true, 0, true, true);
                camera.transform.position = definition.Mode == VehicleMode.Car ? new Vector3(5,3,6) : new Vector3(3,2.3f,3.6f);
                camera.transform.LookAt(new Vector3(0, definition.Mode == VehicleMode.Car ? .65f : .85f,0));
                var target = new RenderTexture(1200,900,24,RenderTextureFormat.ARGB32); camera.targetTexture = target;
                for (int i = 0; i < 5; i++) camera.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(1200,900,TextureFormat.RGB24,false); pixels.ReadPixels(new Rect(0,0,1200,900),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(output,definition.Id + ".png"),pixels.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(host);
            }
            Debug.Log("Vehicle captures ready: " + output);
        }

        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
