using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        readonly HashSet<string> controlsSeen = new();
        readonly HashSet<string> controlsPracticeStarted = new();
        readonly HashSet<string> exploreBraking = new(), exploreStopped = new();
        const string ControlsCompleteKey = "RoadSense.Onboarding.MovementBrake.v1";
        const string EntryTutorialCompleteKey = "RoadSense.Tutorial.pasar-bunga-right-of-way.Completed.v1";
        string completedTutorialScenarioId;
        bool entryTutorial;
        Journey controlsOriginal;
        string controlsPracticeContext;
        bool controlsMoved, controlsBraked;
        public bool ControlsOnboardingActive => page == Page.TutorialBrake || page == Page.Controls || journey?.LearningControls == true
            || (!hiActive && (page == Page.Jaksa || page == Page.JaksaPause) && !exploreStopped.Contains("explore:" + mode));
        readonly HashSet<string> cueActionsSeen = new();
        Page controlsOrigin, controlsDrivingPage;
        bool inputReleaseRequired;
        static readonly KeyCode[] DrivingInputKeys =
        {
            KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Space, KeyCode.Q, KeyCode.E,
            KeyCode.C, KeyCode.L, KeyCode.R, KeyCode.H, KeyCode.F, KeyCode.G, KeyCode.Z,
            KeyCode.X, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.F1, KeyCode.F2,
            KeyCode.Tab, KeyCode.Escape, KeyCode.P
        };

        TutorialRunner demoTutorial;

        void PlayFromHome()
        {
            practiceChooser = false;
            bool completed = PlayerPrefs.GetInt(EntryTutorialCompleteKey, 0) == 1;
            // The old demo stored whole-scenario success only for the current session.
            // Its finish timestamp survives the menu; the movement/braking flag does not
            // establish that the crossing and junction were completed.
            if (!completed && demoFinishedAt >= 0 &&
                (string.IsNullOrEmpty(completedTutorialScenarioId) || completedTutorialScenarioId == DemoTutorialScenarios.PasarBunga.Id) &&
                (demoTutorial == null || demoTutorial.Scenario.Id == DemoTutorialScenarios.PasarBunga.Id))
            {
                PlayerPrefs.SetInt(EntryTutorialCompleteKey, 1);
                PlayerPrefs.Save();
                completed = true;
            }
            if (completed) ShowPlayChooser(true);
            else StartEntryTutorial();
        }

        void RememberEntryTutorialCompletion()
        {
            if (learner != null || !PitchComplete) return;
            completedTutorialScenarioId = demoTutorial.Scenario.Id;
            if (completedTutorialScenarioId != DemoTutorialScenarios.PasarBunga.Id
                || PlayerPrefs.GetInt(EntryTutorialCompleteKey, 0) == 1) return;
            PlayerPrefs.SetInt(EntryTutorialCompleteKey, 1);
            PlayerPrefs.Save();
        }

        void StartEntryTutorial()
        {
            PrepareMenuScene();
            mode = VehicleMode.Car;
            StartPitchDemo();
            entryTutorial = true;
        }

        bool BeginDemoTutorialStep()
        {
            if (!pitchDemo || learner != null || demoTutorial == null || !demoTutorial.TryBegin(journey)) return false;
            if (demoTutorial.Simulation == TutorialSimulation.FreezeWorld)
            {
                SetPage(Page.TutorialBrake);
                AddLabel(dialog, GameText.Get(demoTutorial.Active.HeadingKey), "kicker");
                AddLabel(dialog, GameText.Get(demoTutorial.Active.ActionInstructionKey), "tutorial-instruction");
            }
            return true;
        }

        void ResumeDemoTutorial()
        {
            demoTutorial.AcceptAction(journey);
            EnterDemoTutorialWaiting();
        }

        void EnterDemoTutorialWaiting()
        {
            inputReleaseRequired = false;
            SetPage(Page.Drive);
        }

        void ReadDemoTutorialInput()
        {
            if (!Application.isFocused || demoTutorial?.IsActive != true) return;
            if (demoTutorial.Phase == TutorialPhase.AwaitAction)
            {
                var key = demoTutorial.Active.ActionKey;
                if (demoTutorial.TryAction(journey, Input.GetKey(key), Input.GetKeyDown(key)))
                    EnterDemoTutorialWaiting();
            }
            else
            {
                var completed = demoTutorial.TryContinue(journey, Input.GetKey(KeyCode.W), Input.GetKeyDown(KeyCode.W));
                if (completed == null) return;
                toast.text = GameText.Get(completed.SuccessMessageKey);
                toastUntil = Time.unscaledTime + 5;
            }
        }

        void EnterHumanDrive(Page drivingPage)
        {
            string context = (drivingPage == Page.Jaksa ? "explore:" : "training:") + mode;
            SetPage(drivingPage);
            inputReleaseRequired = true;
        }

        Journey NewControlsPractice()
        {
            var save = JsonUtility.FromJson<JourneySave>(JsonUtility.ToJson(controlsOriginal.Save));
            return new Journey(mode, save, persistent: false, mission: controlsOriginal.Activity)
            { LearningControls = true };
        }

        void CompleteControlsPractice(float previousSpeed)
        {
            if (!journey.LearningControls || controlsOriginal == null) return;
            if (journey.Speed > .2f && Pressed(KeyCode.W, KeyCode.UpArrow)) controlsMoved = true;
            if (controlsMoved && Input.GetKey(KeyCode.S) && previousSpeed > .28f && journey.Speed < previousSpeed)
                controlsBraked = true;
            if (!controlsBraked || journey.Speed > .28f) return;
            journey = controlsOriginal;
            controlsOriginal = null;
            controlsSeen.Add(controlsPracticeContext);
            PlayerPrefs.SetInt(ControlsCompleteKey, 1);
            PlayerPrefs.Save();
            if (entryTutorial)
            {
                ShowPlayChooser();
                return;
            }
            district.Snap();
            inputReleaseRequired = true;
            toast.text = GameText.Get("text.practice_complete_back_to_the_start_normal_rules_are");
            toastUntil = Time.unscaledTime + 6;
        }

        void CompleteExploreControls(float previousSpeed)
        {
            string context = "explore:" + mode;
            if (exploreStopped.Contains(context) || !controlsSeen.Contains(context)) return;
            if (Input.GetKey(KeyCode.S) && previousSpeed > .28f && jaksa.Speed < previousSpeed)
                exploreBraking.Add(context);
            if (!exploreBraking.Contains(context) || jaksa.Speed > .28f) return;
            exploreStopped.Add(context);
            toast.text = GameText.Get("text.practice_complete_drive_as_usual");
            toastUntil = Time.unscaledTime + 5;
        }

        void ShowControls(Page origin, Page drivingPage, bool firstEntry)
        {
            controlsOrigin = origin;
            controlsDrivingPage = drivingPage;
            SetPage(Page.Controls);
            AddLabel(dialog, mode == VehicleMode.Car ? GameText.Get("controls.car.title") : GameText.Get("controls.motorcycle.title"), "heading");
            AddLabel(dialog, learner != null ? GameText.Get("text.the_demonstration_is_paused_close_this_guide_to_continue")
                : GameText.Get("text.the_game_stays_paused_while_this_guide_is_open"), "copy controls-intro");
            var body = new ScrollView(ScrollViewMode.Vertical)
            { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            body.AddToClassList("controls-body");
            dialog.Add(body);
            var groups = Element(body, "control-groups");
            var movement = Element(groups, "control-group");
            AddLabel(movement, "Moving & stopping", "subhead");
            ControlKey(movement, "W / ↑", "Throttle", "Hold to accelerate.", DrivingGlyph.Kind.Throttle);
            ControlKey(movement, "S / ↓", "Brake then reverse", "During normal driving, hold after stopping to reverse slowly. Guided tutorial actions only brake.");
            ControlKey(movement, "SPACE", "Brake only", "Slow or stop without reversing. W brakes reverse movement before driving forward.");
            ControlKey(movement, "A / D", "Steer left / right", "You can also use the arrow keys.", DrivingGlyph.Kind.Steering);
            ControlKey(movement, "Z / X", "Look left / right", "Hold to look. Release to face forward.");
            var decisions = Element(groups, "control-group");
            AddLabel(decisions, GameText.Get("text.signals_views"), "subhead");
            ControlKey(decisions, "Q / E", GameText.Get("text.left_right_indicator"), GameText.Get("text.press_once_press_the_same_key_again_to_turn"));
            ControlKey(decisions, "C", GameText.Get("text.change_camera"), GameText.Get("text.switch_between_chase_camera_and_driver_view"), DrivingGlyph.Kind.Camera);
            if (mode == VehicleMode.Motorcycle)
                ControlKey(decisions, "L", GameText.Get("text.motorcycle_headlight"), GameText.Get("text.press_to_switch_on_or_off"));
            ControlKey(decisions, "ESC / TAB", GameText.Get("text.pause"), GameText.Get("text.open_progress_and_journey_options"));
            Fold(body, GameText.Get("text.more_controls"), extra =>
            {
                ControlKey(extra, "R", hiActive ? "Restart HI mission" : drivingPage == Page.Jaksa ? GameText.Get("text.restart_road_section") : GameText.Get("text.retry_from_checkpoint"), GameText.Get("text.restart_your_current_section"));
                ControlKey(extra, "H", GameText.Get("text.horn"), GameText.Get("text.hold_to_sound_the_horn"));
                ControlKey(extra, "M", GameText.Get("text.sound"), GameText.Get("text.press_to_mute_or_unmute"));
                if (drivingPage == Page.Drive)
                    ControlKey(extra, "F / G", journey.IsMarket ? GameText.Get("text.pickup_phone") : GameText.Get("text.phone"),
                        journey.IsMarket ? GameText.Get("text.f_picks_up_a_passenger_while_stopped_in_the")
                        : GameText.Get("text.f_ignores_a_call_g_answers_stop_somewhere_safe"));
                AddLabel(extra, GameText.Get("text.on_the_violation_screen_r_retries_d_shows_a"), "control-note");
            });
            var footer = Element(dialog, "controls-footer");
            string action = origin == Page.Progress || origin == Page.JaksaPause ? GameText.Get("text.back_to_pause_enter")
                : origin == Page.Options ? GameText.Get("text.back_to_options_enter")
                : origin == Page.PlayChooser ? GameText.Get("text.back_to_journey_selection_enter")
                : origin == Page.Menu ? GameText.Get("text.back_to_menu_enter")
                : learner != null ? GameText.Get("text.continue_demonstration_enter") : GameText.Get("text.resume_driving_enter");
            AddButton(footer, action, () => CloseControls(true), "primary tutorial-start");
            AddLabel(footer, GameText.Get("text.f1_opens_this_guide_while_driving_esc_closes_it"), "control-note");
        }

        static void ControlKey(VisualElement parent, string key, string title, string note, DrivingGlyph.Kind? glyph = null)
        {
            var row = Element(parent, "control-row");
            AddLabel(row, key, "keycap");
            var description = Element(row, "control-description");
            var heading = Element(description, "row");
            if (glyph.HasValue) heading.Add(new DrivingGlyph(glyph.Value));
            AddLabel(heading, title, "control-title");
            AddLabel(description, note, "control-note");
        }

        void CloseControls(bool accepted)
        {
            if (page != Page.Controls) return;
            inputReleaseRequired = false;
            if (controlsOrigin == Page.Progress) ShowProgress();
            else if (controlsOrigin == Page.JaksaPause) ShowJaksaPause();
            else if (controlsOrigin == Page.Menu) ShowMenu();
            else if (controlsOrigin == Page.PlayChooser) ShowPlayChooser();
            else if (controlsOrigin == Page.Options) ShowOptions();
            else
            {
                SetPage(controlsOrigin);
                inputReleaseRequired = true;
            }
        }

        bool DrivingInputsReleased()
        {
            if (!inputReleaseRequired) return true;
            foreach (var key in DrivingInputKeys) if (Input.GetKey(key)) return false;
            if (Input.GetMouseButton(0)) return false;
            inputReleaseRequired = false;
            return true;
        }

        void RefreshProgressiveCue()
        {
            var cue = root.Q("progressiveCue");
            Show(cue, false);
            bool guidedDemo = page == Page.Drive && pitchDemo && journey.LearningControls && learner == null;
            hud.EnableInClassList("demo-guided", guidedDemo);
            hud.EnableInClassList("demo-feedback", page == Page.Drive && pitchDemo
                && !guidedDemo && Time.unscaledTime < toastUntil);
            var tutorialCue = root.Q("demoTutorialCue");
            bool initialCue = guidedDemo && !demoTutorial.IsActive && !inputReleaseRequired;
            tutorialCue.EnableInClassList("tutorial-initial", initialCue);
            Show(tutorialCue, guidedDemo && (!initialCue || !demoTutorial.InitialInstructionDismissed));
            if (guidedDemo)
            {
                root.Q<Label>("demoTutorialInstruction").text = inputReleaseRequired
                    ? GameText.Get("tutorial.action.release")
                    : demoTutorial.IsActive ? demoTutorial.Instruction(journey) : GameText.Get(demoTutorial.Scenario.InitialInstructionKey);
                return;
            }
            if ((page != Page.Drive && page != Page.Jaksa) || learner != null
                || inputReleaseRequired || !Application.isFocused) return;
            if (!root.Q("hudHint").ClassListContains("hidden")) return;
            if (page == Page.Drive && journey.LearningControls)
            {
                PresentCue(controlsMoved ? "S" : "W A S D", controlsMoved ? GameText.Get("text.press_s_to_brake") : GameText.Get("text.try_accelerating_and_steering_no_penalties"));
                return;
            }
            string context = (page == Page.Jaksa ? "explore:" : "training:") + mode;
            float drivingSpeed = page == Page.Jaksa ? ExploreSpeed : journey.Speed;
            if (!controlsSeen.Contains(context))
            {
                if (drivingSpeed > .2f && Pressed(KeyCode.W, KeyCode.UpArrow)) controlsSeen.Add(context);
                else { PresentCue("W A S D", GameText.Get("text.try_accelerating_and_steering")); return; }
            }
            if (page == Page.Jaksa)
            {
                if (!hiActive && !exploreStopped.Contains(context)) PresentCue("S", GameText.Get("text.press_s_to_brake"));
                return;
            }
            if (journey.Finished || journey.Failure.HasValue) return;
            float front = journey.Front, z = journey.Position.y;
            int stage = journey.Stage;
            string brake = null;
            bool Near(float line) => front >= line - 25 && front < line + 1;
            if (stage == 1 && !journey.FullStop && Near(RouteStage.StopLine)) brake = "stop";
            else if (stage == 1 && journey.LightPhase != 2 && Near(RouteStage.LightLine)) brake = "light";
            else if ((stage == 2 || journey.IsSchool) && journey.PedestrianBlocksPath && Near(journey.CrossingZebra - 2)) brake = "pedestrian";
            else if (stage == 2 && journey.CrossTraffic.HasValue && Near(RouteStage.YieldLine)) brake = "yield";
            else if (stage >= 5 && stage <= 7 && (!journey.MovementGreen || journey.BoulevardPedestrianBlocksPath))
            {
                float along = Boulevard.Local(stage, journey.Nose).y;
                if (along >= journey.JunctionStopLine - 25 && along < journey.JunctionStopLine + 1) brake = "junction";
            }
            else if (stage == 8 && journey.RailWarning && Near(Boulevard.RailLine)) brake = "rail";
            if (brake != null && drivingSpeed > .28f)
            {
                bool braking = Input.GetKey(KeyCode.Space) || Pressed(KeyCode.S, KeyCode.DownArrow);
                Suggest("brake:" + brake, GameText.Get("text.space_s"), GameText.Get("text.brake_before_the_stopping_area"), braking);
                return;
            }
            int direction = 0;
            if (stage == 3 && !journey.IsMarket && z >= RouteStage.OneWayStart - 12 && z < RouteStage.Merge && !journey.ReturnedLeft)
                direction = journey.ChangedRight ? -1 : 1;
            else if (stage == 6 || stage == 7)
            {
                float along = Boulevard.Local(stage, journey.Position).y;
                if (along >= -70 && along < -11) direction = stage == 6 ? -1 : 1;
            }
            else if (journey.IsMarket && z >= Mission.PickupStart - 15 && z < Mission.PickupEnd)
                direction = journey.PassengerPickedUp ? 1 : journey.Position.x >= -4.25f ? -1 : 0;
            else if (stage == 4 && journey.PhoneRinging && journey.Position.x >= -4.25f && z < RouteStage.PhoneEnd)
                direction = -1;
            if (direction != 0)
                Suggest("indicator:" + direction, direction < 0 ? "Q" : "E",
                    direction < 0 ? GameText.Get("text.signal_left_before_maneuvering") : GameText.Get("text.signal_right_before_maneuvering"), journey.Indicator == direction);

            void Suggest(string action, string keys, string copy, bool performed)
            {
                string id = context + ":" + stage + ":" + journey.Save.checkpoint + ":" + action;
                if (performed) cueActionsSeen.Add(id);
                if (!cueActionsSeen.Contains(id)) PresentCue(keys, copy);
            }
        }

        void PresentCue(string keys, string copy)
        {
            root.Q<Label>("cueKeys").text = keys;
            root.Q<Label>("cueCopy").text = copy;
            Show(root.Q("progressiveCue"), true);
        }

        void AddControlsHelp(VisualElement parent)
        {
            AddButton(parent, GameText.Get("controls.open"), () => ShowControls(page,
                page == Page.Jaksa || page == Page.JaksaPause ? Page.Jaksa : Page.Drive, false), "quiet help-action");
            if (page == Page.Options)
                AddButton(parent, GameText.Get("text.replay_demo_tutorial"), StartEntryTutorial, "quiet help-action");
        }
    }
}
