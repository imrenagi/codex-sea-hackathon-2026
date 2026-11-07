using System;
using UnityEngine;

namespace JalanKita
{
    public enum VehicleMode { Car, Motorcycle }

    public enum Rule
    {
        SeatBelt, Helmet, Headlight, Stop, Signal, Junction, Pedestrian, KeepLeft, NoEntry,
        Indicator, Gap, ReturnLeft, Speed, Phone, OffRoad, Collision
    }

    public sealed class Lesson
    {
        public const string Law = "https://peraturan.bpk.go.id/Details/38654/uu-no-22-tahun-2009";
        public const string Signs = "https://peraturan.bpk.go.id/Details/103683/permenhub-no-13-tahun-2014";
        public const string Signals = "https://peraturan.bpk.go.id/Details/103764/permenhub-no-49-tahun-2014";
        public const string Limits = "https://peraturan.bpk.go.id/Details/103508/permenhub-no-111-tahun-2015";

        public readonly string Title, Explanation, Action, Citation, Url, Regulation;
        public Lesson(string title, string explanation, string action, string citation, string url = Law,
            string regulation = "UU 22/2009")
        {
            Title = title; Explanation = explanation; Action = action; Citation = citation; Url = url;
            Regulation = regulation;
        }

        public static readonly Lesson[] All =
        {
            new("Sabuk pengaman belum terpasang", "Pengemudi dan penumpang depan wajib memakai sabuk pengaman.",
                "Pasang kedua sabuk sebelum menekan Mulai jalan.", "Pasal 106 ayat (6)"),
            new("Helm SNI belum lengkap", "Pengendara dan penumpang motor wajib mengenakan helm SNI.",
                "Pakai helm SNI untuk keduanya. Kaitkan talinya agar helm tetap di kepala.", "Pasal 106 ayat (8)"),
            new("Lampu utama motor masih mati", "Sepeda motor juga wajib menyalakan lampu utama pada siang hari.",
                "Nyalakan lampu utama sebelum berangkat dan biarkan tetap menyala.", "Pasal 107 ayat (2)"),
            new("Melewati garis STOP", "Rambu STOP mengharuskan kendaraan berhenti penuh sebelum garis henti.",
                "Lepas gas, tahan rem sampai 0 km/j, lalu periksa arus silang sebelum maju.", "Pasal 106 ayat (4) huruf a–b"),
            new("Menerobos lampu merah", "Kendaraan harus mematuhi isyarat lampu lalu lintas.",
                "Berhenti sebelum garis putih dan tunggu lampu hijau sebelum melanjutkan.", "Pasal 106 ayat (4) huruf c"),
            new("Arus utama masih melintas", "Saat keluar dari jalan yang lebih kecil, beri hak utama pada lalu lintas di jalan utama.",
                "Tahan rem sebelum persimpangan. Tunggu kendaraan melintas dan pastikan jalan lapang.", "Pasal 113 ayat (1) huruf b"),
            new("Pejalan kaki masih menyeberang", "Pejalan kaki mendapat prioritas saat menyeberang di tempat penyeberangan.",
                "Tunggu di belakang zebra cross sampai pejalan kaki selesai menyeberang.", "Pasal 106 ayat (2) dan Pasal 131 ayat (2)"),
            new("Masuk lajur lawan arah", "Pengemudi menggunakan jalur jalan sebelah kiri.",
                "Kembali ke sisi kiri garis tengah dan tetap di kiri pada jalan dua arah.", "Pasal 108 ayat (1)"),
            new("Masuk dari arah yang dilarang", "Rambu dilarang masuk menutup akses dari arah ini.",
                "Ikuti panah lurus di jalan satu arah dan hindari cabang dengan rambu merah.", "Pasal 106 ayat (4) huruf a"),
            new("Pindah lajur tanpa sein", "Sebelum berpindah lajur, amati lalu lintas dan beri isyarat sesuai arah perpindahan.",
                "Nyalakan E untuk sein kanan atau Q untuk sein kiri sebelum melintasi garis putus-putus.", "Pasal 112 ayat (2)"),
            new("Celah pindah lajur terlalu sempit", "Periksa lalu lintas di depan dan belakang sebelum berpindah lajur.",
                "Tunggu mobil di lajur tujuan menjauh. Jarak aman dalam simulasi mengikuti kecepatan kendaraan.", "Pasal 112 ayat (2)"),
            new("Perpindahan lajur belum selesai", "Latihan ini meminta satu perpindahan aman ke kanan dan kembali ke kiri sebelum jalur menyempit.",
                "Gunakan sein kanan di garis putus-putus, lalu sein kiri dan kembali sebelum marka berakhir.", "Latihan"),
            new("Melebihi batas 30 km/j", "Batas kecepatan yang terpasang berlaku di ruas ini.",
                "Kurangi kecepatan sebelum rambu 30. Lepas gas atau rem ringan agar tetap di bawah batas.", "Pasal 106 ayat (4) huruf g dan Pasal 115 huruf a"),
            new("Menjawab telepon sambil berjalan", "Pengemudi wajib berkonsentrasi saat berkendara. Menggunakan telepon mengganggu konsentrasi.",
                "Abaikan panggilan saat mengemudi. Berhenti di tempat aman sebelum menggunakan telepon.", "Pasal 106 ayat (1) dan Penjelasan"),
            new("Keluar dari rute latihan", "Kendaraan keluar dari permukaan jalan yang disediakan untuk latihan.",
                "Ikuti marka rute dan kendalikan kecepatan sebelum mengarahkan kendaraan.", "Latihan"),
            new("Jarak dengan kendaraan habis", "Beri ruang agar kamu sempat mengerem jika lalu lintas di depan melambat.",
                "Jaga jarak dan rem lebih awal saat mendekati kendaraan lain.", "Tips keselamatan")
        };

        public bool HasSource => Citation.StartsWith("Pasal", StringComparison.Ordinal);
    }

    public sealed class RouteStage
    {
        public readonly string Title, Location, Instruction;
        public readonly float End;
        RouteStage(string title, string location, string instruction, float end)
        {
            Title = title; Location = location; Instruction = instruction; End = end;
        }

        public static readonly RouteStage[] All =
        {
            new("Sebelum berangkat", "GARASI KENANGA", "Periksa perlengkapan pengemudi dan penumpang.", -12),
            new("Rambu & lampu", "JALAN KENANGA", "Berhenti penuh di garis STOP sebelum melanjutkan.", 108),
            new("Hak utama", "PASAR BUNGA", "Dahulukan pejalan kaki di zebra cross.", 204),
            new("Arah & lajur", "JALAN MAWAR", "Ikuti arah panah. Beri sein dan pindah ke lajur kanan saat aman.", 298),
            new("Kecepatan & fokus", "ZONA SEKOLAH", "Kurangi kecepatan sebelum rambu 30 km/j.", 382)
        };

        public const float LeftLane = -2.15f;
        public const float RightLane = 2.15f;
        public const float StopLine = 30, StopJunction = 36, LightLine = 82, LightJunction = 89;
        public const float Zebra = 142, YieldLine = 174, MainJunction = 181;
        public const float OneWayStart = 220, Merge = 282, SpurStart = 236, SpurEnd = 265;
        public const float SchoolLimit = 310, PhoneStart = 330, PhoneEnd = 358;
    }

    [Serializable]
    public sealed class JourneySave
    {
        public int next;
        public int before = -1;
        public int after = -1;
        public int[] mistakes = new int[Lesson.All.Length];

        public static string Key(VehicleMode mode) => "JalanKita.Journey.v1." + mode;

        public static JourneySave Load(VehicleMode mode)
        {
            var data = PlayerPrefs.GetString(Key(mode), "");
            if (string.IsNullOrEmpty(data)) return null;
            JourneySave save;
            try { save = JsonUtility.FromJson<JourneySave>(data); }
            catch (ArgumentException) { return null; }
            return save != null && save.next >= 0 && save.next <= RouteStage.All.Length
                && save.mistakes?.Length == Lesson.All.Length ? save : null;
        }

        public void Store(VehicleMode mode)
        {
            PlayerPrefs.SetString(Key(mode), JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }

    public sealed class Journey
    {
        public VehicleMode Mode { get; }
        public JourneySave Save { get; }
        public Vector2 Position { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public int Indicator { get; private set; }
        public Rule? Failure { get; private set; }
        public bool DriverGear, PassengerGear, Headlight;
        public bool PhoneRinging { get; private set; }
        public int Stage => Save.next;
        public bool Finished => Stage == RouteStage.All.Length;
        public float Kph => Speed * 3.6f;
        public float Front => Position.y + Mathf.Cos(Heading * Mathf.Deg2Rad) * (Mode == VehicleMode.Car ? 1.95f : 1f);

        public Vector2? CrossTraffic { get; private set; }
        public int CrossDirection { get; private set; }
        public Vector2? Pedestrian { get; private set; }
        public int PedestrianDirection { get; private set; }
        public Vector2? LaneTraffic { get; private set; }
        public Vector2? SpurTraffic { get; private set; }

        public bool FullStop { get; private set; }
        public bool ChangedRight { get; private set; }
        public bool ReturnedLeft { get; private set; }
        public bool CrossedZebra => Front > RouteStage.Zebra + 2;
        public int LightPhase => lightTime < 0 || lightTime % 22 < 8 ? 0 : lightTime % 22 < 19 ? 2 : 1;

        float stopTime, lightTime, zebraTime, priorityTime, laneTime, wrongWay, overSpeed;
        float rightSignalAge, leftSignalAge;
        int lane;
        bool phoneResolved;
        readonly bool persistent;

        public Journey(VehicleMode mode, JourneySave save, bool persistent = true)
        {
            Mode = mode; Save = save;
            this.persistent = persistent;
            Retry();
        }

        public void Retry()
        {
            Position = new Vector2(RouteStage.LeftLane, Stage == 0 ? RouteStage.All[0].End : RouteStage.All[Stage - 1].End);
            Heading = Speed = wrongWay = overSpeed = 0;
            Indicator = lane = 0;
            rightSignalAge = leftSignalAge = 0;
            Failure = null;
            DriverGear = PassengerGear = Headlight = Stage > 0;
            FullStop = ChangedRight = ReturnedLeft = phoneResolved = PhoneRinging = false;
            stopTime = lightTime = zebraTime = priorityTime = laneTime = -1;
            CrossTraffic = Pedestrian = LaneTraffic = SpurTraffic = null;
        }

        public bool Depart()
        {
            if (Stage != 0 || Failure.HasValue) return false;
            if (!DriverGear || !PassengerGear)
                Fail(Mode == VehicleMode.Car ? Rule.SeatBelt : Rule.Helmet);
            else if (Mode == VehicleMode.Motorcycle && !Headlight)
                Fail(Rule.Headlight);
            else
            {
                Advance();
                return true;
            }
            return false;
        }

        public void ToggleIndicator(int direction)
        {
            Indicator = Indicator == direction ? 0 : direction;
            rightSignalAge = leftSignalAge = 0;
        }

        public void ToggleHeadlight() => Headlight = !Headlight;

        public void ResolvePhone(bool answer)
        {
            if (Stage != 4 || !PhoneRinging || Failure.HasValue) return;
            if (answer)
            {
                if (Speed > .28f || Position.x > -4.25f || Position.y < RouteStage.PhoneStart + 4)
                {
                    Fail(Rule.Phone);
                    return;
                }
            }
            PhoneRinging = false;
            phoneResolved = true;
        }

        public void Step(float dt, bool throttle, bool brake, float steer)
        {
            if (Stage == 0 || Finished || Failure.HasValue) return;

            float oldFront = Front;
            float targetSpeed = Mode == VehicleMode.Car ? 16.1f : 16.7f;
            float acceleration = Mode == VehicleMode.Car ? 2.85f : 3.3f;
            Speed = brake ? Mathf.MoveTowards(Speed, 0, 10.5f * dt)
                : Mathf.MoveTowards(Speed, throttle ? targetSpeed : 0, dt * (throttle ? acceleration : 1.7f));
            float steerLimit = Speed > .15f ? Mathf.Lerp(37, 20, Speed / targetSpeed) : 0;
            Heading = Mathf.MoveTowards(Heading, steer * steerLimit, (steer == 0 ? 110 : 84) * dt);
            Position += new Vector2(Mathf.Sin(Heading * Mathf.Deg2Rad), Mathf.Cos(Heading * Mathf.Deg2Rad)) * (Speed * dt);
            rightSignalAge = Indicator == 1 ? rightSignalAge + dt : 0;
            leftSignalAge = Indicator == -1 ? leftSignalAge + dt : 0;
            UpdateActors(dt);

            float z = Position.y;
            float x = Position.x;
            bool oneWay = z >= RouteStage.OneWayStart && z <= RouteStage.Merge;
            bool spur = z >= RouteStage.SpurStart && z <= RouteStage.SpurEnd;
            if (spur && x > 5.25f) Fail(Rule.NoEntry);
            bool bay = z >= RouteStage.PhoneStart + 4 && z <= RouteStage.PhoneEnd;
            if (x < (bay ? -7.45f : -4.45f) || x > (spur ? 9.1f : 4.45f)) Fail(Rule.OffRoad);
            if (Mode == VehicleMode.Motorcycle && !Headlight && Kph >= 3) Fail(Rule.Headlight);
            if (!oneWay && x > .42f && Speed > 1)
            {
                wrongWay += dt;
                if (wrongWay > .6f) Fail(Rule.KeepLeft);
            }
            else wrongWay = 0;

            if (Stage == 1)
            {
                if (Front >= RouteStage.StopLine - 5 && Front <= RouteStage.StopLine && Kph <= 1) FullStop = true;
                if (Crosses(oldFront, RouteStage.StopLine) && !FullStop) Fail(Rule.Stop);
                CheckJunction(oldFront, RouteStage.StopJunction);
                if (Crosses(oldFront, RouteStage.LightLine) && LightPhase == 0) Fail(Rule.Signal);
                CheckJunction(oldFront, RouteStage.LightJunction, signalled: true);
            }
            if (Stage == 2)
            {
                if (Crosses(oldFront, RouteStage.Zebra - 2) && Pedestrian.HasValue) Fail(Rule.Pedestrian);
                CheckJunction(oldFront, RouteStage.MainJunction);
            }
            if (Stage == 3 && oneWay)
            {
                int currentLane = x > .32f ? 1 : x < -.32f ? 0 : lane;
                if (currentLane != lane)
                {
                    if ((currentLane == 1 ? rightSignalAge : leftSignalAge) < .28f) Fail(Rule.Indicator);
                    if (currentLane == 1 && LaneTraffic.HasValue)
                    {
                        float gap = LaneTraffic.Value.y - z;
                        float requiredGap = Mathf.Max(5, (gap >= 0 ? Speed - 5.1f : 5.1f - Speed) * 2 + 4);
                        if (Mathf.Abs(gap) < requiredGap) Fail(Rule.Gap);
                    }
                    if (currentLane == 1) ChangedRight = true;
                    else if (ChangedRight) ReturnedLeft = true;
                    lane = currentLane;
                }
                if (LaneTraffic.HasValue && Mathf.Abs(LaneTraffic.Value.y - z) < (Mode == VehicleMode.Car ? 3.9f : 3f)
                    && Mathf.Abs(LaneTraffic.Value.x - x) < (Mode == VehicleMode.Car ? 1.75f : 1.25f))
                    Fail(Rule.Collision);
            }
            if (Stage == 3 && z > RouteStage.Merge && (!ChangedRight || !ReturnedLeft || x >= 0))
                Fail(Rule.ReturnLeft);

            if (Stage == 4)
            {
                if (Front >= RouteStage.SchoolLimit && Kph > 30.75f)
                {
                    overSpeed += dt;
                    if (overSpeed >= .6f) Fail(Rule.Speed);
                }
                else overSpeed = 0;
                if (z >= RouteStage.PhoneStart && !phoneResolved) PhoneRinging = true;
                if (z >= RouteStage.PhoneEnd && PhoneRinging) ResolvePhone(false);
            }

            if (!Failure.HasValue && z >= RouteStage.All[Stage].End) Advance();
        }

        bool Crosses(float oldFront, float line) => oldFront < line && Front >= line;

        void CheckJunction(float oldFront, float center, bool signalled = false)
        {
            if (!CrossTraffic.HasValue || Front < center - 4.3f || Position.y > center + 5.5f) return;
            Vector2 traffic = CrossTraffic.Value;
            if (traffic.y != center) return;
            float distance = (Position.x - traffic.x) * CrossDirection;
            if (!signalled && Crosses(oldFront, center - 4.3f) && distance >= -4 && distance < 18) Fail(Rule.Junction);
            if (Mathf.Abs(traffic.x - Position.x) < 2.65f && Mathf.Abs(center - Position.y) < 2.75f)
                Fail(signalled ? Rule.Collision : Rule.Junction);
        }

        void UpdateActors(float dt)
        {
            float z = Position.y;
            if (Stage == 1)
            {
                Tick(ref stopTime, dt, z >= RouteStage.StopLine - 22);
                Tick(ref lightTime, dt, z >= RouteStage.LightLine - 22);
                CrossDirection = Save.mistakes[(int)Rule.Junction] % 2 == 0 ? 1 : -1;
                CrossTraffic = CrossingCar(stopTime, RouteStage.StopJunction, CrossDirection);
                if (!CrossTraffic.HasValue && lightTime >= 0 && LightPhase == 0)
                {
                    float phase = lightTime % 22;
                    CrossDirection = 1;
                    CrossTraffic = CrossingCar(phase - 1.5f, RouteStage.LightJunction, CrossDirection);
                }
            }
            else if (Stage == 2)
            {
                Tick(ref zebraTime, dt, z >= RouteStage.Zebra - 19);
                Tick(ref priorityTime, dt, z >= RouteStage.YieldLine - 20);
                PedestrianDirection = Save.mistakes[(int)Rule.Pedestrian] % 2 == 0 ? 1 : -1;
                float walkX = -5.6f + Mathf.Max(0, zebraTime - .65f) * 1.83f;
                Pedestrian = walkX < 5.6f ? new Vector2(walkX * PedestrianDirection, RouteStage.Zebra) : null;
                CrossDirection = Save.mistakes[(int)Rule.Junction] % 2 == 0 ? -1 : 1;
                CrossTraffic = CrossingCar(priorityTime, RouteStage.MainJunction, CrossDirection);
            }
            else if (Stage == 3)
            {
                Tick(ref laneTime, dt, z >= RouteStage.OneWayStart - 7);
                float actorZ = RouteStage.OneWayStart + 3 + Mathf.Max(0, laneTime) * 5.1f;
                LaneTraffic = actorZ < RouteStage.Merge + 2 ? new Vector2(RouteStage.RightLane, actorZ) : null;
                SpurTraffic = laneTime < 0 ? null : new Vector2(7.4f, RouteStage.SpurEnd - (laneTime * 5) % 26);
            }
        }

        static Vector2? CrossingCar(float timer, float center, int direction)
        {
            float x = -24 + timer * 6.2f;
            return timer >= 0 && x < 26 ? new Vector2(x * direction, center) : null;
        }

        static void Tick(ref float time, float dt, bool start)
        {
            if (time >= 0) time += dt;
            else if (start) time = 0;
        }

        void Fail(Rule rule)
        {
            if (Failure.HasValue) return;
            Failure = rule;
            Save.mistakes[(int)rule]++;
            if (persistent) Save.Store(Mode);
            Speed = 0;
        }

        void Advance()
        {
            Save.next++;
            if (persistent) Save.Store(Mode);
            CrossTraffic = Pedestrian = LaneTraffic = SpurTraffic = null;
        }
    }
}
