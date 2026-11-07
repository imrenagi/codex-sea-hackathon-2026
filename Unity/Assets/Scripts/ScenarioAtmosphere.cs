using System.Collections.Generic;
using UnityEngine;
using UjianSim.Game;

namespace JalanKita
{
    // Ambient people stay on sidewalks and forecourts. The scored crossing is still owned by Journey.
    public sealed class ScenarioAtmosphere
    {
        sealed class Neighbour
        {
            public StreetPerson Person;
            public Vector3 Start, End;
            public float Pace, Offset, Facing;
            public bool Gesture;
        }

        readonly GameObject village, school, market;
        readonly List<Neighbour> neighbours = new();
        readonly Dictionary<string, Material> materials = new();
        readonly StreetPerson passenger;
        Journey previous;
        Vector3 boardingStart;
        bool wasPickedUp;

        public ScenarioAtmosphere(Transform parent)
        {
            village = Group("Kehidupan kampung", parent);
            school = Group("Jam pulang sekolah", parent);
            market = Group("Kegiatan pasar", parent);

            // Village neighbours and a real destination for the short departure journey.
            Platform(village.transform, new Vector3(-7.6f, .065f, 51), new Vector3(4.6f, .13f, 15));
            Stall(village.transform, new Vector3(-8.8f, .13f, 52), "e2ad54", false);
            Label(village.transform, "WARUNG BU SARI", new Vector3(-7.98f, 1.95f, 52), .055f);
            Add(village, "Penjual warung", -9.5f, 52, -9.5f, 52, "e1b05d", 1, 90, true);
            Add(village, "Pembeli warung", -6.6f, 51.4f, -6.6f, 51.4f, "3c728b", 1, -90, true);
            Add(village, "Tetangga menyapa", -5.8f, 6, -5.8f, 6, "a35662", 1, 90, true);
            Add(village, "Warga berjalan", 5.65f, 9, 5.65f, 25, "497f67", .97f, 180);
            Add(village, "Warga menuju warung", -5.7f, 43, -5.7f, 48, "ded5c0", 1.03f, 0);
            Add(village, "Tetangga di teras", 6.5f, 17, 6.5f, 17, "d0a15a", 1, -90, true);

            // Children and guardians wait behind the curb; only the simulated pedestrian crosses.
            Platform(school.transform, new Vector3(-7.05f, .065f, 318.5f), new Vector3(3.8f, .13f, 21));
            BuildSchool(school.transform);
            Add(school, "Siswa menuju gerbang", -6.35f, 310, -6.35f, 319.5f, "f2eee4", .79f, 0, false, true);
            Add(school, "Teman di gerbang", -7.05f, 320.2f, -7.05f, 320.2f, "f2eee4", .8f, 90, true, true);
            Add(school, "Siswa menunggu jemputan", -6.5f, 321.5f, -6.5f, 321.5f, "f2eee4", .76f, 65, true, true);
            Add(school, "Orang tua menunggu", -7.3f, 316, -7.3f, 316, "c4974c", 1, 90, true);
            Add(school, "Wali murid", -6.65f, 326.8f, -6.65f, 326.8f, "447e80", .98f, 75, true);
            Add(school, "Siswa bersama wali", -7.4f, 327.1f, -7.4f, 327.1f, "f2eee4", .77f, 85, false, true);
            Add(school, "Siswa di seberang", 5.8f, 325.5f, 5.8f, 325.5f, "f2eee4", .8f, -90, false, true);
            Add(school, "Orang tua di seberang", 6.65f, 326, 6.65f, 326, "986d91", 1.02f, -90, true);
            Add(school, "Warga melewati sekolah", 5.65f, 307, 5.65f, 319, "4f748c", 1, 0);
            Bench(school.transform, new Vector3(-7.7f, .13f, 312.7f));
            Box(school.transform, "Papan gerbang", new Vector3(-8.25f, 1.65f, 318), new Vector3(.12f, .65f, 3.2f), "3d786b");
            for (int i = 0; i < 4; i++)
                Box(school.transform, "Tiang pembatas trotoar", new Vector3(-5.16f, .55f, 310 + i * 3), new Vector3(.12f, .85f, .12f), "dcbb68");

            // Market activity occupies the pavement beyond the pickup bay, never its drivable envelope.
            Platform(market.transform, new Vector3(-9.4f, .065f, 264), new Vector3(3.7f, .13f, 29));
            Stall(market.transform, new Vector3(-10.15f, .13f, 255), "ab594a", true);
            Stall(market.transform, new Vector3(-10.15f, .13f, 273), "477f71", true);
            Label(market.transform, "BUAH SEGAR", new Vector3(-9.32f, 1.95f, 255), .06f);
            Label(market.transform, "SAYUR HARI INI", new Vector3(-9.32f, 1.95f, 273), .053f);
            Box(market.transform, "Dinding kios pasar", new Vector3(-12.05f, 1.6f, 264), new Vector3(.25f, 3.2f, 29), "d8c194");
            for (int bay = 0; bay < 6; bay++)
            {
                Box(market.transform, "Bukaan kios", new Vector3(-11.88f, 1.25f, 252 + bay * 4.8f), new Vector3(.08f, 2.25f, 3.1f), bay % 2 == 0 ? "778c7c" : "718399");
                Box(market.transform, "Kanopi kios", new Vector3(-11.2f, 2.85f, 252 + bay * 4.8f), new Vector3(1.85f, .13f, 4.4f), bay % 2 == 0 ? "8b534b" : "527e6e");
            }
            Add(market, "Pedagang buah", -10.9f, 255, -10.9f, 255, "d7b46b", 1.02f, 90, true);
            Add(market, "Pedagang sayur", -10.9f, 273, -10.9f, 273, "6d8b65", .98f, 90, true);
            Add(market, "Pembeli memilih buah", -8.8f, 254.6f, -8.8f, 254.6f, "c08d75", .96f, -90, true);
            Add(market, "Pembeli membawa belanjaan", -8.15f, 250.8f, -8.15f, 260.3f, "547786", 1.01f, 0);
            Add(market, "Pelanggan sayur", -8.9f, 272.5f, -8.9f, 272.5f, "9380aa", 1.03f, -90, true);
            Add(market, "Warga di pasar", -8.15f, 268.4f, -8.15f, 277, "c8a653", .99f, 180);
            Add(market, "Anak bersama pembeli", -8.8f, 256.1f, -8.8f, 256.1f, "6c9b89", .76f, -75);
            passenger = new StreetPerson(market.transform, "Penumpang menunggu di teluk", "4d8991", 1, false, 23);
            Bench(market.transform, new Vector3(-9.9f, .13f, 265));
        }

        public void Present(Journey run)
        {
            village.SetActive(run.Activity?.IsVillage == true);
            school.SetActive(run.IsSchool);
            market.SetActive(run.IsMarket);
            float time = run.ElapsedTime;
            foreach (var npc in neighbours)
            {
                if (!npc.Person.Root.activeInHierarchy) continue;
                Vector3 delta = npc.End - npc.Start;
                float distance = delta.magnitude;
                float yaw = npc.Facing, walking = 0;
                Vector3 position = npc.Start;
                if (distance > .1f)
                {
                    float duration = distance / npc.Pace;
                    const float wait = 2.5f;
                    float cycle = Mathf.Repeat(time + npc.Offset, 2 * (duration + wait));
                    bool outward = cycle < duration + wait;
                    float part = outward ? cycle : cycle - duration - wait;
                    float u = Mathf.Clamp01(part / duration);
                    position = Vector3.Lerp(npc.Start, npc.End, outward ? u : 1 - u);
                    yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg + (outward ? 0 : 180);
                    if (part < duration) walking = npc.Pace / 1.35f;
                    else yaw += 180 * Mathf.SmoothStep(0, 1, (part - duration) / wait);
                }
                npc.Person.Draw(position, yaw, time + npc.Offset, walking, npc.Gesture);
            }

            if (previous != run || !run.PassengerPickedUp)
            {
                previous = run;
                wasPickedUp = false;
            }
            if (!run.IsMarket) return;
            Vector3 waiting = new(-8.12f, .13f, Mission.PickupPoint);
            if (run.PassengerPickedUp && !wasPickedUp)
            {
                boardingStart = waiting;
                wasPickedUp = true;
            }
            passenger.Root.SetActive(!run.PassengerPickedUp || run.IsBoarding);
            if (run.IsBoarding)
            {
                Vector3 door = new(run.Position.x - .76f, .02f, run.Position.y + .25f);
                Vector3 toward = door - boardingStart;
                float yaw = Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
                passenger.Draw(Vector3.Lerp(boardingStart, door, run.BoardingProgress), yaw, time, .85f, false);
            }
            else if (!run.PassengerPickedUp)
                passenger.Draw(waiting, 90, time, 0, run.Position.y > 253);
        }

        void Add(GameObject group, string name, float x, float z, float endX, float endZ, string shirt,
            float height, float facing, bool gesture = false, bool pupil = false)
        {
            int index = neighbours.Count;
            neighbours.Add(new Neighbour
            {
                Person = new StreetPerson(group.transform, name, shirt, height, pupil, index),
                Start = new Vector3(x, .13f, z), End = new Vector3(endX, .13f, endZ),
                Pace = .7f + index % 4 * .11f, Offset = index * 1.71f, Facing = facing, Gesture = gesture
            });
        }

        static GameObject Group(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }

        void Platform(Transform parent, Vector3 center, Vector3 size) => Box(parent, "Pelataran", center, size, "c3b399");

        void Bench(Transform parent, Vector3 origin)
        {
            Box(parent, "Bangku menunggu", origin + new Vector3(0, .44f, 0), new Vector3(.55f, .1f, 2.2f), "8d6041");
            foreach (float z in new[] { -.8f, .8f })
                Box(parent, "Kaki bangku", origin + new Vector3(0, .21f, z), new Vector3(.4f, .4f, .12f), "434b48");
        }

        void BuildSchool(Transform parent)
        {
            Box(parent, "Gedung SD Kenanga", new Vector3(-12.1f, 1.9f, 316), new Vector3(5.8f, 3.7f, 27), "e7ddbc");
            Box(parent, "Atap sekolah", new Vector3(-12.1f, 3.8f, 316), new Vector3(6.3f, .25f, 27.7f), "9a6452");
            Box(parent, "Lis sekolah", new Vector3(-9.13f, 1.0f, 316), new Vector3(.16f, .6f, 27), "477b80");
            for (int i = 0; i < 7; i++)
            {
                float z = 304.5f + i * 3.8f;
                Box(parent, "Bingkai jendela kelas", new Vector3(-9.08f, 2.25f, z), new Vector3(.18f, 1.2f, 2.35f), "f3efdc");
                Box(parent, "Jendela kelas", new Vector3(-8.97f, 2.25f, z), new Vector3(.06f, 1.02f, 2.12f), "5b7f86");
            }
            Box(parent, "Pintu gerbang sekolah", new Vector3(-8.92f, 1.25f, 321), new Vector3(.18f, 2.5f, 2.35f), "315d63");
            Box(parent, "Nama sekolah", new Vector3(-8.8f, 3.16f, 318), new Vector3(.14f, .65f, 7), "477b80");
            Label(parent, "SD KENANGA", new Vector3(-8.7f, 3.16f, 318), .09f);
            Box(parent, "Tiang bendera", new Vector3(-7.8f, 2.3f, 307.6f), new Vector3(.07f, 4.4f, .07f), "c5c7bd");
            Box(parent, "Bendera merah", new Vector3(-7.8f, 4.08f, 308.15f), new Vector3(.035f, .35f, 1.1f), "b6443b");
            Box(parent, "Bendera putih", new Vector3(-7.8f, 3.73f, 308.15f), new Vector3(.035f, .35f, 1.1f), "f4eee1");
        }

        static void Label(Transform parent, string words, Vector3 position, float size)
        {
            var label = new GameObject(words).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = Quaternion.Euler(0, -90, 0);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.text = words;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = size;
            label.color = new Color(.98f, .95f, .84f);
        }

        void Stall(Transform parent, Vector3 origin, string fabric, bool produce)
        {
            Box(parent, "Meja warung", origin + new Vector3(0, .75f, 0), new Vector3(1.1f, .18f, 3.2f), "9c714f");
            Box(parent, "Papan nama warung", origin + new Vector3(.77f, 1.82f, 0), new Vector3(.07f, .42f, 3.35f), fabric);
            foreach (float z in new[] { -1.4f, 1.4f })
                foreach (float x in new[] { -.65f, .65f })
                    Box(parent, "Tiang tenda", origin + new Vector3(x, 1.18f, z), new Vector3(.07f, 2.36f, .07f), "6c6759");
            for (int stripe = 0; stripe < 8; stripe++)
                Box(parent, "Tenda bergaris", origin + new Vector3(.1f, 2.35f, -1.575f + stripe * .45f),
                    new Vector3(1.95f, .12f, .45f), stripe % 2 == 0 ? fabric : "e9dfbf");
            for (int i = 0; i < 3; i++)
            {
                Box(parent, "Peti dagangan", origin + new Vector3(.12f, .94f, -1 + i), new Vector3(.7f, .23f, .83f), "806348");
                if (!produce) continue;
                for (int j = 0; j < 6; j++)
                {
                    var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    fruit.name = "Hasil kebun";
                    fruit.transform.SetParent(parent, false);
                    fruit.transform.localPosition = origin + new Vector3(-.08f + (j % 2) * .28f, 1.1f, -1.25f + i + (j / 2) * .23f);
                    fruit.transform.localScale = Vector3.one * .22f;
                    Models.Kill(fruit.GetComponent<Collider>());
                    fruit.GetComponent<Renderer>().sharedMaterial = Material(i == 0 ? "bb623d" : i == 1 ? "87a355" : "d5b24c");
                }
            }
        }

        void Box(Transform parent, string name, Vector3 position, Vector3 size, string color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            Models.Kill(box.GetComponent<Collider>());
            box.GetComponent<Renderer>().sharedMaterial = Material(color);
        }

        Material Material(string color)
        {
            if (!materials.TryGetValue(color, out var material))
            {
                ColorUtility.TryParseHtmlString("#" + color, out var tint);
                material = MaterialLibrary.Create("Kegiatan " + color, tint);
                materials[color] = material;
            }
            return material;
        }
    }

    // The supplied Person FBX has separate articulated segments. Keep those meshes and animate their real pivots.
    public sealed class StreetPerson
    {
        public GameObject Root { get; }
        readonly Transform body, torso;
        readonly Joint thighL, thighR, shinL, shinR, armL, armR, foreL, foreR;
        readonly Quaternion torsoRest;
        readonly Vector3 torsoUp;
        readonly Dictionary<string, Material> colors;
        readonly float size;

        struct Joint
        {
            public Transform Transform;
            public Quaternion Rest;
            public Vector3 Axis;
            public static Joint Find(Transform body, string name, Vector3 right)
            {
                var t = Models.Find(body, name);
                return t ? new Joint { Transform = t, Rest = t.localRotation, Axis = t.parent.InverseTransformDirection(right) } : default;
            }
            public void Set(float angle)
            {
                if (Transform) Transform.localRotation = Quaternion.AngleAxis(angle, Axis) * Rest;
            }
        }

        public StreetPerson(Transform parent, string name, string shirt, float scale, bool pupil, int variation)
        {
            Root = new GameObject(name);
            Root.transform.SetParent(parent, false);
            body = Models.Spawn("Person", Root.transform, out colors).transform;
            size = scale;
            body.localScale = Vector3.one * scale;
            Tint("M_Shirt", shirt);
            Tint("M_Pants", pupil ? "8e3536" : variation % 2 == 0 ? "3d464f" : "625a50");
            Tint("M_Skin", new[] { "b98361", "956242", "bf8e6a", "a46e4d" }[variation % 4]);
            torso = Models.Find(body, "Torso");
            foreach (var side in new[] { "L", "R" })
            {
                Models.Find(body, "UpperArm_" + side)?.SetParent(torso, true);
                Models.Find(body, "ForeArm_" + side)?.SetParent(Models.Find(body, "UpperArm_" + side), true);
                Models.Find(body, "Thigh_" + side)?.SetParent(torso, true);
                Models.Find(body, "Shin_" + side)?.SetParent(Models.Find(body, "Thigh_" + side), true);
            }
            // Detect FBX forward from the foot, rather than assuming its imported root rotation.
            var shin = Models.Find(body, "Shin_L");
            var filter = shin ? shin.GetComponent<MeshFilter>() : null;
            if (filter && filter.sharedMesh)
            {
                var center = Root.transform.InverseTransformPoint(shin.TransformPoint(filter.sharedMesh.bounds.center));
                var knee = Root.transform.InverseTransformPoint(shin.position);
                if (center.z < knee.z) body.localRotation = Quaternion.Euler(0, 180, 0) * body.localRotation;
            }
            Vector3 right = Root.transform.right;
            thighL = Joint.Find(body, "Thigh_L", right); thighR = Joint.Find(body, "Thigh_R", right);
            shinL = Joint.Find(body, "Shin_L", right); shinR = Joint.Find(body, "Shin_R", right);
            armL = Joint.Find(body, "UpperArm_L", right); armR = Joint.Find(body, "UpperArm_R", right);
            foreL = Joint.Find(body, "ForeArm_L", right); foreR = Joint.Find(body, "ForeArm_R", right);
            torsoRest = torso ? torso.localRotation : Quaternion.identity;
            torsoUp = torso ? torso.parent.InverseTransformDirection(Vector3.up) : Vector3.up;
        }

        public void SchoolAppearance(bool school)
        {
            body.localScale = Vector3.one * (school ? .83f : size);
            Tint("M_Shirt", school ? "f2eee4" : "c9ad71");
            Tint("M_Pants", school ? "8e3536" : "3d464f");
        }

        public void Draw(Vector3 position, float yaw, float time, float walking, bool gesture)
        {
            float gait = time * 6.1f, swing = Mathf.Sin(gait), stride = Mathf.Clamp01(walking);
            thighL.Set(-25 * swing * stride); thighR.Set(25 * swing * stride);
            shinL.Set((5 + 37 * Mathf.Max(0, Mathf.Cos(gait + .3f))) * stride);
            shinR.Set((5 + 37 * Mathf.Max(0, -Mathf.Cos(gait + .3f))) * stride);
            armL.Set(18 * swing * stride + 4);
            armR.Set(-18 * swing * stride + (gesture ? -32 - 12 * Mathf.Sin(time * 1.8f) : 4));
            foreL.Set(-12 - 12 * stride);
            foreR.Set(gesture ? -55 - 12 * Mathf.Sin(time * 2.1f) : -12 - 12 * stride);
            if (torso) torso.localRotation = Quaternion.AngleAxis(stride > .1f ? swing * 3 : Mathf.Sin(time * .8f) * 7, torsoUp) * torsoRest;
            float bob = (Mathf.Cos(gait * 2) + 1) * .014f * stride;
            Root.transform.SetPositionAndRotation(position + Vector3.up * bob, Quaternion.Euler(0, yaw, 0));
        }

        void Tint(string key, string color)
        {
            if (!colors.TryGetValue(key, out var material)) return;
            ColorUtility.TryParseHtmlString("#" + color, out var tint);
            material.SetColor("_BaseColor", tint);
        }
    }
}
