using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UjianSim.Game;
using static JalanKita.RouteStage;

namespace JalanKita
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class JalanGame : MonoBehaviour
    {
        enum Page { Menu, Before, Preflight, Drive, Lesson, Checkpoint, Progress, After, Jaksa, JaksaPause, Catalogue, Brief, CityMaps, CityPreview }

        District district;
        GameObject trainingRoot;
        JaksaDrive jaksa;
        CityMapPreview cityPreview;
        bool pitchDemo;
        bool PitchComplete => pitchDemo && journey.Stage > 2;
        SoundEngine sound;
        bool blinkWas;
        Journey journey;
        Journey learner;
        JourneySave stored;
        VehicleMode mode;
        Page page;
        bool confirmNew, hood;
        float toastUntil;
        int quizIndex, quizScore;
        int practiceStart = -1;
        VisualElement root, hud, dialog, overlay, phone;
        Label location, title, instruction, detail, speed, routeCount, toast;
        HudIcon signalLeft, signalRight, headlight;
        Button progress;
        readonly Button[] routeSteps = new Button[All.Length];

        void Start()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            trainingRoot = new GameObject("Kampung Kenanga training");
            trainingRoot.transform.SetParent(transform, false);
            district = new District(trainingRoot.transform);
            sound = SoundEngine.Create(transform);
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
            var indicators = root.Q("indicators");
            signalLeft = new HudIcon(HudIcon.Shape.Left) { name = "signalLeft" };
            headlight = new HudIcon(HudIcon.Shape.Headlight) { name = "headlight" };
            signalRight = new HudIcon(HudIcon.Shape.Right) { name = "signalRight" };
            indicators.Add(signalLeft);
            indicators.Add(headlight);
            indicators.Add(signalRight);
            routeCount = root.Q<Label>("routeCount");
            toast = root.Q<Label>("toast");
            for (int i = 0; i < routeSteps.Length; i++)
            {
                int mission = i;
                var button = new Button(() => JumpToMission(mission))
                {
                    name = "mission" + (i+1).ToString("00"),
                    text = (i+1).ToString("00"),
                    tooltip = "Latihan misi " + (i+1) + " · " + All[i].Title
                };
                button.AddToClassList("step");
                root.Q("steps").Add(button);
                routeSteps[i] = button;
            }

            progress = root.Q<Button>("progress");
            progress.clicked += () => { if (page == Page.Jaksa) ShowJaksaPause(); else if (learner != null) EndDemonstration(); else ShowProgress(); };
            root.Q<Button>("ignorePhone").clicked += () => ResolvePhone(false);
            root.Q<Button>("answerPhone").clicked += () => ResolvePhone(true);
            root.Q<Button>("pickupPassenger").clicked += PickupPassenger;
            ShowMenu();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                sound.SetMuted(!sound.Muted);
                var toggle = root.Q<Button>("soundToggle");
                if (toggle != null) toggle.text = sound.Muted ? "Suara: mati (M)" : "Suara: aktif (M)";
            }
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
            if (page == Page.CityPreview)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ShowCityMaps();
                else cityPreview?.Step(Time.unscaledDeltaTime);
                return;
            }
            if (page == Page.CityMaps && Input.GetKeyDown(KeyCode.Escape)) { ShowMenu(); return; }
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
                { if (journey.IsMarket) PickupPassenger(); else ResolvePhone(false); }
                if (Input.GetKeyDown(KeyCode.G)) ResolvePhone(true);
            }
            else if (page == Page.Lesson)
            {
                if (Input.GetKeyDown(KeyCode.R)) Retry();
                else if (Input.GetKeyDown(KeyCode.D) && journey.Stage > 0) StartDemonstration();
                else if (Input.GetKeyDown(KeyCode.Escape)) BackToMenu();
            }
            else if (page == Page.Checkpoint)
            {
                if (Input.GetKeyDown(KeyCode.R) && (PitchComplete || !journey.Finished)) Retry();
                else if (Input.GetKeyDown(KeyCode.Escape)) BackToMenu();
            }
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
            else if (journey.Activity != null && journey.Finished) ShowMissionProgress();
            else if (journey.Stage != priorStage || journey.Save.checkpoint != priorCheckpoint) AnnounceCheckpoint();
        }

        void LateUpdate()
        {
            if (journey == null) return;
            UpdateSound(Time.unscaledDeltaTime);
            if (page == Page.CityPreview) return;
            if (page == Page.Jaksa || page == Page.JaksaPause)
            {
                jaksa.Present(Time.unscaledDeltaTime);
                if (page == Page.Jaksa) RefreshJaksaHud();
                return;
            }
            district.Present(journey, hood, Time.unscaledDeltaTime, page == Page.Drive);
            if (page == Page.Drive) RefreshHud();
        }

        void UpdateSound(float dt)
        {
            bool driving = page == Page.Drive && !journey.Failure.HasValue;
            var frame = new SoundFrame { SirenDist = float.PositiveInfinity, BellDist = float.PositiveInfinity };
            if (driving)
            {
                frame.Speed = journey.Speed;
                frame.Throttle = journey.Throttle;
                frame.Motorcycle = journey.Mode == VehicleMode.Motorcycle;
                frame.Ringing = journey.PhoneRinging;
                frame.Horn = learner == null && Input.GetKey(KeyCode.H);
                if (journey.Ambulance.HasValue) frame.SirenDist = Vector2.Distance(journey.Position, journey.Ambulance.Value);
                if (journey.RailWarning) frame.BellDist = Vector2.Distance(journey.Position, new Vector2(-180, Boulevard.RailwayZ));
            }
            if (page == Page.Jaksa)
            {
                frame.Speed = jaksa.Speed;
                frame.Throttle = Pressed(KeyCode.W, KeyCode.UpArrow) ? 1 : 0;
                frame.Motorcycle = mode == VehicleMode.Motorcycle;
                frame.Horn = Input.GetKey(KeyCode.H);
            }
            sound.UpdateFrame(dt, frame);
            bool blink = Time.unscaledTime % .7f < .42f;
            if (blink != blinkWas && (driving && journey.Indicator != 0 || page == Page.Jaksa && jaksa.Indicator != 0)) sound.Tick(blink);
            blinkWas = blink;
        }

        static bool Pressed(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);

        void ShowMenu()
        {
            CloseCityPreview();
            pitchDemo = false;
            practiceStart = -1;
            learner = null;
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            trainingRoot.SetActive(true);
            district.RestoreAtmosphere();
            progress.text = "Kemajuan  [Tab]";
            root.Q<Label>(className: "controls").text = "W / ↑ Gas    S / Spasi Rem    A / D Setir    Q / E Sein    C Kamera    R Ulang";
            Show(root.Q("steps"), true);
            stored = JourneySave.Load(mode);
            journey = new Journey(mode, new JourneySave(), persistent: false);
            hood = false;
            district.Snap();
            SetPage(Page.Menu);
            var content = MenuBody();
            AddLabel(content, "SIMULASI KESELAMATAN JALAN  /  INDONESIA", "kicker");
            AddLabel(content, "Jalan Kita", "hero");
            AddLabel(content, "Uji keterampilan, selesaikan misi sehari-hari di Kampung Kenanga, atau jelajahi area kantor Sea di Mega Kuningan.", "copy");

            var modes = Element(content, "row section");
            AddButton(modes, "Mobil", () => SelectMode(VehicleMode.Car), mode == VehicleMode.Car ? "selected" : "quiet");
            AddButton(modes, "Motor", () => SelectMode(VehicleMode.Motorcycle), mode == VehicleMode.Motorcycle ? "selected" : "quiet");
            AddVehiclePicker(content);
            var activities = Element(content, "section");
            AddButton(activities, "Uji keterampilan  ·  5 tes singkat", () => ShowCatalogue(MissionKind.SkillTest), "option primary");
            AddButton(activities, "Misi sehari-hari  ·  3 perjalanan", () => ShowCatalogue(MissionKind.Scenario), "option primary");
            AddButton(activities, "Demo singkat · Hak utama", StartPitchDemo, "option quiet");
            AddLabel(content, "JELAJAH & PERJALANAN LENGKAP", "kicker section");
            var actions = Element(content, "row actions");
            AddButton(actions, "Jelajahi kantor Sea · Sopo Del", StartJaksa, "primary");
            if (stored == null)
                AddButton(actions, "Perjalanan lengkap · 10 tahap", StartNew, "primary");
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
                if (confirmNew) AddLabel(content, "Ini mengganti progres yang tersimpan untuk kendaraan ini.", "copy");
            }
            AddButton(actions, "Peta Jakarta · 6 pratinjau", ShowCityMaps, "quiet");
            AddSoundControls(content);
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                AddButton(actions, "Keluar", Application.Quit, "quiet");
        }


        ScrollView MenuBody()
        {
            var content = new ScrollView(ScrollViewMode.Vertical)
            { horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            content.AddToClassList("menu-scroll");
            content.contentViewport.style.overflow = Overflow.Hidden;
            dialog.Add(content);
            return content;
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
            bool blink = Time.unscaledTime % .7f < .42f;
            signalLeft.On = jaksa.Indicator < 0 && blink;
            signalRight.On = jaksa.Indicator > 0 && blink;
            headlight.On = mode == VehicleMode.Motorcycle && jaksa.Headlight;
            Show(root.Q("practiceHint"), false);
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


        void CloseCityPreview()
        {
            cityPreview?.Dispose();
            cityPreview = null;
            trainingRoot.SetActive(true);
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            district.RestoreAtmosphere();
        }

        void ShowCityMaps()
        {
            CloseCityPreview();
            SetPage(Page.CityMaps);
            AddLabel(dialog, "JAKARTA / OPENSTREETMAP", "kicker");
            AddLabel(dialog, "Pilih peta", "heading");
            AddLabel(dialog, "Pratinjau kota 3D, sekitar 1,7 × 1,5 km per area. Belum berisi misi atau simulasi lalu lintas.", "copy");
            var list = Element(dialog, "section");
            for (int i = 0; i < CityMapData.Ids.Length; i++)
            {
                int index = i;
                AddButton(list, CityMapData.Names[i], () => OpenCityMap(index), "option");
            }
            AddButton(dialog, "Kembali ke latihan", ShowMenu, "quiet");
        }

        void OpenCityMap(int index)
        {
            try
            {
                var map = CityMapData.Load(CityMapData.Ids[index]);
                CloseCityPreview();
                trainingRoot.SetActive(false);
                cityPreview = new CityMapPreview(transform, map);
                SetPage(Page.CityPreview);
                AddLabel(dialog, CityMapData.Names[index], "heading");
                AddLabel(dialog, "PRATINJAU PETA · BUKAN RUTE LATIHAN", "kicker");
                AddLabel(dialog, "WASD / panah: geser · + / −: zoom · R: reset", "copy");
                AddLabel(dialog, "Bangunan dan jalan dari OSM. Tinggi/lebar sebagian perkiraan; bukan model survei. Utara ke depan.", "copy");
                AddLabel(dialog, "© OpenStreetMap contributors · ODbL", "rule-source");
                var actions = Element(dialog, "row actions");
                AddButton(actions, "+", () => cityPreview.Zoom(-.2f), "quiet");
                AddButton(actions, "−", () => cityPreview.Zoom(.2f), "quiet");
                AddButton(actions, "Reset", () => cityPreview.Reset(), "quiet");
                AddButton(actions, "Pilih peta [Esc]", ShowCityMaps, "primary");
                AddButton(actions, "Sumber OSM", () => Application.OpenURL("https://www.openstreetmap.org/copyright"), "quiet");
            }
            catch (Exception error)
            {
                ShowCityMaps();
                AddLabel(dialog, "Peta belum dapat dibuka: " + error.Message, "copy negative");
                Debug.LogException(error);
            }
        }

        void StartPitchDemo()
        {
            CloseCityPreview();
            pitchDemo = true;
            practiceStart = -1;
            learner = null;
            journey = new Journey(mode, new JourneySave { next = 2 }, persistent: false);
            hood = false;
            district.Snap();
            progress.text = "Jeda demo  [Tab]";
            SetPage(Page.Drive);
            toast.text = "DEMO · W: GAS · SPACE: REM · R: ULANG";
            toastUntil = Time.unscaledTime + 6;
        }

        void ShowPitchComplete()
        {
            SetPage(Page.Checkpoint);
            var sheet = Result("DEMO SELESAI!", "good", "Hak utama · Pasar Bunga");
            var body = ResultBody(sheet);
            AddLabel(body, "Kamu memberi prioritas kepada pejalan kaki dan arus di jalan utama.", "sheet-lead");
            int mistakes = journey.Save.mistakes.Sum();
            AddLabel(body, mistakes > 0
                ? mistakes + " pelanggaran dalam sesi ini. Setelah mencoba ulang, kamu melewati situasi dengan benar."
                : "Situasi ini kamu lewati tanpa pelanggaran dalam sesi demo.", "sheet-text");
            AddLabel(body, "Pelajaran hari ini: tunggu penyeberangan aman, lalu periksa arus silang. Progres perjalanan tidak diubah.", "copy");
            var actions = Element(sheet, "sheet-actions");
            var replay = AddButton(actions, "Ulangi demo", StartPitchDemo, "big primary");
            AddButton(actions, "Menu utama", BackToMenu, "big quiet");
            AddLabel(sheet, "R: ulangi demo · Esc: menu", "result-keys");
            replay.schedule.Execute(replay.Focus).StartingIn(550);
        }

        void OnDestroy() => cityPreview?.Dispose();

        void SelectMode(VehicleMode value)
        {
            mode = value;
            confirmNew = false;
            ShowMenu();
        }

        void StartNew()
        {
            practiceStart = -1;
            confirmNew = false;
            journey = new Journey(mode, new JourneySave());
            StartQuiz(false);
        }

        void Resume()
        {
            practiceStart = -1;
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

        void JumpToMission(int stage)
        {
            // Scenario counters describe substeps; they must never launch unrelated route practice.
            if (journey?.Activity != null || pitchDemo || page == Page.Jaksa || page == Page.JaksaPause
                || page == Page.CityPreview || page == Page.CityMaps) return;
            if (stage < 0 || stage >= All.Length) return;
            root.focusController?.focusedElement?.Blur();
            learner = null;
            practiceStart = stage;
            journey = new Journey(mode, new JourneySave { next = stage }, persistent: false);
            progress.text = "Kemajuan  [Tab]";
            district.Snap();
            toast.text = "LATIHAN MISI " + (stage+1).ToString("00") + " · PROGRES PERJALANAN TETAP TERSIMPAN";
            toastUntil = Time.unscaledTime + 5;
            if (stage == 0) ShowPreflight();
            else { SetPage(Page.Drive); RefreshHud(); }
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
            sound.Chime(true);
            SetPage(Page.Lesson);
            var lesson = Lesson.All[(int)rule];
            var sheet = Result(lesson.HasSource ? "PELANGGARAN!" : "AWAS!",
                lesson.HasSource ? "bad" : "warn", lesson.Title);
            var body = ResultBody(sheet);
            AddLabel(body, lesson.HasSource ? "PELANGGARAN HUKUM" : "SARAN KESELAMATAN",
                lesson.HasSource ? "kind legal" : "kind advice");
            AddLabel(body, lesson.Explanation, "sheet-lead");
            AddLabel(body, "YANG BENAR", "result-label");
            AddLabel(body, lesson.Action, "sheet-text");
            if (journey.Activity != null) AddLabel(body, journey.Activity.Title + " · Percobaan " + journey.Save.attempts, "kicker");
            if (lesson.HasSource) AddLabel(body, lesson.Regulation + " · " + lesson.Citation, "rule-source");
            Fold(body, "Kenapa ini penting?", content =>
                AddLabel(content, Why(rule), "sheet-text"));
            if (lesson.HasSource) Fold(body, "Dasar hukum", content =>
            {
                AddLabel(content, lesson.Regulation + " · " + lesson.Citation, "sheet-text");
                AddButton(content, "Buka sumber resmi", () => Application.OpenURL(lesson.Url), "quiet");
                if (rule == Rule.Stop) AddButton(content, "Peraturan rambu", () => Application.OpenURL(Lesson.Signs), "quiet");
                if (rule == Rule.Signal) AddButton(content, "Peraturan lampu", () => Application.OpenURL(Lesson.Signals), "quiet");
                if (rule == Rule.Speed) AddButton(content, "Peraturan kecepatan", () => Application.OpenURL(Lesson.Limits), "quiet");
            });
            var actions = Element(sheet, "sheet-actions");
            var retry = AddButton(actions, "Coba lagi", Retry, "big primary");
            if (journey.Stage > 0) AddButton(actions, "Lihat cara benar", StartDemonstration, "big quiet");
            AddButton(actions, "Menu", BackToMenu, "big quiet");
            AddLabel(sheet, journey.Stage > 0
                ? "R: coba lagi dari checkpoint · D: lihat cara benar · Esc: menu"
                : "R: ulangi persiapan · Esc: menu", "result-keys");
            retry.schedule.Execute(retry.Focus).StartingIn(550);
        }

        VisualElement Result(string stamp, string tone, string caption)
        {
            dialog.AddToClassList("result-layout");
            var head = Element(dialog, "result-head");
            var banner = AddLabel(head, stamp, "stamp " + tone + " pre");
            AddLabel(head, caption, "stamp-caption");
            var sheet = Element(dialog, "sheet " + tone + " rise pre");
            banner.schedule.Execute(() => banner.RemoveFromClassList("pre")).StartingIn(40);
            sheet.schedule.Execute(() => sheet.RemoveFromClassList("pre")).StartingIn(240);
            return sheet;
        }

        static ScrollView ResultBody(VisualElement sheet)
        {
            var body = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden
            };
            body.AddToClassList("sheet-body");
            sheet.Add(body);
            return body;
        }

        static void Fold(VisualElement parent, string title, Action<VisualElement> fill)
        {
            var fold = Element(parent, "fold");
            var details = new VisualElement();
            details.AddToClassList("fold-body");
            Label sign = null;
            var button = AddButton(fold, "", () =>
            {
                fold.ToggleInClassList("open");
                sign.text = fold.ClassListContains("open") ? "-" : "+";
            }, "fold-head");
            AddLabel(button, title, "fold-title");
            sign = AddLabel(button, "+", "fold-sign");
            fill(details);
            fold.Add(details);
        }

        static string Why(Rule rule) => rule switch
        {
            Rule.SeatBelt => "Sabuk menahan tubuh saat pengereman mendadak atau benturan.",
            Rule.Helmet => "Helm yang sesuai dan terkunci membantu melindungi kepala saat terjatuh.",
            Rule.Headlight => "Lampu membantu pengguna jalan lain melihat sepeda motor lebih awal.",
            Rule.Stop or Rule.Junction or Rule.Signal => "Berhenti dan memberi jalan menyediakan waktu untuk melihat kendaraan dari arah silang.",
            Rule.Pedestrian => "Pejalan kaki tidak memiliki pelindung seperti pengendara. Tunggu sampai jalur mereka aman.",
            Rule.Indicator or Rule.Gap or Rule.ReturnLeft => "Isyarat dan ruang yang cukup membantu pengendara lain memahami perpindahan lajurmu.",
            Rule.KeepLeft or Rule.NoEntry => "Mengikuti arah yang ditetapkan mengurangi risiko berhadapan dengan kendaraan dari arah berlawanan.",
            Rule.Speed or Rule.BoulevardSpeed => "Kecepatan lebih rendah memberi waktu untuk bereaksi, terutama saat anak-anak berada di dekat jalan.",
            Rule.Railway => "Kereta membutuhkan jarak sangat panjang untuk berhenti. Palang yang mulai naik belum berarti jalur sudah boleh dilalui.",
            Rule.Emergency => "Memberi ruang membantu ambulans membawa pasien ke pertolongan lebih cepat tanpa memaksa manuver berbahaya.",
            Rule.RightArrow or Rule.MovementLane or Rule.TurnSignal => "Lajur dan isyarat yang tepat memisahkan arus kendaraan. Hijau tetap mengharuskan pengemudi memperhatikan pejalan kaki.",
            Rule.Phone => "Perhatian yang teralihkan membuat bahaya di depan lebih mudah terlewat.",
            _ => "Jaga ruang di sekitar kendaraan agar ada waktu untuk mengerem dan menghindari bahaya."
        };

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
            progress.text = pitchDemo ? "Jeda demo  [Tab]" : "Kemajuan  [Tab]";
            district.Snap();
            ShowLesson(journey.Failure.Value);
        }

        void Retry()
        {
            if (PitchComplete) { StartPitchDemo(); return; }
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
            if (PitchComplete) { ShowPitchComplete(); return; }
            if (journey.Activity != null) { ShowMissionProgress(); return; }
            SetPage(Page.Progress);
            AddLabel(dialog, journey.Finished ? "PERJALANAN SELESAI" : "JURNAL PERJALANAN", "kicker");
            AddLabel(dialog, journey.Finished ? "Tiba dengan selamat" : "Kemajuan latihan", "heading");
            AddLabel(dialog, pitchDemo ? "DEMO · Hak utama · Progres latihan tidak diubah" : (practiceStart >= 0 ? "Latihan dari misi " + (practiceStart+1) + " · Progres perjalanan tetap tersimpan  ·  "
                : journey.Stage + " dari " + All.Length + " checkpoint tersimpan  ·  ")
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
            AddSoundControls(dialog);
        }

        void AddSoundControls(VisualElement parent)
        {
            var button = AddButton(parent, sound.Muted ? "Suara: mati (M)" : "Suara: aktif (M)", null, "quiet tiny");
            button.name = "soundToggle";
            button.clicked += () =>
            {
                sound.SetMuted(!sound.Muted);
                button.text = sound.Muted ? "Suara: mati (M)" : "Suara: aktif (M)";
            };
            AddLabel(parent, "H: klakson · M: aktif/nonaktif suara", "copy");
        }

        void ReturnToJourney()
        { if (PitchComplete) ShowPitchComplete(); else SetPage(Page.Drive); }

        void AnnounceCheckpoint()
        {
            sound.Chime(false);
            if (PitchComplete) { ShowPitchComplete(); return; }
            if (journey.Activity != null)
            {
                toast.text = "✓  KEMAJUAN TERSIMPAN";
                toastUntil = Time.unscaledTime + 3;
                return;
            }
            SetPage(Page.Checkpoint);
            int completed = journey.Stage - 1;
            var sheet = Result("MISI SELESAI!", "good",
                "Misi " + journey.Stage + " · " + All[completed].Title);
            string[] recaps =
            {
                "Semua orang memakai perlengkapan keselamatan sebelum kendaraan bergerak.",
                "Kamu berhenti di rambu STOP, memberi jalan, dan mematuhi lampu lalu lintas.",
                "Kamu memberi prioritas kepada pejalan kaki dan arus di jalan utama.",
                "Kamu mengikuti arah jalan, memberi sein, dan berpindah lajur dengan aman.",
                "Kamu menjaga kecepatan dan tetap fokus melewati zona sekolah.",
                "Kamu memilih lajur lurus, mematuhi lampu, dan menunggu pejalan kaki.",
                "Kamu berbelok kiri dengan sein setelah lampu hijau dan zebra cross kosong.",
                "Kamu berbelok kanan dari lajur yang tepat setelah panah kanan hijau.",
                "Kamu menunggu kereta lewat dan palang terbuka penuh sebelum melintas.",
                "Kamu memberi jalan kepada ambulans dengan aman tanpa melampaui batas kecepatan."
            };
            var body = ResultBody(sheet);
            AddLabel(body, recaps[completed], "sheet-lead");
            AddLabel(body, practiceStart >= 0 ? "Latihan selesai. Progres perjalanan tetap tersimpan."
                : "Checkpoint " + journey.Stage.ToString("00") + "/" + All.Length.ToString("00") + " tersimpan. "
                + (journey.Finished ? "Seluruh rute selesai." : "Lanjutkan di jalan yang sama."), "checkpoint-saved");
            var actions = Element(sheet, "sheet-actions");
            var next = AddButton(actions, journey.Finished ? practiceStart >= 0 ? "Hasil latihan" : "Kuis akhir" : "Lanjutkan perjalanan",
                ContinueCheckpoint, "big primary");
            if (!journey.Finished) AddButton(actions, "Ulang dari checkpoint", Retry, "big quiet");
            AddButton(actions, "Menu", BackToMenu, "big quiet");
            AddLabel(sheet, journey.Finished ? practiceStart >= 0 ? "Enter: hasil latihan · Esc: menu" : "Enter: kuis akhir · Esc: menu"
                : "Enter: lanjutkan · R: ulang dari checkpoint · Esc: menu", "result-keys");
            next.schedule.Execute(next.Focus).StartingIn(550);
        }

        void ContinueCheckpoint()
        {
            if (PitchComplete) { ShowPitchComplete(); return; }
            if (journey.Finished && practiceStart >= 0) ShowProgress();
            else if (journey.Finished) StartQuiz(true);
            else SetPage(Page.Drive);
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
                else if (journey.Front >= SchoolLimit) prompt = "Jaga kecepatan hingga checkpoint sekolah.";
                signal = "BATAS 30 KM/J";
            }
            if (stage >= 5)
            {
                string status;
                if (stage <= 7)
                {
                    status = stage == 7 ? "PANAH KANAN " + (journey.ArrowPhase == 2 ? "HIJAU" : "MERAH")
                        : journey.MainPhase == 2 ? "HIJAU" : journey.MainPhase == 1 ? "KUNING" : "MERAH";
                    if (journey.BoulevardPedestrian.HasValue) status += " · TUNGGU PEJALAN KAKI";
                    prompt += mode == VehicleMode.Car ? " Mobil berhenti sebelum kotak merah." : " Motor berhenti di garis depan kotak merah.";
                }
                else if (stage == 8) status = journey.RailWarning ? "BERHENTI · PERINGATAN KERETA" : "PALANG TERBUKA";
                else status = journey.AmbulancePassed ? "AMBULANS SUDAH LEWAT"
                    : journey.Ambulance.HasValue ? "SIRENE · AMBULANS " + Mathf.RoundToInt(Mathf.Abs(journey.AmbulanceGap)) + " M"
                    : "ANGKOT BERHENTI DI LAJUR KIRI";
                detail.text = status + " · BATAS 50 KM/J";
            }
            RefreshMissionPrompt(ref prompt, ref signal, ref target);
            instruction.text = prompt;
            if (stage < 5) detail.text = (signal.Length == 0 ? "" : signal + "   /   ") + Mathf.Max(0, Mathf.RoundToInt(target - journey.Front)) + " M";
            routeCount.text = pitchDemo ? "DEMO · HAK UTAMA" : practiceStart >= 0 ? "LATIHAN MISI  " + (stage+1).ToString("00") + " / " + All.Length.ToString("00")
                : "CHECKPOINT  " + stage.ToString("00") + " / " + All.Length.ToString("00");
            if (journey.Activity != null)
                routeCount.text = journey.Activity.Kind == MissionKind.SkillTest ? "UJI KETERAMPILAN / PERCOBAAN " + journey.Save.attempts
                    : "MISI / " + journey.Activity.CompletedSteps(journey) + " DARI 3 BAGIAN";
            for (int i = 0; i < routeSteps.Length; i++)
            {
                bool route = journey.Activity == null && !pitchDemo;
                int complete = route ? stage : journey.Activity?.CompletedSteps(journey) ?? 0;
                Show(routeSteps[i], !pitchDemo && (route || journey.Activity.Kind == MissionKind.Scenario && i < 3));
                routeSteps[i].SetEnabled(route);
                routeSteps[i].focusable = route;
                routeSteps[i].EnableInClassList("step-done", i < complete && (practiceStart < 0 || i >= practiceStart));
                routeSteps[i].EnableInClassList("step-current", i == complete);
            }
            Show(root.Q("practiceHint"), journey.Activity == null && !pitchDemo);
            speed.text = Mathf.RoundToInt(journey.Kph).ToString();
            speed.EnableInClassList("speed-alert", stage == 4 && journey.Front >= SchoolLimit && journey.Kph > 30.75f || stage >= 5 && journey.Kph > 50.75f);
            bool blink = Time.unscaledTime % .7f < .42f;
            signalLeft.On = journey.Indicator == -1 && blink;
            signalRight.On = journey.Indicator == 1 && blink;
            headlight.On = journey.Mode == VehicleMode.Motorcycle && journey.Headlight;
            Show(phone, journey.PhoneRinging && learner == null);
            Show(root.Q("pickup"), journey.IsMarket && !journey.PassengerPickedUp && learner == null && journey.Position.y >= Mission.PickupStart - 6);
            Show(toast, Time.unscaledTime < toastUntil);
        }

        void SetPage(Page value)
        {
            page = value;
            Show(hud, page == Page.Drive || page == Page.Jaksa);
            Show(overlay, page != Page.Drive && page != Page.Jaksa);
            overlay.EnableInClassList("landing", page == Page.Menu);
            dialog.EnableInClassList("landing-card", page == Page.Menu);
            overlay.EnableInClassList("city-preview", page == Page.CityPreview);
            dialog.EnableInClassList("catalogue-card", page == Page.Catalogue);
            dialog.RemoveFromClassList("rule-card");
            dialog.RemoveFromClassList("result-layout");
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
            if (practiceStart < 0 && !pitchDemo) journey.Save.Store(mode, journey.Activity);
            if (after) ShowProgress();
            else ShowPreflight();
        }
    }
}
