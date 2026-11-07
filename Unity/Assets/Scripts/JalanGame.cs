using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static JalanKita.RouteStage;

namespace JalanKita
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class JalanGame : MonoBehaviour
    {
        enum Page { Menu, Before, Preflight, Drive, Lesson, Progress, After }

        District district;
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
            district = new District(transform);
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
            progress.clicked += () => { if (learner != null) EndDemonstration(); else ShowProgress(); };
            root.Q<Button>("ignorePhone").clicked += () => ResolvePhone(false);
            root.Q<Button>("answerPhone").clicked += () => ResolvePhone(true);
            ShowMenu();
        }

        void Update()
        {
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
                if (Input.GetKeyDown(KeyCode.F)) ResolvePhone(false);
                if (Input.GetKeyDown(KeyCode.G)) ResolvePhone(true);
            }
            else if (page == Page.Lesson && Input.GetKeyDown(KeyCode.R)) Retry();
            else if (page == Page.Progress && !journey.Finished &&
                (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))) ReturnToJourney();
        }

        void FixedUpdate()
        {
            if (page != Page.Drive) return;
            int priorStage = journey.Stage;
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
            else if (journey.Finished) StartQuiz(true);
            else if (journey.Stage != priorStage) AnnounceCheckpoint();
        }

        void LateUpdate()
        {
            if (journey == null) return;
            district.Present(journey, hood, Time.unscaledDeltaTime);
            if (page == Page.Drive) RefreshHud();
        }

        static bool Pressed(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);

        void ShowMenu()
        {
            stored = JourneySave.Load(mode);
            journey = new Journey(mode, new JourneySave());
            hood = false;
            district.Snap();
            SetPage(Page.Menu);
            AddLabel(dialog, "SIMULASI KESELAMATAN JALAN  /  INDONESIA", "kicker");
            AddLabel(dialog, "Jalan Kita", "hero");
            AddLabel(dialog, "Satu perjalanan melewati lima latihan berkendara di Kampung Kenanga.", "copy");
            var list = Element(dialog, "section");
            for (int i = 0; i < All.Length; i++)
            {
                var row = Element(list, "row route-row");
                AddLabel(row, (i+1).ToString("00"), "route-num");
                AddLabel(row, All[i].Title, "route-text");
            }

            var modes = Element(dialog, "row section");
            AddButton(modes, "Mobil", () => SelectMode(VehicleMode.Car), mode == VehicleMode.Car ? "selected" : "quiet");
            AddButton(modes, "Motor", () => SelectMode(VehicleMode.Motorcycle), mode == VehicleMode.Motorcycle ? "selected" : "quiet");
            var actions = Element(dialog, "row actions");
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
                    SetPage(Page.Drive);
                    AnnounceCheckpoint();
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
            AddLabel(dialog, "LATIHAN " + (journey.Stage + 1).ToString("00") + "  /  COBA LAGI", "kicker negative");
            AddLabel(dialog, lesson.Title, "heading");
            AddLabel(dialog, lesson.Explanation, "copy");
            var note = Element(dialog, "note");
            AddLabel(note, "CARA MELAKUKANNYA", "kicker");
            AddLabel(note, lesson.Action, "copy");
            if (lesson.HasSource) AddLabel(dialog, lesson.Regulation + " · " + lesson.Citation, "rule-source");
            var actions = Element(dialog, "row actions");
            AddButton(actions, journey.Stage == 0 ? "Ulangi persiapan  [R]" : "Ulangi dari checkpoint  [R]", Retry, "primary");
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
                mistakes = (int[])learner.Save.mistakes.Clone()
            }, persistent: false);
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
            toast.text = "✓  CHECKPOINT " + journey.Stage.ToString("00") + " TERSIMPAN";
            toastUntil = Time.unscaledTime + 3;
        }

        void RefreshHud()
        {
            int stage = journey.Stage;
            float z = journey.Position.y;
            location.text = learner == null ? All[stage].Location : "PERAGAAN  /  " + All[stage].Location;
            title.text = All[stage].Title;
            string prompt = All[stage].Instruction, signal = "";
            float target = All[stage].End;

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
            instruction.text = prompt;
            detail.text = (signal.Length == 0 ? "" : signal + "   /   ") + Mathf.Max(0, Mathf.RoundToInt(target - journey.Front)) + " M";
            routeCount.text = "CHECKPOINT  " + stage.ToString("00") + " / " + All.Length.ToString("00");
            for (int i = 0; i < routeSteps.Length; i++)
            {
                routeSteps[i].EnableInClassList("step-done", i < stage);
                routeSteps[i].EnableInClassList("step-current", i == stage);
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
            Show(toast, Time.unscaledTime < toastUntil);
        }

        void SetPage(Page value)
        {
            page = value;
            Show(hud, page == Page.Drive);
            Show(overlay, page != Page.Drive);
            overlay.EnableInClassList("landing", page == Page.Menu);
            dialog.EnableInClassList("landing-card", page == Page.Menu);
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
