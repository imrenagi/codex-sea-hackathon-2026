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
        // Weathered plaster, canvas and painted shopfronts keep safety colors distinct.
        static readonly string[] WallColors = { "c9c7b8", "c8b7a1", "d6d3c8", "b5bcba", "c4afa0",
            "b8b2b0", "cabc9e", "acb7ad", "d1c9b9", "b8ad98" };
        static readonly string[] ShopColors = { "626e67", "786a5c", "53656b", "8a7662", "785b51", "687168" };
        readonly Dictionary<string, Material> palette = new();
        readonly Dictionary<string, Material> surfaces = new();
        readonly GameObject scenery;
        readonly Transform actors;
        readonly TrafficVehicleView crossCar, laneCar, spurCar, pickupCar;
        readonly PlayerVehicleView playerVehicle;
        readonly StreetPerson pedestrian;
        readonly ScenarioAtmosphere atmosphere;
        readonly GameObject schoolScenery, marketScenery, marketCurb, finishMarker;
        readonly Renderer[] signal = new Renderer[3];
        readonly Color[] signalColors = { new(.96f, .13f, .15f), new(1, .69f, .18f), new(.12f, 1, .53f) };
        readonly MaterialPropertyBlock paint = new();
        readonly Camera camera;
        readonly Light headlamp;
        readonly StreetLook streetLook;
        readonly DrivingCamera followCamera = new();
        public readonly DrivingMirrors Mirrors;
        public readonly IntersectionSignals Signals;
        readonly GameObject routeLighting;
        readonly Atmosphere routeAtmosphere;
        bool routeLook;
        bool snapCamera = true;

        public District(Transform parent)
        {
            scenery = new GameObject("Kampung Kenanga");
            scenery.transform.SetParent(parent);
            streetLook = new StreetLook(scenery.transform);
            actors = new GameObject("Traffic and player").transform;
            actors.SetParent(parent);
            Signals = new IntersectionSignals(parent);
            schoolScenery = new GameObject("Penyeberangan sekolah");
            marketScenery = new GameObject("Teluk jemput pasar");
            marketCurb = new GameObject("Trotoar pasar pada rute biasa");
            finishMarker = new GameObject("Tujuan misi");
            foreach (var group in new[] { schoolScenery, marketScenery, marketCurb, finishMarker })
                group.transform.SetParent(actors, false);
            SetupLight();
            BuildRoad();
            BuildBuildings();
            BuildFlowerMarket();
            BuildSigns();
            BuildMissionScenery();
            streetLook.StreetDetails();
            BuildStreetProps();
            BuildBoulevard();
            BuildCheckpointMarkers();
            StaticBatchingUtility.Combine(scenery);

            playerVehicle = new PlayerVehicleView(actors);
            crossCar = new TrafficVehicleView(actors, "Lalu lintas persimpangan");
            laneCar = new TrafficVehicleView(actors, "Kendaraan di lajur kanan");
            spurCar = new TrafficVehicleView(actors, "Lalu lintas satu arah");
            pedestrian = new StreetPerson(actors, "Pejalan kaki pada penyeberangan", "c9ad71", 1, false, 7);
            atmosphere = new ScenarioAtmosphere(actors);
            pickupCar = new TrafficVehicleView(actors, "Arus di belakang teluk jemput");
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

            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(.65f, .81f, .79f);
            camera.fieldOfView = 55;
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 285;
            DrivingCamera.Configure(camera);
            camera.farClipPlane = 500;
            camera.gameObject.AddComponent<AudioListener>();
            routeLighting = new GameObject("Full journey lighting");
            routeLighting.transform.SetParent(parent, false);
            routeAtmosphere = new Atmosphere(routeLighting.transform, camera);
            Mirrors = new DrivingMirrors(parent);
            RestoreAtmosphere();
        }

        void SetupLight()
        {
            RestoreAtmosphere();
        }

        public void RestoreAtmosphere()
        {
            if (routeLighting) routeLighting.SetActive(false);
            routeLook = false;
            streetLook.SetLighting(true);
            streetLook.Activate();
        }

        void BuildRoad()
        {
            SurfaceBox("Tanah", new Vector3(0, -.3f, 184), new Vector3(245, .45f, 475), "Grass");
            Road("Koridor Kenanga", 0, -28, 382, 9.5f);
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

            for (int z = -25; z < 382; z += 4)
            {
                if (NearJunction(z, 5.3f) || Mathf.Abs(z - Zebra) < 3.5f) continue;
                if (z >= OneWayStart && z <= Merge) Mark("Marka pindah lajur", 0, z, .14f, 1.95f);
                else Mark("Marka tengah", 0, z, .14f, 2.05f, "e9e9e2");
            }
            foreach (int side in Side)
            {
                for (int z = -24; z < 382; z += 4)
                {
                    if (NearJunction(z, 6.1f)) continue;
                    bool opening = side == 1 ? z > SpurStart - 3 && z < SpurEnd + 3
                        : z > PhoneStart + 1 && z < PhoneEnd + 3;
                    if (!opening)
                    {
                        Transform curbParent = side == -1 && z >= Mission.PickupStart && z <= Mission.PickupEnd
                            ? marketCurb.transform : null;
                        Mark("Garis tepi", side * 4.39f, z, .12f, 4.02f, parent: curbParent);
                        bool landing = Mathf.Abs(z - Zebra) < 4;
                        float pavingZ = landing ? z + (z < Zebra ? -1 : 1) : z;
                        Box("Trotoar", new Vector3(side * 5.5f, .035f, pavingZ), new Vector3(1.5f, .18f, landing ? 2 : 4.02f),
                            (z / 4) % 2 == 0 ? "c4b499" : "cdbea4", curbParent);
                    }
                    else if (side == 1)
                        Mark("Pemisah cabang", 5.02f, z, .13f, 2.25f, "e9e9e2");
                    else
                        Mark("Batas teluk", -4.45f, z, .12f, 1.9f);
                }
            }

            BuildCrossingLandings();
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
            for (int i = 0; i < 12; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (145 + i * 8);
                float z = 429 + (i % 3) * 11;
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = "Bukit";
                hill.transform.SetParent(scenery.transform);
                hill.transform.position = new Vector3(x, -4, z);
                hill.transform.localScale = new Vector3(35 + i % 3 * 12, 18 + i % 4 * 5, 27);
                Models.Kill(hill.GetComponent<SphereCollider>());
                hill.GetComponent<Renderer>().sharedMaterial = Material(i % 2 == 0 ? "5d8b71" : "76a17c");
            }
        }

        void BuildCrossingLandings()
        {
            foreach (int side in Side)
            {
                float x0 = side * 4.67f, x1 = side * 5.5f;
                var vertices = new[] {
                    new Vector3(x0, .005f, Zebra - 2), new Vector3(x0, .005f, Zebra + 2),
                    new Vector3(x1, .125f, Zebra + 2), new Vector3(x1, .125f, Zebra - 2) };
                var mesh = new Mesh { name = "Landasan penyeberangan" };
                mesh.vertices = vertices;
                mesh.triangles = side > 0 ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
                var uv = new Vector2[4];
                for (int i = 0; i < vertices.Length; i++) uv[i] = new Vector2(vertices[i].x, vertices[i].z) * .5f;
                mesh.uv = uv;
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var ramp = new GameObject("Landasan penyeberangan");
                ramp.transform.SetParent(scenery.transform, false);
                ramp.AddComponent<MeshFilter>().sharedMesh = mesh;
                ramp.AddComponent<MeshRenderer>().sharedMaterial = streetLook.BoxMaterial("Trotoar penyeberangan");
                Box("Trotoar penyeberangan", new Vector3(side * 5.875f, .035f, Zebra),
                    new Vector3(.75f, .18f, 4), "c4b499");
            }
            var paving = new MeshBuilder();
            foreach (int side in Side)
                for (int row = 0; row < 3; row++)
                    for (int dot = 0; dot < 8; dot++)
                    {
                        Vector3 center = new(side * (5.65f + row * .14f), .136f, Zebra - 1.4f + dot * .4f);
                        paving.Box(center - new Vector3(.045f, .01f, .045f), center + new Vector3(.045f, .01f, .045f));
                    }
            paving.Build("Pola taktil penyeberangan", scenery.transform, Material("d6aa56"), false);
        }

        void BuildCheckpointMarkers()
        {
            var boards = new MeshBuilder();
            var numbers = new MeshBuilder();
            int[] digits = { 63, 6, 91, 79, 102, 109, 125, 7, 127, 111 };
            Vector2[] segments = { new(0, .25f), new(.14f, .13f), new(.14f, -.13f),
                new(0, -.25f), new(-.14f, -.13f), new(-.14f, .13f), new(0, 0) };
            for (int i = 0; i < All.Length; i++)
            {
                float heading = All[i].Heading;
                var rotation = Quaternion.Euler(0, heading, 0);
                var position = World(All[i].Checkpoint) + rotation * Vector3.left * 3.57f;
                if (i >= 5)
                    position = i == 6
                        ? new Vector3(All[i].Checkpoint.x, 0, Boulevard.Center(6).y - 12.4f)
                        : new Vector3(Boulevard.Center(i == 5 ? 5 : 7).x - 12.4f, 0, All[i].Checkpoint.y);
                boards.OrientedBox(position + Vector3.up * 1.2f, new Vector3(.08f, 2.4f, .08f), heading);
                boards.OrientedBox(position + Vector3.up * 2.45f, new Vector3(.9f, .75f, .08f), heading);
                for (int digit = 0; digit < 2; digit++)
                {
                    int value = digit == 0 ? (i + 1) / 10 : (i + 1) % 10;
                    for (int part = 0; part < segments.Length; part++)
                    {
                        if ((digits[value] & (1 << part)) == 0) continue;
                        var local = new Vector3((digit == 0 ? -.21f : .21f) + segments[part].x,
                            2.45f + segments[part].y, -.055f);
                        bool horizontal = part == 0 || part == 3 || part == 6;
                        numbers.OrientedBox(position + rotation * local,
                            horizontal ? new Vector3(.23f, .045f, .025f) : new Vector3(.045f, .2f, .025f), heading);
                    }
                }
            }
            boards.Build("Nomor checkpoint · papan", scenery.transform, Material("25272b"));
            numbers.Build("Nomor checkpoint · angka", scenery.transform, Material("f7efe1"), false);
        }

        void BuildBuildings()
        {
            var rng = new System.Random(11);
            foreach (int side in Side)
            {
                float rotation = side < 0 ? -90 : 90;
                for (int z = -20; z < 388; z += 7)
                {
                    if (NearJunction(z, 10) || (side < 0 && (z > 300 && z < 375 || z > 244 && z < 283 || z > 43 && z < 61))) continue;
                    if (side < 0 && z > Zebra + 2 && z < Zebra + 23) continue;
                    float edge = side == 1 && z > SpurStart - 4 && z < SpurEnd + 5 ? 12 : 8.9f;
                    if ((z + 20) % 28 == 14)
                    {
                        if (Mathf.Abs(z - Zebra) < 22) continue;
                        Model("Tree_Shade_0", new Vector3(side * (edge + 1.2f), 0, z), z * 31);
                        continue;
                    }
                    // Residential frontages are authored separately, outside the walkable verge.
                    if (z < 24) continue;
                    string shop = Shops[rng.Next(Shops.Length)];
                    GameObject building = Model(shop, new Vector3(side * edge, .06f, z), rotation);
                    building.transform.localScale = new Vector3(6.85f / 4.5f, 1, .85f);
                    if ((z + 20) % 35 == 0)
                        Model("StreetLamp", new Vector3(side * 5.1f, .12f, z + 2.4f), rotation);
                }
            }
            BuildSchoolFrontage();
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

        void BuildSchoolFrontage()
        {
            var batches = new Dictionary<string, MeshBuilder>();
            void Detail(string color, Vector3 center, Vector3 size)
            {
                if (!batches.TryGetValue(color, out var mesh))
                    batches.Add(color, mesh = new MeshBuilder());
                mesh.Box(center - size * .5f, center + size * .5f);
            }

            SurfaceBox("Halaman sekolah", new Vector3(-14.7f, .005f, 347), new Vector3(12, .1f, 24), "Paving");
            Detail("efe2c2", new Vector3(-17.5f, 2.45f, 347), new Vector3(7, 4.7f, 20));
            Detail("a6533a", new Vector3(-17.5f, 4.9f, 347), new Vector3(7.8f, .35f, 20.8f));
            Detail("bb6849", new Vector3(-17.5f, .4f, 347), new Vector3(7.1f, .65f, 20.1f));
            for (int classroom = 0; classroom < 6; classroom++)
            {
                float z = 339 + classroom * 3.2f;
                if (Mathf.Abs(z - 347) < 2) continue;
                Detail("f7efe1", new Vector3(-13.96f, 2.4f, z), new Vector3(.12f, 1.7f, 2.1f));
                Detail("35636e", new Vector3(-13.88f, 2.4f, z), new Vector3(.05f, 1.45f, 1.85f));
                Detail("f7efe1", new Vector3(-13.84f, 2.4f, z), new Vector3(.04f, 1.5f, .08f));
            }
            Detail("6e503c", new Vector3(-13.93f, 1.5f, 347), new Vector3(.15f, 2.8f, 1.6f));
            Detail("a6533a", new Vector3(-12.5f, 3.4f, 347), new Vector3(3.2f, .2f, 3.4f));
            foreach (float z in new[] { 345.5f, 348.5f })
                Detail("efe2c2", new Vector3(-11.1f, 1.7f, z), new Vector3(.15f, 3.3f, .15f));

            foreach (float z in new[] { 343f, 351f })
                Detail("efe2c2", new Vector3(-8.6f, 1.65f, z), new Vector3(.65f, 3.3f, .65f));
            Detail("efe2c2", new Vector3(-8.6f, 3.1f, 347), new Vector3(.65f, .45f, 8.6f));
            Detail("a6533a", new Vector3(-8.6f, 3.45f, 347), new Vector3(1.1f, .2f, 9.3f));
            Detail("bb6849", new Vector3(-8.6f, .45f, 338.5f), new Vector3(.35f, .8f, 8.4f));
            Detail("bb6849", new Vector3(-8.6f, .45f, 354.5f), new Vector3(.35f, .8f, 6.4f));

            Detail("8f8a85", new Vector3(-11, 2.65f, 339), new Vector3(.06f, 5.3f, .06f));
            Detail("c63b32", new Vector3(-11, 4.65f, 339.9f), new Vector3(.035f, .5f, 1.8f));
            Detail("f7efe1", new Vector3(-11, 4.15f, 339.9f), new Vector3(.035f, .5f, 1.8f));
            foreach (var batch in batches)
                batch.Value.Build("Sekolah · " + batch.Key, scenery.transform, Material(batch.Key));
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
            light.transform.localScale = new Vector3(-1,1,1);
            Signals.EnlargeLenses(light.transform);
            Signals.Register(1,light.transform,Vector3.forward);
            string[] parts = { "Light_Red", "Light_Amber", "Light_Green" };
            for (int i = 0; i < signal.Length; i++)
            {
                signal[i] = Models.Find(light.transform, parts[i]).GetComponent<Renderer>();
                Models.OwnMaterials(signal[i]);
            }
        }

        public void Snap() => snapCamera = true;

        void BuildMissionScenery()
        {
            for (int i = -5; i <= 5; i++)
                Mark("Zebra sekolah", i * .8f, Mission.SchoolZebra, .43f, 3.35f, parent: schoolScenery.transform);
            Model("Sign_Crossing", new Vector3(-5.45f, .12f, Mission.SchoolZebra - 8), parent: schoolScenery.transform);
            Box("Jalan teluk jemput", new Vector3(-6.15f, -.042f, (Mission.PickupStart + Mission.PickupEnd) / 2),
                new Vector3(3.4f, .08f, Mission.PickupEnd - Mission.PickupStart), "535d63", marketScenery.transform);
            for (float z = Mission.PickupStart + 1; z < Mission.PickupEnd; z += 3)
            {
                Mark("Marka masuk teluk", -4.45f, z, .12f, 1.4f, parent: marketScenery.transform);
                Mark("Batas kiri teluk", -7.55f, z, .12f, 3, parent: marketScenery.transform);
            }
            Model("Sign_NoStopping", new Vector3(-5.45f, .12f, Mission.NoStopStart), parent: marketScenery.transform);
            Model("Sign_Parking", new Vector3(-7.7f, .12f, Mission.PickupStart + 2), parent: marketScenery.transform);
            Mark("Tempat jemput", -5.9f, Mission.PickupPoint - 4, 2.5f, .2f, "65dfbf", marketScenery.transform);
            Mark("Tempat jemput", -5.9f, Mission.PickupPoint + 4, 2.5f, .2f, "65dfbf", marketScenery.transform);
            for (int i = 0; i < 8; i++)
                Mark("Garis tujuan", -4.32f + i * .55f, 0, .27f, .5f, "fac86a", finishMarker.transform);
            foreach (float x in new[] { -5.1f, .7f })
                Box("Penanda tujuan", new Vector3(x, 1.1f, 0), new Vector3(.3f, 2.2f, .3f), "fac86a", finishMarker.transform);
        }

        public void Present(Journey run, bool hood, float dt, bool driving = true, float lookInput = 0)
        {
            bool useRouteLook = run.UsesRouteSteering;
            if (useRouteLook != routeLook)
            {
                routeLook = useRouteLook;
                routeLighting.SetActive(routeLook);
                streetLook.SetLighting(!routeLook);
                if (routeLook) routeAtmosphere.Activate(); else streetLook.Activate();
            }
            PresentBoulevard(run, driving);
            bool onMotorcycle = run.Mode == VehicleMode.Motorcycle;
            var point = World(run.Position);
            var rotation = Quaternion.Euler(0, run.Heading, 0);
            playerVehicle.Present(run.Mode, point, run.Heading, run.Speed, dt, run.Headlight, run.Indicator,
                !run.IsMarket || run.PassengerPickedUp && run.BoardingProgress >= 1, !hood, run.DriverGear, run.PassengerGear);
            headlamp.transform.SetPositionAndRotation(point + rotation * new Vector3(0, .87f, .85f), rotation);
            headlamp.enabled = false; // The selected scooter supplies its own correctly positioned lamps.
            Pose(crossCar, run.Traffic.Cross, run.CrossTraffic, run.CrossDirection * 90, dt);
            pedestrian.Root.SetActive(run.Pedestrian.HasValue);
            if (run.Pedestrian.HasValue)
            {
                pedestrian.SchoolAppearance(run.IsSchool);
                pedestrian.Draw(World(run.Pedestrian.Value), run.PedestrianDirection * 90, run.ElapsedTime, 1, false);
            }
            Pose(laneCar, run.Traffic.Lane, run.LaneTraffic, 0, dt);
            Pose(spurCar, run.Traffic.Spur, run.SpurTraffic, 180, dt);
            Pose(pickupCar, run.Traffic.Pickup, run.PickupTraffic, 0, dt);
            schoolScenery.SetActive(run.IsSchool);
            marketScenery.SetActive(run.IsMarket);
            marketCurb.SetActive(!run.IsMarket);
            atmosphere.Present(run);
            finishMarker.SetActive(run.Activity != null && run.Stage > 0 && !run.Finished);
            finishMarker.transform.position = new Vector3(0, 0, run.StageEnd);
            for (int i = 0; i < signal.Length; i++)
                foreach(var mat in signal[i].sharedMaterials) IntersectionSignals.Light(mat,i,i==run.LightPhase);

            followCamera.Present(camera, point, run.Heading, run.Speed, onMotorcycle, hood, dt, snapCamera, driving ? lookInput : 0);
            Mirrors.Present(run, driving, snapCamera);
            Signals.Present(run, driving);
            snapCamera = false;
        }

        void Tint(Renderer renderer, Color color, bool lit)
        {
            paint.Clear();
            paint.SetColor("_BaseColor", color);
            paint.SetColor("_EmissionColor", lit ? color * .75f : Color.black);
            renderer.SetPropertyBlock(paint);
        }

        static Vector3 World(Vector2 p) => new(p.x, 0, p.y);

        static void Pose(TrafficVehicleView view, TrafficChoice choice, Vector2? p, float yaw, float dt)
        {
            view.Select(choice);
            if (p.HasValue) view.Present(World(p.Value), yaw, -1, dt);
            else view.Hide();
        }

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
            if (asset == "Tree_Shade_0") return streetLook.Tree(pos, yaw, parent ? parent : scenery.transform);
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
                    awning.SetColor("_BaseColor", ColorOf(ShopColors[(int)(random.NextDouble() * ShopColors.Length)]));
                if (mats.TryGetValue("M_ShopSign", out var board))
                    board.SetColor("_BaseColor", ColorOf(ShopColors[(int)(random.NextDouble() * ShopColors.Length)]));
                var sign = Models.Spawn("ShopSign_" + random.Next(10), model.transform);
                sign.transform.localPosition = new Vector3(0, 4.35f, .1f);
                sign.transform.localRotation = Quaternion.identity;
                sign.transform.localScale = new Vector3(4.5f / 6.85f, 1, 1 / .85f);
                Models.Spawn(random.NextDouble() < .62 ? "Shutter_Half" : "Shutter_Closed", model.transform).transform.localRotation = Quaternion.identity;
                streetLook.DressBuilding(model, pos.z);
            }
            return wrapper;
        }

        static Quaternion ModelRotation(float yaw) => Quaternion.Euler(0, yaw, 0);

        void Road(string name, float x, float a, float b, float width) =>
            SurfaceBox(name, new Vector3(x, -.042f, (a+b)/2), new Vector3(width, .08f, b-a), "Asphalt");

        void Mark(string name, float x, float z, float w, float l, string color = "f4edda", Transform parent = null) =>
            Box(name, new Vector3(x, .012f, z), new Vector3(w, .019f, l), color, parent);

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
                    { lastPole = null; continue; }
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
            Models.Kill(obj.GetComponent<BoxCollider>());
            var renderer = obj.GetComponent<Renderer>();
            var surface = streetLook.BoxMaterial(name);
            renderer.sharedMaterial = surface ? surface : Material(color);
            if (surface) StreetLook.ProjectSurface(obj.GetComponent<MeshFilter>(), name == "Tanah" ? 5 : 3);
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
