using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static JalanKita.RouteStage;

namespace JalanKita
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class JalanGame : MonoBehaviour
    {
        enum Page { Menu, Before, Preflight, Drive, Lesson, Progress, After, Jaksa, JaksaPause, Catalogue, Brief }

        District district;
        GameObject trainingRoot;
        JaksaDrive jaksa;
        Journey journey;
        Journey learner;
        JourneySave stored;
        VehicleMode mode;
        Page page;
        bool confirmNew, hood;
        float toastUntil;
        int quizIndex, quizScore;
        VisualElement root, hud, dialog, overlay, phone;
        Label location, title, instruction, detail, speed, vehicle, routeCount, toast;
        Button progress;
        readonly Label[] routeSteps = new Label[All.Length];

        void Start()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            trainingRoot = new GameObject("Kampung Kenanga training");
            trainingRoot.transform.SetParent(transform, false);
            district = new District(trainingRoot.transform);
            root = GetComponent<UIDocument>().rootVisualElement;
            root.AddToClassList("jalan-root");
            root.pickingMode = PickingMode.Ignore;
            hud = root.Q("hud");
            dialog = root.Q("dialog");
            overlay = root.Q("overlay");
            phone = root.Q("phone");
            location = root.Q<Label>("district");
            title = root.Q<Label>("objectiveTitle");
            instruction = root.Q<Label>("instruction");
            detail = root.Q<Label>("detail");
            speed = root.Q<Label>("speed");
            vehicle = root.Q<Label>("vehicleState");
            routeCount = root.Q<Label>("routeCount");
            toast = root.Q<Label>("toast");
            for (int i = 0; i < routeSteps.Length; i++)
                routeSteps[i] = AddLabel(root.Q("steps"), (i+1).ToString("00"), "step");

            progress = root.Q<Button>("progress");
            progress.clicked += () => { if (page == Page.Jaksa) ShowJaksaPause(); else if (learner != null) EndDemonstration(); else ShowProgress(); };
            root.Q<Button>("ignorePhone").clicked += () => ResolvePhone(false);
            root.Q<Button>("answerPhone").clicked += () => ResolvePhone(true);
            root.Q<Button>("pickupPassenger").clicked += PickupPassenger;
            ShowMenu();
        }

        void Update()
        {
            if (page == Page.Jaksa)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) { ShowJaksaPause(); return; }
                if (Input.GetKeyDown(KeyCode.C)) jaksa.ToggleCamera();
                if (Input.GetKeyDown(KeyCode.Q)) jaksa.ToggleIndicator(-1);
                if (Input.GetKeyDown(KeyCode.E)) jaksa.ToggleIndicator(1);
                if (Input.GetKeyDown(KeyCode.L) && mode == VehicleMode.Motorcycle) jaksa.ToggleHeadlight();
                if (Input.GetKeyDown(KeyCode.R)) jaksa.Reset(mode);
                return;
            }
            if (page == Page.JaksaPause)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) SetPage(Page.Jaksa);
                return;
            }
            if (learner != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.R))
                    EndDemonstration();
                return;
            }
            if (page == Page.Drive)
            {
                if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape)) { ShowProgress(); return; }
                if (Input.GetKeyDown(KeyCode.R)) Retry();
                if (Input.GetKeyDown(KeyCode.C)) { hood = !hood; district.Snap(); }
                if (Input.GetKeyDown(KeyCode.Q)) journey.ToggleIndicator(-1);
                if (Input.GetKeyDown(KeyCode.E)) journey.ToggleIndicator(1);
                if (Input.GetKeyDown(KeyCode.L) && mode == VehicleMode.Motorcycle) journey.ToggleHeadlight();
                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (journey.IsMarket) PickupPassenger();
                    else ResolvePhone(false);
                }
                if (Input.GetKeyDown(KeyCode.G)) ResolvePhone(true);
            }
            else if (page == Page.Lesson && Input.GetKeyDown(KeyCode.R)) Retry();
            else if (page == Page.Progress && !journey.Finished &&
                Input.GetKeyDown(KeyCode.Escape)) ReturnToJourney();
        }

        void FixedUpdate()
        {
            if (page == Page.Jaksa)
            {
                float turn = (Pressed(KeyCode.D, KeyCode.RightArrow) ? 1 : 0) - (Pressed(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
                jaksa.Step(Time.fixedDeltaTime, Pressed(KeyCode.W, KeyCode.UpArrow),
                    Pressed(KeyCode.S, KeyCode.DownArrow) || Input.GetKey(KeyCode.Space), turn);
                return;
            }
            if (page != Page.Drive) return;
            int priorStage = journey.Stage;
            int priorCheckpoint = journey.Save.checkpoint;
            if (learner != null)
            {
                if (journey.PhoneRinging)
                {
                    toast.text = "PANGGILAN DIABAIKAN · TETAP FOKUS";
                    toastUntil = Time.unscaledTime + 3;
                }
                DrivingCoach.Step(journey, Time.fixedDeltaTime);
                if (journey.Stage != priorStage || journey.Failure.HasValue) EndDemonstration();
                return;
            }
            float steer = (Pressed(KeyCode.D, KeyCode.RightArrow) ? 1 : 0)
                - (Pressed(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
            journey.Step(Time.fixedDeltaTime, Pressed(KeyCode.W, KeyCode.UpArrow),
                Pressed(KeyCode.S, KeyCode.DownArrow) || Input.GetKey(KeyCode.Space), steer);
            if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
            else if (journey.Finished)
            {
                if (journey.Activity == null) StartQuiz(true);
                else ShowProgress();
            }
            else if (journey.Stage != priorStage || journey.Save.checkpoint != priorCheckpoint) AnnounceCheckpoint();
        }

        void LateUpdate()
        {
            if (page == Page.Jaksa || page == Page.JaksaPause)
            {
                jaksa.Present(Time.unscaledDeltaTime);
                if (page == Page.Jaksa) RefreshJaksaHud();
                return;
            }
            if (journey == null) return;
            district.Present(journey, hood, Time.unscaledDeltaTime);
            if (page == Page.Drive) RefreshHud();
        }

        static bool Pressed(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);

        void ShowMenu()
        {
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            trainingRoot.SetActive(true);
            district.RestoreAtmosphere();
            progress.text = "Kemajuan  [Tab]";
            root.Q<Label>(className: "controls").text = "W / ↑ Gas    S / Spasi Rem    A / D Setir    Q / E Sein    C Kamera    R Ulang";
            Show(root.Q("steps"), true);
            stored = JourneySave.Load(mode);
            journey = new Journey(mode, new JourneySave());
            hood = false;
            district.Snap();
            SetPage(Page.Menu);
            AddLabel(dialog, "SIMULASI KESELAMATAN JALAN  /  INDONESIA", "kicker");
            AddLabel(dialog, "Jalan Kita", "hero");
            AddLabel(dialog, "Uji keterampilan, selesaikan misi sehari-hari di Kampung Kenanga, atau jelajahi area kantor Sea di Mega Kuningan.", "copy");

            var modes = Element(dialog, "row section");
            AddButton(modes, "Mobil", () => SelectMode(VehicleMode.Car), mode == VehicleMode.Car ? "selected" : "quiet");
            AddButton(modes, "Motor", () => SelectMode(VehicleMode.Motorcycle), mode == VehicleMode.Motorcycle ? "selected" : "quiet");
            AddVehiclePicker();
            var activities = Element(dialog, "section");
            AddButton(activities, "Uji keterampilan  ·  5 tes singkat", () => ShowCatalogue(MissionKind.SkillTest), "option primary");
            AddButton(activities, "Misi sehari-hari  ·  3 perjalanan", () => ShowCatalogue(MissionKind.Scenario), "option primary");
            AddLabel(dialog, "JELAJAH & PERJALANAN LENGKAP", "kicker section");
            var actions = Element(dialog, "row actions");
            AddButton(actions, "Jelajahi kantor Sea · Sopo Del", StartJaksa, "primary");
            if (stored == null)
                AddButton(actions, "Mulai perjalanan", StartNew, "primary");
            else
            {
                string resume = stored.next == All.Length && stored.after >= 0 ? "Lihat hasil"
                    : stored.next > 0 ? "Lanjutkan perjalanan" : "Lanjutkan persiapan";
                AddButton(actions, resume, Resume, "primary");
                AddButton(actions, confirmNew ? "Ya, mulai dari awal" : "Mulai dari awal", () =>
                {
                    if (confirmNew) StartNew();
                    else { confirmNew = true; ShowMenu(); }
                }, "quiet");
                if (confirmNew) AddLabel(dialog, "Ini mengganti progres yang tersimpan untuk kendaraan ini.", "copy");
            }
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                AddButton(actions, "Keluar", Application.Quit, "quiet");
        }

        void StartJaksa()
        {
            trainingRoot.SetActive(false);
            jaksa ??= new JaksaDrive(transform);
            jaksa.Reset(mode);
            progress.text = "Jeda / menu  [Esc]";
            root.Q<Label>(className: "controls").text = "W / ↑ Gas    S / Spasi Rem    A / D Setir    Q / E Sein    L Lampu motor    C Kamera    Esc Jeda";
            SetPage(Page.Jaksa);
            Show(root.Q("steps"), false);
            Show(phone, false); Show(toast, false);
            Show(root.Q("pickup"), false);
        }

        void RefreshJaksaHud()
        {
            location.text = "MEGA KUNINGAN  /  JAKARTA SELATAN";
            title.text = jaksa.AtEnd ? "Selesai menjelajahi Sopo Del" : "Sea Labs Indonesia · Sopo Del";
            instruction.text = jaksa.AtEnd ? "Tekan R untuk mengulang, atau Esc untuk kembali ke menu."
                : jaksa.CurbContact ? "Tepi trotoar. Arahkan kendaraan kembali ke jalan."
                : jaksa.TrafficContact ? "Jaga jarak. Tunggu ruang aman di depan kendaraan."
                : "W / ↑ gas · S / ↓ / Spasi rem · A / D kemudi · C kamera";
            detail.text = "MEGA KUNINGAN BARAT III · " + Mathf.RoundToInt(Mathf.Max(0,246-jaksa.Position.y)) + " M RUAS TERSISA";
            routeCount.text = "SOPO DEL · SEKITAR 250 M";
            speed.text = Mathf.RoundToInt(jaksa.Speed*3.6f).ToString("00");
            speed.EnableInClassList("speed-alert", false);
            vehicle.text = jaksa.Indicator < 0 ? "‹  SEIN KIRI" : jaksa.Indicator > 0 ? "SEIN KANAN  ›"
                : mode == VehicleMode.Car ? "MOBIL" : jaksa.Headlight ? "MOTOR · LAMPU NYALA" : "MOTOR · LAMPU MATI";
        }

        void ShowJaksaPause()
        {
            SetPage(Page.JaksaPause);
            AddLabel(dialog, "SOPO DEL  /  MEGA KUNINGAN", "kicker");
            AddLabel(dialog, "Jeda perjalanan", "heading");
            AddLabel(dialog, "Jelajahi sekitar 250 meter Jl. Mega Kuningan Barat III di depan Sopo Del, alamat kantor Sea Labs Indonesia. Jalan dan tapak bangunan mengikuti peta; fasad dibuat dari referensi publik. Ukuran detail kecil, pohon, dan furnitur jalan merupakan perkiraan.", "copy");
            var actions=Element(dialog,"row actions");
            AddButton(actions,"Lanjutkan",()=>SetPage(Page.Jaksa),"primary");
            AddButton(actions,"Ulangi ruas",()=>{jaksa.Reset(mode);SetPage(Page.Jaksa);},"quiet");
            AddButton(actions,"Menu utama",ShowMenu,"quiet");
            AddLabel(dialog,"Data peta © OpenStreetMap contributors · ODbL. Material permukaan: Poly Haven · CC0.","rule-source");
        }

        void SelectMode(VehicleMode value)
        {
            mode = value;
            confirmNew = false;
            ShowMenu();
        }

        void StartNew()
        {
            confirmNew = false;
            journey = new Journey(mode, new JourneySave());
            StartQuiz(false);
        }

        void Resume()
        {
            journey = new Journey(mode, stored);
            district.Snap();
            if (journey.Finished)
            {
                if (stored.after < 0) StartQuiz(true);
                else ShowProgress();
            }
            else if (journey.Stage == 0)
            {
                if (stored.before < 0) StartQuiz(false);
                else ShowPreflight();
            }
            else SetPage(Page.Drive);
        }

        void ShowPreflight()
        {
            SetPage(Page.Preflight);
            bool motor = mode == VehicleMode.Motorcycle;
            AddLabel(dialog, "01  /  GARASI KENANGA", "kicker");
            AddLabel(dialog, "Sebelum berangkat", "heading");
            if (journey.Activity != null) AddLabel(dialog, journey.Activity.Title, "kicker section");
            AddLabel(dialog, motor ? "Siapkan dua helm SNI dan nyalakan lampu utama."
                : "Periksa sabuk pengemudi dan penumpang depan.", "copy");
            var checklist = Element(dialog, "section");
            Equipment(checklist, motor ? "Helm SNI pengendara" : "Sabuk pengemudi", journey.DriverGear, () => journey.DriverGear = !journey.DriverGear);
            Equipment(checklist, motor ? "Helm SNI penumpang" : "Sabuk penumpang depan", journey.PassengerGear, () => journey.PassengerGear = !journey.PassengerGear);
            if (motor) Equipment(checklist, "Lampu utama", journey.Headlight, journey.ToggleHeadlight);
            var actions = Element(dialog, "row actions");
            AddButton(actions, "Mulai jalan", () =>
            {
                if (journey.Depart())
                {
                    if (journey.Finished) ShowProgress();
                    else { SetPage(Page.Drive); AnnounceCheckpoint(); }
                }
                else if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
            }, "primary");
            AddButton(actions, "Menu", BackToMenu, "quiet");
        }

        void Equipment(VisualElement group, string label, bool ready, Action change)
        {
            AddButton(group, (ready ? "[OK]  " : "[  ]  ") + label, () => { change(); ShowPreflight(); },
                ready ? "option selected" : "option");
        }

        void ShowLesson(Rule rule)
        {
            SetPage(Page.Lesson);
            dialog.AddToClassList("rule-card");
            var lesson = Lesson.All[(int)rule];
            AddLabel(dialog, journey.Activity == null ? "LATIHAN " + (journey.Stage + 1).ToString("00") + "  /  COBA LAGI"
                : journey.Activity.Kind == MissionKind.SkillTest ? "PERCOBAAN BELUM LULUS" : "MISI  /  COBA BAGIAN INI LAGI", "kicker negative");
            AddLabel(dialog, lesson.Title, "heading");
            AddLabel(dialog, lesson.Explanation, "copy");
            var note = Element(dialog, "note");
            AddLabel(note, "CARA MELAKUKANNYA", "kicker");
            AddLabel(note, lesson.Action, "copy");
            if (lesson.HasSource) AddLabel(dialog, lesson.Regulation + " · " + lesson.Citation, "rule-source");
            var actions = Element(dialog, "row actions");
            AddButton(actions, journey.Stage == 0 ? "Ulangi persiapan  [R]"
                : journey.Activity?.Kind == MissionKind.SkillTest ? "Ulangi tes  [R]" : "Ulangi dari checkpoint  [R]", Retry, "primary");
            if (journey.Stage > 0) AddButton(actions, "Lihat peragaan", StartDemonstration, "quiet");
            if (lesson.HasSource) AddButton(actions, "Buka sumber resmi", () => Application.OpenURL(lesson.Url), "quiet");
            if (rule == Rule.Stop) AddButton(actions, "Peraturan rambu", () => Application.OpenURL(Lesson.Signs), "quiet");
            if (rule == Rule.Signal) AddButton(actions, "Peraturan lampu", () => Application.OpenURL(Lesson.Signals), "quiet");
            if (rule == Rule.Speed) AddButton(actions, "Peraturan kecepatan", () => Application.OpenURL(Lesson.Limits), "quiet");
            AddButton(actions, "Menu", BackToMenu, "quiet");
        }

        void StartDemonstration()
        {
            learner = journey;
            journey = new Journey(mode, new JourneySave
            {
                next = learner.Stage,
                checkpoint = learner.Save.checkpoint,
                variant = learner.Save.variant,
                trafficSeed = learner.Save.trafficSeed,
                mistakes = (int[])learner.Save.mistakes.Clone()
            }, persistent: false, mission: learner.Activity);
            district.Snap();
            SetPage(Page.Drive);
            progress.text = "Akhiri peragaan  [Tab]";
            toast.text = "PERAGAAN · IKUTI PETUNJUK DI LAYAR";
            toastUntil = Time.unscaledTime + 3;
        }

        void EndDemonstration()
        {
            journey = learner;
            learner = null;
            progress.text = "Kemajuan  [Tab]";
            district.Snap();
            ShowLesson(journey.Failure.Value);
        }

        void Retry()
        {
            journey.Retry();
            district.Snap();
            if (journey.Stage == 0) ShowPreflight();
            else SetPage(Page.Drive);
        }

        void ResolvePhone(bool answer)
        {
            if (page != Page.Drive || learner != null) return;
            journey.ResolvePhone(answer);
            if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
        }

        void BackToMenu()
        {
            confirmNew = false;
            ShowMenu();
        }

        void ShowProgress()
        {
            if (page == Page.Menu) return;
            if (journey.Activity != null) { ShowMissionProgress(); return; }
            SetPage(Page.Progress);
            AddLabel(dialog, journey.Finished ? "PERJALANAN SELESAI" : "JURNAL PERJALANAN", "kicker");
            AddLabel(dialog, journey.Finished ? "Tiba dengan selamat" : "Kemajuan latihan", "heading");
            AddLabel(dialog, journey.Stage + " dari " + All.Length + " checkpoint tersimpan  ·  "
                + (mode == VehicleMode.Car ? "Mobil" : "Motor"), "copy");
            if (journey.Save.before >= 0)
                AddLabel(dialog, "Kuis awal  " + journey.Save.before + "/3"
                    + (journey.Save.after >= 0 ? "     Kuis akhir  " + journey.Save.after + "/3" : ""), "copy");

            var frequent = Enumerable.Range(0, Lesson.All.Length)
                .Where(i => journey.Save.mistakes[i] > 0)
                .OrderByDescending(i => journey.Save.mistakes[i]).Take(3).ToArray();
            var box = Element(dialog, "note");
            AddLabel(box, frequent.Length > 0 ? "PALING SERING DIULANG" : "CATATAN LATIHAN", "kicker");
            if (frequent.Length == 0) AddLabel(box, "Belum ada aturan yang perlu diulang.", "copy");
            foreach (int i in frequent)
                AddLabel(box, journey.Save.mistakes[i] + "×   " + Lesson.All[i].Title, "copy");
            if (frequent.Length > 0) AddLabel(dialog, "Saran latihan berikutnya · " + Lesson.All[frequent[0]].Action, "copy");

            var actions = Element(dialog, "row actions");
            if (!journey.Finished) AddButton(actions, "Kembali berkendara", ReturnToJourney, "primary");
            AddButton(actions, "Menu utama", BackToMenu, journey.Finished ? "primary" : "quiet");
        }

        void ReturnToJourney() => SetPage(Page.Drive);

        void AnnounceCheckpoint()
        {
            toast.text = journey.Activity == null ? "✓  CHECKPOINT " + journey.Stage.ToString("00") + " TERSIMPAN"
                : "✓  KEMAJUAN TERSIMPAN";
            toastUntil = Time.unscaledTime + 3;
        }

        void RefreshHud()
        {
            int stage = journey.Stage;
            float z = journey.Position.y;
            location.text = learner == null ? All[stage].Location : "PERAGAAN  /  " + All[stage].Location;
            title.text = All[stage].Title;
            string prompt = All[stage].Instruction, signal = "";
            float target = journey.StageEnd;

            if (stage == 1)
            {
                if (journey.Front < StopLine)
                {
                    target = StopLine;
                    if (journey.FullStop) prompt = "STOP tercatat. Beri jalan pada arus silang.";
                }
                else if (z <= StopJunction + 5.5f)
                    prompt = "Beri jalan pada kendaraan dari kiri atau kanan.";
                else if (journey.Front < LightLine)
                {
                    target = LightLine;
                    prompt = journey.LightPhase == 1 ? "Bersiap berhenti jika masih aman."
                        : "Patuhi lampu lalu lintas dan garis henti putih.";
                    signal = journey.LightPhase switch { 2 => "HIJAU", 1 => "KUNING", _ => "MERAH" };
                }
                else prompt = "Lurus melewati tiang checkpoint hijau.";
            }
            else if (stage == 2)
            {
                if (!journey.CrossedZebra)
                {
                    target = Zebra - 2;
                    if (journey.Pedestrian.HasValue) signal = "TUNGGU PEJALAN KAKI";
                }
                else if (z < MainJunction + 5.5f)
                {
                    target = YieldLine;
                    prompt = "Beri hak utama pada kendaraan di jalan besar.";
                    if (journey.CrossTraffic.HasValue) signal = "PERHATIKAN ARUS SILANG";
                }
                else prompt = "Terus lurus menuju Jalan Mawar.";
            }
            else if (stage == 3)
            {
                target = journey.ReturnedLeft ? All[stage].End : journey.ChangedRight ? Merge : OneWayStart;
                if (journey.ReturnedLeft) prompt = "Tetap di kiri dan ikuti rute ke zona sekolah.";
                else if (journey.ChangedRight) prompt = "Nyalakan sein kiri [Q], lalu kembali ke kiri.";
                else if (z < OneWayStart) prompt = "Tetap di kiri sampai memasuki jalan satu arah.";
                if (journey.LaneTraffic.HasValue && z >= OneWayStart - 7)
                {
                    float gap = journey.LaneTraffic.Value.y - z;
                    signal = "MOBIL LAJUR KANAN  " + Mathf.RoundToInt(Mathf.Abs(gap)) + " M "
                        + (gap >= 0 ? "DI DEPAN" : "DI BELAKANG");
                }
            }
            else if (stage == 4)
            {
                target = journey.Front < SchoolLimit ? SchoolLimit : All[stage].End;
                if (journey.Position.x < -4.25f && !journey.PhoneRinging)
                    prompt = "Kembali ke lajur kiri saat aman sebelum teluk berakhir.";
                else if (journey.Front >= SchoolLimit) prompt = "Jaga kecepatan hingga garis finis.";
                signal = "BATAS 30 KM/J";
            }
            RefreshMissionPrompt(ref prompt, ref signal, ref target);
            instruction.text = prompt;
            detail.text = (signal.Length == 0 ? "" : signal + "   /   ") + Mathf.Max(0, Mathf.RoundToInt(target - journey.Front)) + " M";
            routeCount.text = "CHECKPOINT  " + stage.ToString("00") + " / " + All.Length.ToString("00");
            if (journey.Activity != null)
                routeCount.text = journey.Activity.Kind == MissionKind.SkillTest ? "UJI KETERAMPILAN  /  PERCOBAAN " + journey.Save.attempts
                    : "MISI  /  " + journey.Activity.CompletedSteps(journey) + " DARI 3 BAGIAN";
            for (int i = 0; i < routeSteps.Length; i++)
            {
                int completed = journey.Activity == null ? stage : journey.Activity.CompletedSteps(journey);
                Show(routeSteps[i], journey.Activity == null || journey.Activity.Kind == MissionKind.Scenario && i < 3);
                routeSteps[i].EnableInClassList("step-done", i < completed);
                routeSteps[i].EnableInClassList("step-current", i == completed);
            }
            speed.text = Mathf.RoundToInt(journey.Kph).ToString("00");
            speed.EnableInClassList("speed-alert", stage == 4 && journey.Front >= SchoolLimit && journey.Kph > 30.75f);
            vehicle.text = journey.Indicator switch
            {
                -1 => "‹  SEIN KIRI",
                1 => "SEIN KANAN  ›",
                _ => mode == VehicleMode.Car ? "MOBIL" : journey.Headlight ? "MOTOR · LAMPU NYALA" : "MOTOR · LAMPU MATI"
            };
            Show(phone, journey.PhoneRinging && learner == null);
            Show(root.Q("pickup"), journey.IsMarket && !journey.PassengerPickedUp && learner == null);
            root.Q<Button>("pickupPassenger").SetEnabled(journey.CanPickup);
            Show(toast, Time.unscaledTime < toastUntil);
        }

        void SetPage(Page value)
        {
            page = value;
            if (page == Page.Drive)
                root.Q<Label>(className: "controls").text = "W / ↑ Gas    S / Spasi Rem    A / D Setir    Q / E Sein    C Kamera    R Ulang";
            Show(hud, page == Page.Drive || page == Page.Jaksa);
            Show(overlay, page != Page.Drive && page != Page.Jaksa);
            overlay.EnableInClassList("landing", page == Page.Menu);
            dialog.EnableInClassList("landing-card", page == Page.Menu);
            dialog.EnableInClassList("catalogue-card", page == Page.Catalogue);
            dialog.RemoveFromClassList("rule-card");
            dialog.Clear();
        }

        static void Show(VisualElement e, bool visible) => e.EnableInClassList("hidden", !visible);

        static void Classes(VisualElement element, string names)
        {
            foreach (string name in names.Split(' ', StringSplitOptions.RemoveEmptyEntries)) element.AddToClassList(name);
        }

        static VisualElement Element(VisualElement parent, string classes)
        {
            var element = new VisualElement();
            Classes(element, classes);
            parent.Add(element);
            return element;
        }

        static Label AddLabel(VisualElement parent, string text, string classes)
        {
            var label = new Label(text);
            label.AddToClassList("label");
            Classes(label, classes);
            parent.Add(label);
            return label;
        }

        static Button AddButton(VisualElement parent, string text, Action action, string classes)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("button");
            Classes(button, classes);
            parent.Add(button);
            return button;
        }

        sealed class Question
        {
            public readonly string Prompt, Explanation;
            public readonly string[] Choices;
            public readonly int Correct;
            public Question(string prompt, string explanation, int correct, params string[] choices)
            { Prompt = prompt; Explanation = explanation; Choices = choices; Correct = correct; }
        }

        static readonly Question[] Before =
        {
            new("Apa yang kamu lakukan saat mendekati rambu STOP?", "Berhenti penuh sebelum garis henti, lalu periksa lalu lintas.",
                1, "Pelankan kendaraan dan langsung jalan", "Berhenti penuh sebelum garis", "Berhenti setelah melewati garis"),
            new("Siapa yang didahulukan saat zebra cross sedang digunakan?", "Beri prioritas kepada pejalan kaki yang sedang menyeberang.",
                2, "Kendaraan yang datang lebih dulu", "Kendaraan yang membunyikan klakson", "Pejalan kaki yang menyeberang"),
            new("Ponsel berdering saat kamu mengemudi. Apa yang aman?", "Tetap fokus pada jalan. Gunakan ponsel setelah berhenti di tempat aman.",
                0, "Abaikan sampai bisa berhenti dengan aman", "Jawab sambil melepas gas", "Baca layar untuk melihat siapa yang menelepon")
        };
        static readonly Question[] After =
        {
            new("Jalan tampak kosong di depan rambu STOP. Apa tindakanmu?", "Rambu STOP tetap memerlukan berhenti penuh sebelum garis henti.",
                2, "Lanjutkan dengan kecepatan rendah", "Ikuti kendaraan di depan tanpa berhenti", "Berhenti penuh, lihat kedua arah, lalu maju"),
            new("Seorang anak masih berada di zebra cross. Apa yang kamu lakukan?", "Tunggu sampai penyeberangan aman sebelum melanjutkan.",
                0, "Tunggu sebelum garis penyeberangan", "Lewati dari sisi lain zebra cross", "Beri klakson lalu maju pelan"),
            new("Kamu harus membalas panggilan penting. Kapan bisa melakukannya?", "Berhenti di tempat yang aman sebelum menggunakan ponsel.",
                1, "Saat kendaraan berjalan pelan", "Setelah menepi dan berhenti di tempat yang aman", "Saat jalan lurus dan kosong")
        };

        void StartQuiz(bool after)
        {
            quizIndex = quizScore = 0;
            ShowQuestion(after);
        }

        void ShowQuestion(bool after)
        {
            SetPage(after ? Page.After : Page.Before);
            var question = (after ? After : Before)[quizIndex];
            AddLabel(dialog, (after ? "KUIS AKHIR" : "KUIS AWAL") + "  /  " + (quizIndex + 1) + " DARI 3", "kicker");
            AddLabel(dialog, question.Prompt, "heading");
            var options = Element(dialog, "section");
            for (int i = 0; i < question.Choices.Length; i++)
            {
                int choice = i;
                AddButton(options, question.Choices[i], () =>
                {
                    if (choice == question.Correct) quizScore++;
                    options.SetEnabled(false);
                    if (!after)
                    {
                        NextQuestion(false);
                        return;
                    }
                    var note = Element(dialog, "note");
                    AddLabel(note, choice == question.Correct ? "TEPAT" : "INGAT KEMBALI", "kicker");
                    AddLabel(note, question.Explanation, "copy");
                    AddButton(Element(dialog, "row actions"), quizIndex == 2 ? "Lihat hasil" : "Lanjut", () => NextQuestion(true), "primary");
                }, "option");
            }
        }

        void NextQuestion(bool after)
        {
            quizIndex++;
            if (quizIndex < 3)
            {
                ShowQuestion(after);
                return;
            }
            if (after) journey.Save.after = quizScore;
            else journey.Save.before = quizScore;
            journey.Save.Store(mode);
            if (after) ShowProgress();
            else ShowPreflight();
        }
    }
}
