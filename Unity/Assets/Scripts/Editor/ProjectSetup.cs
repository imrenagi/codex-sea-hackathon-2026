using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/KampungKenanga.unity";

        public static void CreateProjectAssets()
        {
            Directory.CreateDirectory("Assets/Settings");
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            var renderer = GetOrCreate("Assets/Settings/DistrictRenderer.asset",
                ScriptableObject.CreateInstance<UniversalRendererData>);
            ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
            var pipeline = GetOrCreate("Assets/Settings/DistrictPipeline.asset",
                () => UniversalRenderPipelineAsset.Create(renderer));
            pipeline.shadowDistance = 57;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 2;
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = null;
            }
            QualitySettings.SetQualityLevel(Mathf.Min(2, QualitySettings.names.Length - 1), false);
            PlayerSettings.companyName = "Jalan Kita";
            PlayerSettings.productName = "Jalan Kita";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "id.jalankita.hackathon");
            PlayerSettings.resizableWindow = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach (string shaderName in new[] { "Universal Render Pipeline/Lit", "Skybox/Procedural", "Universal Render Pipeline/Particles/Unlit" })
            {
                var shader = Shader.Find(shaderName);
                if (!shader) throw new System.InvalidOperationException(shaderName + " is unavailable.");
                if (!Enumerable.Range(0, shaders.arraySize).Any(i => shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader))
                    shaders.GetArrayElementAtIndex(shaders.arraySize++).objectReferenceValue = shader;
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();

            foreach (var model in Directory.GetFiles("Assets/Resources/Models", "*.fbx"))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(model);
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.isReadable = true;
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(model, ImportAssetOptions.ForceUpdate);
            }

            foreach (var variant in new[] { "Emission", "Transparent", "Metallic" })
            {
                string path = "Assets/Resources/UI/Lit" + variant + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path)) continue;
                var material = UjianSim.Game.MaterialLibrary.Create("Lit" + variant, Color.white,
                    alpha: variant == "Transparent" ? .55f : 1,
                    emission: variant == "Emission" ? Color.white : (Color?)null);
                if (variant == "Emission") material.SetColor("_EmissionColor", Color.white);
                if (variant == "Metallic")
                {
                    material.SetTexture("_MetallicGlossMap", Texture2D.whiteTexture);
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                AssetDatabase.CreateAsset(material, path);
            }

            var panel = GetOrCreate("Assets/Settings/JalanPanel.asset", ScriptableObject.CreateInstance<PanelSettings>);
            var textSettings = GetOrCreate("Assets/Settings/JalanText.asset", ScriptableObject.CreateInstance<PanelTextSettings>);
            textSettings.defaultFontAsset = CreateFont();
            EditorUtility.SetDirty(textSettings);
            panel.textSettings = textSettings;
            AssetDatabase.ImportAsset("Assets/Resources/UI/Jalan.uss", ImportAssetOptions.ForceUpdate);
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/Resources/UI/JalanTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = .55f;
            EditorUtility.SetDirty(panel);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var game = new GameObject("Jalan Kita");
            var doc = game.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Resources/UI/Jalan.uxml");
            game.AddComponent<JalanGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        static FontAsset CreateFont()
        {
            const string path = "Assets/Settings/JalanFont.asset";
            var font = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (font)
            {
                font.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(font);
                return font;
            }
            font = FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            font.hideFlags = HideFlags.None;
            var glyphs = new string(Enumerable.Range(32, 95).Select(code => (char)code).ToArray()) + "·–×‹›↑↓←→✓";
            font.TryAddCharacters(glyphs, out _);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            font.material.name = "Jalan UI Text";
            font.material.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
            {
                atlas.name = "Jalan UI Glyph Atlas";
                atlas.hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
            return font;
        }

        static T GetOrCreate<T>(string path, System.Func<T> create) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing) return existing;
            var asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        [MenuItem("Jalan Kita/Build WebGL")]
        public static void BuildWeb()
        {
            var args = System.Environment.GetCommandLineArgs();
            int outputIndex = System.Array.IndexOf(args, "-buildOutput");
            string destination = "Builds/WebGL";
            if (outputIndex >= 0)
            {
                if (outputIndex + 1 >= args.Length || args[outputIndex + 1].StartsWith("-"))
                    throw new System.ArgumentException("-buildOutput requires a destination path.");
                destination = args[outputIndex + 1];
            }
            Build(BuildTarget.WebGL, destination);
            ApplyWebBranding(destination);
        }

        [MenuItem("Jalan Kita/Build macOS")]
        public static void BuildMac()
        {
#if UNITY_EDITOR_OSX
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS/RoadSense.app");
#else
            throw new System.PlatformNotSupportedException("Build RoadSense ARM64 on macOS.");
#endif
        }

        internal static void ApplyWebBranding(string destination)
        {
            string exports = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Brand/exports"));
            Directory.CreateDirectory(Path.Combine(destination, "TemplateData"));
            File.Copy(Path.Combine(exports, "favicon.ico"), Path.Combine(destination, "TemplateData/favicon.ico"), true);
            foreach (string name in new[] { "favicon.ico", "favicon-16.png", "favicon-32.png", "favicon-48.png", "apple-touch-icon.png" })
                File.Copy(Path.Combine(exports, name), Path.Combine(destination, name), true);
            string index = Path.Combine(destination, "index.html");
            string html = File.ReadAllText(index);
            if (!html.Contains("</head>")) throw new System.InvalidOperationException("WebGL template has no closing head.");
            const string links = "<link rel=\"apple-touch-icon\" href=\"apple-touch-icon.png\">\n"
                + "<link rel=\"icon\" type=\"image/png\" sizes=\"32x32\" href=\"favicon-32.png\">\n";
            File.WriteAllText(index, html.Replace("</head>", links + "</head>"));
        }

        static void Build(BuildTarget target, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Brand/RoadSenseIcon.png");
            if (!icon) throw new System.InvalidOperationException("RoadSense build icon is missing.");
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            string company = PlayerSettings.companyName, product = PlayerSettings.productName;
            try
            {
                // Keep editor/other-platform preference paths stable; macOS saves use the unchanged bundle ID.
                PlayerSettings.companyName = "We Will Handle";
                PlayerSettings.productName = "RoadSense";
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = destination,
                    target = target,
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.InvalidOperationException($"Build {target}: {report.summary.result}");
                Debug.Log($"RoadSense build ready: {Path.GetFullPath(destination)}");
            }
            finally
            {
                PlayerSettings.companyName = company;
                PlayerSettings.productName = product;
            }
        }
    }
}
