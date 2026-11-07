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
        enum Page { Menu, Before, Preflight, Drive, Lesson, Checkpoint, Progress, After, Jaksa, JaksaPause, Catalogue, Brief, CityMaps, CityPreview, Controls, PlayChooser, Options, TutorialBrake }

        District district;
        GameObject trainingRoot;
        JaksaDrive jaksa;
        CityMapPreview cityPreview;
        bool pitchDemo;
        double demoStartedAt, demoFinishedAt = -1;
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
        float heldLook;
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
            root.RegisterCallback<GeometryChangedEvent>(change =>
                root.EnableInClassList("compact", change.newRect.height < 700 || change.newRect.width < 1000));
            root.pickingMode = PickingMode.Ignore;
            BindDrivingKeyboard();
            hud = root.Q("hud");
            var mirrors = new VisualElement { name="mirrors", pickingMode=PickingMode.Ignore };
            mirrors.AddToClassList("mirrors");
            hud.Insert(0,mirrors);
            for(int i=0;i<3;i++)
            {
                var frame=Element(mirrors,"mirror mirror-"+new[] { "left","centre","right" }[i]);
                frame.pickingMode=PickingMode.Ignore;
                var image=new Image { image=district.Mirrors.Textures[i], scaleMode=ScaleMode.ScaleAndCrop, pickingMode=PickingMode.Ignore };
                image.AddToClassList("mirror-image");
                frame.Add(image);
                AddLabel(frame,new[] { "SPION KIRI","SPION TENGAH","SPION KANAN" }[i],"mirror-caption");
            }
            root.Q<Image>("signalZoom").image=district.Signals.Texture;
            root.Q<Image>("signalArrowZoom").image=district.Signals.ArrowTexture;
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
                    text = (i+1).ToString("00") + " · " + All[i].Title,
                    tooltip = "Latihan misi " + (i+1) + " · " + All[i].Title
                };
                button.AddToClassList("step");
                root.Q("steps").Add(button);
                routeSteps[i] = button;
            }

            root.Q<Button>("toggleMissions").clicked += () =>
            {
                var drawer = root.Q("missionDrawer");
                bool opening = drawer.ClassListContains("hidden");
                Show(drawer, opening);
                RefreshMissionPickerLabel();
            };
            progress = root.Q<Button>("progress");
            progress.clicked += () => { if (page == Page.Jaksa) ShowJaksaPause(); else if (learner != null) EndDemonstration(); else ShowProgress(); };
            root.Q<Button>("ignorePhone").clicked += () => ResolvePhone(false);
            root.Q<Button>("answerPhone").clicked += () => ResolvePhone(true);
            root.Q<Button>("pickupPassenger").clicked += PickupPassenger;
            BindLook(root.Q<Button>("lookLeft"),-1);
            BindLook(root.Q<Button>("lookRight"),1);
            BindHudControls();
            ShowMenu();
        }

        bool DrivingKeyboardActive => page == Page.Drive || page == Page.Jaksa;

        void BindDrivingKeyboard()
        {
            // Legacy UI navigation shares W/S/arrows and Submit shares Space with driving.
            // Capture these before a focused HUD button can turn braking into a click.
            root.RegisterCallback<NavigationMoveEvent>(e =>
            {
                if (DrivingKeyboardActive) ConsumeDrivingKey(e);
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(e =>
            {
                if (DrivingKeyboardActive && Input.GetKey(KeyCode.Space)) ConsumeDrivingKey(e);
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(e =>
            {
                if ((page == Page.Before || page == Page.After) && e.keyCode == KeyCode.Escape)
                {
                    ConsumeDrivingKey(e);
                    BackFromQuiz();
                    return;
                }
                if (DrivingKeyboardActive && e.keyCode == KeyCode.Space) ConsumeDrivingKey(e);
            }, TrickleDown.TrickleDown);
        }

        void ConsumeDrivingKey(EventBase e)
        {
            e.StopImmediatePropagation();
            root.focusController?.IgnoreEvent(e);
        }

        void RefreshMissionPickerLabel()
        {
            bool open = !root.Q("missionDrawer").ClassListContains("hidden");
            bool scenario = journey?.Activity != null;
            root.Q<Button>("toggleMissions").text = open ? scenario ? "Tutup tahapan" : "Tutup pilihan misi"
                : scenario ? "Lihat tahapan" : "Pilih misi";
        }

        void BindLook(Button button,float direction)
        {
            button.RegisterCallback<PointerDownEvent>(e =>
            {
                if(e.button!=0) return;
                heldLook=direction;
                button.CapturePointer(e.pointerId);
                e.StopPropagation();
            },TrickleDown.TrickleDown);
            button.RegisterCallback<PointerUpEvent>(e =>
            {
                heldLook=0;
                button.ReleasePointer(e.pointerId);
            });
            button.RegisterCallback<PointerCaptureOutEvent>(_=>heldLook=0);
        }

        void OpenMissionPicker()
        {
            if (page != Page.Drive || journey.Activity != null || pitchDemo || learner != null) return;
            ShowProgress();
            var fold = dialog.Query<VisualElement>(className: "fold").ToList().Find(f =>
                f.Q<Label>(className: "fold-title")?.text == "Latihan dari checkpoint");
            fold.AddToClassList("open");
            string prefix = (journey.Stage + 1).ToString("00") + " · ";
            fold.Query<Button>().ToList().Find(b => b.text.StartsWith(prefix))?.Focus();
        }

        void CloseMissionPicker()
        {
            if (page == Page.Progress) ReturnToJourney();
        }

        void Update()
        {
            if (page == Page.TutorialBrake)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ResumeDemoCrossing();
                else if (Input.GetKeyDown(KeyCode.R)) StartPitchDemo();
                else if (Application.isFocused)
                {
                    if (!Input.GetKey(KeyCode.S)) demoActionReleased = true;
                    else if (demoActionReleased && Input.GetKeyDown(KeyCode.S)) ResumeDemoCrossing();
                }
                return;
            }
            if ((page == Page.Before || page == Page.After) && Input.GetKeyDown(KeyCode.Escape)) { BackFromQuiz(); return; }
            if ((page == Page.Menu || page == Page.PlayChooser) && Input.GetKeyDown(KeyCode.F2)) { StartPitchDemo(); return; }
            if (Input.GetKeyDown(KeyCode.M)) ToggleSound();
            if (page == Page.Controls)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseControls(false);
                return;
            }
            if (Input.GetKeyDown(KeyCode.F1) && (page == Page.Menu || page == Page.PlayChooser || page == Page.Options || page == Page.Drive || page == Page.Jaksa || page == Page.Progress || page == Page.JaksaPause))
            {
                ShowControls(page, page == Page.Jaksa || page == Page.JaksaPause ? Page.Jaksa : Page.Drive, false);
                return;
            }
            if ((page == Page.Drive || page == Page.Jaksa) && inputReleaseRequired && !DrivingInputsReleased())
            {
                toast.text = "Lepaskan tombol, lalu mulai berkendara. F1: panduan";
                toastUntil = Time.unscaledTime + 1;
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
                {
                    if (page == Page.Jaksa) ShowJaksaPause(); else ShowProgress();
                }
                return;
            }
            if ((page == Page.PlayChooser || page == Page.Options) && Input.GetKeyDown(KeyCode.Escape)) { ShowMenu(); return; }
            if (page == Page.Catalogue && Input.GetKeyDown(KeyCode.Escape)) { ShowPlayChooser(); return; }
            if (page == Page.Brief && Input.GetKeyDown(KeyCode.Escape)) { ShowCatalogue(catalogueKind); return; }
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
                if (Input.GetKeyDown(KeyCode.Escape)) EnterHumanDrive(Page.Jaksa);
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
                if (demoBrakeLatched)
                {
                    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) ShowProgress();
                    else if (Input.GetKeyDown(KeyCode.R)) Retry();
                    else if (demoCrossingReady && Application.isFocused)
                    {
                        if (!Input.GetKey(KeyCode.W)) demoActionReleased = true;
                        else if (demoActionReleased && Input.GetKeyDown(KeyCode.W))
                        {
                            demoBrakeLatched = demoCrossingReady = demoActionReleased = false;
                            journey.LearningControls = false;
                            toast.text = "Well done! Continue driving. Normal road rules now apply.";
                            toastUntil = Time.unscaledTime + 5;
                        }
                    }
                    return;
                }
                if (Input.GetKeyDown(KeyCode.P) && journey.Activity == null && !pitchDemo) { OpenMissionPicker(); return; }
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
            else if (page == Page.Progress && pitchDemo && Input.GetKeyDown(KeyCode.R)) StartPitchDemo();
            else if (page == Page.Progress && !journey.Finished &&
                (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))) ReturnToJourney();
        }

        void FixedUpdate()
        {
            if (page == Page.Controls || inputReleaseRequired) return;
            if (page == Page.Jaksa)
            {
                float turn = (Pressed(KeyCode.D, KeyCode.RightArrow) ? 1 : 0) - (Pressed(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
                float previousExploreSpeed = jaksa.Speed;
                jaksa.Step(Time.fixedDeltaTime, Pressed(KeyCode.W, KeyCode.UpArrow),
                    Pressed(KeyCode.S, KeyCode.DownArrow) || Input.GetKey(KeyCode.Space), turn);
                CompleteExploreControls(previousExploreSpeed);
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
            if (PauseForDemoCrossing()) return;
            if (demoBrakeLatched)
            {
                // Keep pedestrians moving while the teaching step holds the vehicle stopped.
                // LearningControls also blocks scoring/progression while this step owns input.
                journey.Step(Time.fixedDeltaTime, false, true, 0);
                demoCrossingReady = journey.Speed == 0 && !journey.Pedestrian.HasValue;
                return;
            }
            float steer = (Pressed(KeyCode.D, KeyCode.RightArrow) ? 1 : 0)
                - (Pressed(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
            float previousSpeed = journey.Speed;
            journey.Step(Time.fixedDeltaTime, !demoBrakeLatched && Pressed(KeyCode.W, KeyCode.UpArrow),
                demoBrakeLatched || Pressed(KeyCode.S, KeyCode.DownArrow) || Input.GetKey(KeyCode.Space), steer);
            CompleteControlsPractice(previousSpeed);
            if (page != Page.Drive) return;
            if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
            else if (journey.Activity != null && journey.Finished) ShowMissionProgress();
            else if (journey.Stage != priorStage || journey.Save.checkpoint != priorCheckpoint) AnnounceCheckpoint();
        }

        void LateUpdate()
        {
            if (journey == null) return;
            RefreshProgressiveCue();
            float presentationDt = page == Page.Controls || page == Page.TutorialBrake || inputReleaseRequired ? 0 : Time.unscaledDeltaTime;
            RefreshVehiclePreview(Time.unscaledDeltaTime);
            float look = Mathf.Clamp(heldLook + (Input.GetKey(KeyCode.X) ? 1 : 0) - (Input.GetKey(KeyCode.Z) ? 1 : 0), -1, 1);
            UpdateSound(Time.unscaledDeltaTime);
            if (page == Page.CityPreview) return;
            if (page == Page.Jaksa || page == Page.JaksaPause || page == Page.Controls && controlsDrivingPage == Page.Jaksa)
            {
                jaksa.Present(presentationDt, page == Page.Jaksa && !inputReleaseRequired, look);
                if (page == Page.Jaksa) RefreshJaksaHud();
                return;
            }
            district.Present(journey, hood, presentationDt, page == Page.Drive && !inputReleaseRequired, look);
            if (page == Page.Drive) RefreshHud();
        }

        void UpdateSound(float dt)
        {
            bool driving = page == Page.Drive && !inputReleaseRequired && !journey.Failure.HasValue;
            var frame = new SoundFrame { SirenDist = float.PositiveInfinity, BellDist = float.PositiveInfinity,
                DecorativeAmbienceSuppressed = ControlsOnboardingActive };
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
            if (page == Page.Jaksa && !inputReleaseRequired)
            {
                frame.Speed = jaksa.Speed;
                frame.Throttle = Pressed(KeyCode.W, KeyCode.UpArrow) ? 1 : 0;
                frame.Motorcycle = mode == VehicleMode.Motorcycle;
                frame.Horn = Input.GetKey(KeyCode.H);
            }
            sound.UpdateFrame(dt, frame);
            bool blink = Time.unscaledTime % .7f < .42f;
            if (blink != blinkWas && (driving && journey.Indicator != 0 || page == Page.Jaksa && !inputReleaseRequired && jaksa.Indicator != 0)) sound.Tick(blink);
            blinkWas = blink;
        }

        static bool Pressed(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);
        void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            heldLook = 0;
            demoActionReleased = false;
            if (root == null || journey == null) return;
            if (page == Page.Jaksa) ShowJaksaPause();
            else if (page == Page.Drive)
            {
                if (learner != null) EndDemonstration();
                else ShowProgress();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) OnApplicationFocus(false);
        }

        void PrepareMenuScene()
        {
            CloseCityPreview();
            pitchDemo = false;
            demoBrakeLatched = demoCrossingReady = demoActionReleased = false;
            practiceStart = -1;
            learner = null;
            controlsOriginal = null;
            entryTutorial = false;
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            trainingRoot.SetActive(true);
            district.RestoreAtmosphere();
            progress.tooltip = "Jeda / kemajuan [Tab]";
            root.Q<Label>(className: "controls").text = "";
            Show(root.Q("steps"), true);
            stored = JourneySave.Load(mode);
            journey = new Journey(mode, new JourneySave(), persistent: false);
            hood = false;
            district.Snap();
        }

        void ShowMenu()
        {
            PrepareMenuScene();
            SetPage(Page.Menu);
            AddLabel(dialog, "RoadSense", "hero home-wordmark");
            AddLabel(dialog, "Simulasi keselamatan berkendara di Indonesia", "home-tagline");
            AddLabel(dialog, "We Will Handle", "team-credit");
            var actions = Element(dialog, "home-actions");
            AddButton(actions, "Play", PlayFromHome, "primary home-action");
            AddButton(actions, "Options", ShowOptions, "quiet home-action");
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                AddButton(actions, "Exit", Application.Quit, "quiet home-action");
        }

        void ShowPlayChooser()
        {
            PrepareMenuScene();
            SetPage(Page.PlayChooser);
            var content = MenuBody();
            AddLabel(content, "Pilih perjalanan", "heading");
            AddLabel(content, "Pilih kendaraan untuk perjalananmu.", "copy");
            var modes = Element(content, "row chooser-vehicles");
            AddChooserVehicle(modes, VehicleMode.Car, "Mobil", DrivingGlyph.Kind.Steering);
            AddChooserVehicle(modes, VehicleMode.Motorcycle, "Motor", DrivingGlyph.Kind.Lamp);
            AddLabel(content, stored == null ? "Perjalanan lengkap · 10 tahap. Mulai dari awal."
                : stored.next == All.Length && stored.after >= 0 ? "Perjalanan selesai. Buka hasil yang tersimpan."
                : "Lanjutkan progres perjalanan untuk kendaraan ini.", "copy chooser-summary");
            AddButton(content, "Mulai Perjalanan", () =>
            {
                if (stored == null) StartNew(); else Resume();
            }, "primary chooser-start");
            Fold(content, "Mode lainnya", other =>
            {
                AddButton(other, "Coba demo · Hak utama [F2]", StartPitchDemo, "quiet option");
                AddLabel(other, "Demo singkat Pasar Bunga. Progres perjalanan tidak berubah.", "menu-note");
                AddButton(other, "Uji keterampilan · 5 tes", () => ShowCatalogue(MissionKind.SkillTest), "quiet option");
                AddButton(other, "Misi sehari-hari · 3 perjalanan", () => ShowCatalogue(MissionKind.Scenario), "quiet option");
                AddLabel(other, "Hasil tersimpan untuk tiap tes dan misi.", "menu-note");
                Fold(other, "Kendaraan · " + VehicleCatalogue.Selected(mode).Label, AddVehiclePicker);
                Fold(other, stored == null ? "Jelajah & perjalanan lengkap" : "Jelajah & perjalanan tersimpan", explore =>
                {
                    AddButton(explore, "Sopo Del · berkendara", StartJaksa, "option quiet");
                    AddButton(explore, "Pratinjau peta – belum bisa dimainkan", ShowCityMaps, "option quiet");
                    AddLabel(explore, "Lihat 6 area Jakarta dengan geser/zoom kamera. Untuk berkendara, pilih jelajah Sopo Del.", "menu-note");
                    if (stored == null)
                        AddButton(explore, "Perjalanan lengkap · 10 tahap", StartNew, "option quiet");
                    else
                    {
                        string resume = stored.next == All.Length && stored.after >= 0 ? "Lihat hasil perjalanan"
                            : stored.next > 0 ? "Lanjutkan perjalanan" : "Lanjutkan persiapan";
                        AddButton(explore, resume, Resume, "option quiet");
                        AddButton(explore, confirmNew ? "Ya, mulai ulang perjalanan" : "Mulai ulang perjalanan", () =>
                        {
                            if (confirmNew) StartNew();
                            else { confirmNew = true; ShowPlayChooser(); }
                        }, "option quiet");
                        if (confirmNew) AddLabel(explore, "Ini mengganti progres perjalanan untuk kendaraan ini.", "copy");
                    }
                }, confirmNew);
            }, confirmNew);
            AddButton(content, "Menu utama [Esc]", ShowMenu, "quiet exit-action");
        }

        void AddChooserVehicle(VisualElement parent, VehicleMode choice, string title, DrivingGlyph.Kind glyph)
        {
            bool selected = mode == choice;
            var card = AddButton(parent, "", () => SelectMode(choice),
                selected ? "chooser-vehicle selected" : "chooser-vehicle quiet");
            card.tooltip = title + (selected ? " · Dipilih" : " · Pilih kendaraan");
            card.Add(new DrivingGlyph(glyph));
            AddLabel(card, selected ? "DIPILIH" : "PILIH", "chooser-selection");
            AddLabel(card, title, "chooser-vehicle-title");
            foreach (var child in card.Children()) child.pickingMode = PickingMode.Ignore;
        }

        void ShowOptions()
        {
            PrepareMenuScene();
            SetPage(Page.Options);
            var content = ScrollDialog();
            AddLabel(content, "Options", "heading");
            AddLabel(content, "Suara & panduan", "menu-section-title");
            AddSoundControlsBody(content);
            AddControlsHelp(content);
            AddButton(content, "Menu utama [Esc]", ShowMenu, "quiet exit-action");
        }


        ScrollView MenuBody()
        {
            var content = new ScrollView(ScrollViewMode.Vertical)
            { horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            content.AddToClassList("menu-scroll");
            content.contentViewport.style.overflow = Overflow.Hidden;
            RevealFocusedChildren(content);
            dialog.Add(content);
            return content;
        }

        static void RevealFocusedChildren(ScrollView scroll)
        {
            scroll.RegisterCallback<FocusInEvent>(e =>
            {
                if (e.target is not VisualElement focused || !scroll.contentContainer.Contains(focused)) return;
                scroll.schedule.Execute(() =>
                {
                    if (scroll.panel != null && scroll.focusController?.focusedElement == focused)
                        scroll.ScrollTo(focused);
                });
            });
        }

        void StartJaksa()
        {
            trainingRoot.SetActive(false);
            jaksa ??= new JaksaDrive(transform);
            jaksa.Reset(mode);
            progress.tooltip = "Jeda / menu  [Esc]";
            root.Q<Label>(className: "controls").text = "";
            EnterHumanDrive(Page.Jaksa);
            Show(root.Q("steps"), false);
            Show(phone, false); Show(toast, false);
            Show(root.Q("pickup"), false);
        }

        void RefreshJaksaHud()
        {
            location.text = "MEGA KUNINGAN  /  JAKARTA SELATAN";
            title.text = jaksa.AtEnd ? "Selesai menjelajahi Sopo Del" : "Sea Labs Indonesia · Sopo Del";
            instruction.text = jaksa.AtEnd ? "Tekan R untuk mengulang, atau Esc untuk kembali ke menu."
                : jaksa.VehicleCollision ? "Benturan kendaraan. Berhenti dan beri ruang sebelum melanjutkan."
                : jaksa.CurbContact ? "Tepi trotoar. Arahkan kendaraan kembali ke jalan."
                : jaksa.TrafficContact ? "Jaga jarak. Tunggu ruang aman di depan kendaraan."
                : "Ikuti jalan dan jaga jarak aman.";
            detail.text = "MEGA KUNINGAN BARAT III · " + Mathf.RoundToInt(Mathf.Max(0,246-jaksa.Position.y)) + " M RUAS TERSISA";
            routeCount.text = "SOPO DEL · SEKITAR 250 M";
            speed.text = Mathf.RoundToInt(jaksa.Speed*3.6f).ToString("00");
            speed.EnableInClassList("speed-alert", false);
            bool blink = Time.unscaledTime % .7f < .42f;
            signalLeft.On = jaksa.Indicator < 0 && blink;
            signalRight.On = jaksa.Indicator > 0 && blink;
            headlight.On = mode == VehicleMode.Motorcycle && jaksa.Headlight;
            Show(root.Q("practiceHint"), false);
            Show(root.Q("toggleMissions"), false);
            Show(toast, Time.unscaledTime < toastUntil);
        }

        void ShowJaksaPause()
        {
            SetPage(Page.JaksaPause);
            AddLabel(dialog, "SOPO DEL  /  MEGA KUNINGAN", "kicker");
            AddLabel(dialog, "Jeda perjalanan", "heading");
            AddLabel(dialog, "Jelajahi sekitar 250 meter Jl. Mega Kuningan Barat III di depan Sopo Del, alamat kantor Sea Labs Indonesia. Jalan dan tapak bangunan mengikuti peta; fasad dibuat dari referensi publik. Ukuran detail kecil, pohon, dan furnitur jalan merupakan perkiraan.", "copy");
            AddControlsHelp(dialog);
            var actions=Element(dialog,"row actions");
            AddButton(actions,"Lanjutkan",()=>EnterHumanDrive(Page.Jaksa),"primary");
            AddButton(actions,"Ulangi ruas",()=>{jaksa.Reset(mode);EnterHumanDrive(Page.Jaksa);},"quiet");
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
            AddButton(dialog, "Menu utama", ShowMenu, "quiet");
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
            demoCrossingTaught = demoBrakeLatched = demoCrossingReady = demoActionReleased = false;
            controlsOriginal = null;
            demoStartedAt = Time.realtimeSinceStartupAsDouble;
            demoFinishedAt = -1;
            CloseCityPreview();
            pitchDemo = true;
            practiceStart = -1;
            learner = null;
            journey = new Journey(mode, new JourneySave { next = 2 }, persistent: false) { LearningControls = true };
            hood = false;
            district.Snap();
            progress.tooltip = "Jeda demo  [Tab]";
            EnterHumanDrive(Page.Drive);
            toast.text = "DEMO";
            toastUntil = Time.unscaledTime + 6;
        }

        string DemoDuration()
        {
            double end = demoFinishedAt < 0 ? Time.realtimeSinceStartupAsDouble : demoFinishedAt;
            double elapsed = Math.Max(0, end - demoStartedAt);
            return ((int)(elapsed / 60)).ToString("00") + ":" + ((int)(elapsed % 60)).ToString("00");
        }

        void ShowPitchComplete()
        {
            if (demoFinishedAt < 0) demoFinishedAt = Time.realtimeSinceStartupAsDouble;
            SetPage(Page.Checkpoint);
            var sheet = Result("DEMO SELESAI!", "good", "Hak utama · Pasar Bunga");
            var body = ResultBody(sheet);
            AddLabel(body, "Penyeberangan dan persimpangan berhasil dilewati pada sesi ini.", "sheet-lead");
            int mistakes = journey.Save.mistakes.Sum();
            AddLabel(body, mistakes > 0
                ? mistakes + " catatan keselamatan dalam sesi ini. Setelah mencoba ulang, kamu melewati situasi dengan benar."
                : "Situasi ini kamu lewati tanpa kesalahan dalam sesi demo.", "sheet-text");
            AddLabel(body, "Durasi sesi: " + DemoDuration() + " · termasuk jeda dan penjelasan", "copy");
            AddLabel(body, "RoadSense · We Will Handle", "team-credit");
            AddLabel(body, "Pelajaran hari ini: tunggu penyeberangan aman, lalu periksa arus silang. Progres perjalanan tidak diubah.", "copy");
            var actions = Element(sheet, "sheet-actions");
            var replay = AddButton(actions, "Ulangi demo", StartPitchDemo, "big primary");
            AddButton(actions, "Menu utama", BackToMenu, "big quiet");
            AddLabel(sheet, "R: ulangi demo · Esc: menu", "result-keys");
        }

        void OnDestroy()
        { vehiclePreview?.Dispose(); cityPreview?.Dispose(); district?.Mirrors.Dispose(); district?.Signals.Dispose(); jaksa?.Mirrors.Dispose(); }

        void SelectMode(VehicleMode value)
        {
            bool choosing = page == Page.PlayChooser;
            mode = value;
            confirmNew = false;
            if (choosing) ShowPlayChooser(); else ShowMenu();
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
            else EnterHumanDrive(Page.Drive);
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
            progress.tooltip = "Kemajuan  [Tab]";
            district.Snap();
            toast.text = "LATIHAN MISI " + (stage+1).ToString("00") + " · PROGRES PERJALANAN TETAP TERSIMPAN";
            toastUntil = Time.unscaledTime + 5;
            if (stage == 0) ShowPreflight();
            else { EnterHumanDrive(Page.Drive); if (page == Page.Drive) RefreshHud(); }
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
                    else { SetPage(Page.Drive); AnnounceCheckpoint(); if (page == Page.Drive) EnterHumanDrive(Page.Drive); }
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
            bool yellowTraining = journey.Activity == null && (journey.Stage == 5 || journey.Stage == 6)
                && journey.MainPhase == 1 && (rule == Rule.Signal || rule == Rule.StopBox);
            bool legal = lesson.HasSource && !yellowTraining;
            var sheet = Result(legal ? "PELANGGARAN!" : "AWAS!", legal ? "bad" : "warn",
                yellowTraining ? "Lampu kuning: bersiap berhenti" : lesson.Title);
            var body = ResultBody(sheet);
            AddLabel(body, legal ? "PELANGGARAN HUKUM" : yellowTraining ? "ATURAN LATIHAN" : "SARAN KESELAMATAN",
                legal ? "kind legal" : "kind advice");
            AddLabel(body, "Langkah berikutnya", "result-label");
            AddLabel(body, yellowTraining ? "Berhenti sebelum garis dan lanjut saat hijau." : lesson.Action, "sheet-action");
            AddLabel(body, yellowTraining
                ? "Lampu kuning setelah hijau menandakan lampu akan merah. Dalam latihan ini, berhenti sebelum garis dan lanjut saat hijau."
                : lesson.Explanation, "sheet-text lesson-explanation");
            if (journey.Activity != null) AddLabel(body, journey.Activity.Title + " · Percobaan " + journey.Save.attempts, "kicker");
            if (legal) AddLabel(body, lesson.Regulation + " · " + lesson.Citation, "rule-source");
            Fold(body, "Kenapa ini penting?", content =>
                AddLabel(content, Why(rule), "sheet-text"));
            if (legal) Fold(body, "Dasar hukum dan sanksi", content =>
            {
                var law = LawDetails.For(rule);
                AddLabel(content, lesson.Regulation + " · " + lesson.Citation, "sheet-text");
                AddLabel(content, law.Meaning, "sheet-text");
                AddLabel(content, "SANKSI", "result-label");
                AddLabel(content, law.Sanction, "sheet-text penalty");
                AddLabel(content, "Angka di atas adalah batas maksimum, bukan tarif tilang tetap. UU 1/2026 Pasal II ayat (3)–(4) menghapus ancaman kurungan terkait; nominal denda UU LLAJ tetap.", "sheet-text legal-note");
                AddButton(content, "Penyesuaian pidana 2026", () => Application.OpenURL(LawDetails.Amendment), "quiet");
                AddButton(content, "Buka sumber resmi", () => Application.OpenURL(lesson.Url), "quiet");
                if (rule == Rule.Stop) AddButton(content, "Peraturan rambu", () => Application.OpenURL(Lesson.Signs), "quiet");
                if (rule == Rule.Signal) AddButton(content, "Peraturan lampu", () => Application.OpenURL(Lesson.Signals), "quiet");
                if (rule == Rule.Speed) AddButton(content, "Peraturan kecepatan", () => Application.OpenURL(Lesson.Limits), "quiet");
            });
            if (yellowTraining) Fold(body, "Arti lampu kuning", content =>
            {
                AddLabel(content, "PM 49/2014 Pasal 6 ayat (4) huruf a: lampu kuning menunjukkan lampu merah akan menyala.", "sheet-text");
                AddButton(content, "Buka sumber resmi", () => Application.OpenURL(Lesson.Signals), "quiet");
            });
            var actions = Element(sheet, "sheet-actions");
            var retry = AddButton(actions, "Ulangi dari checkpoint [R]", Retry, "big primary");
            if (journey.Stage > 0) AddButton(actions, "Lihat contoh [D]", StartDemonstration, "big quiet");
            AddButton(actions, "Menu utama [Esc]", BackToMenu, "quiet menu-action");
            AddLabel(sheet, journey.Stage > 0
                ? "Gulir untuk rincian · R: checkpoint · D: cara benar · Esc: menu"
                : "R: ulangi persiapan · Esc: menu", "result-keys");
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

        static void Fold(VisualElement parent, string title, Action<VisualElement> fill, bool expanded = false)
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
            if (expanded) { fold.AddToClassList("open"); sign.text = "-"; }
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
            progress.tooltip = "Akhiri peragaan  [Tab]";
            toast.text = "PERAGAAN · IKUTI PETUNJUK DI LAYAR";
            toastUntil = Time.unscaledTime + 3;
        }

        void EndDemonstration()
        {
            journey = learner;
            learner = null;
            progress.tooltip = pitchDemo ? "Jeda demo  [Tab]" : "Kemajuan  [Tab]";
            district.Snap();
            ShowLesson(journey.Failure.Value);
        }

        void Retry()
        {
            sound.RetryCue();
            if (pitchDemo) { StartPitchDemo(); return; }
            journey.Retry();
            district.Snap();
            if (journey.Stage == 0) ShowPreflight();
            else EnterHumanDrive(Page.Drive);
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
            var content = ScrollDialog();
            AddLabel(content, journey.Finished ? "PERJALANAN SELESAI" : "JURNAL PERJALANAN", "kicker");
            AddLabel(content, journey.Finished ? "Tiba dengan selamat" : "Kemajuan latihan", "heading");
            AddLabel(content, pitchDemo ? "DEMO · Hak utama · Progres latihan tidak diubah" : (practiceStart >= 0 ? "Latihan dari misi " + (practiceStart+1) + " · Progres perjalanan tetap tersimpan  ·  "
                : journey.Stage + " dari " + All.Length + " checkpoint tersimpan  ·  ")
                + (mode == VehicleMode.Car ? "Mobil" : "Motor"), "copy");
            if (pitchDemo) AddLabel(content, "Durasi sesi: " + DemoDuration() + " · termasuk jeda dan penjelasan", "copy");
            if (journey.Save.before >= 0)
                AddLabel(content, "Kuis awal  " + journey.Save.before + "/3"
                    + (journey.Save.after >= 0 ? "     Kuis akhir  " + journey.Save.after + "/3" : ""), "copy");

            var frequent = Enumerable.Range(0, Lesson.All.Length)
                .Where(i => journey.Save.mistakes[i] > 0)
                .OrderByDescending(i => journey.Save.mistakes[i]).Take(3).ToArray();
            var box = Element(content, "note");
            AddLabel(box, frequent.Length > 0 ? "PALING SERING DIULANG" : "CATATAN LATIHAN", "kicker");
            if (frequent.Length == 0) AddLabel(box, "Belum ada aturan yang perlu diulang.", "copy");
            foreach (int i in frequent)
                AddLabel(box, journey.Save.mistakes[i] + "×   " + Lesson.All[i].Title, "copy");
            if (frequent.Length > 0) AddLabel(content, "Saran latihan berikutnya · " + Lesson.All[frequent[0]].Action, "copy");

            var actions = Element(content, "row actions");
            if (!journey.Finished) AddButton(actions, "Kembali berkendara", ReturnToJourney, "primary");
            if (pitchDemo) AddButton(actions, "Ulangi demo [R]", StartPitchDemo, "quiet");
            AddButton(actions, "Menu utama", BackToMenu, journey.Finished ? "primary" : "quiet");
            if (!pitchDemo)
                Fold(content, "Latihan dari checkpoint", picker =>
                {
                    AddLabel(picker, "Memulai latihan terpisah; progres perjalanan tersimpan tetap utuh.", "copy");
                    for (int i = 0; i < All.Length; i++)
                    {
                        int checkpoint = i;
                        AddButton(picker, (i + 1).ToString("00") + " · " + All[i].Title,
                            () => JumpToMission(checkpoint), "quiet option");
                    }
                });
            AddControlsHelp(content);
            AddSoundControls(content);
        }

        void ToggleSound()
        {
            sound.SetMuted(!sound.Muted);
            var toggle = root.Q<Button>("soundToggle");
            if (toggle != null) toggle.text = sound.Muted ? "Suara: mati (M)" : "Suara: aktif (M)";
            toast.text = sound.Muted ? "SUARA DIMATIKAN · M UNTUK MENGAKTIFKAN" : "SUARA DIAKTIFKAN";
            toastUntil = Time.unscaledTime + 3;
        }

        void AddSoundControls(VisualElement parent) => Fold(parent, "Suara & presentasi", AddSoundControlsBody);

        void AddSoundControlsBody(VisualElement parent)
        {
            var button = AddButton(parent, sound.Muted ? "Suara: mati (M)" : "Suara: aktif (M)", null, "quiet tiny");
            button.name = "soundToggle";
            button.clicked += ToggleSound;
            var ambience = AddButton(parent, sound.AmbienceEnabled ? "Ambience: aktif" : "Ambience: mati", null, "quiet tiny");
            ambience.clicked += () =>
            {
                sound.SetAmbience(!sound.AmbienceEnabled);
                ambience.text = sound.AmbienceEnabled ? "Ambience: aktif" : "Ambience: mati";
            };
            AddLabel(parent, "Matikan ambience saat presentasi. Sirene dan peringatan tetap terdengar.", "copy");
            var volume = new Slider(0, 100) { name = "masterVolume", value = sound.Volume * 100 };
            volume.label = "Volume: " + Mathf.RoundToInt(volume.value) + "%";
            volume.AddToClassList("volume-control");
            volume.RegisterValueChangedCallback(change =>
            {
                sound.SetVolume(change.newValue / 100f);
                volume.label = "Volume: " + Mathf.RoundToInt(change.newValue) + "%";
            });
            parent.Add(volume);
            AddButton(parent, "Coba suara", () => sound.Chime(false), "quiet tiny");
            AddLabel(parent, "H: klakson · M: aktif/nonaktif suara", "copy");
        }

        void ReturnToJourney()
        { if (PitchComplete) ShowPitchComplete(); else EnterHumanDrive(Page.Drive); }

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
                "Kamu berbelok kiri dengan sein setelah lampu hijau dan pejalan kaki melewati jalur belokanmu.",
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
        }

        void ContinueCheckpoint()
        {
            if (PitchComplete) { ShowPitchComplete(); return; }
            if (journey.Finished && practiceStart >= 0) ShowProgress();
            else if (journey.Finished) StartQuiz(true);
            else EnterHumanDrive(Page.Drive);
        }

        void RefreshHud()
        {
            var signals=district.Signals;
            Show(root.Q("signalPanel"),page == Page.Drive && signals.Visible);
            if(signals.Visible)
            {
                string[] names={"MERAH","KUNING","HIJAU"};
                void State(string id,int phase,string prefix)
                {
                    var label=root.Q<Label>(id); label.text=prefix+names[phase];
                    for(int i=0;i<3;i++) label.EnableInClassList(new[] { "signal-red","signal-amber","signal-green" }[i],phase==i);
                }
                State("signalMain",signals.MainPhase,signals.HasArrow?"LURUS / KIRI: ":"");
                State("signalRight",signals.ArrowPhase,"BELOK KANAN: ");
                Show(root.Q("signalRight"),signals.HasArrow);
                Show(root.Q("signalArrowZoom"),signals.HasArrow);
                root.Q<Label>("signalHint").text=journey.Stage==7?"Belok kanan hanya saat panah hijau. Tetap dahulukan pejalan kaki.":"Hijau: tetap dahulukan pejalan kaki dan pastikan jalan kosong.";
            }
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
                    if (journey.PedestrianBlocksPath) signal = "TUNGGU PEJALAN KAKI";
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
                    if (journey.BoulevardPedestrianBlocksPath) status += " · TUNGGU PEJALAN KAKI";
                    prompt += mode == VehicleMode.Car ? " Mobil berhenti sebelum kotak merah." : " Motor berhenti di garis depan kotak merah.";
                }
                else if (stage == 8) status = journey.RailWarning ? "BERHENTI · PERINGATAN KERETA" : "PALANG TERBUKA";
                else status = journey.AmbulancePassed ? "AMBULANS SUDAH LEWAT"
                    : journey.Ambulance.HasValue ? "SIRENE · AMBULANS " + Mathf.RoundToInt(Mathf.Abs(journey.AmbulanceGap)) + " M"
                    : "ANGKOT BERHENTI DI LAJUR KIRI";
                if (stage == 9 && journey.Ambulance.HasValue && sound.Muted) status += " · SUARA MATI (M)";
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
            Show(root.Q("toggleMissions"), false);
            RefreshMissionPickerLabel();
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

        ScrollView ScrollDialog()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden
            };
            scroll.AddToClassList("dialog-scroll");
            RevealFocusedChildren(scroll);
            dialog.Add(scroll);
            return scroll;
        }

        void SetPage(Page value)
        {
            heldLook=0;
            root.focusController?.focusedElement?.Blur();
            Show(root.Q("missionDrawer"), false);
            RefreshMissionPickerLabel();
            page = value;
            if (page != Page.Drive) Show(root.Q("signalPanel"), false);
            var mirrorImages = root.Q("mirrors").Query<Image>().ToList();
            var textures = (page == Page.Jaksa || page == Page.Controls && controlsDrivingPage == Page.Jaksa) && jaksa != null ? jaksa.Mirrors.Textures : district.Mirrors.Textures;
            for (int i = 0; i < mirrorImages.Count; i++) mirrorImages[i].image = textures[i];
            Show(hud, page == Page.Drive || page == Page.Jaksa);
            Show(progress, !pitchDemo);
            Show(overlay, page != Page.Drive && page != Page.Jaksa);
            overlay.EnableInClassList("landing", page == Page.Menu);
            dialog.EnableInClassList("landing-card", page == Page.Menu);
            overlay.EnableInClassList("city-preview", page == Page.CityPreview);
            overlay.EnableInClassList("tutorial-overlay", page == Page.TutorialBrake);
            overlay.pickingMode = page == Page.TutorialBrake ? PickingMode.Ignore : PickingMode.Position;
            dialog.EnableInClassList("catalogue-card", page == Page.Catalogue);
            dialog.EnableInClassList("controls-card", page == Page.Controls);
            dialog.EnableInClassList("play-card", page == Page.PlayChooser);
            dialog.EnableInClassList("options-card", page == Page.Options);
            dialog.EnableInClassList("tutorial-card", page == Page.TutorialBrake);
            RefreshHudControls();
            dialog.RemoveFromClassList("rule-card");
            dialog.RemoveFromClassList("result-layout");
            dialog.Clear();
            ShowVehiclePreview(page == Page.Brief || page == Page.Preflight);
            if (value != Page.Drive && value != Page.Jaksa)
                dialog.schedule.Execute(() => FocusPage(value)).StartingIn(value == Page.Lesson || value == Page.Checkpoint ? 320 : 60);
        }

        void FocusPage(Page expected)
        {
            if (page != expected || dialog.panel == null) return;
            bool Available(Button button)
            {
                if (!button.focusable || !button.enabledInHierarchy) return false;
                for (VisualElement element = button; element != null; element = element.parent)
                    if (element.resolvedStyle.display == DisplayStyle.None || element.resolvedStyle.visibility == Visibility.Hidden) return false;
                return true;
            }
            var buttons = dialog.Query<Button>().ToList().Where(Available).ToArray();
            var primary = buttons.FirstOrDefault(button => button.ClassListContains("primary")) ?? buttons.FirstOrDefault();
            primary?.Focus();
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

        void BackFromQuiz()
        {
            if (page == Page.After) ShowProgress();
            else if (page == Page.Before) ShowPlayChooser();
        }

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
            AddButton(dialog, "Kembali [Esc]", BackFromQuiz, "quiet");
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
