using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UjianSim.Game
{
    /// <summary>
    /// URP Lit materials for every surface the Blender scripts name (M_*). Imported FBX materials are replaced by
    /// name, so colours, smoothness, glass and lamp emission are defined once here and match the Blender look-dev.
    /// Lamps get their own instance per model so each vehicle or light can switch on independently.
    /// </summary>
    public static class MaterialLibrary
    {
        struct Spec
        {
            public string Hex;
            public float Metal, Smooth, Alpha;
            public string Emit;
            public bool PerInstance;
        }

        static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
        {
            // Vehicles.
            { "M_CarPaint", S("#b9bec4", 0.6f, 0.82f, inst: true) },
            { "M_MotoPaint", S("#a3131b", 0.5f, 0.82f, inst: true) },
            { "M_AmbulanceWhite", S("#f1f2ef", 0.1f, 0.75f) },
            { "M_Glass", S("#0e1418", 0f, 0.95f, 0.55f) },
            { "M_WindowGlass", S("#22303a", 0.3f, 0.92f) },
            { "M_Visor", S("#10161c", 0f, 0.95f, 0.7f) },
            { "M_Mirror", S("#cfd6dc", 1f, 0.97f) },
            { "M_TrimBlack", S("#121314", 0f, 0.45f) },
            { "M_Chrome", S("#d8d8d8", 1f, 0.88f) },
            { "M_Rubber", S("#161616", 0f, 0.12f) },
            { "M_Alloy", S("#a6aab0", 0.95f, 0.75f) },
            { "M_BrakeDisc", S("#5d5e60", 0.9f, 0.55f) },
            { "M_Plate", S("#0d0d0d", 0f, 0.5f) },
            { "M_PlateRim", S("#e8e8e8", 0.5f, 0.7f) },
            { "M_Interior", S("#2b2a28", 0f, 0.2f) },
            { "M_SeatFabric", S("#3a3836", 0f, 0.05f) },
            { "M_SeatVinyl", S("#1b1a19", 0f, 0.5f) },
            { "M_Underbody", S("#0b0b0b", 0f, 0.1f) },
            { "M_EngineCase", S("#5b5f63", 0.7f, 0.6f) },
            { "M_Siren_Red", S("#a3141a", 0f, 0.85f, emit: "#ff2020", inst: true) },
            { "M_Siren_Blue", S("#132f9c", 0f, 0.85f, emit: "#2a5bff", inst: true) },
            { "M_StripeRed", S("#c01a20", 0f, 0.65f) },
            { "M_AngkotBand", S("#f5f5f0", 0f, 0.65f) },
            { "M_BedLiner", S("#1e1f20", 0f, 0.1f) },
            { "M_Lamp_Head", S("#e8edf2", 0.3f, 0.95f, emit: "#fff3d6", inst: true) },
            { "M_Lamp_Tail", S("#6e0a0d", 0f, 0.9f, emit: "#ff1a12", inst: true) },
            { "M_Lamp_Indicator", S("#c26d0c", 0f, 0.9f, emit: "#ff9a18", inst: true) },
            // People.
            { "M_Skin", S("#b07a55", 0f, 0.4f, inst: true) },
            { "M_Hair", S("#151210", 0f, 0.3f) },
            { "M_Jacket", S("#2f3b4c", 0f, 0.15f, inst: true) },
            { "M_Shirt", S("#c9c3b5", 0f, 0.15f, inst: true) },
            { "M_Pants", S("#262a30", 0f, 0.15f, inst: true) },
            { "M_Shoes", S("#1a1817", 0f, 0.4f) },
            { "M_HelmetShell", S("#e9e9e6", 0.1f, 0.88f) },
            { "M_HelmetLiner", S("#1a1a1a", 0f, 0.1f) },
            { "M_HelmetCatok", S("#e8c21a", 0f, 0.55f) },
            { "M_Strap", S("#121212", 0f, 0.2f) },
            { "M_Buckle", S("#7d7d7d", 0.8f, 0.7f) },
            { "M_SniLabel", S("#1c56b8", 0f, 0.6f) },
            // Street furniture and signs.
            { "M_PoleGalv", S("#8d9398", 0.8f, 0.55f) },
            { "M_PoleDark", S("#2a2d30", 0.5f, 0.5f) },
            { "M_PoleConcrete", S("#9c9a94", 0f, 0.15f) },
            { "M_SignBack", S("#9ea3a8", 0.7f, 0.5f) },
            { "M_SignWhite", S("#f4f4f2", 0f, 0.65f) },
            { "M_SignRed", S("#c4161c", 0f, 0.65f) },
            { "M_SignBlue", S("#0f4ea3", 0f, 0.65f) },
            { "M_SignYellow", S("#f2c400", 0f, 0.65f) },
            { "M_SignBlack", S("#111111", 0f, 0.6f) },
            { "M_SignalHousing", S("#1c1e20", 0f, 0.45f) },
            { "M_SignalVisor", S("#141516", 0f, 0.4f) },
            { "M_Light_Red", S("#3a0606", 0f, 0.9f, emit: "#ff2a1a", inst: true) },
            { "M_Light_Amber", S("#3a2604", 0f, 0.9f, emit: "#ffb21a", inst: true) },
            { "M_Light_Green", S("#04301a", 0f, 0.9f, emit: "#22ff88", inst: true) },
            { "M_Arrow_Red", S("#3a0606", 0f, 0.9f, emit: "#ff2a1a", inst: true) },
            { "M_Arrow_Amber", S("#3a2604", 0f, 0.9f, emit: "#ffb21a", inst: true) },
            { "M_Arrow_Green", S("#04301a", 0f, 0.9f, emit: "#22ff88", inst: true) },
            { "M_Ped_Red", S("#3a0606", 0f, 0.9f, emit: "#ff2a1a", inst: true) },
            { "M_Ped_Green", S("#04301a", 0f, 0.9f, emit: "#22ff88", inst: true) },
            { "M_Lamp_Crossing", S("#3a0606", 0f, 0.9f, emit: "#ff2a1a", inst: true) },
            { "M_Lamp_Street", S("#e9e5d8", 0f, 0.8f, emit: "#ffd9a0", inst: true) },
            { "M_Concrete", S("#a7a49c", 0f, 0.1f) },
            { "M_RoadPaintText", S("#e9e9e2", 0f, 0.35f) },
            { "M_Insulator", S("#6b4a2e", 0f, 0.7f) },
            // Buildings and nature.
            { "M_WallPaint", S("#e7dfcf", 0f, 0.1f, inst: true) },
            { "M_WallAccent", S("#c9b28a", 0f, 0.1f) },
            { "M_Shutter", S("#9aa0a4", 0.8f, 0.55f) },
            { "M_WindowFrame", S("#3b3a37", 0.4f, 0.5f) },
            { "M_RoofTile", S("#9b4a2c", 0f, 0.25f) },
            { "M_WaterTank", S("#d9661c", 0f, 0.5f) },
            { "M_ACUnit", S("#d8d8d2", 0f, 0.5f) },
            { "M_Awning", S("#2f6b8a", 0f, 0.4f, inst: true) },
            { "M_ShopSign", S("#c4161c", 0f, 0.6f, inst: true) },
            { "M_ShopSignText", S("#fff7e0", 0f, 0.6f, emit: "#fff2c8", inst: true) },
            { "M_Tiles", S("#b9b3a6", 0f, 0.4f) },
            { "M_ShopInterior", S("#4a3f33", 0f, 0.1f) },
            { "M_Bark", S("#4a3a2b", 0f, 0.05f) },
            { "M_Leaf", S("#2f5a22", 0f, 0.2f) },
            { "M_LeafLight", S("#4c7a2c", 0f, 0.2f) },
            { "M_Wood", S("#7a5233", 0f, 0.2f) },
            { "M_Tarp", S("#2c5f9e", 0f, 0.3f) },
            { "M_TrainWhite", S("#eceeee", 0.3f, 0.7f) },
            { "M_TrainOrange", S("#e0601c", 0f, 0.65f) },
            { "M_TrainBlue", S("#1f3f8c", 0f, 0.65f) },
            { "M_TrainUnder", S("#1c1c1c", 0.5f, 0.4f) },
            { "M_SteelWheel", S("#5a5a5a", 0.9f, 0.6f) },
        };

        /// <summary>
        /// Atlas materials: one Lit material whose base and metallic/smoothness maps hold a flat cell per listed
        /// surface, so a model with many plain surfaces costs one draw instead of one per surface. The Blender scripts
        /// point each face's UVs at its surface's cell, in this order (env.py RUKO_TRIM).
        /// </summary>
        static readonly Dictionary<string, string[]> Atlases = new Dictionary<string, string[]>
        {
            { "M_RukoTrim", new[] { "M_WallAccent", "M_Concrete", "M_Shutter", "M_WindowGlass", "M_WindowFrame",
                "M_RoofTile", "M_WaterTank", "M_ACUnit", "M_Tiles", "M_ShopInterior" } },
        };
        const int AtlasGrid = 4, AtlasCell = 4;

        static Spec S(string hex, float metal, float smooth, float alpha = 1f, string emit = null, bool inst = false) =>
            new Spec { Hex = hex, Metal = metal, Smooth = smooth, Alpha = alpha, Emit = emit, PerInstance = inst };

        static readonly Dictionary<string, Material> Shared = new Dictionary<string, Material>();
        static readonly Dictionary<Material, Color> EmissionBase = new Dictionary<Material, Color>();
        static Shader lit;

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        /// <summary>Strips the importer's suffixes (".001", " (Instance)") from a material name.</summary>
        public static string BaseName(string name)
        {
            int i = name.IndexOf(" (");
            if (i >= 0) name = name.Substring(0, i);
            int dot = name.IndexOf('.');
            return dot >= 0 ? name.Substring(0, dot) : name;
        }

        public static Material Create(string name, Color color, float metal = 0f, float smooth = 0.5f, float alpha = 1f,
            Color? emission = null)
        {
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(lit) { name = name };
            color.a = alpha;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Smoothness", smooth);
            if (alpha < 1f)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", Color.black);
                EmissionBase[m] = emission.Value;
            }
            return m;
        }

        static Material Make(string name, Spec s) =>
            Create(name, Hex(s.Hex), s.Metal, s.Smooth, s.Alpha, s.Emit != null ? Hex(s.Emit) : (Color?)null);

        /// <summary>Replace every renderer's materials under root with library materials. Returns per-instance ones by base name.</summary>
        public static Dictionary<string, Material> Apply(GameObject root, bool shadows = true)
        {
            var own = new Dictionary<string, Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = BaseName(mats[i].name);
                    if (!Specs.TryGetValue(key, out var spec))
                    {
                        if (Atlases.ContainsKey(key)) mats[i] = Atlas(key);
                        continue;
                    }
                    if (spec.PerInstance)
                    {
                        if (!own.TryGetValue(key, out var m)) own[key] = m = Make(key, spec);
                        mats[i] = m;
                    }
                    else
                    {
                        if (!Shared.TryGetValue(key, out var m) || m == null) Shared[key] = m = Make(key, spec);
                        mats[i] = m;
                    }
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
            return own;
        }

        /// <summary>The shared atlas material: cell i of a point-sampled grid holds surface i's colour, metallic and smoothness.</summary>
        static Material Atlas(string name)
        {
            if (Shared.TryGetValue(name, out var m) && m != null) return m;
            var cells = Atlases[name];
            int size = AtlasGrid * AtlasCell;
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, false, false) { name = name + "_Base" };
            var gloss = new Texture2D(size, size, TextureFormat.RGBA32, false, true) { name = name + "_MetallicGloss" };
            for (int i = 0; i < cells.Length; i++)
            {
                var s = Specs[cells[i]];
                int x0 = i % AtlasGrid * AtlasCell, y0 = i / AtlasGrid * AtlasCell;
                for (int y = 0; y < AtlasCell; y++)
                    for (int x = 0; x < AtlasCell; x++)
                    {
                        albedo.SetPixel(x0 + x, y0 + y, Hex(s.Hex));
                        gloss.SetPixel(x0 + x, y0 + y, new Color(s.Metal, 0f, 0f, s.Smooth));
                    }
            }
            foreach (var t in new[] { albedo, gloss })
            {
                t.filterMode = FilterMode.Point;
                t.wrapMode = TextureWrapMode.Clamp;
                t.Apply(false, true);
            }
            // URP Lit with a metallic map reads metallic from its red channel and smoothness from alpha × _Smoothness.
            m = Create(name, Color.white, 0f, 1f);
            m.SetTexture("_BaseMap", albedo);
            m.SetTexture("_MetallicGlossMap", gloss);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            Shared[name] = m;
            return m;
        }

        /// <summary>Set a lamp material's glow: 0 off, 1 normal, above 1 for brake lights or flashing.</summary>
        public static void SetGlow(Material m, float intensity)
        {
            if (m == null) return;
            if (!EmissionBase.TryGetValue(m, out var baseColor))
            {
                // A per-renderer copy (Renderer.materials): look its glow colour up by name.
                if (!Specs.TryGetValue(BaseName(m.name), out var spec) || spec.Emit == null) return;
                EmissionBase[m] = baseColor = Hex(spec.Emit);
            }
            var c = baseColor * Mathf.Max(0f, intensity) * 4f;
            m.SetColor("_EmissionColor", c);
        }
    }
}
