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
        bool entryTutorial;
        Journey controlsOriginal;
        string controlsPracticeContext;
        bool controlsMoved, controlsBraked;
        public bool ControlsOnboardingActive => page == Page.TutorialBrake || (pitchDemo && !demoCrossingTaught) || page == Page.Controls || journey?.LearningControls == true
            || ((page == Page.Jaksa || page == Page.JaksaPause) && !exploreStopped.Contains("explore:" + mode));
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

        bool demoCrossingTaught, demoBrakeLatched, demoCrossingReady, demoActionReleased;

        void PlayFromHome() => StartEntryTutorial();

        void StartEntryTutorial()
        {
            PrepareMenuScene();
            mode = VehicleMode.Car;
            StartPitchDemo();
        }

        bool PauseForDemoCrossing()
        {
            if (!pitchDemo || demoCrossingTaught || learner != null || journey.Stage != 2
                || !journey.Pedestrian.HasValue || journey.Position.y < journey.CrossingZebra - 19) return false;
            demoCrossingTaught = true;
            demoActionReleased = false;
            journey.LearningControls = true;
            SetPage(Page.TutorialBrake);
            AddLabel(dialog, "Pedestrian crossing ahead", "kicker");
            AddLabel(dialog, "Press [S] to brake", "tutorial-instruction");
            return true;
        }

        void ResumeDemoCrossing()
        {
            demoBrakeLatched = true;
            demoCrossingReady = demoActionReleased = false;
            inputReleaseRequired = false;
            SetPage(Page.Drive);
            toast.text = "Braking. Wait for the pedestrian to finish crossing.";
            toastUntil = Time.unscaledTime + 6;
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
            toast.text = "Practice complete. Back to the start; normal rules are active.";
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
            toast.text = "Practice complete. Drive as usual.";
            toastUntil = Time.unscaledTime + 5;
        }

        void ShowControls(Page origin, Page drivingPage, bool firstEntry)
        {
            controlsOrigin = origin;
            controlsDrivingPage = drivingPage;
            SetPage(Page.Controls);
            AddLabel(dialog, mode == VehicleMode.Car ? "Car controls" : "Motorcycle controls", "heading");
            AddLabel(dialog, learner != null ? "The demonstration is paused. Close this guide to continue it."
                : "The game stays paused while this guide is open.", "copy controls-intro");
            var body = new ScrollView(ScrollViewMode.Vertical)
            { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            body.AddToClassList("controls-body");
            dialog.Add(body);
            var groups = Element(body, "control-groups");
            var movement = Element(groups, "control-group");
            AddLabel(movement, "Moving & stopping", "subhead");
            ControlKey(movement, "W / ↑", "Throttle", "Hold to accelerate.", DrivingGlyph.Kind.Throttle);
            ControlKey(movement, "S / ↓ / SPACE", "Brake", "Hold to slow down or stop.");
            ControlKey(movement, "A / D", "Steer left / right", "You can also use the ← / → arrow keys.", DrivingGlyph.Kind.Steering);
            ControlKey(movement, "Z / X", "Look left / right", "Hold to look. Release to face forward.");
            var decisions = Element(groups, "control-group");
            AddLabel(decisions, "Signals & views", "subhead");
            ControlKey(decisions, "Q / E", "Left / right indicator", "Press once. Press the same key again to turn it off.");
            ControlKey(decisions, "C", "Change camera", "Switch between chase camera and driver view.", DrivingGlyph.Kind.Camera);
            if (mode == VehicleMode.Motorcycle)
                ControlKey(decisions, "L", "Motorcycle headlight", "Press to switch on or off.");
            ControlKey(decisions, "ESC / TAB", "Pause", "Open progress and journey options.");
            Fold(body, "More controls", extra =>
            {
                ControlKey(extra, "R", drivingPage == Page.Jaksa ? "Restart road section" : "Retry from checkpoint", "Restart your current section.");
                ControlKey(extra, "H", "Horn", "Hold to sound the horn.");
                ControlKey(extra, "M", "Sound", "Press to mute or unmute.");
                if (drivingPage == Page.Drive)
                    ControlKey(extra, "F / G", journey.IsMarket ? "Pickup / phone" : "Phone",
                        journey.IsMarket ? "F picks up a passenger while stopped in the bay. G answers a call."
                        : "F ignores a call. G answers; stop somewhere safe first.");
                AddLabel(extra, "On the violation screen: R retries, D shows a correct demonstration.", "control-note");
            });
            var footer = Element(dialog, "controls-footer");
            string action = origin == Page.Progress || origin == Page.JaksaPause ? "Back to pause [Enter]"
                : origin == Page.Options ? "Back to Options [Enter]"
                : origin == Page.PlayChooser ? "Back to journey selection [Enter]"
                : origin == Page.Menu ? "Back to menu [Enter]"
                : learner != null ? "Continue demonstration [Enter]" : "Resume driving [Enter]";
            AddButton(footer, action, () => CloseControls(true), "primary tutorial-start");
            AddLabel(footer, "F1 opens this guide while driving. Esc closes it.", "control-note");
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
            Show(root.Q("demoTutorialCue"), guidedDemo);
            if (guidedDemo)
            {
                root.Q<Label>("demoTutorialInstruction").text = inputReleaseRequired
                    ? "Release the controls to continue"
                    : demoBrakeLatched
                        ? demoCrossingReady ? "Press [W] to continue"
                            : "Wait for the pedestrian to finish crossing"
                        : "Press [W] to start driving";
                return;
            }
            if ((page != Page.Drive && page != Page.Jaksa) || learner != null
                || inputReleaseRequired || !Application.isFocused) return;
            if (!root.Q("hudHint").ClassListContains("hidden")) return;
            if (page == Page.Drive && journey.LearningControls)
            {
                PresentCue(controlsMoved ? "S" : "W A S D", controlsMoved ? "Press S to brake" : "Try accelerating and steering. No penalties.");
                return;
            }
            string context = (page == Page.Jaksa ? "explore:" : "training:") + mode;
            float drivingSpeed = page == Page.Jaksa ? jaksa.Speed : journey.Speed;
            if (!controlsSeen.Contains(context))
            {
                if (drivingSpeed > .2f && Pressed(KeyCode.W, KeyCode.UpArrow)) controlsSeen.Add(context);
                else { PresentCue("W A S D", "Try accelerating and steering."); return; }
            }
            if (page == Page.Jaksa)
            {
                if (!exploreStopped.Contains(context)) PresentCue("S", "Press S to brake");
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
                Suggest("brake:" + brake, "SPACE / S", "Brake before the stopping area.", braking);
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
                    direction < 0 ? "Signal left before maneuvering." : "Signal right before maneuvering.", journey.Indicator == direction);

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
            AddButton(parent, "Driving controls [F1]", () => ShowControls(page,
                page == Page.Jaksa || page == Page.JaksaPause ? Page.Jaksa : Page.Drive, false), "quiet help-action");
            if (page == Page.Options)
                AddButton(parent, "Replay demo tutorial", StartEntryTutorial, "quiet help-action");
        }
    }
}
