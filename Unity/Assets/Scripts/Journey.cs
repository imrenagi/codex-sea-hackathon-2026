using System;
using UnityEngine;

namespace JalanKita
{
    public enum VehicleMode { Car, Motorcycle }

    public enum Rule
    {
        SeatBelt, Helmet, Headlight, Stop, Signal, Junction, Pedestrian, KeepLeft, NoEntry,
        Indicator, Gap, ReturnLeft, Speed, Phone, OffRoad, Collision, NoStopping, PickupPlace, PickupIncomplete,
        TurnSignal, MovementLane, RightArrow, StopBox, Railway, Emergency, BoulevardSpeed
    }

    public sealed class Lesson
    {
        public const string Law = "https://peraturan.bpk.go.id/Details/38654/uu-no-22-tahun-2009";
        public const string Signs = "https://peraturan.bpk.go.id/Details/103683/permenhub-no-13-tahun-2014";
        public const string Signals = "https://peraturan.bpk.go.id/Details/103764/permenhub-no-49-tahun-2014";
        public const string Limits = "https://peraturan.bpk.go.id/Details/103508/permenhub-no-111-tahun-2015";

        public readonly string Url, Regulation;
        readonly string titleKey, explanationKey, actionKey, citationKey;
        public string Title => GameText.Get(titleKey);
        public string Explanation => GameText.Get(explanationKey);
        public string Action => GameText.Get(actionKey);
        public string Citation => GameText.Get(citationKey);
        public Lesson(string title, string explanation, string action, string citation, string url = Law,
            string regulation = "UU 22/2009")
        {
            titleKey = title; explanationKey = explanation; actionKey = action; citationKey = citation; Url = url;
            Regulation = regulation;
        }

        public static readonly Lesson[] All =
        {
            new("text.seat_belts_are_not_fastened", "text.the_driver_and_front_passenger_must_wear_seat_belts",
                "text.fasten_both_seat_belts_before_selecting_start_driving", "Pasal 106 ayat (6)"),
            new("text.sni_helmets_are_not_ready", "text.the_motorcycle_rider_and_passenger_must_wear_sni_approved",
                "text.equip_both_people_with_sni_helmets_fasten_the_straps", "Pasal 106 ayat (8)"),
            new("text.motorcycle_headlight_is_off", "text.motorcycles_must_use_their_headlights_during_the_day_too",
                "text.turn_on_the_headlight_before_setting_off_and_keep", "Pasal 107 ayat (2)"),
            new("text.crossed_the_stop_line", "text.a_stop_sign_requires_a_complete_stop_before_the",
                "text.release_the_throttle_brake_to_0_km_h_then", "Pasal 106 ayat (4) huruf a–b"),
            new("text.ran_a_red_light", "text.drivers_must_obey_traffic_signals",
                "text.stop_before_the_white_line_and_wait_for_green", "Pasal 106 ayat (4) huruf c"),
            new("text.main_road_traffic_is_still_passing", "text.when_leaving_a_smaller_road_yield_to_traffic_on",
                "text.hold_the_brake_before_the_junction_wait_for_traffic", "Pasal 113 ayat (1) huruf b"),
            new("text.pedestrians_are_still_crossing", "text.pedestrians_have_priority_at_a_crossing",
                "text.wait_behind_the_crossing_until_pedestrians_have_cleared_your", "Pasal 106 ayat (2) dan Pasal 131 ayat (2)"),
            new("text.entered_the_oncoming_lane", "text.drive_on_the_left_side_of_the_road",
                "text.return_to_the_left_of_the_center_line_and", "Pasal 108 ayat (1)"),
            new("text.entered_against_a_no_entry_sign", "text.the_no_entry_sign_prohibits_access_from_this_direction",
                "text.follow_the_straight_arrows_on_the_one_way_road", "Pasal 106 ayat (4) huruf a"),
            new("text.changed_lanes_without_indicating", "text.check_traffic_and_signal_in_the_intended_direction_before",
                "text.press_e_for_the_right_indicator_or_q_for", "Pasal 112 ayat (2)"),
            new("text.lane_change_gap_is_too_small", "text.check_traffic_ahead_and_behind_before_changing_lanes",
                "text.wait_for_the_car_in_your_target_lane_to", "Pasal 112 ayat (2)"),
            new("text.lane_change_exercise_is_incomplete", "text.this_exercise_requires_a_safe_move_right_and_a",
                "text.signal_right_at_the_dashed_line_then_signal_left", "text.practice"),
            new("text.exceeded_the_30_km_h_limit", "text.the_posted_speed_limit_applies_on_this_road",
                "text.slow_down_before_the_30_sign_release_the_throttle", "Pasal 106 ayat (4) huruf g dan Pasal 115 huruf a"),
            new("text.answered_the_phone_while_driving", "text.drivers_must_stay_focused_using_a_phone_takes_attention",
                "text.ignore_calls_while_driving_stop_somewhere_safe_before_using", "Pasal 106 ayat (1) dan Penjelasan"),
            new("text.left_the_practice_route", "text.your_vehicle_left_the_road_provided_for_this_exercise",
                "text.follow_the_route_markings_and_control_your_speed_before", "text.practice"),
            new("text.too_close_to_another_vehicle", "text.leave_enough_space_to_brake_if_traffic_ahead_slows",
                "text.keep_your_distance_and_brake_early_when_approaching_other", "text.safety_advice_9131f9"),
            new("text.stopped_in_a_no_stopping_zone", "text.a_no_stopping_sign_still_applies_when_a_passenger",
                "text.continue_to_the_pickup_bay_on_the_left_beyond", "Pasal 118 huruf a"),
            new("text.pickup_location_is_not_safe_yet", "text.in_this_mission_pick_up_the_passenger_after_stopping",
                "text.signal_left_enter_the_bay_stop_beside_the_passenger", "text.practice"),
            new("text.passenger_has_not_been_picked_up", "text.pick_up_the_passenger_then_merge_back_into_traffic",
                "text.stop_in_the_pickup_bay_press_pick_up_f", "text.practice"),
            new("text.turned_without_indicating", "text.check_traffic_and_signal_in_the_intended_direction_before_96621f",
                "text.press_q_before_turning_left_or_e_before_turning", "Pasal 112 ayat (1)"),
            new("text.wrong_lane_for_this_direction", "text.follow_lane_arrows_and_do_not_cross_solid_lane",
                "text.choose_your_lane_before_the_solid_line_left_to", "Pasal 106 ayat (4) huruf a–b"),
            new("text.right_turn_arrow_is_not_green", "text.a_green_light_for_straight_traffic_does_not_permit",
                "text.stop_in_the_right_lane_wait_for_a_green", "Pasal 106 ayat (4) huruf c"),
            new("text.car_entered_the_motorcycle_stop_box", "text.the_car_crossed_its_stop_line_on_red_and",
                "text.cars_stop_before_the_red_box_motorcycles_may_use", "Pasal 106 ayat (4) huruf b–c"),
            new("text.entered_a_closed_railway_crossing", "text.stop_when_the_warning_sounds_or_the_barriers_begin",
                "text.stop_before_the_line_wait_for_the_train_to", "Pasal 114"),
            new("text.blocked_an_ambulance", "text.an_ambulance_carrying_a_patient_has_priority_this_exercise",
                "text.locate_the_ambulance_signal_left_and_move_into_the", "Pasal 134 huruf b dan Pasal 135 ayat (1)"),
            new("text.exceeded_the_50_km_h_limit", "text.the_50_km_h_limit_applies_on_this_main",
                "text.ease_off_the_throttle_and_give_way_calmly_do", "Pasal 106 ayat (4) huruf g dan Pasal 115 huruf a")
        };

        public bool HasSource => Citation.StartsWith("Pasal", StringComparison.Ordinal);
    }

    public sealed class RouteStage
    {
        readonly string titleKey, locationKey, instructionKey;
        public string Title => GameText.Get(titleKey);
        public string Location => GameText.Get(locationKey);
        public string Instruction => GameText.Get(instructionKey);
        public readonly float End, Heading;
        public readonly Vector2 Checkpoint;
        RouteStage(string title, string location, string instruction, float end)
        {
            titleKey = title; locationKey = location; instructionKey = instruction; End = end;
            Checkpoint = new Vector2(LeftLane, end);
        }

        RouteStage(string title, string location, string instruction, Vector2 checkpoint, float heading = 0)
            : this(title, location, instruction, checkpoint.y)
        { Checkpoint = checkpoint; Heading = heading; }

        public static readonly RouteStage[] All =
        {
            new("text.before_you_drive", "text.kenanga_garage", "text.check_the_driver_and_passenger_safety_equipment", -12),
            new("text.signs_signals", "text.jalan_kenanga", "text.stop_completely_at_the_stop_line_before_continuing", 108),
            new("text.right_of_way", "text.pasar_bunga", "text.give_pedestrians_priority_at_the_crossing", 204),
            new("text.directions_lanes", "text.jalan_mawar", "text.follow_the_arrows_signal_and_move_into_the_right", 298),
            new("text.speed_focus", "text.school_zone", "text.slow_down_before_the_30_km_h_sign", 382),
            new("text.straight_through_the_main_road", "text.jalan_thamrin", "text.choose_the_middle_lane_before_the_solid_line_wait", new Vector2(-5.4f, 550)),
            new("text.left_turn_pedestrians", "text.thamrin_diponegoro", "text.choose_the_leftmost_lane_press_q_then_wait_for", new Vector2(-70, 631), 270),
            new("text.protected_right_turn", "text.diponegoro_ahmad_yani", "text.choose_the_right_lane_before_the_solid_line_press", new Vector2(-181.8f, 720)),
            new("text.railway_crossing", "text.ahmad_yani_crossing", "text.stop_when_the_bell_sounds_wait_for_the_train", new Vector2(-185.4f, 850)),
            new("text.yield_to_the_ambulance", "text.jalan_rumah_sakit", "text.pass_the_minibus_in_the_middle_lane_when_the", new Vector2(-189, 1040))
        };

        public const float LeftLane = -2.15f;
        public const float RightLane = 2.15f;
        public const float StopLine = 30, StopJunction = 36, LightLine = 82, LightJunction = 89;
        public const float Zebra = 142, YieldLine = 174, MainJunction = 181;
        public const float OneWayStart = 220, Merge = 282, SpurStart = 236, SpurEnd = 265;
        public const float SchoolLimit = 310, PhoneStart = 330, PhoneEnd = 358;
    }

    [Serializable]
    public sealed class JourneySave
    {
        public int next;
        public int before = -1;
        public int after = -1;
        public int[] mistakes = new int[Lesson.All.Length];
        public int checkpoint, variant, attempts = 1, completions;
        public int trafficSeed;
        public int bestMistakes = -1;
        public bool retryPending;

        // Schema 2 fixes Rule IDs after two independently extended v1 builds.
        // Keep all v1 values untouched so either original build can still open its own save.
        public int schema;
        public int legacyAfter = -1;
        public static string Key(VehicleMode mode, Mission mission = null) => mission == null
            ? "JalanKita.Journey.v2." + mode : "JalanKita.Mission.v2." + mode + "." + mission.Id;
        public static string LegacyKey(VehicleMode mode, Mission mission = null) => mission == null
            ? "JalanKita.Journey.v1." + mode : "JalanKita.Mission.v1." + mode + "." + mission.Id;

        public static JourneySave Load(VehicleMode mode, Mission mission = null)
        {
            var data = PlayerPrefs.GetString(Key(mode, mission), "");
            bool legacy = string.IsNullOrEmpty(data);
            if (legacy) data = PlayerPrefs.GetString(LegacyKey(mode, mission), "");
            if (string.IsNullOrEmpty(data)) return null;
            JourneySave save;
            try { save = JsonUtility.FromJson<JourneySave>(data); }
            catch (ArgumentException) { return null; }
            if (save == null || save.next < (mission?.FirstStage ?? 0)
                || save.next > (mission == null ? RouteStage.All.Length : mission.LastStage + 1)
                || save.mistakes == null || save.checkpoint < 0 || save.checkpoint > 2) return null;
            int count = save.mistakes.Length;
            if (legacy)
            {
                if ((count < 16 || count > 19) && count != 23) return null;
                var mapped = new int[Lesson.All.Length];
                Array.Copy(save.mistakes, mapped, Mathf.Min(count, 16));
                if (count > 16 && count <= 19) Array.Copy(save.mistakes, 16, mapped, 16, count - 16);
                if (count == 23) Array.Copy(save.mistakes, 16, mapped, 19, 7);
                save.mistakes = mapped;
                // The original five-stage final quiz remains recorded; the expanded route has its own final quiz.
                if (mission == null && save.next < RouteStage.All.Length && save.after >= 0)
                { save.legacyAfter = save.after; save.after = -1; }
            }
            else if (save.schema != 2 || count != Lesson.All.Length) return null;
            save.attempts = Mathf.Max(1, save.attempts);
            save.schema = 2; // Reads migrate in memory; an explicit gameplay save writes the v2 key.
            return save;
        }

        public void Store(VehicleMode mode, Mission mission = null)
        {
            schema = 2;
            PlayerPrefs.SetString(Key(mode, mission), JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

    }

    public sealed partial class Journey
    {
        readonly VehicleSteering steering = new();
        public bool LearningControls { get; set; }
        public bool UsesRouteSteering => Activity == null;
        public float SteerAngle => UsesRouteSteering ? steering.Angle : motion.SteeringAngle * Mathf.Deg2Rad;
        public float Throttle { get; private set; }
        public VehicleMode Mode { get; }
        public JourneySave Save { get; }
        public Mission Activity { get; }
        public Vector2 Position { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public int Indicator { get; private set; }
        public Rule? Failure { get; private set; }
        public bool DriverGear, PassengerGear, Headlight;
        public bool PhoneRinging { get; private set; }
        public int Stage => Save.next;
        public bool Finished => Stage > (Activity?.LastStage ?? RouteStage.All.Length - 1);
        public bool IsSchool => Activity?.IsSchool == true;
        public bool IsMarket => Activity?.IsMarket == true;
        public bool PassengerPickedUp { get; private set; }
        public float ElapsedTime { get; private set; }
        public float BoardingProgress => PassengerPickedUp ? Mathf.Clamp01(1 - boardingTime / 2.2f) : 0;
        public bool IsBoarding => boardingTime > 0;
        public bool CanPickup => IsMarket && !PassengerPickedUp && Mathf.Abs(Speed) < .28f
            && Position.x < -5 && Position.y >= Mission.PickupPoint - 5 && Position.y <= Mission.PickupPoint + 4;
        public int MistakeCount { get { int count = 0; foreach (int n in Save.mistakes) count += n; return count; } }
        public float StageEnd => Stage == Activity?.LastStage ? Activity.Finish : RouteStage.All[Mathf.Min(Stage, RouteStage.All.Length - 1)].End;
        public float CrossingZebra => IsSchool ? Mission.SchoolZebra : RouteStage.Zebra;
        public Vector2? PickupTraffic { get; private set; }
        public float Kph => Mathf.Abs(Speed) * 3.6f;
        public float Front => Position.y + Mathf.Cos(Heading * Mathf.Deg2Rad) * (Mode == VehicleMode.Car ? 1.95f : 1f);

        public Vector2? CrossTraffic { get; private set; }
        public int CrossDirection { get; private set; }
        public Vector2? Pedestrian { get; private set; }
        public int PedestrianDirection { get; private set; }
        public Vector2? LaneTraffic { get; private set; }
        public Vector2? SpurTraffic { get; private set; }

        public bool FullStop { get; private set; }
        public bool ChangedRight { get; private set; }
        public bool ReturnedLeft { get; private set; }
        public bool CrossedZebra => Front > CrossingZebra + 2;
        public bool MainRoadEncounterStarted => priorityTime >= 0;
        public int LightPhase => lightTime < 0 || lightTime % 22 < 8 ? 0 : lightTime % 22 < 19 ? 2 : 1;

        float stopTime, lightTime, zebraTime, priorityTime, laneTime, wrongWay, overSpeed;
        float rightSignalAge, leftSignalAge, stoppedInRestriction, pickupTime, boardingTime;
        int lane;
        bool phoneResolved;
        readonly bool persistent;
        readonly RoadVehicleMotion motion = new();
        Vector2 motionFrom;
        float motionHeading;
        bool reverseSweep;
        readonly int trafficSeed;
        public TrafficRoster Traffic { get; private set; }

        public Journey(VehicleMode mode, JourneySave save, bool persistent = true, Mission mission = null, int? trafficSeed = null)
        {
            Mode = mode; Save = save; Activity = mission;
            this.persistent = persistent;
            // Cosmetic randomness is saved once: retry, reload and demonstration share a roster.
            bool newTrafficSeed=!trafficSeed.HasValue&&save.trafficSeed==0;
            if(newTrafficSeed)save.trafficSeed=UnityEngine.Random.Range(1,int.MaxValue);
            this.trafficSeed = trafficSeed ?? save.trafficSeed;
            if(newTrafficSeed)Persist();
            if (Activity != null && Save.retryPending)
            {
                Save.attempts++;
                Save.variant++;
                Save.retryPending = false;
                Persist();
            }
            ResetEncounter();
        }

        public void Retry()
        {
            Save.retryPending = false;
            if (Activity != null)
            {
                Save.attempts++;
                Save.variant++;
                Persist();
            }
            ResetEncounter();
        }

        // Recovery is deliberately from the current checkpoint, never from inside a collision/barrier.
        // Unlike a fresh attempt, it preserves all counted violations for feedback and rating.
        public void ContinueAfterFailure()
        {
            if (!Failure.HasValue) return;
            Save.retryPending = false;
            ResetEncounter();
            Persist();
        }

        void ResetEncounter()
        {
            Traffic = new TrafficRoster(unchecked(trafficSeed + Save.variant * 7919));
            var checkpoint = RouteStage.All[Mathf.Clamp(Stage - 1, 0, RouteStage.All.Length - 1)];
            Position = checkpoint.Checkpoint;
            Heading = checkpoint.Heading;
            if (!Finished && IsSchool && Save.checkpoint > 0)
                Position = new Vector2(RouteStage.LeftLane, Save.checkpoint == 1 ? 313 : Mission.SchoolPhoneCheckpoint);
            if (!Finished && IsMarket && Save.checkpoint > 0)
                Position = Save.checkpoint == 1 ? new Vector2(RouteStage.LeftLane, 246) : new Vector2(-5.8f, Mission.PickupPoint);
            Speed = wrongWay = overSpeed = Throttle = 0;
            motion.Reset();
            motionFrom = Position; motionHeading = Heading; reverseSweep = false;
            steering.Reset();
            ResetCityTraffic();
            ResetBoulevard();
            Indicator = lane = 0;
            rightSignalAge = leftSignalAge = 0;
            Failure = null;
            DriverGear = PassengerGear = Headlight = Stage > 0;
            FullStop = ChangedRight = ReturnedLeft = phoneResolved = PhoneRinging = false;
            stopTime = lightTime = zebraTime = priorityTime = laneTime = -1;
            stoppedInRestriction = 0;
            pickupTime = -1;
            PassengerPickedUp = IsMarket && Save.checkpoint >= 2;
            boardingTime = ElapsedTime = 0;
            if (PassengerPickedUp) pickupTime = 0;
            CrossTraffic = Pedestrian = LaneTraffic = SpurTraffic = PickupTraffic = null;
            ClearCrossingPedestrians();
            ResetMainRoadTraffic();
        }

        public bool Depart()
        {
            if (Stage != 0 || Failure.HasValue) return false;
            if (!DriverGear || !PassengerGear)
                Fail(Mode == VehicleMode.Car ? Rule.SeatBelt : Rule.Helmet);
            else if (Mode == VehicleMode.Motorcycle && !Headlight)
                Fail(Rule.Headlight);
            else
            {
                Advance();
                return true;
            }
            return false;
        }

        public void ToggleIndicator(int direction)
        {
            Indicator = Indicator == direction ? 0 : direction;
            rightSignalAge = leftSignalAge = 0;
        }

        public void ToggleHeadlight() => Headlight = !Headlight;

        public void Pickup()
        {
            if (!IsMarket || Finished || Failure.HasValue || PassengerPickedUp) return;
            if (Position.y >= Mission.NoStopStart && Position.y <= Mission.NoStopEnd) Fail(Rule.NoStopping);
            else if (!CanPickup) Fail(Rule.PickupPlace);
            else
            {
                PassengerPickedUp = true;
                boardingTime = 2.2f;
                Speed = 0;
                pickupTime = 0;
                Checkpoint(2);
            }
        }

        public void ResolvePhone(bool answer)
        {
            if (Stage != 4 || !PhoneRinging || Failure.HasValue) return;
            if (answer)
            {
                if (Mathf.Abs(Speed) > .28f || Position.x > -4.25f || Position.y < RouteStage.PhoneStart + 4)
                {
                    Fail(Rule.Phone);
                    return;
                }
            }
            PhoneRinging = false;
            phoneResolved = true;
        }

        public void Step(float dt, bool throttle, bool brake, float steer, bool reverse = false)
        {
            if (Stage == 0 || Finished || Failure.HasValue) return;
            motionFrom = Position; motionHeading = Heading; reverseSweep = reverse || Speed < 0;
            ElapsedTime += dt;
            Throttle = (throttle || reverse) && !brake ? 1 : 0;
            // Boarding is a simulation state: the vehicle stays stopped while the passenger approaches.
            if (boardingTime > 0)
            {
                boardingTime = Mathf.Max(0, boardingTime - dt);
                Speed = 0;
                UpdateActors(dt);
                return;
            }

            if (Stage >= 5) { StepBoulevard(dt, throttle, brake, steer, reverse); return; }

            var oldPosition = Position;
            float oldHeading = Heading;
            var oldActors = (CrossTraffic, LaneTraffic, SpurTraffic, PickupTraffic);
            float oldFront = Front;
            float oldX = Position.x;
            float targetSpeed = Mode == VehicleMode.Car ? 16.1f : 16.7f;
            float speed = Speed, heading = Heading;
            if (UsesRouteSteering)
            {
                speed = reverse || speed < 0 ? RoadVehicleMotion.AdvanceSpeed(dt, Mode, throttle, brake, reverse, targetSpeed, speed)
                    : brake ? Mathf.MoveTowards(speed, 0, 10.5f * dt)
                    : Mathf.MoveTowards(speed, throttle ? targetSpeed : 0, dt * (throttle ? (Mode == VehicleMode.Car ? 3.3f : 3.8f) : 1.7f));
                heading = steering.Step(Mode, speed, heading, steer, dt);
            }
            else motion.Step(dt, Mode, throttle, brake, steer, targetSpeed, ref speed, ref heading, reverse);
            Speed = speed; Heading = heading;
            Position += new Vector2(Mathf.Sin(Heading * Mathf.Deg2Rad), Mathf.Cos(Heading * Mathf.Deg2Rad)) * (Speed * dt);
            if (RejectReverseOffRoad()) return;
            rightSignalAge = Indicator == 1 ? rightSignalAge + dt : 0;
            leftSignalAge = Indicator == -1 ? leftSignalAge + dt : 0;
            UpdateActors(dt, oldPosition, oldHeading);

            float z = Position.y;
            float x = Position.x;
            bool oneWay = z >= RouteStage.OneWayStart && z <= RouteStage.Merge;
            bool spur = z >= RouteStage.SpurStart && z <= RouteStage.SpurEnd;
            if (spur && x > 5.25f) Fail(Rule.NoEntry);
            bool bay = z >= RouteStage.PhoneStart + 4 && z <= RouteStage.PhoneEnd;
            bay |= IsMarket && z >= Mission.PickupStart && z <= Mission.PickupEnd;
            if (x < (bay ? -7.45f : -4.45f) || x > (spur ? 9.1f : 4.45f)) Fail(Rule.OffRoad);
            if (Mode == VehicleMode.Motorcycle && !Headlight && Kph >= 3) Fail(Rule.Headlight);
            if (!oneWay && x > .42f && Mathf.Abs(Speed) > 1)
            {
                wrongWay += dt;
                if (wrongWay > .6f) Fail(Rule.KeepLeft);
            }
            else wrongWay = 0;

            if (Stage == 1)
            {
                if (Front >= RouteStage.StopLine - 5 && Front <= RouteStage.StopLine && Kph <= 1) FullStop = true;
                if (Crosses(oldFront, RouteStage.StopLine) && !FullStop) Fail(Rule.Stop);
                CheckJunction(oldFront, RouteStage.StopJunction);
                if (Crosses(oldFront, RouteStage.LightLine) && LightPhase == 0) Fail(Rule.Signal);
                CheckJunction(oldFront, RouteStage.LightJunction, signalled: true);
            }
            if (Stage == 2)
            {
                if (Crosses(oldFront, RouteStage.Zebra - 2) && PedestrianBlocksPath) Fail(Rule.Pedestrian);
                CheckJunction(oldFront, RouteStage.MainJunction);
            }
            if (Stage == 3 && oneWay)
            {
                int currentLane = x > .32f ? 1 : x < -.32f ? 0 : lane;
                if (currentLane != lane)
                {
                    if ((currentLane == 1 ? rightSignalAge : leftSignalAge) < .28f) Fail(Rule.Indicator);
                    if (currentLane == 1 && LaneTraffic.HasValue)
                    {
                        float gap = LaneTraffic.Value.y - z;
                        float requiredGap = Mathf.Max(5, (gap >= 0 ? Speed - 5.1f : 5.1f - Speed) * 2 + 4);
                        if (Mathf.Abs(gap) < requiredGap) Fail(Rule.Gap);
                    }
                    if (currentLane == 1) ChangedRight = true;
                    else if (ChangedRight) ReturnedLeft = true;
                    lane = currentLane;
                }
                if (LaneTraffic.HasValue && TrafficCollision(Traffic.Lane, LaneTraffic.Value, 0))
                    Fail(Rule.Collision);
            }
            if (Stage == 3 && !IsMarket && z > RouteStage.Merge && (!ChangedRight || !ReturnedLeft || x >= 0))
                Fail(Rule.ReturnLeft);

            if (IsMarket)
            {
                bool enteringBay = oldX >= -4.25f && x < -4.25f;
                bool leavingBay = oldX < -4.25f && x >= -4.25f;
                if (enteringBay && leftSignalAge < .28f || leavingBay && rightSignalAge < .28f) Fail(Rule.Indicator);
                if (leavingBay && PickupTraffic.HasValue && Mathf.Abs(PickupTraffic.Value.y - z) < 11) Fail(Rule.Gap);
                if (PickupTraffic.HasValue && TrafficCollision(Traffic.Pickup, PickupTraffic.Value, 0))
                    // A rotated nose can leave the bay before its center crosses the boundary.
                    // Preserve the unsafe-rejoin lesson when that nose meets passing traffic.
                    Fail(PassengerPickedUp && x < -4.25f && Heading > 0 ? Rule.Gap : Rule.Collision);
                if (z >= Mission.NoStopStart && z <= Mission.NoStopEnd && Mathf.Abs(Speed) < .28f)
                {
                    stoppedInRestriction += dt;
                    if (stoppedInRestriction > 2) Fail(Rule.NoStopping);
                }
                else stoppedInRestriction = 0;
                if (z >= 246) Checkpoint(1);
                if (z > Mission.PickupEnd && !PassengerPickedUp) Fail(Rule.PickupIncomplete);
            }

            if (Stage == 4)
            {
                if (Front >= RouteStage.SchoolLimit && Kph > 30.75f)
                {
                    overSpeed += dt;
                    if (overSpeed >= .6f) Fail(Rule.Speed);
                }
                else overSpeed = 0;
                if (IsSchool)
                {
                    if (Crosses(oldFront, CrossingZebra - 2) && PedestrianBlocksPath) Fail(Rule.Pedestrian);
                    if (z >= 313) Checkpoint(1);
                    if (z >= Mission.SchoolPhoneCheckpoint) Checkpoint(2);
                }
                if (z >= RouteStage.PhoneStart && !phoneResolved) PhoneRinging = true;
                if (z >= RouteStage.PhoneEnd && PhoneRinging) ResolvePhone(false);
            }

            CheckEncounterContacts(oldPosition, oldHeading, oldActors);
            for (int i = 0; i < CrossingPedestrianCount; i++)
                if (crossingPedestrians[i].HasValue && TouchesPedestrian(crossingPedestrians[i].Value))
                    Fail(Rule.Pedestrian);
            if (!Failure.HasValue && z >= StageEnd) Advance();
        }

        bool Crosses(float oldFront, float line) => oldFront < line && Front >= line;

        void CheckJunction(float oldFront, float center, bool signalled = false)
        {
            if (Stage == 2 && center == RouteStage.MainJunction)
            {
                if (Front < center - 4.3f || Position.y > center + 5.5f) return;
                for (int i = 0; i < MainRoadTrafficCount; i++)
                {
                    if (!mainRoadTraffic[i].HasValue) continue;
                    var carPosition = mainRoadTraffic[i].Value;
                    float carDistance = (Position.x - carPosition.x) * CrossDirection;
                    if (Crosses(oldFront, center - 4.3f) && carDistance >= -4 && carDistance < 18) Fail(Rule.Junction);
                    if (TrafficCollision(MainRoadTrafficChoice(i), carPosition, CrossDirection * 90)) Fail(Rule.Junction);
                }
                return;
            }
            if (!CrossTraffic.HasValue || Front < center - 4.3f || Position.y > center + 5.5f) return;
            Vector2 traffic = CrossTraffic.Value;
            if (traffic.y != center) return;
            float distance = (Position.x - traffic.x) * CrossDirection;
            if (!signalled && Crosses(oldFront, center - 4.3f) && distance >= -4 && distance < 18) Fail(Rule.Junction);
            if (TrafficCollision(Traffic.Cross, traffic, CrossDirection * 90))
                Fail(signalled ? Rule.Collision : Rule.Junction);
        }

        public bool TrafficCollision(TrafficChoice choice, Vector2 position, float yaw) =>
            TrafficFootprint.Overlaps(Position, Heading, TrafficFootprint.PlayerHalfWidth(Mode), TrafficFootprint.PlayerHalfLength(Mode),
                position, yaw, choice.Vehicle.HalfWidth, choice.Vehicle.HalfLength)
            || reverseSweep && TrafficFootprint.SweptOverlaps(motionFrom,motionHeading,Position,Heading,
                TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode),
                position,yaw,choice.Vehicle.HalfWidth,choice.Vehicle.HalfLength);

        bool TouchesPedestrian(Vector2 pedestrian)
        {
            if(PedestrianClearance.Touches(pedestrian,Position,Heading,Mode))return true;
            if(!reverseSweep)return false;
            float turn=Mathf.DeltaAngle(motionHeading,Heading);
            int samples=Mathf.Max(1,Mathf.CeilToInt((Vector2.Distance(motionFrom,Position)+Mathf.Abs(turn)*Mathf.Deg2Rad*
                new Vector2(TrafficFootprint.PlayerHalfWidth(Mode),TrafficFootprint.PlayerHalfLength(Mode)).magnitude)/.1f));
            for(int i=0;i<=samples;i++)
                if(PedestrianClearance.Touches(pedestrian,Vector2.Lerp(motionFrom,Position,(float)i/samples),motionHeading+turn*i/samples,Mode))return true;
            return false;
        }

        bool RejectReverseOffRoad()
        {
            if (!reverseSweep) return false;
            float angle=Heading*Mathf.Deg2Rad;
            Vector2 forward=new(Mathf.Sin(angle),Mathf.Cos(angle)),right=new(forward.y,-forward.x);
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                var point=Position+right*(x*RoadBoundary.HalfWidth(Mode))+forward*(z*RoadBoundary.HalfLength(Mode));
                if(TrainingRoadContains(point))continue;
                Position=motionFrom;Heading=motionHeading;Fail(Rule.OffRoad);return true;
            }
            return false;
        }

        // Bounds match the finite asphalt rectangles in District.BuildRoad and
        // BuildBoulevard. Reversing cannot escape an otherwise infinite approach
        // test; the existing off-road lesson keeps the last valid vehicle pose.
        bool TrainingRoadContains(Vector2 p)
        {
            bool Rect(float left,float right,float start,float end)=>p.x>=left&&p.x<=right&&p.y>=start&&p.y<=end;
            if(Rect(-4.75f,4.75f,-28,382)||Rect(4.675f,9.025f,RouteStage.SpurStart,RouteStage.SpurEnd+1)
                ||Rect(-7.85f,-4.45f,RouteStage.PhoneStart+4,RouteStage.PhoneEnd)
                ||IsMarket&&Rect(-7.85f,-4.45f,Mission.PickupStart,Mission.PickupEnd))return true;
            if(Mathf.Abs(p.x)<=45&&(Mathf.Abs(p.y-RouteStage.StopJunction)<=4.55f
                ||Mathf.Abs(p.y-RouteStage.LightJunction)<=4.55f||Mathf.Abs(p.y-RouteStage.MainJunction)<=4.55f))return true;
            if(Rect(-10.8f,10.8f,382,710)||Rect(-85,85,469.2f,490.8f)
                ||Rect(-260,85,629.2f,650.8f)||Rect(-190.8f,-169.2f,565,1165))return true;
            for(int stage=5;stage<=7;stage++)
            {var d=p-Boulevard.Center(stage);if(Mathf.Abs(d.x)<=17&&Mathf.Abs(d.y)<=17)return true;}
            return false;
        }

        void UpdateActors(float dt, Vector2? playerFrom = null, float? playerHeadingFrom = null)
        {
            var previousCross = CrossTraffic;
            var previousLane = LaneTraffic;
            var previousSpur = SpurTraffic;
            var previousPickup = PickupTraffic;
            float z = Position.y;
            if (Stage == 1)
            {
                Tick(ref stopTime, dt, z >= RouteStage.StopLine - 22);
                Tick(ref lightTime, dt, z >= RouteStage.LightLine - 22);
                CrossDirection = (Activity == null ? Save.mistakes[(int)Rule.Junction] : Save.variant) % 2 == 0 ? 1 : -1;
                CrossTraffic = CrossingCar(stopTime, RouteStage.StopJunction, CrossDirection);
                if (!CrossTraffic.HasValue && lightTime >= 0 && LightPhase == 0)
                {
                    float phase = lightTime % 22;
                    CrossDirection = 1;
                    CrossTraffic = CrossingCar(phase - 1.5f, RouteStage.LightJunction, CrossDirection);
                }
            }
            else if (Stage == 2)
            {
                Tick(ref zebraTime, dt, z >= RouteStage.Zebra - 19);
                Tick(ref priorityTime, dt, z >= RouteStage.YieldLine - 20);
                PedestrianDirection = Save.mistakes[(int)Rule.Pedestrian] % 2 == 0 ? 1 : -1;
                UpdateCrossingPedestrians(RouteStage.Zebra, 2.45f);
                CrossDirection = Save.mistakes[(int)Rule.Junction] % 2 == 0 ? -1 : 1;
                UpdateMainRoadTraffic(dt, playerFrom, playerHeadingFrom);
            }
            else if (Stage == 3)
            {
                Tick(ref laneTime, dt, z >= RouteStage.OneWayStart - 7);
                float actorZ = RouteStage.OneWayStart + 3 + Mathf.Max(0, laneTime) * 5.1f;
                LaneTraffic = actorZ < RouteStage.Merge + 2 ? new Vector2(RouteStage.RightLane, actorZ) : null;
                SpurTraffic = laneTime < 0 ? null : new Vector2(7.4f, RouteStage.SpurEnd - (laneTime * 5) % 26);
                if (IsMarket && PassengerPickedUp)
                {
                    Tick(ref pickupTime, dt, true);
                    float trafficZ = Mission.PickupPoint - 22 + pickupTime * (Save.variant % 2 == 0 ? 5.1f : 4.6f);
                    PickupTraffic = trafficZ < RouteStage.Merge + 12 ? new Vector2(RouteStage.LeftLane, trafficZ) : null;
                }
            }
            else if (IsSchool && Save.checkpoint < 2)
            {
                Tick(ref zebraTime, dt, z >= 307);
                PedestrianDirection = Save.variant % 2 == 0 ? 1 : -1;
                UpdateCrossingPedestrians(CrossingZebra, 1.5f);
            }
            else if (IsSchool) ClearCrossingPedestrians();
            if (Stage != 2)
                CrossTraffic = FollowEncounter(previousCross, CrossTraffic, Traffic.Cross, Vector2.right * CrossDirection, 6.2f, dt, playerFrom, playerHeadingFrom);
            LaneTraffic = FollowEncounter(previousLane, LaneTraffic, Traffic.Lane, Vector2.up, 5.1f, dt, playerFrom, playerHeadingFrom);
            SpurTraffic = FollowEncounter(previousSpur, SpurTraffic, Traffic.Spur, Vector2.down, 5, dt, playerFrom, playerHeadingFrom);
            PickupTraffic = FollowEncounter(previousPickup, PickupTraffic, Traffic.Pickup, Vector2.up, Save.variant % 2 == 0 ? 5.1f : 4.6f, dt, playerFrom, playerHeadingFrom);
        }

        static Vector2? CrossingCar(float timer, float center, int direction)
        {
            float x = -24 + timer * 6.2f;
            return timer >= 0 && x < 26 ? new Vector2(x * direction, center) : null;
        }

        static void Tick(ref float time, float dt, bool start)
        {
            if (time >= 0) time += dt;
            else if (start) time = 0;
        }

        void Fail(Rule rule)
        {
            if (LearningControls || Failure.HasValue) return;
            Failure = rule;
            Save.retryPending = true;
            Save.mistakes[(int)rule]++;
            Persist();
            Speed = 0;
        }

        void Advance()
        {
            if (LearningControls) return;
            Save.next++;
            ResetBoulevard();
            if (Stage == 5) ResetCityTraffic();
            Save.checkpoint = 0;
            if (Activity != null && Finished)
            {
                Save.completions++;
                Save.bestMistakes = Save.bestMistakes < 0 ? MistakeCount : Mathf.Min(Save.bestMistakes, MistakeCount);
            }
            Persist();
            CrossTraffic = Pedestrian = LaneTraffic = SpurTraffic = PickupTraffic = null;
            ClearCrossingPedestrians();
            ResetMainRoadTraffic();
        }

        void Checkpoint(int index)
        {
            if (LearningControls || Failure.HasValue || index <= Save.checkpoint) return;
            Save.checkpoint = index;
            Persist();
        }

        void Persist() { if (persistent) Save.Store(Mode, Activity); }
    }
}
