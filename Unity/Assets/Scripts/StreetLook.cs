using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JalanKita
{
    // Original street treatment. Surface maps come from the project's existing CC0 library.
    // World-scale UVs keep asphalt and plaster consistent across differently sized objects.
    public sealed class StreetLook
    {
        readonly Dictionary<string, Material> materials = new();
        readonly Material sky;
        readonly Light sun;
        readonly Transform parent;
        readonly System.Random random = new(9126);
        float Range(float low, float high) => Mathf.Lerp(low, high, (float)random.NextDouble());

        public StreetLook(Transform parent)
        {
            this.parent = parent;
            sun = new GameObject("Kenanga daylight").AddComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48, -34, 0);
            sun.color = new Color(1, .975f, .93f);
            sun.intensity = 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = .035f;
            sun.shadowNormalBias = .3f;
            sky = new Material(Shader.Find("Skybox/Procedural")) { name = "Kenanga daylight sky" };
            sky.SetColor("_SkyTint", new Color(.56f, .65f, .77f));
            sky.SetColor("_GroundColor", new Color(.42f, .43f, .43f));
            sky.SetFloat("_AtmosphereThickness", .9f);
            sky.SetFloat("_Exposure", 1.1f);
            sky.SetFloat("_SunSize", .025f);
            var volume = new GameObject("Kenanga natural colour").AddComponent<Volume>();
            volume.transform.SetParent(parent, false);
            volume.isGlobal = true; volume.priority = 1;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var colour = volume.profile.Add<ColorAdjustments>();
            colour.postExposure.Override(.25f);
            colour.contrast.Override(6);
            colour.saturation.Override(-7);
            Activate();
        }

        public void Activate()
        {
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .71f, .82f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .46f, .49f);
            RenderSettings.ambientGroundColor = new Color(.25f, .26f, .24f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.70f, .76f, .82f);
            RenderSettings.fogStartDistance = 115;
            RenderSettings.fogEndDistance = 305;
            RenderSettings.reflectionIntensity = .75f;
        }

        public Material Surface(string name, string texture, string tint, float smoothness = .12f, float metal = 0)
        {
            if (materials.TryGetValue(name, out var found)) return found;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Kenanga " + name };
            ColorUtility.TryParseHtmlString("#" + tint, out var colour);
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metal);
            material.enableInstancing = true;
            if (texture != null)
            {
                string path = texture.Contains("/") ? texture : "Jaksa/" + texture;
                material.SetTexture("_BaseMap", Resources.Load<Texture2D>(path + "_albedo"));
                material.SetTexture("_BumpMap", Resources.Load<Texture2D>(path + "_normal"));
                material.SetFloat("_BumpScale", .38f);
                material.EnableKeyword("_NORMALMAP");
            }
            materials.Add(name, material);
            return material;
        }

        public Material BoxMaterial(string name)
        {
            if (name == "Tanah") return Surface("Verges", "SopoDel/grass_ground", "9b9d83");
            if (name.StartsWith("Trotoar")) return Surface("Paving", "brick_pavement", "c4c3ba");
            if (name == "Koridor Kenanga" || name == "Jalan melintang" || name == "Akses Mawar"
                || name == "Teluk berhenti" || name == "Jalan teluk jemput")
                return Surface("Asphalt", "asphalt_02", "92979b", .1f);
            if (name == "Pagar sekolah") return Surface("Boundary concrete", "plastered_wall", "b9b7a9");
            return null;
        }

        public static void ProjectSurface(MeshFilter filter, float metres = 3, int submesh = -1)
        {
            var original = filter.sharedMesh;
            if (!original || !original.isReadable) return;
            var copy = Object.Instantiate(original);
            copy.name = original.name + " / metre UV";
            var vertices = copy.vertices; var normals = copy.normals;
            var uv = copy.uv;
            if (uv.Length != vertices.Length) uv = new Vector2[vertices.Length];
            var selected = submesh >= 0 ? new HashSet<int>(copy.GetTriangles(submesh)) : null;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (selected != null && !selected.Contains(i)) continue;
                Vector3 p = filter.transform.TransformPoint(vertices[i]);
                Vector3 n = filter.transform.TransformDirection(normals[i]);
                uv[i] = Mathf.Abs(n.y) > .65f ? new Vector2(p.x, p.z) / metres
                    : Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) / metres : new Vector2(p.x, p.y) / metres;
            }
            copy.uv = uv; copy.RecalculateTangents(); filter.sharedMesh = copy;
        }

        public void DressBuilding(GameObject building, float location)
        {
            string[] tones = { "c9c7b8", "b5bcba", "c8b7a1", "acb7ad", "d6d3c8", "adaeaa" };
            int index = Mathf.Abs(Mathf.RoundToInt(location * 3)) % tones.Length;
            foreach (var renderer in building.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i] || !slots[i].name.StartsWith("M_WallPaint")) continue;
                    slots[i] = Surface("Plaster " + index, "plastered_wall", tones[index]);
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter) ProjectSurface(filter, 3.4f, i);
                }
                renderer.sharedMaterials = slots;
            }
        }

        public GameObject Tree(Vector3 position, float yaw, Transform target)
        {
            var root = new GameObject("Kenanga branching street tree");
            root.transform.SetParent(target, false);
            var mesh = new StreetMesh();
            var bark = Surface("Tree bark", "bark_brown_02", "c0b5a4");
            var leaves = Surface("Tree foliage", null, "c5cfad", .05f);
            if (!leaves.GetTexture("_BaseMap"))
            {
                leaves.SetTexture("_BaseMap", Resources.Load<Texture2D>("StreetLook/tropical_foliage") ?? Resources.Load<Texture2D>("Jaksa/leaves"));
                leaves.SetFloat("_AlphaClip", 1); leaves.SetFloat("_Cutoff", .4f);
                leaves.SetFloat("_Cull", 0); leaves.EnableKeyword("_ALPHATEST_ON");
                leaves.renderQueue = 2450;
            }
            float height = Range(6.5f, 8.4f), radius = Range(2.4f, 3.3f);
            var crown = new Vector3(.15f, height * .73f, .12f);
            mesh.Tube(bark, Vector3.zero, crown, .2f, .075f, 9);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4 + .22f;
                var branch = new Vector3(Mathf.Cos(angle) * radius * .72f, height * Range(.68f, .9f), Mathf.Sin(angle) * radius * .72f);
                mesh.Tube(bark, new Vector3(.09f, height * .43f, .06f), branch, .095f, .018f, 7);
                mesh.Tube(bark, branch * .85f, branch + new Vector3(.35f, .6f, -.25f), .035f, .008f, 5);
            }
            for (int i = 0; i < 155; i++)
            {
                float angle = Range(0, Mathf.PI * 2), r = Mathf.Sqrt(Range(0, 1)) * radius;
                var p = crown + new Vector3(Mathf.Cos(angle) * r, Range(-1, 1.35f) * (1 - .45f * r / radius), Mathf.Sin(angle) * r);
                var rot = Quaternion.Euler(Range(-75, 75), Range(0, 360), Range(-30, 30));
                float size = Range(.95f, 1.55f);
                var u = rot * Vector3.right * size * .5f; var v = rot * Vector3.up * size * .5f;
                mesh.Quad(leaves, p - u - v, p + u - v, p + u + v, p - u + v, atlas: new Rect(0, 0, 1, 1));
            }
            mesh.Build(root.transform);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return root;
        }

        public void StreetDetails()
        {
            var mesh = new StreetMesh();
            var concrete = Surface("Kerb concrete", "concrete_floor_02", "c5c6c0");
            var gutter = Surface("Gutter", "concrete_floor_02", "8f9593");
            var metal = Surface("Drain iron", null, "333a3c", .22f, .65f);
            var wall = Surface("Boundary concrete", "plastered_wall", "b9b7a9");
            var cap = Surface("Wall coping", "concrete_floor_02", "d6d4c9");
            foreach (int side in new[] { -1, 1 })
            {
                for (float z = -24; z < 407; z += 2)
                {
                    if (Junction(z, 6.5f)) continue;
                    if (side > 0 && z > RouteStage.SpurStart - 3 && z < RouteStage.SpurEnd + 3) continue;
                    if (side < 0 && (z > Mission.PickupStart - 1 && z < Mission.PickupEnd + 2
                        || z > RouteStage.PhoneStart + 1 && z < RouteStage.PhoneEnd + 3)) continue;
                    mesh.Box(concrete, new Vector3(side * 4.76f, .065f, z), new Vector3(.19f, .25f, 1.975f));
                    mesh.Box(gutter, new Vector3(side * 4.57f, .004f, z), new Vector3(.19f, .016f, 1.97f));
                    if (Mathf.RoundToInt(z) % 18 != 0) continue;
                    mesh.Box(metal, new Vector3(side * 4.56f, .02f, z), new Vector3(.21f, .025f, .65f));
                    for (int bar = 0; bar < 6; bar++)
                        mesh.Box(concrete, new Vector3(side * 4.56f, .038f, z - .26f + bar * .105f), new Vector3(.23f, .02f, .035f));
                }
            }
            // School boundary: masonry base, coping, piers and open metal railing.
            // The forecourt and crossing sight lines remain clear.
            foreach (float start in new[] { 302f, 358f })
                for (float z = start; z < start + 16; z += 2)
                {
                    mesh.Box(wall, new Vector3(-8.5f, .45f, z), new Vector3(.32f, .9f, 1.98f));
                    mesh.Box(cap, new Vector3(-8.5f, .94f, z), new Vector3(.4f, .1f, 2));
                    mesh.Box(cap, new Vector3(-8.5f, 1.05f, z - .9f), new Vector3(.42f, 2.1f, .35f));
                    for (int bar = 0; bar < 7; bar++)
                        mesh.Box(metal, new Vector3(-8.5f, 1.43f, z - .72f + bar * .24f), new Vector3(.055f, .94f, .045f));
                    mesh.Box(metal, new Vector3(-8.5f, 1.86f, z), new Vector3(.065f, .065f, 1.8f));
                }
            // A few asphalt repairs add material variation without changing collision or lesson geometry.
            var repair = Surface("Asphalt repairs", "asphalt_02", "858a8c");
            foreach (float z in new[] { 15f, 68f, 112f, 183f, 211f, 287f, 352f, 395f })
                mesh.Box(repair, new Vector3(z % 3 > 1 ? -2.5f : 2.6f, .002f, z), new Vector3(1.55f, .008f, 2.9f), repeat: 3);
            mesh.Build(parent);
            GangStreetDetails.Build(parent, this);
        }

        static bool Junction(float z, float radius) => Mathf.Abs(z - RouteStage.StopJunction) < radius
            || Mathf.Abs(z - RouteStage.LightJunction) < radius || Mathf.Abs(z - RouteStage.MainJunction) < radius;
    }
}
