using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        MissionKind catalogueKind;
        void ShowCatalogue(MissionKind kind)
        {
            catalogueKind = kind;
            SetPage(Page.Catalogue);
            AddLabel(dialog, (mode == VehicleMode.Car ? "CAR" : "MOTORCYCLE") + "  /  CHOOSE AN ACTIVITY", "kicker");
            AddLabel(dialog, kind == MissionKind.Scenario ? "Everyday missions" : "Skill tests", "heading");
            AddLabel(dialog, kind == MissionKind.Scenario
                ? "One goal, several decisions. Retry difficult sections and try the next traffic variation."
                : "Choose a skill to test. Results and attempts are saved for each test.", "copy");
            var list = new ScrollView(ScrollViewMode.Vertical);
            list.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            list.contentViewport.style.overflow = Overflow.Hidden;
            list.AddToClassList("mission-list");
            dialog.Add(list);
            foreach (var activity in kind == MissionKind.Scenario ? Mission.Scenarios : Mission.Tests)
            {
                var saved = JourneySave.Load(mode, activity);
                var card = Element(list.contentContainer, "mission-card");
                var row = Element(card, "row split");
                AddLabel(row, activity.Title, "subhead mission-title");
                AddLabel(row, activity.Duration, "kicker duration");
                AddLabel(card, activity.Focus, "copy");
                string status = saved == null ? "Not attempted"
                    : saved.next <= activity.LastStage ? "In progress  /  Attempt " + saved.attempts
                    : "Completed " + saved.completions + " times  /  Fewest mistakes: " + saved.bestMistakes;
                AddLabel(card, status, "objective-detail");
                AddButton(card, saved != null && saved.next <= activity.LastStage ? "Continue" : "Select", () => ShowMissionBrief(activity), "primary");
            }
            var actions = Element(dialog, "row actions catalogue-footer");
            AddButton(actions, "Main menu", BackToMenu, "quiet");
            AddButton(actions, kind == MissionKind.Scenario ? "View skill tests" : "View everyday missions",
                () => ShowCatalogue(kind == MissionKind.Scenario ? MissionKind.SkillTest : MissionKind.Scenario), "quiet");
        }

        void ShowMissionBrief(Mission activity)
        {
            catalogueKind = activity.Kind;
            SetPage(Page.Brief);
            var saved = JourneySave.Load(mode, activity);
            bool resume = saved != null && saved.next <= activity.LastStage;
            AddLabel(dialog, (activity.Kind == MissionKind.Scenario ? "EVERYDAY MISSION" : "SKILL TEST")
                + "  /  " + activity.Duration, "kicker");
            AddLabel(dialog, activity.Title, "heading");
            AddLabel(dialog, activity.Brief, "copy");
            var note = Element(dialog, "note");
            AddLabel(note, "PRACTICE FOCUS", "kicker");
            AddLabel(note, activity.Focus, "copy");
            AddLabel(dialog, activity.Kind == MissionKind.Scenario
                ? "Complete the goal safely. Progress saves after each section; there is no bonus for rushing."
                : "Complete the test without violations. After a mistake, read the explanation and try again.", "copy");
            if (resume) AddLabel(dialog, "Saved progress is available for this vehicle.", "objective-detail");
            var actions = Element(dialog, "row actions");
            AddButton(actions, resume ? "Continue" : "Start", () => StartMission(activity, saved), "primary");
            AddButton(actions, "Back", () => ShowCatalogue(activity.Kind), "quiet");
        }

        void StartMission(Mission activity, JourneySave saved, bool restart = false)
        {
            pitchDemo = false; practiceStart = -1; learner = null;
            var save = !restart && saved != null && saved.next <= activity.LastStage ? saved : activity.NewSave(saved);
            journey = new Journey(mode, save, mission: activity);
            save.Store(mode, activity);
            hood = false;
            district.Snap();
            if (journey.Stage == 0) ShowPreflight();
            else EnterHumanDrive(Page.Drive);
        }

        void PickupPassenger()
        {
            if (page != Page.Drive || learner != null) return;
            journey.Pickup();
            if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
            else if (journey.PassengerPickedUp)
            {
                toast.text = "PASSENGER BOARDING  /  STAY STOPPED";
                toastUntil = Time.unscaledTime + 4;
            }
        }

        void ShowMissionProgress()
        {
            SetPage(Page.Progress);
            var activity = journey.Activity;
            bool scenario = activity.Kind == MissionKind.Scenario;
            AddLabel(dialog, journey.Finished ? scenario ? "MISSION COMPLETE" : "TEST COMPLETE" : "PROGRESS SAVED", "kicker");
            AddLabel(dialog, activity.Title, "heading");
            AddLabel(dialog, journey.Finished ? scenario ? "Destination reached safely." : "Latest attempt passed."
                : "Continue or choose another activity.", "copy");
            AddLabel(dialog, "Attempts: " + journey.Save.attempts + "  /  Total practice mistakes: " + journey.MistakeCount, "copy");
            if (journey.Finished)
                AddLabel(dialog, "Best result: " + journey.Save.bestMistakes + " mistakes  /  Completed " + journey.Save.completions + " times", "objective-detail");
            var note = Element(dialog, "note");
            AddLabel(note, journey.Finished ? "SKILLS PRACTICED" : "PRACTICE FOCUS", "kicker");
            AddLabel(note, activity.Focus, "copy");
            var errors = Enumerable.Range(0, Lesson.All.Length).Where(i => journey.Save.mistakes[i] > 0)
                .OrderByDescending(i => journey.Save.mistakes[i]).Take(2).ToArray();
            foreach (int i in errors) AddLabel(note, journey.Save.mistakes[i] + "×  " + Lesson.All[i].Title, "objective-detail");
            if (journey.Finished && errors.Length == 0) AddLabel(note, "Completed this session without mistakes.", "objective-detail");
            AddControlsHelp(dialog);
            var actions = Element(dialog, "row actions");
            if (!journey.Finished) AddButton(actions, "Continue", () =>
            {
                if (journey.Stage == 0) ShowPreflight();
                else if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
                else ReturnToJourney();
            }, "primary");
            else AddButton(actions, scenario ? "Retry with a variation" : "Retry test",
                () => StartMission(activity, journey.Save, restart: true), "primary");
            AddButton(actions, scenario ? "Choose mission" : "Choose test", () => ShowCatalogue(activity.Kind), "quiet");
            AddButton(actions, "Main menu", BackToMenu, "quiet");
        }

        void RefreshMissionPrompt(ref string prompt, ref string signal, ref float target)
        {
            if (journey.Activity == null) return;
            title.text = journey.Activity.Title;
            if (journey.Activity.IsVillage && journey.Front > RouteStage.StopJunction + 5.5f)
            {
                prompt = "The shop is nearby. Continue to the yellow destination marker.";
                signal = "HEADING TO THE SHOP";
                target = journey.StageEnd;
            }
            if (journey.IsSchool && !journey.CrossedZebra)
            {
                prompt = journey.Front < RouteStage.SchoolLimit ? "School is finishing. Watch for the 30 sign and pedestrian crossing."
                    : "Check the crossing before continuing.";
                target = journey.CrossingZebra - 2;
                if (journey.PedestrianBlocksPath) signal = "PEDESTRIANS CROSSING";
            }
            if (journey.IsMarket)
            {
                if (journey.PassengerPickedUp)
                {
                    prompt = "Signal right [E], check traffic, then leave the bay when safe.";
                    if (journey.Position.x > -4.25f) prompt = "Passenger on board. Continue to the yellow destination marker.";
                    target = journey.StageEnd;
                    if (journey.PickupTraffic.HasValue)
                    {
                        float gap = journey.PickupTraffic.Value.y - journey.Position.y;
                        signal = "CAR IN LEFT LANE  " + Mathf.RoundToInt(Mathf.Abs(gap)) + " M "
                            + (gap >= 0 ? "AHEAD" : "BEHIND");
                    }
                    else signal = "CHECK TRAFFIC BEFORE MERGING";
                    if (journey.IsBoarding)
                    {
                        prompt = "Passenger boarding. Stay stopped, then check traffic before pulling out.";
                        signal = "WAITING FOR PASSENGER";
                    }
                }
                else
                {
                    prompt = journey.Position.y < Mission.PickupStart ? "Follow the one-way road past the restricted zone to the pickup bay on the left."
                        : "Signal left [Q], enter the bay, then stop beside the passenger.";
                    target = Mission.PickupPoint;
                    signal = journey.CanPickup ? "PRESS PICK UP [F]"
                        : journey.Position.y <= Mission.NoStopEnd ? "PASSENGER ASKED TO WAIT IN THE BAY" : "PICKUP BAY ON THE LEFT";
                }
            }
            if (journey.Activity.Kind == MissionKind.SkillTest && learner == null)
                prompt = journey.Activity.Brief;
        }
    }
}
