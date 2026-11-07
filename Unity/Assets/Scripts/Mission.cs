namespace JalanKita
{
    public enum MissionKind { SkillTest, Scenario }

    // Missions select encounters from the shared district; their scores never replace the full journey.
    public sealed class Mission
    {
        public readonly string Id;
        readonly string titleKey, briefKey, focusKey, durationKey;
        public string Title => GameText.Get(titleKey);
        public string Brief => GameText.Get(briefKey);
        public string Focus => GameText.Get(focusKey);
        public string Duration => GameText.Get(durationKey);
        public readonly MissionKind Kind;
        public readonly int FirstStage, LastStage;
        public readonly float Finish;
        public bool IsVillage => Id == "village";
        public bool IsSchool => Id == "school";
        public bool IsMarket => Id == "market";

        Mission(string id, MissionKind kind, string title, string brief, string focus, string duration,
            int firstStage, int lastStage, float finish)
        {
            Id = id; Kind = kind; titleKey = title; briefKey = brief; focusKey = focus; durationKey = duration;
            FirstStage = firstStage; LastStage = lastStage; Finish = finish;
        }

        public static readonly Mission[] Tests =
        {
            new("preparation", MissionKind.SkillTest, "text.before_you_drive", "text.prepare_safety_equipment_for_the_driver_and_passenger",
                "text.vehicle_safety_equipment", "text.1_minute", 0, 0, -12),
            new("signals", MissionKind.SkillTest, "text.signs_signals", "text.navigate_a_stop_sign_and_a_signal_controlled_junction",
                "text.stop_right_of_way_signals", "text.1_2_minutes", 1, 1, 108),
            new("priority", MissionKind.SkillTest, "text.right_of_way", "text.navigate_a_pedestrian_crossing_and_main_road_traffic",
                "text.pedestrians_main_road_traffic", "text.1_2_minutes", 2, 2, 204),
            new("lanes", MissionKind.SkillTest, "text.directions_lanes", "text.move_right_then_return_left_before_the_road_narrows",
                "text.road_direction_indicators_safe_gaps", "text.1_2_minutes", 3, 3, 298),
            new("focus", MissionKind.SkillTest, "text.speed_focus", "text.obey_the_30_km_h_limit_and_handle_a",
                "text.speed_concentration", "text.1_2_minutes", 4, 4, 382)
        };

        public static readonly Mission[] Scenarios =
        {
            new("village", MissionKind.Scenario, "text.leaving_the_neighborhood",
                "text.drive_with_a_passenger_to_the_local_shop_watch",
                "text.preparation_stop_right_of_way", "text.1_2_minutes", 0, 1, 56),
            new("school", MissionKind.Scenario, "text.passing_the_school",
                "text.drive_past_a_school_at_dismissal_time_someone_will",
                "text.speed_pedestrians_focus", "text.1_2_minutes", 4, 4, 382),
            new("market", MissionKind.Scenario, "text.market_pickup_8eb628",
                "text.your_passenger_requests_a_pickup_in_a_no_stopping",
                "text.road_direction_safe_stopping_indicators_gaps", "text.2_3_minutes", 3, 3, 298)
        };

        public const float SchoolZebra = 324;
        public const float SchoolPhoneCheckpoint = 328;
        public const float PickupStart = 250, PickupEnd = 278, PickupPoint = 266;
        public const float NoStopStart = 229, NoStopEnd = 244;

        public JourneySave NewSave(JourneySave previous = null) => new()
        {
            next = FirstStage,
            completions = previous?.completions ?? 0,
            bestMistakes = previous?.bestMistakes ?? -1,
            variant = (previous?.variant ?? -1) + 1
        };

        public int CompletedSteps(Journey run)
        {
            if (run.Finished) return 3;
            if (IsVillage) return run.Stage == 0 ? 0 : run.FullStop ? 2 : 1;
            if (IsSchool) return run.Save.checkpoint;
            if (IsMarket) return run.PassengerPickedUp ? 2 : run.Save.checkpoint;
            return 0;
        }
    }
}
