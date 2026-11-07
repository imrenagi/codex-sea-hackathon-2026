using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UjianSim.Game;
using static JalanKita.RouteStage;

namespace JalanKita
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class JalanGame : MonoBehaviour
    {
        enum Page { Menu, Before, Preflight, Drive, Lesson, Checkpoint, Progress, After }

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
        Label location, title, instruction, detail, speed, routeCount, toast;
        HudIcon signalLeft, signalRight, headlight;
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
            else if (page == Page.Lesson)
            {
                if (Input.GetKeyDown(KeyCode.R)) Retry();
                else if (Input.GetKeyDown(KeyCode.D) && journey.Stage > 0) StartDemonstration();
                else if (Input.GetKeyDown(KeyCode.Escape)) BackToMenu();
            }
            else if (page == Page.Checkpoint)
            {
                if (Input.GetKeyDown(KeyCode.R) && !journey.Finished) Retry();
                else if (Input.GetKeyDown(KeyCode.Escape)) BackToMenu();
            }
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
            else if (journey.Stage != priorStage) AnnounceCheckpoint();
        }

        void LateUpdate()
        {
            if (journey == null) return;
            district.Present(journey, hood, Time.unscaledDeltaTime, page == Page.Drive);
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
            AddLabel(dialog, "Satu perjalanan: kampung, persimpangan besar, rel kereta, dan jalur ambulans.", "copy");
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
            var lesson = Lesson.All[(int)rule];
            var sheet = Result(lesson.HasSource ? "PELANGGARAN!" : "AWAS!",
                lesson.HasSource ? "bad" : "warn", lesson.Title);
            var body = ResultBody(sheet);
            AddLabel(body, lesson.HasSource ? "PELANGGARAN HUKUM" : "SARAN KESELAMATAN",
                lesson.HasSource ? "kind legal" : "kind advice");
            AddLabel(body, lesson.Explanation, "sheet-lead");
            AddLabel(body, "YANG BENAR", "result-label");
            AddLabel(body, lesson.Action, "sheet-text");
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
            AddLabel(body, "Checkpoint " + journey.Stage.ToString("00") + "/" + All.Length.ToString("00") + " tersimpan. "
                + (journey.Finished ? "Seluruh rute selesai." : "Lanjutkan di jalan yang sama."), "checkpoint-saved");
            var actions = Element(sheet, "sheet-actions");
            var next = AddButton(actions, journey.Finished ? "Kuis akhir" : "Lanjutkan perjalanan",
                ContinueCheckpoint, "big primary");
            if (!journey.Finished) AddButton(actions, "Ulang dari checkpoint", Retry, "big quiet");
            AddButton(actions, "Menu", BackToMenu, "big quiet");
            AddLabel(sheet, journey.Finished ? "Enter: kuis akhir · Esc: menu"
                : "Enter: lanjutkan · R: ulang dari checkpoint · Esc: menu", "result-keys");
            next.schedule.Execute(next.Focus).StartingIn(550);
        }

        void ContinueCheckpoint()
        {
            if (journey.Finished) StartQuiz(true);
            else SetPage(Page.Drive);
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
            instruction.text = prompt;
            if (stage < 5) detail.text = (signal.Length == 0 ? "" : signal + "   /   ") + Mathf.Max(0, Mathf.RoundToInt(target - journey.Front)) + " M";
            routeCount.text = "CHECKPOINT  " + stage.ToString("00") + " / " + All.Length.ToString("00");
            for (int i = 0; i < routeSteps.Length; i++)
            {
                routeSteps[i].EnableInClassList("step-done", i < stage);
                routeSteps[i].EnableInClassList("step-current", i == stage);
            }
            speed.text = Mathf.RoundToInt(journey.Kph).ToString();
            speed.EnableInClassList("speed-alert", stage == 4 && journey.Front >= SchoolLimit && journey.Kph > 30.75f || stage >= 5 && journey.Kph > 50.75f);
            bool blink = Time.unscaledTime % .7f < .42f;
            signalLeft.On = journey.Indicator == -1 && blink;
            signalRight.On = journey.Indicator == 1 && blink;
            headlight.On = journey.Mode == VehicleMode.Motorcycle && journey.Headlight;
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
            journey.Save.Store(mode);
            if (after) ShowProgress();
            else ShowPreflight();
        }
    }
}
