using System;
using UnityEngine;

namespace JalanKita
{
    public enum VehicleMode { Car, Motorcycle }

    public enum Rule
    {
        SeatBelt, Helmet, Headlight, Stop, Signal, Junction, Pedestrian, KeepLeft, NoEntry,
        Indicator, Gap, ReturnLeft, Speed, Phone, OffRoad, Collision, NoStopping, PickupPlace, PickupIncomplete,
        TurnSignal, MovementLane, RightArrow, StopBox, Railway, Emergency, BoulevardSpeed
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
                "Nyalakan E untuk sein kanan atau Q untuk sein kiri sebelum berpindah lajur atau bergerak ke samping.", "Pasal 112 ayat (2)"),
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
                "Jaga jarak dan rem lebih awal saat mendekati kendaraan lain.", "Tips keselamatan"),
            new("Berhenti di area larangan", "Rambu dilarang berhenti berlaku meskipun penumpang meminta dijemput di sana.",
                "Lanjutkan ke teluk jemput di sebelah kiri setelah area larangan.", "Pasal 118 huruf a"),
            new("Tempat jemput belum aman", "Dalam misi ini penumpang dijemput setelah kendaraan berhenti di teluk yang ditandai.",
                "Beri sein kiri, masuk teluk, berhenti di dekat penumpang, lalu tekan Jemput [F].", "Latihan"),
            new("Penumpang belum dijemput", "Tujuan perjalanan ini adalah menjemput penumpang lalu bergabung kembali dengan aman.",
                "Berhenti di teluk jemput, tekan Jemput [F], kemudian beri sein kanan dan tunggu celah aman.", "Latihan"),
            new("Berbelok tanpa lampu sein", "Sebelum berbelok, amati lalu lintas dan beri isyarat sesuai arah belokan.",
                "Nyalakan Q sebelum belok kiri atau E sebelum belok kanan. Tunggu pejalan kaki selesai menyeberang.", "Pasal 112 ayat (1)"),
            new("Lajur tidak sesuai arah", "Ikuti panah lajur dan jangan melintasi garis lajur utuh menjelang persimpangan.",
                "Pilih lajur sebelum marka utuh: kiri untuk belok kiri, tengah untuk lurus, kanan untuk belok kanan.", "Pasal 106 ayat (4) huruf a–b"),
            new("Panah kanan belum hijau", "Lampu hijau untuk lurus bukan izin untuk belok kanan pada persimpangan dengan isyarat terpisah.",
                "Berhenti di lajur kanan. Tunggu panah kanan hijau dan pastikan zebra cross kosong.", "Pasal 106 ayat (4) huruf c"),
            new("Mobil memasuki ruang henti motor", "Mobil melewati garis henti belakang saat lampu merah dan masuk kotak merah khusus motor.",
                "Mobil berhenti di garis sebelum kotak merah. Motor boleh maju ke garis depan, tetapi tetap menunggu isyarat yang berlaku.", "Pasal 106 ayat (4) huruf b–c"),
            new("Menerobos perlintasan kereta", "Kendaraan wajib berhenti saat peringatan berbunyi atau palang mulai menutup, dan mendahulukan kereta api.",
                "Berhenti sebelum garis henti. Tunggu kereta lewat, lampu peringatan padam, dan palang terbuka penuh.", "Pasal 114"),
            new("Menghalangi ambulans", "Ambulans yang membawa orang sakit mendapat hak utama; dalam latihan ini ambulans memakai lampu dan sirene.",
                "Amati posisi ambulans, nyalakan sein kiri, dan pindah ke lajur kiri saat aman. Jangan menghalangi atau mengejarnya.", "Pasal 134 huruf b dan Pasal 135 ayat (1)"),
            new("Melebihi batas 50 km/j", "Batas 50 km/j berlaku di ruas jalan besar ini, termasuk saat ambulans mendekat.",
                "Kurangi gas dan beri jalan dengan tenang. Jangan mempercepat kendaraan untuk mendahului ambulans.", "Pasal 106 ayat (4) huruf g dan Pasal 115 huruf a")
        };

        public bool HasSource => Citation.StartsWith("Pasal", StringComparison.Ordinal);
    }

    public sealed class RouteStage
    {
        public readonly string Title, Location, Instruction;
        public readonly float End, Heading;
        public readonly Vector2 Checkpoint;
        RouteStage(string title, string location, string instruction, float end)
        {
            Title = title; Location = location; Instruction = instruction; End = end;
            Checkpoint = new Vector2(LeftLane, end);
        }

        RouteStage(string title, string location, string instruction, Vector2 checkpoint, float heading = 0)
            : this(title, location, instruction, checkpoint.y)
        { Checkpoint = checkpoint; Heading = heading; }

        public static readonly RouteStage[] All =
        {
            new("Sebelum berangkat", "GARASI KENANGA", "Periksa perlengkapan pengemudi dan penumpang.", -12),
            new("Rambu & lampu", "JALAN KENANGA", "Berhenti penuh di garis STOP sebelum melanjutkan.", 108),
            new("Hak utama", "PASAR BUNGA", "Dahulukan pejalan kaki di zebra cross.", 204),
            new("Arah & lajur", "JALAN MAWAR", "Ikuti arah panah. Beri sein dan pindah ke lajur kanan saat aman.", 298),
            new("Kecepatan & fokus", "ZONA SEKOLAH", "Kurangi kecepatan sebelum rambu 30 km/j.", 382),
            new("Lurus di jalan besar", "JALAN THAMRIN", "Pilih lajur tengah sebelum marka utuh. Tunggu hijau dan pejalan kaki.", new Vector2(-5.4f, 550)),
            new("Belok kiri & pejalan kaki", "THAMRIN – DIPONEGORO", "Pilih lajur paling kiri, nyalakan Q, lalu tunggu hijau dan zebra kosong.", new Vector2(-70, 631), 270),
            new("Belok kanan terlindung", "DIPONEGORO – AHMAD YANI", "Pilih lajur kanan sebelum marka utuh. Nyalakan E dan tunggu panah kanan hijau.", new Vector2(-181.8f, 720)),
            new("Perlintasan kereta api", "PERLINTASAN AHMAD YANI", "Berhenti saat bel berbunyi. Tunggu kereta lewat dan palang terbuka penuh.", new Vector2(-185.4f, 850)),
            new("Beri jalan ambulans", "JALAN RUMAH SAKIT", "Lewati angkot di lajur tengah. Saat sirene terdengar, beri jalan ke kiri dengan sein.", new Vector2(-189, 1040))
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
        public int checkpoint, variant, attempts = 1, completions;
        public int trafficSeed;
        public int bestMistakes = -1;
        public bool retryPending;

        // Schema 2 fixes Rule IDs after two independently extended v1 builds.
        // Keep all v1 values untouched so either original build can still open its own save.
        public int schema;
        public int legacyAfter = -1;
        public static string Key(VehicleMode mode, Mission mission = null) => mission == null
            ? "JalanKita.Journey.v2." + mode : "JalanKita.Mission.v2." + mode + "." + mission.Id;
        public static string LegacyKey(VehicleMode mode, Mission mission = null) => mission == null
            ? "JalanKita.Journey.v1." + mode : "JalanKita.Mission.v1." + mode + "." + mission.Id;

        public static JourneySave Load(VehicleMode mode, Mission mission = null)
        {
            var data = PlayerPrefs.GetString(Key(mode, mission), "");
            bool legacy = string.IsNullOrEmpty(data);
            if (legacy) data = PlayerPrefs.GetString(LegacyKey(mode, mission), "");
            if (string.IsNullOrEmpty(data)) return null;
            JourneySave save;
            try { save = JsonUtility.FromJson<JourneySave>(data); }
            catch (ArgumentException) { return null; }
            if (save == null || save.next < (mission?.FirstStage ?? 0)
                || save.next > (mission == null ? RouteStage.All.Length : mission.LastStage + 1)
                || save.mistakes == null || save.checkpoint < 0 || save.checkpoint > 2) return null;
            int count = save.mistakes.Length;
            if (legacy)
            {
                if ((count < 16 || count > 19) && count != 23) return null;
                var mapped = new int[Lesson.All.Length];
                Array.Copy(save.mistakes, mapped, Mathf.Min(count, 16));
                if (count > 16 && count <= 19) Array.Copy(save.mistakes, 16, mapped, 16, count - 16);
                if (count == 23) Array.Copy(save.mistakes, 16, mapped, 19, 7);
                save.mistakes = mapped;
                // The original five-stage final quiz remains recorded; the expanded route has its own final quiz.
                if (mission == null && save.next < RouteStage.All.Length && save.after >= 0)
                { save.legacyAfter = save.after; save.after = -1; }
            }
            else if (save.schema != 2 || count != Lesson.All.Length) return null;
            save.attempts = Mathf.Max(1, save.attempts);
            save.schema = 2; // Reads migrate in memory; an explicit gameplay save writes the v2 key.
            return save;
        }

        public void Store(VehicleMode mode, Mission mission = null)
        {
            schema = 2;
            PlayerPrefs.SetString(Key(mode, mission), JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

    }

    public sealed partial class Journey
    {
        readonly VehicleSteering steering = new();
        public bool UsesRouteSteering => Activity == null;
        public float SteerAngle => UsesRouteSteering ? steering.Angle : motion.SteeringAngle * Mathf.Deg2Rad;
        public float Throttle { get; private set; }
        public VehicleMode Mode { get; }
        public JourneySave Save { get; }
        public Mission Activity { get; }
        public Vector2 Position { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public int Indicator { get; private set; }
        public Rule? Failure { get; private set; }
        public bool DriverGear, PassengerGear, Headlight;
        public bool PhoneRinging { get; private set; }
        public int Stage => Save.next;
        public bool Finished => Stage > (Activity?.LastStage ?? RouteStage.All.Length - 1);
        public bool IsSchool => Activity?.IsSchool == true;
        public bool IsMarket => Activity?.IsMarket == true;
        public bool PassengerPickedUp { get; private set; }
        public float ElapsedTime { get; private set; }
        public float BoardingProgress => PassengerPickedUp ? Mathf.Clamp01(1 - boardingTime / 2.2f) : 0;
        public bool IsBoarding => boardingTime > 0;
        public bool CanPickup => IsMarket && !PassengerPickedUp && Speed < .28f
            && Position.x < -5 && Position.y >= Mission.PickupPoint - 5 && Position.y <= Mission.PickupPoint + 4;
        public int MistakeCount { get { int count = 0; foreach (int n in Save.mistakes) count += n; return count; } }
        public float StageEnd => Stage == Activity?.LastStage ? Activity.Finish : RouteStage.All[Mathf.Min(Stage, RouteStage.All.Length - 1)].End;
        public float CrossingZebra => IsSchool ? Mission.SchoolZebra : RouteStage.Zebra;
        public Vector2? PickupTraffic { get; private set; }
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
        public bool CrossedZebra => Front > CrossingZebra + 2;
        public int LightPhase => lightTime < 0 || lightTime % 22 < 8 ? 0 : lightTime % 22 < 19 ? 2 : 1;

        float stopTime, lightTime, zebraTime, priorityTime, laneTime, wrongWay, overSpeed;
        float rightSignalAge, leftSignalAge, stoppedInRestriction, pickupTime, boardingTime;
        int lane;
        bool phoneResolved;
        readonly bool persistent;
        readonly RoadVehicleMotion motion = new();
        readonly int trafficSeed;
        public TrafficRoster Traffic { get; private set; }

        public Journey(VehicleMode mode, JourneySave save, bool persistent = true, Mission mission = null, int? trafficSeed = null)
        {
            Mode = mode; Save = save; Activity = mission;
            this.persistent = persistent;
            // Cosmetic randomness is saved once: retry, reload and demonstration share a roster.
            bool newTrafficSeed=!trafficSeed.HasValue&&save.trafficSeed==0;
            if(newTrafficSeed)save.trafficSeed=UnityEngine.Random.Range(1,int.MaxValue);
            this.trafficSeed = trafficSeed ?? save.trafficSeed;
            if(newTrafficSeed)Persist();
            if (Activity != null && Save.retryPending)
            {
                Save.attempts++;
                Save.variant++;
                Save.retryPending = false;
                Persist();
            }
            ResetEncounter();
        }

        public void Retry()
        {
            Save.retryPending = false;
            if (Activity != null)
            {
                Save.attempts++;
                Save.variant++;
                Persist();
            }
            ResetEncounter();
        }

        void ResetEncounter()
        {
            Traffic = new TrafficRoster(unchecked(trafficSeed + Save.variant * 7919));
            var checkpoint = RouteStage.All[Mathf.Clamp(Stage - 1, 0, RouteStage.All.Length - 1)];
            Position = checkpoint.Checkpoint;
            Heading = checkpoint.Heading;
            if (!Finished && IsSchool && Save.checkpoint > 0)
                Position = new Vector2(RouteStage.LeftLane, Save.checkpoint == 1 ? 313 : Mission.SchoolPhoneCheckpoint);
            if (!Finished && IsMarket && Save.checkpoint > 0)
                Position = Save.checkpoint == 1 ? new Vector2(RouteStage.LeftLane, 246) : new Vector2(-5.8f, Mission.PickupPoint);
            Speed = wrongWay = overSpeed = Throttle = 0;
            motion.Reset();
            steering.Reset();
            ResetBoulevard();
            Indicator = lane = 0;
            rightSignalAge = leftSignalAge = 0;
            Failure = null;
            DriverGear = PassengerGear = Headlight = Stage > 0;
            FullStop = ChangedRight = ReturnedLeft = phoneResolved = PhoneRinging = false;
            stopTime = lightTime = zebraTime = priorityTime = laneTime = -1;
            stoppedInRestriction = 0;
            pickupTime = -1;
            PassengerPickedUp = IsMarket && Save.checkpoint >= 2;
            boardingTime = ElapsedTime = 0;
            if (PassengerPickedUp) pickupTime = 0;
            CrossTraffic = Pedestrian = LaneTraffic = SpurTraffic = PickupTraffic = null;
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

        public void Pickup()
        {
            if (!IsMarket || Finished || Failure.HasValue || PassengerPickedUp) return;
            if (Position.y >= Mission.NoStopStart && Position.y <= Mission.NoStopEnd) Fail(Rule.NoStopping);
            else if (!CanPickup) Fail(Rule.PickupPlace);
            else
            {
                PassengerPickedUp = true;
                boardingTime = 2.2f;
                Speed = 0;
                pickupTime = 0;
                Checkpoint(2);
            }
        }

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
            ElapsedTime += dt;
            Throttle = throttle && !brake ? 1 : 0;
            // Boarding is a simulation state: the vehicle stays stopped while the passenger approaches.
            if (boardingTime > 0)
            {
                boardingTime = Mathf.Max(0, boardingTime - dt);
                Speed = 0;
                UpdateActors(dt);
                return;
            }

            if (Stage >= 5) { StepBoulevard(dt, throttle, brake, steer); return; }

            float oldFront = Front;
            float oldX = Position.x;
            float targetSpeed = Mode == VehicleMode.Car ? 16.1f : 16.7f;
            float speed = Speed, heading = Heading;
            if (UsesRouteSteering)
            {
                speed = brake ? Mathf.MoveTowards(speed, 0, 10.5f * dt)
                    : Mathf.MoveTowards(speed, throttle ? targetSpeed : 0, dt * (throttle ? (Mode == VehicleMode.Car ? 2.85f : 3.3f) : 1.7f));
                heading = steering.Step(Mode, speed, heading, steer, dt);
            }
            else motion.Step(dt, Mode, throttle, brake, steer, targetSpeed, ref speed, ref heading);
            Speed = speed; Heading = heading;
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
            bay |= IsMarket && z >= Mission.PickupStart && z <= Mission.PickupEnd;
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
                if (LaneTraffic.HasValue && TrafficCollision(Traffic.Lane, LaneTraffic.Value, 0))
                    Fail(Rule.Collision);
            }
            if (Stage == 3 && !IsMarket && z > RouteStage.Merge && (!ChangedRight || !ReturnedLeft || x >= 0))
                Fail(Rule.ReturnLeft);

            if (IsMarket)
            {
                bool enteringBay = oldX >= -4.25f && x < -4.25f;
                bool leavingBay = oldX < -4.25f && x >= -4.25f;
                if (enteringBay && leftSignalAge < .28f || leavingBay && rightSignalAge < .28f) Fail(Rule.Indicator);
                if (leavingBay && PickupTraffic.HasValue && Mathf.Abs(PickupTraffic.Value.y - z) < 11) Fail(Rule.Gap);
                if (PickupTraffic.HasValue && TrafficCollision(Traffic.Pickup, PickupTraffic.Value, 0))
                    // A rotated nose can leave the bay before its center crosses the boundary.
                    // Preserve the unsafe-rejoin lesson when that nose meets passing traffic.
                    Fail(PassengerPickedUp && x < -4.25f && Heading > 0 ? Rule.Gap : Rule.Collision);
                if (z >= Mission.NoStopStart && z <= Mission.NoStopEnd && Speed < .28f)
                {
                    stoppedInRestriction += dt;
                    if (stoppedInRestriction > 2) Fail(Rule.NoStopping);
                }
                else stoppedInRestriction = 0;
                if (z >= 246) Checkpoint(1);
                if (z > Mission.PickupEnd && !PassengerPickedUp) Fail(Rule.PickupIncomplete);
            }

            if (Stage == 4)
            {
                if (Front >= RouteStage.SchoolLimit && Kph > 30.75f)
                {
                    overSpeed += dt;
                    if (overSpeed >= .6f) Fail(Rule.Speed);
                }
                else overSpeed = 0;
                if (IsSchool)
                {
                    if (Crosses(oldFront, CrossingZebra - 2) && Pedestrian.HasValue) Fail(Rule.Pedestrian);
                    if (z >= 313) Checkpoint(1);
                    if (z >= Mission.SchoolPhoneCheckpoint) Checkpoint(2);
                }
                if (z >= RouteStage.PhoneStart && !phoneResolved) PhoneRinging = true;
                if (z >= RouteStage.PhoneEnd && PhoneRinging) ResolvePhone(false);
            }

            if (!Failure.HasValue && z >= StageEnd) Advance();
        }

        bool Crosses(float oldFront, float line) => oldFront < line && Front >= line;

        void CheckJunction(float oldFront, float center, bool signalled = false)
        {
            if (!CrossTraffic.HasValue || Front < center - 4.3f || Position.y > center + 5.5f) return;
            Vector2 traffic = CrossTraffic.Value;
            if (traffic.y != center) return;
            float distance = (Position.x - traffic.x) * CrossDirection;
            if (!signalled && Crosses(oldFront, center - 4.3f) && distance >= -4 && distance < 18) Fail(Rule.Junction);
            if (TrafficCollision(Traffic.Cross, traffic, CrossDirection * 90))
                Fail(signalled ? Rule.Collision : Rule.Junction);
        }

        public bool TrafficCollision(TrafficChoice choice, Vector2 position, float yaw) =>
            TrafficFootprint.Overlaps(Position, Heading, TrafficFootprint.PlayerHalfWidth(Mode), TrafficFootprint.PlayerHalfLength(Mode),
                position, yaw, choice.Vehicle.HalfWidth, choice.Vehicle.HalfLength);

        void UpdateActors(float dt)
        {
            float z = Position.y;
            if (Stage == 1)
            {
                Tick(ref stopTime, dt, z >= RouteStage.StopLine - 22);
                Tick(ref lightTime, dt, z >= RouteStage.LightLine - 22);
                CrossDirection = (Activity == null ? Save.mistakes[(int)Rule.Junction] : Save.variant) % 2 == 0 ? 1 : -1;
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
                if (IsMarket && PassengerPickedUp)
                {
                    Tick(ref pickupTime, dt, true);
                    float trafficZ = Mission.PickupPoint - 22 + pickupTime * (Save.variant % 2 == 0 ? 5.1f : 4.6f);
                    PickupTraffic = trafficZ < RouteStage.Merge + 12 ? new Vector2(RouteStage.LeftLane, trafficZ) : null;
                }
            }
            else if (IsSchool && Save.checkpoint < 2)
            {
                Tick(ref zebraTime, dt, z >= 307);
                PedestrianDirection = Save.variant % 2 == 0 ? 1 : -1;
                float walkX = -5.6f + Mathf.Max(0, zebraTime - .65f) * 1.5f;
                Pedestrian = zebraTime >= 0 && walkX < 5.6f
                    ? new Vector2(walkX * PedestrianDirection, CrossingZebra) : null;
            }
            else if (IsSchool) Pedestrian = null;
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
            Save.retryPending = true;
            Save.mistakes[(int)rule]++;
            Persist();
            Speed = 0;
        }

        void Advance()
        {
            Save.next++;
            ResetBoulevard();
            Save.checkpoint = 0;
            if (Activity != null && Finished)
            {
                Save.completions++;
                Save.bestMistakes = Save.bestMistakes < 0 ? MistakeCount : Mathf.Min(Save.bestMistakes, MistakeCount);
            }
            Persist();
            CrossTraffic = Pedestrian = LaneTraffic = SpurTraffic = PickupTraffic = null;
        }

        void Checkpoint(int index)
        {
            if (Failure.HasValue || index <= Save.checkpoint) return;
            Save.checkpoint = index;
            Persist();
        }

        void Persist() { if (persistent) Save.Store(Mode, Activity); }
    }
}
