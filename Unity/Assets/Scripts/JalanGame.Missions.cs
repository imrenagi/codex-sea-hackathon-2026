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
            AddLabel(dialog, GameText.Get("missions.choose_activity", mode == VehicleMode.Car ? GameText.Get("text.car") : GameText.Get("text.motorcycle")), "kicker");
            AddLabel(dialog, kind == MissionKind.Scenario ? GameText.Get("text.everyday_missions") : GameText.Get("level.choose.title"), "heading");
            AddLabel(dialog, kind == MissionKind.Scenario
                ? GameText.Get("text.one_goal_several_decisions_retry_difficult_sections_and_try")
                : GameText.Get("text.choose_a_skill_to_test_results_and_attempts_are"), "copy");
            var list = new ScrollView(ScrollViewMode.Vertical);
            list.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            list.contentViewport.style.overflow = Overflow.Hidden;
            list.AddToClassList("mission-list");
            RevealFocusedChildren(list);
            dialog.Add(list);
            AddActivityChoices(list.contentContainer, kind);
            var actions = Element(dialog, "row actions catalogue-footer");
            AddButton(actions, GameText.Get("text.main_menu"), BackToMenu, "quiet");
            AddButton(actions, kind == MissionKind.Scenario ? GameText.Get("text.view_skill_tests") : GameText.Get("text.view_everyday_missions"),
                () => ShowCatalogue(kind == MissionKind.Scenario ? MissionKind.SkillTest : MissionKind.Scenario), "quiet");
        }

        // The primary chooser follows the real saved journey, not the separate skill-test catalogue.
        // All launches use non-persistent checkpoint practice; saved journey and test scores stay intact.
        void AddJourneyLevelChoices(VisualElement parent)
        {
            var tutorial = Element(parent, "mission-card selection-mission-card");
            AddTutorialThumbnail(tutorial);
            var tutorialBody = Element(tutorial, "selection-mission-body");
            AddLabel(tutorialBody, GameText.Get("level.tutorial.title"), "subhead mission-title");
            AddLabel(tutorialBody, GameText.Get("level.tutorial.description"), "copy");
            AddLabel(tutorialBody, GameText.Get("level.tutorial.detail"), "objective-detail");

            string[] thumbnails = { "preparation", "signals", "priority", "lanes", "focus",
                "straight", "left_turn", "right_turn", "railway", "ambulance" };
            string[] fallback = { "preparation", "signals", "priority", "lanes", "focus",
                "lanes", "priority", "signals", "signals", "priority" };
            for (int i = 0; i < RouteStage.All.Length; i++)
            {
                int checkpoint = i;
                var stage = RouteStage.All[i];
                string asset = "route_" + thumbnails[i];
                if (!Resources.Load<Texture2D>("UI/Thumbnails/" + asset))
                    asset = "mission_" + fallback[i];
                var card = Element(parent, "mission-card selection-mission-card");
                AddSelectionThumbnail(card, asset, GameText.Get("missions.thumbnail_caption"));
                var body = Element(card, "selection-mission-body");
                AddLabel(body, GameText.Get("level.checkpoint.title", i + 1, stage.Title), "subhead mission-title");
                AddLabel(body, stage.Location, "kicker");
                AddLabel(body, stage.Instruction, "copy");
                AddLabel(body, GameText.Get("level.checkpoint.detail"), "objective-detail");
                AddButton(body, GameText.Get("level.checkpoint.start"), () => JumpToMission(checkpoint), "primary");
            }
        }

        void AddChooserModeCard(VisualElement parent, string asset, string titleText, string description,
            string actionText, System.Action action)
        {
            var card = Element(parent, "mission-card selection-mission-card");
            AddSelectionThumbnail(card, asset, titleText);
            var body = Element(card, "selection-mission-body");
            AddLabel(body, titleText, "subhead mission-title");
            AddLabel(body, description, "copy");
            AddButton(body, actionText, action, "primary");
        }

        void AddActivityChoices(VisualElement parent, MissionKind kind)
        {
            if (kind == MissionKind.SkillTest)
            {
                var tutorial = Element(parent, "mission-card selection-mission-card");
                AddTutorialThumbnail(tutorial);
                var body = Element(tutorial, "selection-mission-body");
                AddLabel(body, GameText.Get("level.tutorial.title"), "subhead mission-title");
                AddLabel(body, GameText.Get("level.tutorial.description"), "copy");
                AddLabel(body, GameText.Get("level.tutorial.detail"), "objective-detail");
            }
            foreach (var activity in kind == MissionKind.Scenario ? Mission.Scenarios : Mission.Tests)
            {
                var saved = JourneySave.Load(mode, activity);
                var card = Element(parent, "mission-card selection-mission-card");
                AddSelectionThumbnail(card, "mission_" + activity.Id, GameText.Get("missions.thumbnail_caption"));
                var body = Element(card, "selection-mission-body");
                var row = Element(body, "row split");
                AddLabel(row, activity.Title, "subhead mission-title");
                AddLabel(row, activity.Duration, "kicker duration");
                AddLabel(body, activity.Focus, "copy");
                string status = saved == null ? GameText.Get("text.not_attempted")
                    : saved.next <= activity.LastStage ? GameText.Get("missions.in_progress", saved.attempts)
                    : GameText.Get("missions.completed", saved.completions, saved.bestMistakes);
                AddLabel(body, status, "objective-detail");
                AddButton(body, saved != null && saved.next <= activity.LastStage ? GameText.Get("text.continue") : GameText.Get("text.select_2a7802"), () => ShowMissionBrief(activity), "primary");
            }
            if (kind == MissionKind.Scenario) AddHiMissionCard(parent);
        }

        void ShowMissionBrief(Mission activity)
        {
            catalogueKind = activity.Kind;
            SetPage(Page.Brief);
            var saved = JourneySave.Load(mode, activity);
            bool resume = saved != null && saved.next <= activity.LastStage;
            AddLabel(dialog, (activity.Kind == MissionKind.Scenario ? GameText.Get("text.everyday_mission") : GameText.Get("text.skill_test"))
                + "  /  " + activity.Duration, "kicker");
            AddLabel(dialog, activity.Title, "heading");
            AddLabel(dialog, activity.Brief, "copy");
            var note = Element(dialog, "note");
            AddLabel(note, GameText.Get("text.practice_focus"), "kicker");
            AddLabel(note, activity.Focus, "copy");
            AddLabel(dialog, activity.Kind == MissionKind.Scenario
                ? GameText.Get("text.complete_the_goal_safely_progress_saves_after_each_section")
                : GameText.Get("text.complete_the_test_without_violations_after_a_mistake_read"), "copy");
            if (resume) AddLabel(dialog, GameText.Get("text.saved_progress_is_available_for_this_vehicle"), "objective-detail");
            var actions = Element(dialog, "row actions");
            AddButton(actions, resume ? GameText.Get("text.continue") : GameText.Get("text.start"), () => StartMission(activity, saved), "primary");
            AddButton(actions, GameText.Get("text.back"), () => ShowCatalogue(activity.Kind), "quiet");
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
                toast.text = GameText.Get("text.passenger_boarding_stay_stopped");
                toastUntil = Time.unscaledTime + 4;
            }
        }

        void ShowMissionProgress()
        {
            SetPage(Page.Progress);
            var activity = journey.Activity;
            bool scenario = activity.Kind == MissionKind.Scenario;
            AddLabel(dialog, journey.Finished ? scenario ? GameText.Get("text.mission_complete") : GameText.Get("text.test_complete") : GameText.Get("text.progress_saved"), "kicker");
            AddLabel(dialog, activity.Title, "heading");
            AddLabel(dialog, journey.Finished ? scenario ? GameText.Get("text.destination_reached_safely") : GameText.Get("text.latest_attempt_passed")
                : GameText.Get("text.continue_or_choose_another_activity"), "copy");
            AddLabel(dialog, GameText.Get("missions.attempts", journey.Save.attempts, journey.MistakeCount), "copy");
            if (journey.Finished)
                AddLabel(dialog, GameText.Get("missions.best_result", journey.Save.bestMistakes, journey.Save.completions), "objective-detail");
            var note = Element(dialog, "note");
            AddLabel(note, journey.Finished ? GameText.Get("text.skills_practiced") : GameText.Get("text.practice_focus"), "kicker");
            AddLabel(note, activity.Focus, "copy");
            var errors = Enumerable.Range(0, Lesson.All.Length).Where(i => journey.Save.mistakes[i] > 0)
                .OrderByDescending(i => journey.Save.mistakes[i]).Take(2).ToArray();
            foreach (int i in errors) AddLabel(note, journey.Save.mistakes[i] + "×  " + Lesson.All[i].Title, "objective-detail");
            if (journey.Finished && errors.Length == 0) AddLabel(note, GameText.Get("text.completed_this_session_without_mistakes"), "objective-detail");
            AddControlsHelp(dialog);
            var actions = Element(dialog, "row actions");
            if (!journey.Finished) AddButton(actions, GameText.Get("text.continue"), () =>
            {
                if (journey.Stage == 0) ShowPreflight();
                else if (journey.Failure.HasValue) ShowLesson(journey.Failure.Value);
                else ReturnToJourney();
            }, "primary");
            else AddButton(actions, scenario ? GameText.Get("text.retry_with_a_variation") : GameText.Get("text.retry_test"),
                () => StartMission(activity, journey.Save, restart: true), "primary");
            AddButton(actions, scenario ? GameText.Get("text.choose_mission") : GameText.Get("text.choose_test"), () => ShowCatalogue(activity.Kind), "quiet");
            AddButton(actions, GameText.Get("text.main_menu"), BackToMenu, "quiet");
        }

        void RefreshMissionPrompt(ref string prompt, ref string signal, ref float target)
        {
            if (journey.Activity == null) return;
            title.text = journey.Activity.Title;
            if (journey.Activity.IsVillage && journey.Front > RouteStage.StopJunction + 5.5f)
            {
                prompt = GameText.Get("text.the_shop_is_nearby_continue_to_the_yellow_destination");
                signal = GameText.Get("text.heading_to_the_shop");
                target = journey.StageEnd;
            }
            if (journey.IsSchool && !journey.CrossedZebra)
            {
                prompt = journey.Front < RouteStage.SchoolLimit ? GameText.Get("text.school_is_finishing_watch_for_the_30_sign_and")
                    : GameText.Get("text.check_the_crossing_before_continuing");
                target = journey.CrossingZebra - 2;
                if (journey.PedestrianBlocksPath) signal = GameText.Get("text.pedestrians_crossing");
            }
            if (journey.IsMarket)
            {
                if (journey.PassengerPickedUp)
                {
                    prompt = GameText.Get("text.signal_right_e_check_traffic_then_leave_the_bay");
                    if (journey.Position.x > -4.25f) prompt = GameText.Get("text.passenger_on_board_continue_to_the_yellow_destination_marker");
                    target = journey.StageEnd;
                    if (journey.PickupTraffic.HasValue)
                    {
                        float gap = journey.PickupTraffic.Value.y - journey.Position.y;
                        signal = GameText.Get("hud.car_left_gap", Mathf.RoundToInt(Mathf.Abs(gap)),
                            gap >= 0 ? GameText.Get("text.ahead") : GameText.Get("text.behind"));
                    }
                    else signal = GameText.Get("text.check_traffic_before_merging");
                    if (journey.IsBoarding)
                    {
                        prompt = GameText.Get("text.passenger_boarding_stay_stopped_then_check_traffic_before_pulling");
                        signal = GameText.Get("text.waiting_for_passenger");
                    }
                }
                else
                {
                    prompt = journey.Position.y < Mission.PickupStart ? GameText.Get("text.follow_the_one_way_road_past_the_restricted_zone")
                        : GameText.Get("text.signal_left_q_enter_the_bay_then_stop_beside");
                    target = Mission.PickupPoint;
                    signal = journey.CanPickup ? GameText.Get("text.press_pick_up_f")
                        : journey.Position.y <= Mission.NoStopEnd ? GameText.Get("text.passenger_asked_to_wait_in_the_bay") : GameText.Get("text.pickup_bay_on_the_left");
                }
            }
            if (journey.Activity.Kind == MissionKind.SkillTest && learner == null)
                prompt = journey.Activity.Brief;
        }
    }
}
