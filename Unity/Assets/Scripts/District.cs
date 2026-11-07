using System.Collections.Generic;
using UnityEngine;
using static JalanKita.RouteStage;
using UjianSim.Game;

namespace JalanKita
{
    public sealed partial class District
    {
        static readonly int[] Side = { -1, 1 };
        static readonly float[] Junctions = { StopJunction, LightJunction, MainJunction };
        static readonly string[] Shops = { "Ruko_A", "Ruko_B", "Ruko_C", "Ruko_D" };
        // Ujian SIM TownLayout.Buildings wall palette, unchanged.
        static readonly string[] WallColors = { "efe2c2", "f3b27a", "ececec", "bfe0de", "f4c9bf",
            "d9c8e6", "ffd98a", "c9d8b6", "f7efe1", "e8cfa8" };
        readonly Dictionary<string, Material> palette = new();
        readonly Dictionary<string, Material> surfaces = new();
        readonly GameObject scenery;
        readonly Transform actors;
        readonly GameObject car, motorcycle, crossCar, laneCar, spurCar, pedestrian;
        readonly Renderer[] signal = new Renderer[3];
        readonly Renderer[] indicators = new Renderer[2];
        readonly Color[] signalColors = { new(.96f, .13f, .15f), new(1, .69f, .18f), new(.12f, 1, .53f) };
        readonly MaterialPropertyBlock paint = new();
        readonly Camera camera;
        readonly Light headlamp;
        bool snapCamera = true;

        public District(Transform parent)
        {
            scenery = new GameObject("Kampung Kenanga");
            scenery.transform.SetParent(parent);
            actors = new GameObject("Traffic and player").transform;
            actors.SetParent(parent);
            BuildRoad();
            BuildBuildings();
            BuildFlowerMarket();
            BuildSigns();
            BuildStreetProps();
            BuildBoulevard();
            StaticBatchingUtility.Combine(scenery);

            car = Model("Car_City", Vector3.zero, 0, actors);
            motorcycle = Model("Motorcycle", Vector3.zero, 0, actors);
            crossCar = TrafficCar("Lalu lintas persimpangan", "698db4");
            laneCar = TrafficCar("Mobil di lajur kanan", "d57e67");
            spurCar = TrafficCar("Lalu lintas satu arah", "74aa8c");
            pedestrian = Model("Person", Vector3.zero, 0, actors);
            var markerParent = new GameObject("Sein").transform;
            markerParent.SetParent(actors);
            for (int i = 0; i < 2; i++)
                indicators[i] = Box("Lampu sein", new Vector3(0, .67f, 0), new Vector3(.22f, .13f, .095f),
                    "ef6b23", markerParent, true);
            headlamp = new GameObject("Lampu utama motor").AddComponent<Light>();
            headlamp.transform.SetParent(actors, false);
            headlamp.type = LightType.Spot;
            headlamp.spotAngle = 56;
            headlamp.color = new Color(1, .93f, .73f);
            headlamp.intensity = 4;
            headlamp.range = 11;

            camera = new GameObject("Kamera pengemudi").AddComponent<Camera>();
            camera.transform.SetParent(parent);
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(.65f, .81f, .79f);
            camera.fieldOfView = 55;
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 500;
            _ = new Atmosphere(parent, camera);
        }

        void BuildRoad()
        {
            SurfaceBox("Tanah", new Vector3(0, -.3f, 184), new Vector3(245, .45f, 475), "Grass");
            Road("Koridor Kenanga", 0, -28, 415, 9.5f);
            Road("Akses Mawar", 6.85f, SpurStart, SpurEnd + 1, 4.35f);
            Road("Teluk berhenti", -6.15f, PhoneStart + 4, PhoneEnd, 3.4f);
            foreach (float center in Junctions)
            {
                SurfaceBox("Jalan melintang", new Vector3(0, -.045f, center), new Vector3(90, .08f, 9.1f), "Asphalt");
                for (int x = -42; x <= 42; x += 4)
                {
                    if (Mathf.Abs(x) < 7) continue;
                    Mark("Marka jalan utama", x, center, 1.8f, .1f);
                }
            }

            for (int z = -25; z < 390; z += 4)
            {
                if (NearJunction(z, 5.3f) || Mathf.Abs(z - Zebra) < 3.5f) continue;
                if (z >= OneWayStart && z <= Merge) Mark("Marka pindah lajur", 0, z, .14f, 1.95f);
                else Mark("Marka tengah", 0, z, .14f, 2.05f, "e9e9e2");
            }
            foreach (int side in Side)
            {
                for (int z = -24; z < 390; z += 4)
                {
                    if (NearJunction(z, 6.1f)) continue;
                    bool opening = side == 1 ? z > SpurStart - 3 && z < SpurEnd + 3
                        : z > PhoneStart + 1 && z < PhoneEnd + 3;
                    if (!opening)
                    {
                        Mark("Garis tepi", side * 4.39f, z, .12f, 4.02f);
                        SurfaceBox("Trotoar", new Vector3(side * 6.1f, .035f, z), new Vector3(2.7f, .18f, 4.02f), "Paving");
                        for (int k = 0; k < 4; k++)
                            Box("Kanstin hitam putih", new Vector3(side * 4.78f, .06f, z - 1.5f + k),
                                new Vector3(.22f, .22f, 1), k % 2 == 0 ? "eeeeea" : "25272b");
                        if (z % 12 == 0)
                            Box("Drainase", new Vector3(side * 4.56f, .016f, z), new Vector3(.3f, .025f, .65f), "2b2d2f");
                    }
                    else if (side == 1)
                        Mark("Pemisah cabang", 5.02f, z, .13f, 2.25f, "e9e9e2");
                    else
                        Mark("Batas teluk", -4.45f, z, .12f, 1.9f);
                }
            }

            Mark("Garis henti STOP", -2.35f, StopLine, 4.45f, .32f);
            Mark("Garis henti APILL", -2.35f, LightLine, 4.45f, .32f);
            for (int i = -5; i <= 5; i++) Mark("Zebra cross", i * .8f, Zebra, .43f, 3.35f);
            for (int i = 0; i < 6; i++) Mark("Beri jalan", -4.16f + i * .65f, YieldLine, .36f, .28f);
            for (int z = 225; z < Merge; z += 15)
            {
                Arrow(LeftLane, z, 1);
                Arrow(RightLane, z + 4, 1);
            }
            Arrow(7.4f, 253, -1);
            Arrow(7.4f, 262, -1);
            for (float z = PhoneStart + 5; z < PhoneEnd - 1; z += 4)
                Mark("Teluk berhenti", -7.1f, z, .12f, 4);

            for (int i = 1; i < 5; i++)
            {
                float z = All[i].End;
                string color = "2fae64";
                for (int stripe = 0; stripe < 8; stripe++)
                    Mark("Garis checkpoint", -4.32f + stripe * .55f, z, .27f, .22f, color);
                foreach (float x in new[] { -5.72f, 5.72f })
                {
                    Box("Tiang checkpoint", new Vector3(x, .76f, z), new Vector3(.25f, 1.53f, .25f), "15181d");
                    Box("Puncak checkpoint", new Vector3(x, 1.46f, z), new Vector3(.29f, .21f, .29f), color);
                }
            }
        }

        void BuildBuildings()
        {
            var rng = new System.Random(11);
            foreach (int side in Side)
            {
                float rotation = side < 0 ? -90 : 90;
                for (int z = -20; z < 388; z += 7)
                {
                    if (NearJunction(z, 10) || (side < 0 && z > 320 && z < 375)) continue;
                    // Reserve the market frontage beside the pedestrian lesson.
                    if (side < 0 && z > Zebra + 2 && z < Zebra + 23) continue;
                    float edge = side == 1 && z > SpurStart - 4 && z < SpurEnd + 5 ? 12 : 7.6f;
                    string shop = Shops[rng.Next(Shops.Length)];
                    GameObject building = Model(shop, new Vector3(side * edge, .06f, z), rotation);
                    building.transform.localScale = new Vector3(6.85f / 4.5f, 1, .85f);
                    if ((z + 20) % 35 == 0)
                        Model("StreetLamp", new Vector3(side * 5.1f, .12f, z + 2.4f), rotation);
                }
            }
            Model("Ruko_D", new Vector3(-10.2f, .04f, 341), -90);
            Model("Ruko_D", new Vector3(-10.2f, .04f, 353), -90);
            Model("RoadText_ZonaSekolah", new Vector3(LeftLane, .03f, SchoolLimit + 4));
            Model("RoadText_Stop", new Vector3(LeftLane, .03f, StopLine - 5));
            Model("Warung", new Vector3(9, .12f, 151), 90);
            for (int z = 324; z <= 370; z += 4)
            {
                if (z >= 334 && z <= 358) continue;
                Box("Pagar sekolah", new Vector3(-8.6f, .65f, z), new Vector3(.19f, 1.3f, 3.8f), "f1c36e");
            }
            foreach (float junction in Junctions)
            {
                for (int i = 0; i < 5; i++)
                {
                    foreach (int side in Side)
                        Model(i % 3 == 0 ? "Tree_Shade_0" : Shops[(i + 1) % 3],
                            new Vector3(side * (14 + i * 7), 0, junction + 8.25f), 0);
                }
            }
            for (int i = 0; i < 40; i++)
            {
                float z = rng.Next(-24, 385);
                float x = (i % 2 == 0 ? -1 : 1) * rng.Next(16, 41);
                if (!NearJunction(z, 5)) Model("Tree_Shade_0", new Vector3(x, 0, z), i * 33);
            }
        }

        void BuildFlowerMarket()
        {
            // Keep the sidewalk and crossing clear; batch decorative geometry by material.
            var batches = new Dictionary<string, MeshBuilder>();
            void Detail(string color, Vector3 center, Vector3 size)
            {
                if (!batches.TryGetValue(color, out var mesh))
                    batches.Add(color, mesh = new MeshBuilder());
                mesh.Box(center - size * .5f, center + size * .5f);
            }

            SurfaceBox("Pelataran Pasar Bunga", new Vector3(-9.1f, .025f, Zebra + 12),
                new Vector3(3.3f, .16f, 14), "Paving");
            string[] petals = { "edbc59", "cf6d79", "f7efe1" };
            for (int stall = 0; stall < 2; stall++)
            {
                float z = Zebra + 7 + stall * 7;
                string fabric = stall == 0 ? "407e78" : "bb6849";
                for (int stripe = 0; stripe < 8; stripe++)
                {
                    string color = stripe % 2 == 0 ? fabric : "f7efe1";
                    float az = z - 1.75f + stripe * .5f;
                    Detail(color, new Vector3(-9.1f, 2.55f, az), new Vector3(2.9f, .12f, .5f));
                    Detail(color, new Vector3(-7.65f, 2.4f, az), new Vector3(.1f, .3f, .5f));
                }
                foreach (float x in new[] { -10.4f, -7.8f })
                    foreach (float offset in new[] { -1.8f, 1.8f })
                        Detail("6e503c", new Vector3(x, 1.3f, z + offset), new Vector3(.09f, 2.4f, .09f));

                Detail("9b7551", new Vector3(-8.3f, .87f, z), new Vector3(1.1f, .2f, 3.4f));
                Detail("6e503c", new Vector3(-8.3f, .5f, z), new Vector3(.8f, .65f, 3.1f));
                for (int pot = 0; pot < 6; pot++)
                {
                    float pz = z - 1.35f + pot * .54f;
                    float height = pot % 2 == 0 ? 1.45f : 1.62f;
                    Detail("bb6849", new Vector3(-8.1f, 1.1f, pz), new Vector3(.3f, .28f, .3f));
                    Detail("507357", new Vector3(-8.1f, (1.18f + height) * .5f, pz),
                        new Vector3(.035f, height - 1.18f, .035f));
                    Detail("507357", new Vector3(-8.1f, 1.32f, pz), new Vector3(.32f, .08f, .16f));
                    string flower = petals[(pot + stall) % petals.Length];
                    Detail(flower, new Vector3(-8.1f, height, pz), new Vector3(.34f, .1f, .14f));
                    Detail(flower, new Vector3(-8.1f, height, pz), new Vector3(.14f, .1f, .34f));
                    Detail("edbc59", new Vector3(-8.1f, height + .055f, pz), new Vector3(.1f, .03f, .1f));
                }

                Detail("9b7551", new Vector3(-9.8f, .3f, z), new Vector3(.7f, .35f, 2.3f));
                Detail("507357", new Vector3(-9.8f, .6f, z), new Vector3(.6f, .4f, 2.1f));
            }
            foreach (var batch in batches)
                batch.Value.Build("Pasar Bunga · " + batch.Key, scenery.transform, Material(batch.Key));
        }

        void BuildSigns()
        {
            Model("Sign_Stop", new Vector3(-5.45f, .12f, StopLine - .25f));
            Model("Sign_Crossing", new Vector3(-5.45f, .12f, Zebra - 12));
            Model("Sign_School", new Vector3(-5.45f, .12f, SchoolLimit - 14));
            Model("Sign_OneWay", new Vector3(-5.45f, .12f, OneWayStart));
            Model("Sign_NoEntry", new Vector3(7.45f, .12f, SpurStart + .8f));
            Model("Sign_Speed30", new Vector3(-5.45f, .12f, SchoolLimit));
            Model("Sign_Speed30", new Vector3(5.45f, .12f, SchoolLimit));
            var light = Model("TrafficLight", new Vector3(-5.35f, .12f, LightLine + .35f), 0, actors);
            string[] parts = { "Light_Red", "Light_Amber", "Light_Green" };
            for (int i = 0; i < signal.Length; i++)
            {
                signal[i] = Models.Find(light.transform, parts[i]).GetComponent<Renderer>();
                Models.OwnMaterials(signal[i]);
            }
        }

        public void Snap() => snapCamera = true;

        public void Present(Journey run, bool hood, float dt, bool driving = true)
        {
            PresentBoulevard(run, driving);
            bool onMotorcycle = run.Mode == VehicleMode.Motorcycle;
            var player = onMotorcycle ? motorcycle : car;
            car.SetActive(!onMotorcycle && !hood);
            motorcycle.SetActive(onMotorcycle && !hood);
            var point = World(run.Position);
            var rotation = Quaternion.Euler(0, run.Heading, 0);
            player.transform.SetPositionAndRotation(point, ModelRotation(run.Heading + 180));
            headlamp.transform.SetPositionAndRotation(point + rotation * new Vector3(0, .87f, .85f), rotation);
            headlamp.enabled = onMotorcycle && run.Headlight;
            Pose(crossCar, run.CrossTraffic, run.CrossDirection * 90);
            Pose(pedestrian, run.Pedestrian, run.PedestrianDirection * 90);
            Pose(laneCar, run.LaneTraffic, 0);
            Pose(spurCar, run.SpurTraffic, 180);
            for (int i = 0; i < signal.Length; i++)
                Tint(signal[i], i == run.LightPhase ? signalColors[i] : signalColors[i] * .24f, i == run.LightPhase);
            for (int i = 0; i < indicators.Length; i++)
            {
                var light = indicators[i];
                light.transform.SetPositionAndRotation(point + rotation * new Vector3((i == 0 ? -1 : 1) *
                    (onMotorcycle ? .35f : .73f), onMotorcycle ? .75f : .69f, onMotorcycle ? -.92f : -2.03f), rotation);
                light.enabled = !hood && run.Indicator == (i == 0 ? -1 : 1) && Time.unscaledTime % .7f < .42f;
                if (light.enabled) Tint(light, signalColors[1], true);
            }

            Vector3 eye = hood ? point + rotation * new Vector3(0, onMotorcycle ? 1.39f : 1.28f, .62f)
                : point + rotation * new Vector3(0, 4.25f, -7.85f);
            Vector3 look = point + rotation * new Vector3(0, hood ? 1.33f : 1.12f, hood ? 24 : 12);
            var view = Quaternion.LookRotation(look - eye);
            float blend = snapCamera ? 1 : 1 - Mathf.Exp(-6.5f * dt);
            camera.transform.SetPositionAndRotation(Vector3.Lerp(camera.transform.position, eye, blend),
                Quaternion.Slerp(camera.transform.rotation, view, blend));
            snapCamera = false;
        }

        GameObject TrafficCar(string name, string color)
        {
            var obj = Model("Car_City", Vector3.zero, 0, actors);
            obj.name = name;
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.sharedMaterials)
                    if (material && MaterialLibrary.BaseName(material.name) == "M_CarPaint")
                        material.SetColor("_BaseColor", ColorOf(color));
            }
            return obj;
        }

        void Tint(Renderer renderer, Color color, bool lit)
        {
            paint.Clear();
            paint.SetColor("_BaseColor", color);
            paint.SetColor("_EmissionColor", lit ? color * .75f : Color.black);
            renderer.SetPropertyBlock(paint);
        }

        static Vector3 World(Vector2 p) => new(p.x, 0, p.y);

        static void Pose(GameObject obj, Vector2? p, float yaw)
        {
            obj.SetActive(p.HasValue);
            if (p.HasValue) obj.transform.SetPositionAndRotation(World(p.Value), ModelRotation(yaw + 180));
        }

        static bool NearJunction(float z, float radius)
        {
            foreach (float j in Junctions)
                if (Mathf.Abs(z - j) < radius) return true;
            return false;
        }

        GameObject Model(string asset, Vector3 pos, float yaw = 0, Transform parent = null)
        {
            var wrapper = new GameObject(asset);
            wrapper.transform.SetParent(parent ? parent : scenery.transform, false);
            wrapper.transform.SetPositionAndRotation(pos, ModelRotation(yaw));
            var model = Models.Spawn(asset, wrapper.transform, out var mats);
            if (mats.TryGetValue("M_CarPaint", out var carPaint))
                carPaint.SetColor("_BaseColor", ColorOf("9a1b1f"));
            if (mats.TryGetValue("M_MotoPaint", out var motoPaint))
                motoPaint.SetColor("_BaseColor", ColorOf("1d4fa3"));
            if (asset == "Motorcycle")
                foreach (var part in model.GetComponentsInChildren<Transform>(true))
                    if (part.name.Contains("Helmet_Catok") || part.name.Contains("Strap_Open") || part.name.Contains("_Hair"))
                        part.gameObject.SetActive(false);
            if (asset.StartsWith("Ruko_"))
            {
                int seed = Mathf.Abs(Mathf.RoundToInt(pos.z * 7.31f + pos.x * 31)) + 11;
                var random = new System.Random(seed);
                if (mats.TryGetValue("M_WallPaint", out var wall))
                    wall.SetColor("_BaseColor", ColorOf(WallColors[random.Next(WallColors.Length)]));
                if (mats.TryGetValue("M_Awning", out var awning))
                    awning.SetColor("_BaseColor", Color.HSVToRGB((float)random.NextDouble(), .55f, .55f));
                if (mats.TryGetValue("M_ShopSign", out var board))
                    board.SetColor("_BaseColor", Color.HSVToRGB((float)random.NextDouble(), .75f, .6f));
                var sign = Models.Spawn("ShopSign_" + random.Next(10), model.transform);
                sign.transform.localPosition = new Vector3(0, 4.35f, .1f);
                sign.transform.localRotation = Quaternion.identity;
                sign.transform.localScale = new Vector3(4.5f / 6.85f, 1, 1 / .85f);
                Models.Spawn(random.NextDouble() < .62 ? "Shutter_Half" : "Shutter_Closed", model.transform)
                    .transform.localRotation = Quaternion.identity;
            }
            return wrapper;
        }

        static Quaternion ModelRotation(float yaw) => Quaternion.Euler(0, yaw, 0);

        void Road(string name, float x, float a, float b, float width) =>
            SurfaceBox(name, new Vector3(x, -.042f, (a+b)/2), new Vector3(width, .08f, b-a), "Asphalt");

        void Mark(string name, float x, float z, float w, float l, string color = "e9e9e2") =>
            Box(name, new Vector3(x, .012f, z), new Vector3(w, .019f, l), color);

        void Arrow(float x, float z, int direction) =>
            Model("LaneArrow_Straight", new Vector3(x, .025f, z), direction > 0 ? 0 : 180);

        void SurfaceBox(string name, Vector3 pos, Vector3 size, string surface)
        {
            if (!surfaces.TryGetValue(surface, out var material))
            {
                material = MaterialLibrary.Create("M_" + surface, Color.white, 0,
                    surface == "Asphalt" ? .35f : surface == "Paving" ? .2f : .05f);
                var state = Random.state;
                Random.InitState(11);
                material.SetTexture("_BaseMap", surface == "Asphalt" ? Textures.Asphalt()
                    : surface == "Paving" ? Textures.Paving() : Textures.Grass());
                Random.state = state;
                surfaces.Add(surface, material);
            }
            var mesh = new MeshBuilder(surface == "Asphalt" ? .12f : surface == "Paving" ? .5f : .05f);
            mesh.Box(pos - size * .5f, pos + size * .5f);
            mesh.Build(name, scenery.transform, material, surface == "Paving");
        }

        void BuildStreetProps()
        {
            foreach (int side in Side)
            {
                Vector3? lastPole = null;
                for (int z = -15; z < 388; z += 28)
                {
                    bool opening = side < 0 && z > PhoneStart && z < PhoneEnd
                        || side > 0 && z > SpurStart - 3 && z < SpurEnd + 3;
                    if (Mathf.Abs(z - Zebra) < 22)
                    {
                        // Keep foliage, poles, and their cables out of the crossing sightline.
                        lastPole = null;
                        continue;
                    }
                    if (NearJunction(z, 9) || opening) continue;
                    Model(z % 3 == 0 ? "Tree_Palm" : "Tree_Shade_1", new Vector3(side * 6.1f, .13f, z), z * 13);
                    Vector3 pole = new(side * 7.1f, .13f, z + 5);
                    Model("PowerPole", pole);
                    if (lastPole.HasValue)
                    {
                        for (int wire = 0; wire < 2; wire++)
                        {
                            var cable = new GameObject("Kabel listrik").AddComponent<LineRenderer>();
                            cable.transform.SetParent(scenery.transform);
                            cable.sharedMaterial = Material("151515");
                            cable.widthMultiplier = .025f;
                            cable.positionCount = 13;
                            var offset = new Vector3(wire * .28f, 7.1f, 0);
                            for (int i = 0; i <= 12; i++)
                            {
                                float t = i / 12f;
                                cable.SetPosition(i, Vector3.Lerp(lastPole.Value, pole, t) + offset
                                    + Vector3.down * .55f * 4 * t * (1-t));
                            }
                        }
                    }
                    lastPole = pole;
                }
            }
            Model("Sign_Parking", new Vector3(-7.6f, .13f, PhoneStart + 5));
            StreetName(-6.5f, StopJunction - 7, "kenanga");
            StreetName(-6.5f, OneWayStart + 2, "mawar");
        }

        void StreetName(float x, float z, string name)
        {
            var pole = Model("StreetName_Pole", new Vector3(x, .13f, z));
            var plate = Models.Spawn("StreetName_" + name, pole.transform);
            plate.transform.localPosition = new Vector3(0, 3.1f, 0);
            plate.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        Renderer Box(string name, Vector3 pos, Vector3 size, string color, Transform parent = null, bool emissive = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent ? parent : scenery.transform);
            obj.transform.position = pos;
            obj.transform.localScale = size;
            Object.Destroy(obj.GetComponent<BoxCollider>());
            var renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = Material(color);
            if (emissive) renderer.sharedMaterial.EnableKeyword("_EMISSION");
            return renderer;
        }

        Material Material(string value)
        {
            if (palette.TryGetValue(value, out var material)) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = value };
            material.SetColor("_BaseColor", ColorOf(value));
            material.SetFloat("_Smoothness", .12f);
            palette.Add(value, material);
            return material;
        }

        static Color ColorOf(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}
