namespace JalanKita
{
    // Summaries of UU 22/2009; fines read with UU 1/2026 Pasal II (3)-(4).
    // No fixed fine is inferred where the simulated event does not establish an offence.
    public readonly struct LawDetails
    {
        public const string Amendment = "https://peraturan.bpk.go.id/Details/337869/uu-no-1-tahun-2026";
        public readonly string Meaning, Sanction;
        LawDetails(string meaning,string sanction) { Meaning=meaning; Sanction=sanction; }
        static string Fine(string article,string amount) => GameText.Get("law.maximum_fine", article, amount);
        public static LawDetails For(Rule rule) => rule switch
        {
            Rule.SeatBelt => new(GameText.Get("text.drivers_and_passengers_seated_beside_the_driver_in_vehicles"),Fine("Pasal 289","250.000")),
            Rule.Helmet => new(GameText.Get("text.motorcycle_riders_and_passengers_must_wear_helmets_that_meet"),Fine("Pasal 291 ayat (1)–(2)","250.000")+GameText.Get("text.applies_to_riders_without_a_helmet_or_riders_who")),
            Rule.Headlight => new(GameText.Get("text.in_addition_to_nighttime_and_other_required_lighting_motorcycle"),Fine("Pasal 293 ayat (2)","100.000")),
            Rule.NoStopping => new(GameText.Get("text.vehicles_may_not_stop_where_signs_or_markings_prohibit"),Fine(GameText.Get("text.pasal_287_ayat_1_for_violating_prohibitory_signs"),"500.000")),
            Rule.Stop or Rule.NoEntry or Rule.MovementLane => new(GameText.Get("text.drivers_must_obey_regulatory_signs_and_road_markings_in"),Fine("Pasal 287 ayat (1)","500.000")),
            Rule.Signal or Rule.RightArrow => new(GameText.Get("text.drivers_must_obey_traffic_signals_apill_at_junctions_with"),Fine("Pasal 287 ayat (2)","500.000")),
            Rule.StopBox => new(GameText.Get("text.drivers_must_obey_road_markings_and_traffic_lights_cars"),Fine(GameText.Get("text.pasal_287_ayat_1_for_markings_ayat_2_for"),"500.000")+GameText.Get("text.the_applicable_provision_depends_on_the_offense_penalties_are")),
            Rule.Junction => new(GameText.Get("text.at_an_unsignalized_junction_vehicles_from_a_smaller_road"),GameText.Get("text.pasal_113_does_not_directly_specify_a_fine_amount")),
            Rule.Pedestrian => new(GameText.Get("text.drivers_must_prioritize_pedestrian_and_cyclist_safety_pedestrians_have"),Fine("Pasal 284","500.000")),
            Rule.KeepLeft => new(GameText.Get("text.road_users_keep_left_the_right_lane_may_be"),GameText.Get("text.pasal_108_does_not_directly_specify_a_fine_a")),
            Rule.Indicator => new(GameText.Get("text.before_changing_lanes_or_moving_sideways_drivers_must_check"),Fine(GameText.Get("text.pasal_295_for_moving_without_signaling"),"250.000")),
            Rule.Gap => new(GameText.Get("text.pasal_112_ayat_2_requires_observation_before_a_lane"),GameText.Get("text.pasal_295_covers_moving_without_signaling_maximum_fine_rp250")),
            Rule.TurnSignal => new(GameText.Get("text.before_turning_or_making_a_u_turn_drivers_must"),Fine("Pasal 294","250.000")),
            Rule.Speed or Rule.BoulevardSpeed => new(GameText.Get("text.drivers_must_obey_maximum_and_minimum_speed_limits_exceeding"),Fine("Pasal 287 ayat (5)","500.000")),
            Rule.Phone => new(GameText.Get("text.drivers_must_drive_responsibly_and_with_full_concentration_the"),Fine("Pasal 283","750.000")),
            Rule.Railway => new(GameText.Get("text.at_a_level_crossing_drivers_must_stop_when_a"),Fine(GameText.Get("text.pasal_296_for_failing_to_stop_at_a_warning"),"750.000")),
            Rule.Emergency => new(GameText.Get("text.ambulances_carrying_patients_have_priority_priority_vehicles_use_a"),Fine("Pasal 287 ayat (4)","250.000")),
            _ => new(GameText.Get("text.this_is_a_practice_objective_or_safety_recommendation"),GameText.Get("text.this_exercise_does_not_assign_an_automatic_fine"))
        };
    }
}
