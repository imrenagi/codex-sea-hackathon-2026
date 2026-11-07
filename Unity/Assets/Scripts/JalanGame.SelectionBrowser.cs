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
            var origin = page;
            bool wasPractice = practiceChooser;
            var kind = catalogueKind;
            Func<List<ISelectionItem>> selections = origin == Page.Catalogue ? () => MissionSelections(kind)
                : origin == Page.CityMaps ? CitySelections
                : wasPractice ? PracticeSelections : PlaySelections;
            Action returnToSelection = () =>
            {
                stored = JourneySave.Load(mode);
                SetPage(origin);
                AddLabel(dialog, GameText.Get(origin == Page.CityMaps ? "text.choose_map"
                    : origin == Page.Catalogue ? (kind == MissionKind.Scenario ? "text.everyday_missions" : "level.choose.title")
                    : wasPractice ? "home.practice" : "home.mission"), "heading");
                BuildSelectionBrowser(selections(), back);
            };
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
                foreach (var pair in cards)
                {
                    pair.Value.EnableInClassList("selected", pair.Key == item);
                    var badge = pair.Value.Q<VisualElement>("selectionCheck");
                    if (badge != null) badge.style.display = pair.Key == item ? DisplayStyle.Flex : DisplayStyle.None;
                }
                detail.Clear(); actions.Clear(); detail.scrollOffset = Vector2.zero;
                // Practice details prioritize rules; keep artwork on the list cards.
                bool isLevel = item.Id == "tutorial" || item.Id.StartsWith("checkpoint-")
                    || (origin == Page.Catalogue && kind == MissionKind.SkillTest);
                if (!isLevel) AddBrowserImage(detail, item.Asset, "selection-detail-image", item.Stars);
                AddLabel(detail, item.Meta, "selection-detail-meta kicker");
                AddLabel(detail, item.Title, "selection-detail-title heading");
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
                        row.Add(new RuleGuideIcon(rule));
                        var copy = Element(row, "rule-guide-copy");
                        var summary = AddLabel(copy, GameText.Get("guide.rule." + rule), "copy");
                        summary.enableRichText = true;
                        summary.tooltip = lesson.Action;
                        AddLabel(copy, lesson.Citation, "rule-source");
                    }
                }
                var next = AddActionButton(actions, ActionKind.Next, GameText.Get("text.continue"),
                    () => ShowSelectionVehicle(item.Id, selections, returnToSelection), "primary selection-start");
                next.name = "selectionContinue";
                AddActionButton(actions, ActionKind.Back, GameText.Get("text.back"), back, "quiet");
            }
            foreach (var item in items)
            {
                var current = item;
                var card = AddButton(list.contentContainer, "", () => Select(current), "selection-card quiet");
                card.tooltip = item.Title;
                AddBrowserImage(card, item.Asset, "selection-card-image", item.Stars);
                var body = Element(card, "selection-card-body");
                AddLabel(body, item.Title, "selection-card-title");
                AddLabel(body, item.Meta, "selection-card-meta");
                AddSelectionBadge(card, false);
                foreach (var child in card.Query<VisualElement>().ToList())
                    if (child != card) child.pickingMode = PickingMode.Ignore;
                card.pickingMode = PickingMode.Position;
                card.RegisterCallback<FocusInEvent>(_ => Select(current));
                cards.Add(item, card);
            }
            if (items.Count > 0) Select(items.FirstOrDefault(item => item.Id == selectedActivityId) ?? items[0]);
        }
        Action vehicleSelectionBack;
        void ShowSelectionVehicle(string id, Func<List<ISelectionItem>> selections, Action back)
        {
            vehicleSelectionBack = back;
            stored = JourneySave.Load(mode);
            // Resolve again for the chosen vehicle, never launch a save captured for another mode.
            var item = selections().FirstOrDefault(candidate => candidate.Id == id);
            if (item == null && id.StartsWith("journey-"))
                item = selections().FirstOrDefault(candidate => candidate.Id.StartsWith("journey-"));
            if (item == null) { back(); return; }
            SetPage(Page.VehicleSelect);
            AddLabel(dialog, GameText.Get("vehicles.preview.title"), "heading");
            AddLabel(dialog, item.Title, "subhead");
            AddBrowserImage(dialog, item.Asset, "selection-card-image");
            AddLabel(dialog, item.Description, "copy");
            var types = Element(dialog, "row chooser-vehicles");
            foreach (var choice in new[] { VehicleMode.Car, VehicleMode.Motorcycle })
            {
                var type = choice;
                var button = AddButton(types, GameText.Get(type == VehicleMode.Car ? "text.car_a5cdf0" : "text.motorcycle_965701"), () =>
                {
                    mode = type;
                    ShowSelectionVehicle(id, selections, back);
                }, type == mode ? "chooser-vehicle selected" : "chooser-vehicle quiet");
                button.name = "selectionVehicle-" + type;
                AddSelectionBadge(button, type == mode);
                button.Insert(0, new DrivingGlyph(type == VehicleMode.Car ? DrivingGlyph.Kind.Steering : DrivingGlyph.Kind.Motorcycle));
            }
            ShowVehiclePreview(true);
            // Keep the final action beside the 3D preview, anchored at its lower-right.
            var actions = Element(root.Q("vehicleShowroom"), "row actions");
            actions.style.justifyContent = Justify.FlexEnd;
            actions.style.flexShrink = 0;
            AddActionButton(actions, ActionKind.Back, GameText.Get("text.back"), back, "quiet").name = "selectionVehicleBack";
            bool confirmed = false;
            Button start = null;
            start = AddActionButton(actions, ActionKind.Play, item.ActionLabel, () =>
            {
                if (item.Confirm && !confirmed)
                {
                    confirmed = true;
                    start.text = GameText.Get("text.yes_restart_journey");
                    AddLabel(actions, GameText.Get("text.this_replaces_the_journey_progress_for_this_vehicle"), "copy negative");
                    return;
                }
                item.Launch();
            }, "primary");
            start.name = "selectionStart";
        }

        ScrollView SelectionScroll(VisualElement parent, string css)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            { horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList(css); parent.Add(scroll); RevealFocusedChildren(scroll); return scroll;
        }
        void AddBrowserImage(VisualElement parent, string asset, string css, int stars = -1)
        {
            var image = new Image { image = Resources.Load<Texture2D>("UI/Thumbnails/" + asset),
                scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            image.AddToClassList(css); parent.Add(image);
            if (stars < 0) return;
            bool large = css == "selection-detail-image";
            var rating = new VisualElement { name = "thumbnailStars", pickingMode = PickingMode.Ignore };
            rating.style.position = Position.Absolute;
            rating.style.left = 0; rating.style.right = 0; rating.style.bottom = 0;
            rating.style.flexDirection = FlexDirection.Row;
            rating.style.justifyContent = Justify.Center;
            rating.style.alignItems = Align.Center;
            rating.style.backgroundColor = new Color(.035f, .05f, .06f, .65f);
            for (int i = 0; i < 3; i++)
            {
                var star = new CompletionStar(i < Mathf.Clamp(stars, 0, 3), false);
                star.style.width = Length.Percent(33.33f);
                star.style.maxWidth = large ? 64 : 34;
                star.style.height = large ? 58 : 32;
                rating.Add(star);
            }
            image.Add(rating);
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
                5 => mode == VehicleMode.Car ? new[] { Rule.MovementLane, Rule.Signal, Rule.StopBox, Rule.BoulevardSpeed }
                    : new[] { Rule.MovementLane, Rule.Signal, Rule.BoulevardSpeed },
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
                ActionLabel = GameText.Get("level.tutorial.start"), Action = StartPitchDemo } };
            string[] ids = { "preparation", "signals", "priority", "lanes", "focus", "straight", "left_turn", "right_turn", "railway", "ambulance" };
            string[] fallback = { "preparation", "signals", "priority", "lanes", "focus", "lanes", "priority", "signals", "signals", "priority" };
            for (int i = 0; i < RouteStage.All.Length; i++)
            {
                int index = i; var stage = RouteStage.All[i];
                string asset = "route_" + ids[i];
                if (!Resources.Load<Texture2D>("UI/Thumbnails/" + asset)) asset = "mission_" + fallback[i];
                items.Add(new SelectionItem { Id = "checkpoint-" + i,
                    Title = GameText.Get("level.checkpoint.title", i + 1, stage.Title), Asset = asset,
                    Meta = stage.Location, Description = stage.Instruction,
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
                if (activity.IsVillage) rules.Remove(Rule.Signal); // Ends before the traffic light.
                if (activity.IsMarket) rules.AddRange(new[] { Rule.NoStopping, Rule.PickupPlace, Rule.PickupIncomplete });
                if (activity.IsSchool) rules.Add(Rule.Pedestrian);
                items.Add(new SelectionItem { Id = "mission-" + activity.Id, Title = activity.Title,
                    Asset = "mission_" + activity.Id, Meta = activity.Focus,
                    Description = activity.Brief, Rules = rules.Distinct().ToArray(),
                    Stars = PracticeScore.Load(mode,"mission-"+activity.Id).bestStars,
                    Notice = resume ? GameText.Get("missions.in_progress",saved.attempts) : "",
                    ActionLabel = GameText.Get(resume ? "text.continue" : "text.start"), Action = () => StartMission(activity, saved) });
            }
            if(kind==MissionKind.Scenario)
                items.Add(new SelectionItem { Id="mission-hi", Title=HiTitle, Asset="city_bundaran_hi", Meta=HiFocus,
                    Description=GameText.Get("hi.brief"), Notice=GameText.Get("hi.save_note"),
                    Rules=new[]{Rule.Junction}, ActionLabel=GameText.Get("text.start"), Action=ShowHiBrief });
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
                    Notice = GameText.Get("text.buildings_and_roads_from_osm_some_heights_and_widths"),
                    ActionLabel = GameText.Get("maps.view_preview"), Action = () => OpenCityMap(index) });
            }
            return items;
        }
    }
}
