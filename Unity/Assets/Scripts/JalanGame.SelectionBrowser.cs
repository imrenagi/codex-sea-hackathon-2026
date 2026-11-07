using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    public sealed partial class JalanGame
    {
        // A single display/launch contract for practice, missions, journeys and map previews.
        interface ISelectionItem
        {
            string Id { get; }
            string Title { get; }
            string Asset { get; }
            string Meta { get; }
            string Description { get; }
            string Notice { get; }
            int Stars { get; }
            string ActionLabel { get; }
            Rule[] Rules { get; }
            bool Confirm { get; }
            void Launch();
        }
        sealed class SelectionItem : ISelectionItem
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Asset { get; set; }
            public string Meta { get; set; }
            public string Description { get; set; }
            public string Notice { get; set; }
            public int Stars { get; set; } = -1;
            public string ActionLabel { get; set; }
            public Rule[] Rules { get; set; } = Array.Empty<Rule>();
            public bool Confirm { get; set; }
            public Action Action;
            public void Launch() => Action?.Invoke();
        }

        string selectedActivityId;
        void BuildSelectionBrowser(List<ISelectionItem> items, Action back)
        {
            EnsureThumbnailStyles();
            ShowVehiclePreview(false);
            var vehicles = Element(dialog, "selection-models row");
            foreach (var choice in VehicleCatalogue.All.Where(v => v.Mode == mode))
            {
                var model = choice;
                var button = AddButton(vehicles, "", () =>
                {
                    VehicleCatalogue.Select(model);
                    foreach (var sibling in vehicles.Query<Button>().ToList())
                        sibling.EnableInClassList("selected", ReferenceEquals(sibling.userData, model));
                }, "vehicle-choice vehicle-thumbnail-choice quiet");
                button.userData = model;
                button.tooltip = model.Label + " · " + model.Description;
                button.EnableInClassList("selected", ReferenceEquals(VehicleCatalogue.Selected(mode), model));
                var image = new Image { image = Resources.Load<Texture2D>("UI/VehicleThumbnails/" + model.Id),
                    scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                image.AddToClassList("vehicle-choice-image"); button.Add(image);
            }
            dialog.AddToClassList("selection-browser-card");
            var browser = Element(dialog, "selection-browser");
            var sidebar = Element(browser, "selection-sidebar");
            var list = SelectionScroll(sidebar, "selection-list");
            var pane = Element(browser, "selection-detail-pane");
            var detail = SelectionScroll(pane, "selection-detail");
            var actions = Element(pane, "selection-detail-actions");
            var cards = new Dictionary<ISelectionItem, Button>();
            void Select(ISelectionItem item)
            {
                selectedActivityId = item.Id;
                foreach (var pair in cards) pair.Value.EnableInClassList("selected", pair.Key == item);
                detail.Clear(); actions.Clear(); detail.scrollOffset = Vector2.zero;
                AddBrowserImage(detail, item.Asset, "selection-detail-image");
                AddLabel(detail, item.Meta, "selection-detail-meta kicker");
                AddLabel(detail, item.Title, "selection-detail-title heading");
                if(item.Stars>=0) AddLabel(detail,PracticeRating.Symbols(item.Stars)+"  "+GameText.Get("rating.best",item.Stars),"subhead");
                AddLabel(detail, item.Description, "selection-detail-copy copy");
                if (!string.IsNullOrEmpty(item.Notice)) AddLabel(detail, item.Notice, "selection-detail-notice objective-detail");
                if (item.Rules.Length > 0)
                {
                    AddLabel(detail, GameText.Get("selection.rules"), "selection-detail-rules-title subhead");
                    var rules = Element(detail, "selection-detail-rules");
                    foreach (var rule in item.Rules.Distinct())
                    {
                        var lesson = Lesson.All[(int)rule];
                        var row = Element(rules, "selection-detail-rule");
                        AddLabel(row, lesson.Action, "copy");
                        AddLabel(row, lesson.Citation, "rule-source");
                    }
                }
                bool confirmed = false;
                Button launch = null;
                launch = AddButton(actions, item.ActionLabel, () =>
                {
                    if (item.Confirm && !confirmed)
                    {
                        confirmed = true;
                        launch.text = GameText.Get("text.yes_restart_journey");
                        AddLabel(actions, GameText.Get("text.this_replaces_the_journey_progress_for_this_vehicle"), "copy negative");
                        return;
                    }
                    item.Launch();
                }, "primary selection-start");
                AddButton(actions, GameText.Get("text.back"), back, "quiet");
            }
            foreach (var item in items)
            {
                var current = item;
                var card = AddButton(list.contentContainer, "", () => Select(current), "selection-card quiet");
                card.tooltip = item.Title;
                AddBrowserImage(card, item.Asset, "selection-card-image");
                var body = Element(card, "selection-card-body");
                AddLabel(body, item.Title, "selection-card-title");
                AddLabel(body, item.Meta, "selection-card-meta");
                if(item.Stars>=0) AddLabel(body,PracticeRating.Symbols(item.Stars),"selection-card-meta");
                foreach (var child in card.Query<VisualElement>().ToList())
                    if (child != card) child.pickingMode = PickingMode.Ignore;
                card.pickingMode = PickingMode.Position;
                card.RegisterCallback<FocusInEvent>(_ => Select(current));
                cards.Add(item, card);
            }
            if (items.Count > 0) Select(items.FirstOrDefault(item => item.Id == selectedActivityId) ?? items[0]);
        }
        ScrollView SelectionScroll(VisualElement parent, string css)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            { horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList(css); parent.Add(scroll); RevealFocusedChildren(scroll); return scroll;
        }
        void AddBrowserImage(VisualElement parent, string asset, string css)
        {
            var image = new Image { image = Resources.Load<Texture2D>("UI/Thumbnails/" + asset),
                scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            image.AddToClassList(css); parent.Add(image);
        }
        Rule[] StageRules(int stage)
        {
            // Mirrors the implemented checkpoints; these are focus rules, not new mechanics.
            return stage switch
            {
                0 => mode == VehicleMode.Car ? new[] { Rule.SeatBelt } : new[] { Rule.Helmet, Rule.Headlight },
                1 => new[] { Rule.Stop, Rule.Signal, Rule.Junction },
                2 => new[] { Rule.Pedestrian, Rule.Junction },
                3 => new[] { Rule.NoEntry, Rule.Indicator, Rule.Gap, Rule.ReturnLeft },
                4 => new[] { Rule.Speed, Rule.Phone },
                5 => new[] { Rule.MovementLane, Rule.Signal, Rule.StopBox, Rule.BoulevardSpeed },
                6 => new[] { Rule.MovementLane, Rule.TurnSignal, Rule.Pedestrian },
                7 => new[] { Rule.MovementLane, Rule.TurnSignal, Rule.RightArrow },
                8 => new[] { Rule.Railway },
                9 => new[] { Rule.Emergency, Rule.Indicator, Rule.Gap, Rule.BoulevardSpeed },
                _ => Array.Empty<Rule>()
            };
        }
        List<ISelectionItem> PracticeSelections()
        {
            var items = new List<ISelectionItem> { new SelectionItem {
                Id = "tutorial", Title = GameText.Get("level.tutorial.title"), Asset = "mission_tutorial",
                Stars = PracticeScore.Load(mode,"tutorial").bestStars,
                Meta = GameText.Get("home.practice"), Description = GameText.Get("level.tutorial.description"),
                Notice = GameText.Get("level.tutorial.detail"), ActionLabel = GameText.Get("level.tutorial.start"), Action = StartPitchDemo } };
            string[] ids = { "preparation", "signals", "priority", "lanes", "focus", "straight", "left_turn", "right_turn", "railway", "ambulance" };
            string[] fallback = { "preparation", "signals", "priority", "lanes", "focus", "lanes", "priority", "signals", "signals", "priority" };
            for (int i = 0; i < RouteStage.All.Length; i++)
            {
                int index = i; var stage = RouteStage.All[i];
                string asset = "route_" + ids[i];
                if (!Resources.Load<Texture2D>("UI/Thumbnails/" + asset)) asset = "mission_" + fallback[i];
                items.Add(new SelectionItem { Id = "checkpoint-" + i,
                    Title = GameText.Get("level.checkpoint.title", i + 1, stage.Title), Asset = asset,
                    Meta = stage.Location, Description = stage.Instruction, Notice = GameText.Get("rating.criteria"),
                    Stars = PracticeScore.Load(mode,"checkpoint-"+i).bestStars,
                    Rules = StageRules(i), ActionLabel = GameText.Get("level.checkpoint.start"), Action = () => JumpToMission(index) });
            }
            return items;
        }
        List<ISelectionItem> MissionSelections(MissionKind kind)
        {
            var items = new List<ISelectionItem>();
            foreach (var mission in kind == MissionKind.Scenario ? Mission.Scenarios : Mission.Tests)
            {
                var activity = mission; var saved = JourneySave.Load(mode, activity);
                bool resume = saved != null && saved.next <= activity.LastStage;
                var rules = Enumerable.Range(activity.FirstStage, activity.LastStage - activity.FirstStage + 1).SelectMany(StageRules).ToList();
                if (activity.IsMarket) rules.AddRange(new[] { Rule.NoStopping, Rule.PickupPlace, Rule.PickupIncomplete });
                if (activity.IsSchool) rules.Add(Rule.Pedestrian);
                items.Add(new SelectionItem { Id = "mission-" + activity.Id, Title = activity.Title,
                    Asset = "mission_" + activity.Id, Meta = activity.Focus,
                    Description = activity.Brief, Rules = rules.Distinct().ToArray(),
                    Stars = PracticeScore.Load(mode,"mission-"+activity.Id).bestStars,
                    Notice = GameText.Get("rating.criteria") + (resume ? " · "+GameText.Get("missions.in_progress",saved.attempts):""),
                    ActionLabel = GameText.Get(resume ? "text.continue" : "text.start"), Action = () => StartMission(activity, saved) });
            }
            return items;
        }
        List<ISelectionItem> PlaySelections()
        {
            var items = MissionSelections(MissionKind.Scenario);
            items.Add(new SelectionItem { Id = "sopo-del", Title = GameText.Get("text.sopo_del_free_drive"), Asset = "city_sea_labs",
                Meta = GameText.Get("text.sopo_del_approx_250_m"), Description = GameText.Get("text.explore_about_250_meters_of_jl_mega_kuningan_barat"),
                Notice = GameText.Get("text.follow_the_road_and_keep_a_safe_distance"), ActionLabel = GameText.Get("text.start"), Action = StartJaksa });
            items.AddRange(CitySelections());
            if (stored == null)
                items.Add(new SelectionItem { Id = "journey-new", Title = GameText.Get("text.full_journey_10_stages"), Asset = "mission_lanes",
                    Meta = GameText.Get("selection.journey"), Description = GameText.Get("chooser.full_journey.description"),
                    ActionLabel = GameText.Get("text.start"), Action = StartNew });
            else
            {
                string resume = stored.next == RouteStage.All.Length && stored.after >= 0 ? GameText.Get("text.view_journey_results")
                    : stored.next > 0 ? GameText.Get("text.continue_journey") : GameText.Get("text.continue_preparation");
                items.Add(new SelectionItem { Id = "journey-resume", Title = resume, Asset = "mission_lanes",
                    Meta = GameText.Get("selection.journey"), Description = GameText.Get("chooser.saved_journey.description"),
                    ActionLabel = resume, Action = Resume });
                items.Add(new SelectionItem { Id = "journey-restart", Title = GameText.Get("text.restart_journey"), Asset = "mission_preparation",
                    Meta = GameText.Get("selection.journey"), Description = GameText.Get("text.this_replaces_the_journey_progress_for_this_vehicle"),
                    Confirm = true, ActionLabel = GameText.Get("text.restart_journey"), Action = StartNew });
            }
            return items;
        }
        List<ISelectionItem> CitySelections()
        {
            var items = new List<ISelectionItem>();
            for (int i = 0; i < CityMapData.Ids.Length; i++)
            {
                int index = i; string id = CityMapData.Ids[i];
                items.Add(new SelectionItem { Id = "city-" + id, Title = CityMapData.Names[i], Asset = "city_" + id,
                    Meta = GameText.Get("text.map_preview_not_a_practice_route"), Description = GameText.Get("maps.summary." + id),
                    Notice = GameText.Get("text.buildings_and_roads_from_osm_some_heights_and_widths") + "\n© OpenStreetMap contributors · ODbL",
                    ActionLabel = GameText.Get("maps.view_preview"), Action = () => OpenCityMap(index) });
            }
            return items;
        }
    }
}
