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
        enum Page { Menu, Before, Preflight, Drive, Lesson, Checkpoint, Progress, After, Jaksa, JaksaPause, Catalogue, Brief, CityMaps, CityPreview, Controls, PlayChooser, Options, TutorialBrake, Credits, VehicleSelect }

        District district;
        GameObject trainingRoot;
        JaksaDrive jaksa;
        CityMapPreview cityPreview;
        bool pitchDemo;
        double demoStartedAt, demoFinishedAt = -1;
        bool PitchComplete => pitchDemo && demoTutorial != null && demoTutorial.Scenario.IsComplete(journey);
        SoundEngine sound;
        bool blinkWas;
        Journey journey;
        Journey learner;
        ViolationExampleSession demonstration;
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
            GameText.Apply(root);
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
                AddLabel(frame,new[] { GameText.Get("hud.mirror.left"),GameText.Get("hud.mirror.center"),GameText.Get("hud.mirror.right") }[i],"mirror-caption");
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
                    tooltip = GameText.Get("mission.practice_title", i + 1, All[i].Title)
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
            root.Q<Button>("toggleMissions").text = open ? scenario ? GameText.Get("text.hide_stages") : GameText.Get("text.hide_missions")
                : scenario ? GameText.Get("text.view_stages") : GameText.Get("text.choose_mission");
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
                f.Q<Label>(className: "fold-title")?.text == GameText.Get("text.practice_from_a_checkpoint"));
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
                if (Input.GetKeyDown(KeyCode.Escape)) ResumeDemoTutorial();
                else if (Input.GetKeyDown(KeyCode.R)) RestartDemoTutorial();
                else ReadDemoTutorialInput();
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
                toast.text = GameText.Get("text.release_the_keys_then_start_driving_f1_controls");
                toastUntil = Time.unscaledTime + 1;
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
                {
                    if (page == Page.Jaksa) ShowJaksaPause(); else ShowProgress();
                }
                return;
            }
            if (page == Page.VehicleSelect && Input.GetKeyDown(KeyCode.Escape)) { vehicleSelectionBack?.Invoke(); return; }
            if (page == Page.PlayChooser && Input.GetKeyDown(KeyCode.Escape)) { BackFromJourneyChooser(); return; }
            if ((page == Page.Options || page == Page.Credits) && Input.GetKeyDown(KeyCode.Escape)) { ShowMenu(); return; }
            if (page == Page.Catalogue && Input.GetKeyDown(KeyCode.Escape)) { BackFromCatalogue(); return; }
            if (page == Page.Brief && Input.GetKeyDown(KeyCode.Escape)) { ShowCatalogue(catalogueKind); return; }
            if (page == Page.Jaksa)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) { ShowJaksaPause(); return; }
                if (Input.GetKeyDown(KeyCode.C)) ToggleExploreCamera();
                if (Input.GetKeyDown(KeyCode.Q)) ToggleExploreIndicator(-1);
                if (Input.GetKeyDown(KeyCode.E)) ToggleExploreIndicator(1);
                if (Input.GetKeyDown(KeyCode.L) && mode == VehicleMode.Motorcycle) ToggleExploreHeadlight();
                if (Input.GetKeyDown(KeyCode.R)) ResetExplore();
                return;
            }
            if (page == Page.JaksaPause)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { if (hiActive && HiEnded) LeaveHiForCatalogue(); else if(!hiActive && jaksa.Safety.Failure.HasValue) ShowMenu(); else EnterHumanDrive(Page.Jaksa); }
                return;
            }
            if (page == Page.CityPreview)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ShowCityMaps();
                else cityPreview?.Step(Time.unscaledDeltaTime);
                return;
            }
            if (page == Page.CityMaps && Input.GetKeyDown(KeyCode.Escape)) { ShowJourneyChooser(practiceChooser); return; }
            if (learner != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.R))
                    EndDemonstration();
                return;
            }
            if (page == Page.Drive)
            {
                if (pitchDemo && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
                    demoTutorial?.MarkDrivingStarted();
                if (demoTutorial?.IsActive == true)
                {
                    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) ShowProgress();
                    else if (Input.GetKeyDown(KeyCode.R)) Retry();
                    else ReadDemoTutorialInput();
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
                if (Input.GetKeyDown(KeyCode.R) && (PitchComplete || !journey.Finished || practiceStart >= 0)) Retry();
                else if (Input.GetKeyDown(KeyCode.Escape)) BackToMenu();
            }
            else if (page == Page.Progress && pitchDemo && Input.GetKeyDown(KeyCode.R)) RestartDemoTutorial();
            else if (page == Page.Progress && !journey.Finished &&
                (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))) ReturnToJourney();
        }

        void FixedUpdate()
        {
            if (page == Page.Controls || inputReleaseRequired) return;
            if (page == Page.Jaksa)
            {
                float turn = (Pressed(KeyCode.D, KeyCode.RightArrow) ? 1 : 0) - (Pressed(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
                if (hiActive) { StepHi(Time.fixedDeltaTime, Pressed(KeyCode.W, KeyCode.UpArrow), Input.GetKey(KeyCode.Space), turn, Pressed(KeyCode.S, KeyCode.DownArrow)); return; }
                float previousExploreSpeed = jaksa.Speed;
                jaksa.Step(Time.fixedDeltaTime, Pressed(KeyCode.W, KeyCode.UpArrow),
                    Input.GetKey(KeyCode.Space), turn, Pressed(KeyCode.S, KeyCode.DownArrow));
                CompleteExploreControls(previousExploreSpeed);
                if(jaksa.Safety.Failure.HasValue)ShowJaksaPause();
                return;
            }
            if (page != Page.Drive) return;
            int priorStage = journey.Stage;
            int priorCheckpoint = journey.Save.checkpoint;
            if (learner != null)
            {
                if (journey.PhoneRinging)
                {
                    toast.text = "CALL IGNORED · STAY FOCUSED";
                    toastUntil = Time.unscaledTime + 3;
                }
                demonstration.Step(Time.fixedDeltaTime);
                if (demonstration.Finished || journey.Stage != priorStage) EndDemonstration();
                return;
            }
            if (BeginDemoTutorialStep()) return;
            if (demoTutorial?.IsActive == true)
            {
                // Keep traffic moving while the teaching step holds the vehicle stopped.
                // LearningControls also blocks scoring/progression while this step owns input.
                if (demoTutorial.Simulation == TutorialSimulation.BrakeVehicle)
                    journey.Step(Time.fixedDeltaTime, false, true, 0);
                demoTutorial.UpdateReadiness(journey);
                return;
            }
            float steer = (Pressed(KeyCode.D, KeyCode.RightArrow) ? 1 : 0)
                - (Pressed(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
            float previousSpeed = journey.Speed;
            bool reverse = Pressed(KeyCode.S, KeyCode.DownArrow);
            // The guided braking exercise owns S until normal driving resumes.
            // Ordinary driving retains S brake-then-reverse and Space brake-only.
            journey.Step(Time.fixedDeltaTime, Pressed(KeyCode.W, KeyCode.UpArrow),
                Input.GetKey(KeyCode.Space) || journey.LearningControls && reverse, steer,
                reverse && !journey.LearningControls);
            CompleteControlsPractice(previousSpeed);
            if (page != Page.Drive) return;
            if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
            else if (journey.Activity != null && journey.Finished) ShowMissionProgress();
            else if (journey.Stage != priorStage || journey.Save.checkpoint != priorCheckpoint) AnnounceCheckpoint();
        }

        void LateUpdate()
        {
            if (journey == null) return;
            if (page == Page.Menu)
            {
                PresentHomeShowcase(Time.unscaledDeltaTime);
                return;
            }
            RefreshProgressiveCue();
            float presentationDt = page == Page.Controls || page == Page.TutorialBrake || inputReleaseRequired ? 0 : Time.unscaledDeltaTime;
            RefreshVehiclePreview(Time.unscaledDeltaTime);
            float look = Mathf.Clamp(heldLook + (Input.GetKey(KeyCode.X) ? 1 : 0) - (Input.GetKey(KeyCode.Z) ? 1 : 0), -1, 1);
            UpdateSound(Time.unscaledDeltaTime);
            if (page == Page.CityPreview) return;
            if (page == Page.Jaksa || page == Page.JaksaPause || page == Page.Controls && controlsDrivingPage == Page.Jaksa)
            {
                if (hiActive) hi.Present(page == Page.Jaksa && !inputReleaseRequired ? presentationDt : 0, page == Page.Jaksa && !inputReleaseRequired, look);
                else jaksa.Present(presentationDt, page == Page.Jaksa && !inputReleaseRequired, look);
                if (page == Page.Jaksa) RefreshJaksaHud();
                return;
            }
            district.Present(journey, hood, presentationDt, page == Page.Drive && !inputReleaseRequired, look, page == Page.Menu);
            if (page == Page.Drive) RefreshHud();
        }

        void UpdateSound(float dt)
        {
            bool driving = page == Page.Drive && !inputReleaseRequired && !journey.Failure.HasValue;
            var frame = new SoundFrame { SirenDist = float.PositiveInfinity, BellDist = float.PositiveInfinity,
                DecorativeAmbienceSuppressed = ControlsOnboardingActive };
            if (driving)
            {
                frame.Speed = Mathf.Abs(journey.Speed);
                frame.Throttle = journey.Throttle;
                frame.Motorcycle = journey.Mode == VehicleMode.Motorcycle;
                frame.Ringing = journey.PhoneRinging;
                frame.Horn = learner == null && Input.GetKey(KeyCode.H);
                if (journey.Ambulance.HasValue) frame.SirenDist = Vector2.Distance(journey.Position, journey.Ambulance.Value);
                if (journey.RailWarning) frame.BellDist = Vector2.Distance(journey.Position, new Vector2(-180, Boulevard.RailwayZ));
            }
            if (page == Page.Jaksa && !inputReleaseRequired)
            {
                frame.Speed = Mathf.Abs(ExploreSpeed);
                frame.Throttle = Pressed(KeyCode.W, KeyCode.UpArrow) || Pressed(KeyCode.S, KeyCode.DownArrow) ? 1 : 0;
                frame.Motorcycle = mode == VehicleMode.Motorcycle;
                frame.Horn = Input.GetKey(KeyCode.H);
            }
            sound.UpdateFrame(dt, frame);
            if (hiActive) hi.Environment.Water.SetAudio(sound.Muted || !sound.AmbienceEnabled ? 0 : sound.Volume, page == Page.Jaksa && !inputReleaseRequired);
            bool blink = Time.unscaledTime % .7f < .42f;
            if (blink != blinkWas && (driving && journey.Indicator != 0 || page == Page.Jaksa && !inputReleaseRequired && ExploreIndicator != 0)) sound.Tick(blink);
            blinkWas = blink;
        }

        static bool Pressed(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);
        void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            heldLook = 0;
            demoTutorial?.ResetInput();
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
            demoTutorial = null;
            demonstration = null;
            practiceStart = -1;
            learner = null;
            controlsOriginal = null;
            entryTutorial = false;
            if (jaksa != null) jaksa.Street.Root.SetActive(false);
            trainingRoot.SetActive(true);
            district.RestoreAtmosphere();
            progress.tooltip = "Pause / progress [Tab]";
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
            var wordmark = Resources.Load<Texture2D>("UI/Brand/RoadSenseWordmark");
            if (wordmark != null)
            {
                var logo = new Image { image = wordmark, scaleMode = ScaleMode.ScaleToFit,
                    tooltip = GameText.Get("brand.name"), pickingMode = PickingMode.Ignore };
                logo.AddToClassList("home-logo");
                dialog.Add(logo);
            }
            else
                AddLabel(dialog, GameText.Get("brand.name"), "hero home-wordmark");
            AddLabel(dialog, GameText.Get("home.subtitle"), "home-tagline");
            var actions = Element(dialog, "home-actions");
            AddActionButton(actions, ActionKind.Play, GameText.Get("home.play"), ShowPracticeChooser, "primary home-action");
            AddButton(actions, GameText.Get("home.mission"), ShowPlayChooser, "quiet home-action");
            AddActionButton(actions, ActionKind.Options, GameText.Get("home.options"), ShowOptions, "quiet home-action");
            AddButton(actions, GameText.Get("credits.title"), ShowCreditsPage, "quiet home-action");
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                AddButton(actions, GameText.Get("home.exit"), Application.Quit, "quiet home-action");
        }

        System.Action journeyChooserBack;
        void BackFromJourneyChooser() => (journeyChooserBack ?? ShowMenu)();

        bool practiceChooser;
        void ShowPlayChooser() => ShowJourneyChooser(false);
        void ShowPlayChooser(bool expandModes) => ShowJourneyChooser(false);
        void ShowPracticeChooser()
        {
            confirmNew = false;
            ShowJourneyChooser(true);
        }

        void ShowJourneyChooser(bool practice)
        {
            // Rebuilding for a vehicle change must retain the original return destination.
            if (page == Page.Menu || journeyChooserBack == null) journeyChooserBack = ShowMenu;
            if (page == Page.Checkpoint && pitchDemo && demoTutorial != null)
            {
                var previousJourney = journey;
                var previousTutorial = demoTutorial;
                bool previousEntry = entryTutorial;
                journeyChooserBack = () =>
                {
                    journey = previousJourney;
                    demoTutorial = previousTutorial;
                    pitchDemo = true;
                    entryTutorial = previousEntry;
                    ShowPitchComplete();
                };
            }
            practiceChooser = practice;
            PrepareMenuScene();
            SetPage(Page.PlayChooser);
            AddLabel(dialog, GameText.Get(practice ? "home.practice" : "home.mission"), "heading");
            BuildSelectionBrowser(practice ? PracticeSelections() : PlaySelections(), BackFromJourneyChooser);
        }

        void AddChooserVehicle(VisualElement parent, VehicleMode choice, string title, DrivingGlyph.Kind glyph)
        {
            bool selected = mode == choice;
            var card = AddButton(parent, "", () => SelectMode(choice),
                selected ? "chooser-vehicle selected" : "chooser-vehicle quiet");
            card.tooltip = title + (selected ? GameText.Get("text.selected") : GameText.Get("text.select_vehicle"));
            card.Add(new DrivingGlyph(glyph));
            AddLabel(card, title, "chooser-vehicle-title");
            foreach (var child in card.Children()) child.pickingMode = PickingMode.Ignore;
        }

        void ShowOptions()
        {
            PrepareMenuScene();
            SetPage(Page.Options);
            var content = ScrollDialog();
            AddLabel(content, GameText.Get("home.options"), "heading");
            AddLabel(content, GameText.Get("text.sound_controls"), "menu-section-title");
            AddSoundControlsBody(content);
            AddControlsHelp(content);
            AddActionButton(content, ActionKind.Home, GameText.Get("text.main_menu_esc"), ShowMenu, "quiet exit-action");
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
            CloseHi();
            trainingRoot.SetActive(false);
            jaksa ??= new JaksaDrive(transform);
            jaksa.Reset(mode);
            progress.tooltip = GameText.Get("text.pause_menu_esc");
            root.Q<Label>(className: "controls").text = "";
            EnterHumanDrive(Page.Jaksa);
            Show(root.Q("steps"), false);
            Show(phone, false); Show(toast, false);
            Show(root.Q("pickup"), false);
        }

        void RefreshJaksaHud()
        {
            if (hiActive) { RefreshHiHud(); return; }
            location.text = GameText.Get("text.mega_kuningan_south_jakarta");
            title.text = jaksa.AtEnd ? GameText.Get("text.sopo_del_exploration_complete") : GameText.Get("text.sea_labs_indonesia_sopo_del");
            instruction.text = jaksa.AtEnd ? GameText.Get("text.press_r_to_restart_or_esc_to_return_to")
                : jaksa.VehicleCollision ? GameText.Get("text.vehicle_collision_stop_and_leave_space_before_continuing")
                : jaksa.CurbContact ? GameText.Get("text.curb_ahead_steer_back_onto_the_road")
                : jaksa.TrafficContact ? GameText.Get("text.keep_your_distance_wait_for_a_safe_gap_ahead")
                : GameText.Get("text.follow_the_road_and_keep_a_safe_distance");
            detail.text = GameText.Get("hud.explore_remaining", Mathf.RoundToInt(Mathf.Max(0,246-jaksa.Position.y)));
            routeCount.text = GameText.Get("text.sopo_del_approx_250_m");
            speed.text = (jaksa.Speed < -.05f ? "R " : "") + Mathf.RoundToInt(Mathf.Abs(jaksa.Speed)*3.6f).ToString("00");
            RefreshSpeedFeedback(jaksa.Safety,jaksa.PostedLimitKph);
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
            if (hiActive) { ShowHiProgress(); return; }
            SetPage(Page.JaksaPause);
            AddLabel(dialog, GameText.Get("text.sopo_del_mega_kuningan"), "kicker");
            AddLabel(dialog, jaksa.Safety.Failure.HasValue ? Lesson.All[(int)jaksa.Safety.Failure.Value].Title : GameText.Get("text.journey_paused"), "heading");
            AddLabel(dialog, jaksa.Safety.Failure.HasValue ? Lesson.All[(int)jaksa.Safety.Failure.Value].Explanation : GameText.Get("text.explore_about_250_meters_of_jl_mega_kuningan_barat"), "copy");
            AddControlsHelp(dialog);
            var actions=Element(dialog,"row actions");
            if (!jaksa.Safety.Failure.HasValue) AddActionButton(actions, ActionKind.Next, GameText.Get("text.continue"),()=>EnterHumanDrive(Page.Jaksa),"primary");
            AddActionButton(actions, ActionKind.Retry, GameText.Get("text.restart_road_section"),()=>{jaksa.Reset(mode);EnterHumanDrive(Page.Jaksa);},"quiet");
            AddActionButton(actions, ActionKind.Home, GameText.Get("text.main_menu"),ShowMenu,"quiet");
        }


        void CloseCityPreview()
        {
            CloseHi();
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
            AddLabel(dialog, GameText.Get("text.choose_map"), "heading");
            BuildSelectionBrowser(CitySelections(), () => ShowJourneyChooser(practiceChooser));
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
                AddLabel(dialog, GameText.Get("text.map_preview_not_a_practice_route"), "kicker");
                AddLabel(dialog, GameText.Get("text.wasd_arrows_pan_zoom_r_reset"), "copy");
                AddLabel(dialog, GameText.Get("text.buildings_and_roads_from_osm_some_heights_and_widths"), "copy");
                var actions = Element(dialog, "row actions");
                AddButton(actions, "+", () => cityPreview.Zoom(-.2f), "quiet");
                AddButton(actions, "−", () => cityPreview.Zoom(.2f), "quiet");
                AddButton(actions, GameText.Get("text.reset"), () => cityPreview.Reset(), "quiet");
                AddButton(actions, GameText.Get("text.choose_map_esc"), ShowCityMaps, "primary");
                AddActionButton(actions, ActionKind.External, GameText.Get("text.osm_source"), () => Application.OpenURL("https://www.openstreetmap.org/copyright"), "quiet");
            }
            catch (Exception error)
            {
                ShowCityMaps();
                AddLabel(dialog, GameText.Get("maps.open_error", error.Message), "copy negative");
                Debug.LogException(error);
            }
        }

        void StartPitchDemo() => StartTutorial(DemoTutorialScenarios.PasarBunga);

        void RestartDemoTutorial() => StartTutorial(demoTutorial?.Scenario ?? DemoTutorialScenarios.PasarBunga);

        public void StartTutorial(TutorialScenario scenario)
        {
            demoTutorial = new TutorialRunner(scenario);
            demonstration = null;
            controlsOriginal = null;
            demoStartedAt = Time.realtimeSinceStartupAsDouble;
            demoFinishedAt = -1;
            CloseCityPreview();
            pitchDemo = true;
            practiceStart = -1;
            learner = null;
            journey = scenario.CreateJourney(mode);
            journey.LearningControls = scenario.Steps.Count > 0;
            hood = false;
            district.Snap();
            progress.tooltip = "Pause demo  [Tab]";
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
            RememberEntryTutorialCompletion();
            if (demoFinishedAt < 0) demoFinishedAt = Time.realtimeSinceStartupAsDouble;
            PlayerPrefs.SetInt(ControlsCompleteKey, 1);
            PlayerPrefs.Save();
            SetPage(Page.Checkpoint);
            var sheet = Result(GameText.Get("tutorial.complete"), "good", GameText.Get(demoTutorial.Scenario.TitleKey));
            var content = ResultBody(sheet);
            ShowTutorialScore(content);
            var actions = Element(sheet, "sheet-actions");
            AddActionButton(actions, ActionKind.Practice, GameText.Get(practiceChooser ? "home.practice" : "navigation.explore_modes"),
                () => { if (practiceChooser) ShowPracticeChooser(); else ShowPlayChooser(true); }, "big primary");
            AddActionButton(actions, ActionKind.Retry, GameText.Get("demo.replay"), RestartDemoTutorial, "big quiet");
            AddActionButton(actions, ActionKind.Home, GameText.Get("navigation.return_menu"), BackToMenu, "big quiet");
        }

        void OnDestroy()
        { StopHomeShowcase(Page.Menu, tearingDown: true); CloseHi(); vehiclePreview?.Dispose(); cityPreview?.Dispose(); district?.Mirrors.Dispose(); district?.Signals.Dispose(); jaksa?.Mirrors.Dispose(); }

        void SelectMode(VehicleMode value)
        {
            bool choosing = page == Page.PlayChooser;
            mode = value;
            confirmNew = false;
            if (choosing) ShowJourneyChooser(practiceChooser); else ShowMenu();
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
            BeginPracticeScore();
            progress.tooltip = GameText.Get("text.progress_tab");
            district.Snap();
            toast.text = GameText.Get("mission.practice_started", stage + 1);
            toastUntil = Time.unscaledTime + 5;
            if (stage == 0) ShowPreflight();
            else { EnterHumanDrive(Page.Drive); if (page == Page.Drive) RefreshHud(); }
        }

        void ShowPreflight()
        {
            SetPage(Page.Preflight);
            bool motor = mode == VehicleMode.Motorcycle;
            AddLabel(dialog, GameText.Get("text.01_kenanga_garage"), "kicker");
            AddLabel(dialog, GameText.Get("text.before_you_drive"), "heading");
            if (journey.Activity != null) AddLabel(dialog, journey.Activity.Title, "kicker section");
            AddLabel(dialog, motor ? GameText.Get("text.prepare_two_sni_helmets_and_turn_on_the_headlight")
                : GameText.Get("text.check_the_driver_and_front_passenger_seat_belts"), "copy");
            var checklist = Element(dialog, "section");
            Equipment(checklist, motor ? Rule.Helmet : Rule.SeatBelt, motor ? GameText.Get("text.rider_s_sni_helmet") : GameText.Get("text.driver_s_seat_belt"), journey.DriverGear, () => journey.DriverGear = !journey.DriverGear);
            Equipment(checklist, motor ? Rule.Helmet : Rule.SeatBelt, motor ? GameText.Get("text.passenger_s_sni_helmet") : GameText.Get("text.front_passenger_s_seat_belt"), journey.PassengerGear, () => journey.PassengerGear = !journey.PassengerGear);
            if (motor) Equipment(checklist, Rule.Headlight, GameText.Get("text.headlight"), journey.Headlight, journey.ToggleHeadlight);
            var actions = Element(dialog, "row actions");
            actions.style.justifyContent = Justify.FlexEnd;
            AddActionButton(actions, ActionKind.Home, GameText.Get("text.menu"), BackToMenu, "quiet");
            AddActionButton(actions, ActionKind.Play, GameText.Get("text.start_driving"), () =>
            {
                if (journey.Depart())
                {
                    if (journey.Finished) ShowProgress();
                    else { SetPage(Page.Drive); AnnounceCheckpoint(); if (page == Page.Drive) EnterHumanDrive(Page.Drive); }
                }
                else if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
            }, "primary");
        }

        void Equipment(VisualElement group, Rule rule, string label, bool ready, Action change)
        {
            var card = AddButton(group, "", () => { change(); ShowPreflight(); },
                ready ? "option selected equipment-guide" : "option equipment-guide");
            card.Add(new RuleGuideIcon(rule));
            var copy = Element(card, "rule-guide-copy");
            AddLabel(copy, label, "subhead");
            string action = rule == Rule.SeatBelt ? "guide.fasten" : rule == Rule.Helmet ? "guide.helmet" : "guide.lamp";
            AddLabel(copy, GameText.Get(ready ? "guide.ready" : action), "copy");
            AddLabel(card, GameText.Get(ready ? "guide.done" : "guide.click"), "equipment-guide-state");
            card.tooltip = GameText.Get(ready ? "guide.undo" : action);
            foreach (var child in card.Query<VisualElement>().ToList()) child.pickingMode = PickingMode.Ignore;
        }

        void ShowLesson(Rule rule)
        {
            sound.Chime(true);
            SetPage(Page.Lesson);
            var lesson = Lesson.All[(int)rule];
            bool yellowTraining = journey.Activity == null && (journey.Stage == 5 || journey.Stage == 6)
                && journey.MainPhase == 1 && (rule == Rule.Signal || rule == Rule.StopBox);
            bool legal = lesson.HasSource && !yellowTraining;
            var sheet = Result(legal ? GameText.Get("text.violation") : GameText.Get("text.caution"), legal ? "bad" : "warn",
                yellowTraining ? GameText.Get("text.amber_light_prepare_to_stop") : lesson.Title);
            var body = ResultBody(sheet);
            AddLabel(body, legal ? GameText.Get("text.traffic_violation") : yellowTraining ? GameText.Get("text.practice_rule") : GameText.Get("text.safety_advice"),
                legal ? "kind legal" : "kind advice");
            AddLabel(body, GameText.Get("text.what_to_do_next"), "result-label");
            AddLabel(body, yellowTraining ? GameText.Get("text.stop_before_the_line_and_continue_on_green") : lesson.Action, "sheet-action");
            AddLabel(body, yellowTraining
                ? GameText.Get("text.amber_after_green_means_the_light_will_turn_red")
                : lesson.Explanation, "sheet-text lesson-explanation");
            if (journey.Activity != null) AddLabel(body, GameText.Get("mission.attempt_title", journey.Activity.Title, journey.Save.attempts), "kicker");
            if (legal) AddLabel(body, lesson.Regulation + " · " + lesson.Citation, "rule-source");
            Fold(body, GameText.Get("text.why_does_this_matter"), content =>
                AddLabel(content, Why(rule), "sheet-text"));
            if (legal) Fold(body, GameText.Get("text.law_and_penalties"), content =>
            {
                var law = LawDetails.For(rule);
                AddLabel(content, lesson.Regulation + " · " + lesson.Citation, "sheet-text");
                AddLabel(content, law.Meaning, "sheet-text");
                AddLabel(content, GameText.Get("text.penalty"), "result-label");
                AddLabel(content, law.Sanction, "sheet-text penalty");
                AddLabel(content, GameText.Get("text.the_amount_above_is_a_maximum_not_a_fixed"), "sheet-text legal-note");
                AddActionButton(content, ActionKind.External, GameText.Get("text.2026_criminal_penalty_adjustment"), () => Application.OpenURL(LawDetails.Amendment), "quiet");
                AddActionButton(content, ActionKind.External, GameText.Get("text.open_official_source"), () => Application.OpenURL(lesson.Url), "quiet");
                if (rule == Rule.Stop) AddActionButton(content, ActionKind.External, GameText.Get("text.traffic_sign_regulations"), () => Application.OpenURL(Lesson.Signs), "quiet");
                if (rule == Rule.Signal) AddActionButton(content, ActionKind.External, GameText.Get("text.traffic_signal_regulations"), () => Application.OpenURL(Lesson.Signals), "quiet");
                if (rule == Rule.Speed) AddActionButton(content, ActionKind.External, GameText.Get("text.speed_regulations"), () => Application.OpenURL(Lesson.Limits), "quiet");
            });
            if (yellowTraining) Fold(body, GameText.Get("text.what_amber_means"), content =>
            {
                AddLabel(content, GameText.Get("text.pm_49_2014_pasal_6_ayat_4_huruf_a"), "sheet-text");
                AddActionButton(content, ActionKind.External, GameText.Get("text.open_official_source"), () => Application.OpenURL(Lesson.Signals), "quiet");
            });
            var actions = Element(sheet, "sheet-actions");
            AddLabel(body, GameText.Get(journey.Activity != null ? "rating.recovery_mission" : practiceStart >= 0 || pitchDemo ? "rating.recovery" : "rating.recovery_journey"), "copy");
            AddActionButton(actions, ActionKind.Play, GameText.Get("rating.continue"), ContinueAfterViolation, "big primary");
            var retry = AddActionButton(actions, ActionKind.Retry, GameText.Get(journey.Activity != null ? "rating.restart_mission" : "example.retry"), Retry, "big quiet");
            if (journey.Stage > 0)
            {
                bool available = ExampleProvider.TryGetExample(journey, out _);
                var example = AddButton(actions, GameText.Get("example.action"), StartDemonstration, "big quiet");
                example.SetEnabled(available);
                if (!available) AddLabel(body, GameText.Get("example.unavailable"), "copy");
            }
            AddActionButton(actions, ActionKind.Home, GameText.Get("text.main_menu"), BackToMenu, "quiet menu-action");
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
            AddLabel(button, title, "fold-title").pickingMode = PickingMode.Ignore;
            sign = AddLabel(button, "+", "fold-sign");
            sign.pickingMode = PickingMode.Ignore;
            fill(details);
            fold.Add(details);
            if (expanded) { fold.AddToClassList("open"); sign.text = "-"; }
        }

        static string Why(Rule rule) => rule switch
        {
            Rule.SeatBelt => GameText.Get("text.seat_belts_restrain_your_body_during_sudden_braking_or"),
            Rule.Helmet => GameText.Get("text.a_properly_fitted_and_fastened_helmet_helps_protect_your"),
            Rule.Headlight => GameText.Get("text.headlights_help_other_road_users_spot_motorcycles_earlier"),
            Rule.Stop or Rule.Junction or Rule.Signal => GameText.Get("text.stopping_and_giving_way_allow_time_to_spot_crossing"),
            Rule.Pedestrian => GameText.Get("text.pedestrians_lack_the_protection_a_vehicle_provides_wait_until"),
            Rule.Indicator or Rule.Gap or Rule.ReturnLeft => GameText.Get("text.indicators_and_adequate_space_help_other_drivers_understand_your"),
            Rule.KeepLeft or Rule.NoEntry => GameText.Get("text.following_the_permitted_direction_reduces_the_risk_of_meeting"),
            Rule.Speed or Rule.BoulevardSpeed => GameText.Get("text.lower_speeds_give_you_time_to_react_especially_when"),
            Rule.Railway => GameText.Get("text.trains_need_a_very_long_distance_to_stop_barriers"),
            Rule.Emergency => GameText.Get("text.giving_space_helps_an_ambulance_reach_care_sooner_without"),
            Rule.RightArrow or Rule.MovementLane or Rule.TurnSignal => GameText.Get("text.correct_lanes_and_signals_separate_traffic_flows_drivers_must"),
            Rule.Phone => GameText.Get("text.distraction_makes_hazards_ahead_easier_to_miss"),
            _ => GameText.Get("text.keep_space_around_your_vehicle_so_you_have_time")
        };

        IViolationExampleProvider ExampleProvider => demoTutorial?.Scenario.Examples ?? RouteViolationExamples.Instance;

        void StartDemonstration()
        {
            if (!ExampleProvider.TryGetExample(journey, out var example))
            {
                return;
            }
            demonstration = new ViolationExampleSession(journey, example);
            learner = demonstration.FailedJourney;
            journey = demonstration.Preview;
            district.Snap();
            SetPage(Page.Drive);
            progress.tooltip = GameText.Get("example.end");
            toast.text = GameText.Get("example.running");
            toastUntil = Time.unscaledTime + 3;
        }

        void EndDemonstration()
        {
            if (demonstration == null) return;
            journey = demonstration.FailedJourney;
            learner = null;
            demonstration = null;
            progress.tooltip = GameText.Get(pitchDemo ? "example.pause_demo" : "example.progress");
            district.Snap();
            ShowLesson(journey.Failure.Value);
        }

        void Retry()
        {
            sound.RetryCue();
            if (page == Page.Lesson && journey.Activity != null)
            { StartMission(journey.Activity, journey.Save, restart: true); return; }
            if (practiceStart >= 0 && page == Page.Checkpoint && completedPracticeStage >= 0)
            { JumpToMission(completedPracticeStage); return; }
            if (practiceStart >= 0) BeginPracticeScore();
            if (pitchDemo) { RestartDemoTutorial(); return; }
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
            var sheet = journey.Finished
                ? Result(GameText.Get("text.journey_complete"), "good", GameText.Get("text.arrived_safely")) : null;
            var content = sheet != null ? ResultBody(sheet) : ScrollDialog();
            if (!journey.Finished)
            {
                AddLabel(content, GameText.Get("journal.title"), "kicker");
                AddLabel(content, GameText.Get("text.practice_progress"), "heading");
            }
            AddLabel(content, pitchDemo ? GameText.Get("text.demo_right_of_way_practice_progress_unchanged") : practiceStart >= 0
                ? GameText.Get("journal.practice_summary", practiceStart + 1,
                    mode == VehicleMode.Car ? GameText.Get("text.car_a5cdf0") : GameText.Get("text.motorcycle_965701"))
                : GameText.Get("journal.checkpoint_summary", journey.Stage, All.Length,
                    mode == VehicleMode.Car ? GameText.Get("text.car_a5cdf0") : GameText.Get("text.motorcycle_965701")), "copy");
            if (pitchDemo) AddLabel(content, GameText.Get("journal.session_time", DemoDuration()), "copy");
            if (journey.Save.before >= 0)
                AddLabel(content, journey.Save.after >= 0
                    ? GameText.Get("journal.quiz_scores", journey.Save.before, journey.Save.after)
                    : GameText.Get("journal.pre_drive_score", journey.Save.before), "copy");

            var frequent = Enumerable.Range(0, Lesson.All.Length)
                .Where(i => journey.Save.mistakes[i] > 0)
                .OrderByDescending(i => journey.Save.mistakes[i]).Take(3).ToArray();
            var box = Element(content, "note");
            AddLabel(box, frequent.Length > 0 ? GameText.Get("text.most_frequent_retries") : GameText.Get("text.practice_notes"), "kicker");
            if (frequent.Length == 0) AddLabel(box, GameText.Get("text.no_rules_need_another_attempt_yet"), "copy");
            foreach (int i in frequent)
                AddLabel(box, journey.Save.mistakes[i] + "×   " + Lesson.All[i].Title, "copy");
            if (frequent.Length > 0) AddLabel(content, GameText.Get("journal.next_practice", Lesson.All[frequent[0]].Action), "copy");

            var actions = Element(sheet ?? content, sheet != null ? "sheet-actions" : "row actions");
            if (!journey.Finished) AddActionButton(actions, ActionKind.Play, GameText.Get("text.resume_driving"), ReturnToJourney, "primary");
            if (pitchDemo) AddActionButton(actions, ActionKind.Retry, GameText.Get("text.replay_demo_r"), RestartDemoTutorial, "quiet");
            AddActionButton(actions, ActionKind.Home, GameText.Get("text.main_menu"), BackToMenu, journey.Finished ? "big primary" : "quiet");
            if (!pitchDemo)
                Fold(content, GameText.Get("text.practice_from_a_checkpoint"), picker =>
                {
                    AddLabel(picker, GameText.Get("text.start_a_separate_practice_session_your_saved_journey_progress"), "copy");
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
            if (toggle != null) toggle.text = sound.Muted ? GameText.Get("text.sound_off_m_4e1c35") : GameText.Get("text.sound_on_m");
            SetActionIcon(toggle, sound.Muted ? ActionKind.Muted : ActionKind.Sound);
            toast.text = sound.Muted ? GameText.Get("text.sound_muted_press_m_to_unmute") : GameText.Get("text.sound_on");
            toastUntil = Time.unscaledTime + 3;
        }

        void AddSoundControls(VisualElement parent) => Fold(parent, GameText.Get("options.sound_presentation"), AddSoundControlsBody);

        void AddSoundControlsBody(VisualElement parent)
        {
            var button = AddActionButton(parent, sound.Muted ? ActionKind.Muted : ActionKind.Sound, sound.Muted ? GameText.Get("text.sound_off_m_4e1c35") : GameText.Get("text.sound_on_m"), null, "quiet tiny");
            button.name = "soundToggle";
            button.clicked += ToggleSound;
            var ambience = AddActionButton(parent, sound.AmbienceEnabled ? ActionKind.Sound : ActionKind.Muted, sound.AmbienceEnabled ? GameText.Get("text.ambience_on") : GameText.Get("text.ambience_off"), null, "quiet tiny");
            ambience.clicked += () =>
            {
                sound.SetAmbience(!sound.AmbienceEnabled);
                SetActionIcon(ambience, sound.AmbienceEnabled ? ActionKind.Sound : ActionKind.Muted);
                ambience.text = sound.AmbienceEnabled ? GameText.Get("text.ambience_on") : GameText.Get("text.ambience_off");
            };
            AddLabel(parent, GameText.Get("text.turn_off_ambience_for_presentations_sirens_and_warnings_remain"), "copy");
            var volume = new Slider(0, 100) { name = "masterVolume", value = sound.Volume * 100 };
            volume.label = GameText.Get("options.volume", Mathf.RoundToInt(volume.value));
            volume.AddToClassList("volume-control");
            volume.RegisterValueChangedCallback(change =>
            {
                sound.SetVolume(change.newValue / 100f);
                volume.label = GameText.Get("options.volume", Mathf.RoundToInt(change.newValue));
            });
            parent.Add(volume);
            AddButton(parent, GameText.Get("text.test_sound"), () => sound.Chime(false), "quiet tiny");
            AddLabel(parent, GameText.Get("text.h_horn_m_toggle_sound"), "copy");
        }

        void ReturnToJourney()
        { if (PitchComplete) ShowPitchComplete(); else EnterHumanDrive(Page.Drive); }

        void AnnounceCheckpoint()
        {
            sound.Chime(false);
            if (PitchComplete) { ShowPitchComplete(); return; }
            if (journey.Activity != null)
            {
                toast.text = GameText.Get("text.progress_saved_9e9c85");
                toastUntil = Time.unscaledTime + 3;
                return;
            }
            SetPage(Page.Checkpoint);
            int completed = journey.Stage - 1;
            if (practiceStart >= 0) { ShowPracticeScore(completed); return; }
            var sheet = Result(GameText.Get("text.mission_complete_405e76"), "good",
                GameText.Get("mission.completed_title", journey.Stage, All[completed].Title));
            string[] recaps =
            {
                GameText.Get("text.everyone_had_their_safety_equipment_ready_before_the_vehicle"),
                GameText.Get("text.you_stopped_at_the_stop_sign_gave_way_and"),
                GameText.Get("text.you_gave_priority_to_pedestrians_and_main_road_traffic"),
                GameText.Get("text.you_followed_the_road_direction_signaled_and_changed_lanes"),
                GameText.Get("text.you_controlled_your_speed_and_stayed_focused_through_the"),
                GameText.Get("text.you_chose_the_straight_through_lane_obeyed_the_light"),
                GameText.Get("text.you_signaled_left_and_turned_after_green_once_pedestrians"),
                GameText.Get("text.you_turned_right_from_the_correct_lane_after_the"),
                GameText.Get("text.you_waited_for_the_train_to_pass_and_the"),
                GameText.Get("text.you_safely_gave_way_to_the_ambulance_without_exceeding")
            };
            var body = ResultBody(sheet);
            AddLabel(body, recaps[completed], "sheet-lead");
            AddLabel(body, practiceStart >= 0 ? GameText.Get("text.practice_complete_your_journey_progress_stays_saved")
                : GameText.Get("mission.checkpoint_saved", journey.Stage, All.Length,
                    journey.Finished ? GameText.Get("text.entire_route_complete") : GameText.Get("text.continue_along_the_same_road")), "checkpoint-saved");
            if (!journey.Finished)
            {
                AddLabel(body, GameText.Get("mission.up_next", journey.Stage + 1, All[journey.Stage].Title), "result-label");
                AddLabel(body, All[journey.Stage].Instruction, "sheet-text");
            }
            var actions = Element(sheet, "sheet-actions");
            var next = AddActionButton(actions, ActionKind.Next, journey.Finished ? practiceStart >= 0 ? GameText.Get("text.practice_results") : GameText.Get("text.final_quiz_5f0543") : GameText.Get("mission.continue_next", journey.Stage + 1),
                ContinueCheckpoint, "big primary");
            next.name = "continue-mission";
            if (!journey.Finished) AddActionButton(actions, ActionKind.Retry, GameText.Get("text.retry_from_checkpoint"), Retry, "big quiet");
            AddActionButton(actions, ActionKind.Home, GameText.Get("text.menu"), BackToMenu, "big quiet");
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
                string[] names={GameText.Get("text.red"),GameText.Get("text.amber"),GameText.Get("text.green")};
                void State(string id,int phase,string prefix)
                {
                    var label=root.Q<Label>(id); label.text=prefix+names[phase];
                    for(int i=0;i<3;i++) label.EnableInClassList(new[] { "signal-red","signal-amber","signal-green" }[i],phase==i);
                }
                State("signalMain",signals.MainPhase,signals.HasArrow?GameText.Get("text.straight_left"):"");
                State("signalRight",signals.ArrowPhase,GameText.Get("text.right_turn"));
                Show(root.Q("signalRight"),signals.HasArrow);
                Show(root.Q("signalArrowZoom"),signals.HasArrow);
                root.Q<Label>("signalHint").text=journey.Stage==7?GameText.Get("text.turn_right_only_on_a_green_arrow_still_yield"):GameText.Get("text.green_still_yield_to_pedestrians_and_check_that_the");
            }
            int stage = journey.Stage;
            float z = journey.Position.y;
            location.text = learner == null ? All[stage].Location : GameText.Get("hud.example_location", All[stage].Location);
            title.text = All[stage].Title;
            string prompt = All[stage].Instruction, signal = "";
            float target = journey.StageEnd;

            if (stage == 1)
            {
                if (journey.Front < StopLine)
                {
                    target = StopLine;
                    if (journey.FullStop) prompt = GameText.Get("text.stop_recorded_yield_to_crossing_traffic");
                }
                else if (z <= StopJunction + 5.5f)
                    prompt = GameText.Get("text.yield_to_vehicles_from_the_left_or_right");
                else if (journey.Front < LightLine)
                {
                    target = LightLine;
                    prompt = journey.LightPhase == 1 ? GameText.Get("text.prepare_to_stop_if_it_is_safe_to_do")
                        : GameText.Get("text.obey_the_traffic_lights_and_white_stop_line");
                    signal = journey.LightPhase switch { 2 => GameText.Get("text.green"), 1 => GameText.Get("text.amber"), _ => GameText.Get("text.red") };
                }
                else prompt = GameText.Get("text.continue_straight_past_the_green_checkpoint_posts");
            }
            else if (stage == 2)
            {
                if (!journey.CrossedZebra)
                {
                    target = Zebra - 2;
                    if (journey.PedestrianBlocksPath) signal = GameText.Get("text.wait_for_pedestrians_77ec69");
                }
                else if (z < MainJunction + 5.5f)
                {
                    target = YieldLine;
                    prompt = GameText.Get("text.yield_to_vehicles_on_the_main_road");
                    if (journey.CrossTraffic.HasValue) signal = GameText.Get("text.watch_crossing_traffic");
                }
                else prompt = GameText.Get("text.continue_straight_toward_jalan_mawar");
            }
            else if (stage == 3)
            {
                target = journey.ReturnedLeft ? All[stage].End : journey.ChangedRight ? Merge : OneWayStart;
                if (journey.ReturnedLeft) prompt = GameText.Get("text.keep_left_and_follow_the_route_to_the_school");
                else if (journey.ChangedRight) prompt = GameText.Get("text.signal_left_q_then_return_to_the_left_lane");
                else if (z < OneWayStart) prompt = GameText.Get("text.keep_left_until_you_enter_the_one_way_road");
                if (journey.LaneTraffic.HasValue && z >= OneWayStart - 7)
                {
                    float gap = journey.LaneTraffic.Value.y - z;
                    signal = GameText.Get("hud.car_right_gap", Mathf.RoundToInt(Mathf.Abs(gap)),
                        gap >= 0 ? GameText.Get("text.ahead") : GameText.Get("text.behind"));
                }
            }
            else if (stage == 4)
            {
                target = journey.Front < SchoolLimit ? SchoolLimit : All[stage].End;
                if (journey.Position.x < -4.25f && !journey.PhoneRinging)
                    prompt = GameText.Get("text.return_to_the_left_lane_when_safe_before_the");
                else if (journey.Front >= SchoolLimit) prompt = GameText.Get("text.control_your_speed_until_the_school_checkpoint");
                signal = GameText.Get("text.30_km_h_limit");
            }
            if (stage >= 5)
            {
                string status;
                if (stage <= 7)
                {
                    status = stage == 7 ? GameText.Get("hud.right_arrow", journey.ArrowPhase == 2 ? GameText.Get("text.green") : GameText.Get("text.red"))
                        : journey.MainPhase == 2 ? GameText.Get("text.green") : journey.MainPhase == 1 ? GameText.Get("text.amber") : GameText.Get("text.red");
                    if (journey.BoulevardPedestrianBlocksPath) status += GameText.Get("text.wait_for_pedestrians");
                    prompt += mode == VehicleMode.Car ? GameText.Get("text.cars_stop_before_the_red_box") : GameText.Get("text.motorcycles_stop_at_the_front_line_of_the_red");
                }
                else if (stage == 8) status = journey.RailWarning ? GameText.Get("text.stop_railway_warning") : GameText.Get("text.barriers_open");
                else status = journey.AmbulancePassed ? GameText.Get("text.ambulance_has_passed")
                    : journey.Ambulance.HasValue ? GameText.Get("hud.ambulance_gap", Mathf.RoundToInt(Mathf.Abs(journey.AmbulanceGap)))
                    : GameText.Get("text.minibus_stopped_in_left_lane");
                if (stage == 9 && journey.Ambulance.HasValue && sound.Muted) status += GameText.Get("text.sound_off_m");
                detail.text = GameText.Get("hud.speed_limit_status", status, 50);
            }
            RefreshMissionPrompt(ref prompt, ref signal, ref target);
            instruction.text = prompt;
            if (stage < 5) detail.text = (signal.Length == 0 ? "" : signal + "   /   ") + Mathf.Max(0, Mathf.RoundToInt(target - journey.Front)) + " M";
            routeCount.text = pitchDemo ? GameText.Get("text.demo_right_of_way") : practiceStart >= 0 ? GameText.Get("hud.practice_mission_count", stage + 1, All.Length)
                : GameText.Get("hud.checkpoint_count", stage, All.Length);
            if (journey.Activity != null)
                routeCount.text = journey.Activity.Kind == MissionKind.SkillTest ? GameText.Get("hud.skill_attempt", journey.Save.attempts)
                    : GameText.Get("hud.mission_sections", journey.Activity.CompletedSteps(journey), 3);
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
            speed.text = (journey.Speed < -.05f ? "R " : "") + Mathf.RoundToInt(journey.Kph).ToString();
            RefreshSpeedFeedback(journey.Safety,journey.PostedLimitKph);
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
            if (page == Page.Menu && value != Page.Menu) StopHomeShowcase(value);
            heldLook=0;
            root.focusController?.focusedElement?.Blur();
            Show(root.Q("missionDrawer"), false);
            RefreshMissionPickerLabel();
            page = value;
            if (page != Page.Drive) Show(root.Q("signalPanel"), false);
            var mirrorImages = root.Q("mirrors").Query<Image>().ToList();
            var textures = (page == Page.Jaksa || page == Page.Controls && controlsDrivingPage == Page.Jaksa) && (hiActive || jaksa != null) ? (hiActive ? hi.Mirrors.Textures : jaksa.Mirrors.Textures) : district.Mirrors.Textures;
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
            dialog.EnableInClassList("credits-card", page == Page.Credits);
            dialog.EnableInClassList("tutorial-card", page == Page.TutorialBrake);
            RefreshHudControls();
            dialog.RemoveFromClassList("rule-card");
            dialog.RemoveFromClassList("result-layout");
            dialog.RemoveFromClassList("selection-browser-card");
            dialog.Clear();
            ShowVehiclePreview(page == Page.Brief);
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
            // A specific flow or the user may already have focused a control
            // while this generic after-layout callback was pending.
            if (root.focusController?.focusedElement is Button focused && buttons.Contains(focused)) return;
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
            readonly string promptKey, explanationKey;
            readonly string[] choiceKeys;
            public string Prompt => GameText.Get(promptKey);
            public string Explanation => GameText.Get(explanationKey);
            public string[] Choices => choiceKeys.Select(key => GameText.Get(key)).ToArray();
            public readonly int Correct;
            public Question(string prompt, string explanation, int correct, params string[] choices)
            { promptKey = prompt; explanationKey = explanation; choiceKeys = choices; Correct = correct; }
        }

        static readonly Question[] Before =
        {
            new("text.what_should_you_do_when_approaching_a_stop_sign", "text.stop_completely_before_the_stop_line_then_check_traffic",
                1, "text.slow_down_and_keep_going", "text.stop_completely_before_the_line", "text.stop_after_crossing_the_line"),
            new("text.who_has_priority_when_a_pedestrian_crossing_is_in", "text.give_priority_to_pedestrians_who_are_crossing",
                2, "text.the_vehicle_that_arrived_first", "text.the_vehicle_sounding_its_horn", "text.pedestrians_who_are_crossing"),
            new("text.your_phone_rings_while_driving_what_is_safe", "text.stay_focused_on_the_road_use_your_phone_after",
                0, "text.ignore_it_until_you_can_stop_safely", "text.answer_while_releasing_the_throttle", "text.read_the_screen_to_see_who_is_calling")
        };
        static readonly Question[] After =
        {
            new("text.the_road_ahead_of_a_stop_sign_looks_empty", "text.a_stop_sign_still_requires_a_complete_stop_before",
                2, "text.continue_at_low_speed", "text.follow_the_vehicle_ahead_without_stopping", "text.stop_completely_check_both_ways_then_move"),
            new("text.a_child_is_still_on_the_pedestrian_crossing_what", "text.wait_until_the_crossing_is_safe_before_continuing",
                0, "text.wait_before_the_crossing_line", "text.pass_on_the_other_side_of_the_crossing", "text.sound_the_horn_then_move_slowly"),
            new("text.you_need_to_return_an_important_call_when_can", "text.stop_somewhere_safe_before_using_your_phone",
                1, "text.while_driving_slowly", "text.after_pulling_over_and_stopping_somewhere_safe", "text.when_the_road_is_straight_and_empty")
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
            AddLabel(dialog, GameText.Get("quiz.question_count", after ? GameText.Get("text.final_quiz_c99b44") : GameText.Get("text.pre_drive_quiz"), quizIndex + 1, 3), "kicker");
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
                    AddLabel(note, choice == question.Correct ? GameText.Get("text.correct") : GameText.Get("text.remember"), "kicker");
                    AddLabel(note, question.Explanation, "copy");
                    AddButton(Element(dialog, "row actions"), quizIndex == 2 ? GameText.Get("text.view_results") : GameText.Get("text.next"), () => NextQuestion(true), "primary");
                }, "option");
            }
            AddActionButton(dialog, ActionKind.Back, GameText.Get("text.back_esc"), BackFromQuiz, "quiet");
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
