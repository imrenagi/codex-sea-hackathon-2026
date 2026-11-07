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

        public readonly string Title, Explanation, Action, Citation, Url, Regulation;
        public Lesson(string title, string explanation, string action, string citation, string url = Law,
            string regulation = "UU 22/2009")
        {
            Title = title; Explanation = explanation; Action = action; Citation = citation; Url = url;
            Regulation = regulation;
        }

        public static readonly Lesson[] All =
        {
            new("Seat belts are not fastened", "The driver and front passenger must wear seat belts.",
                "Fasten both seat belts before selecting Start driving.", "Pasal 106 ayat (6)"),
            new("SNI helmets are not ready", "The motorcycle rider and passenger must wear SNI-approved helmets.",
                "Equip both people with SNI helmets. Fasten the straps to keep the helmets secure.", "Pasal 106 ayat (8)"),
            new("Motorcycle headlight is off", "Motorcycles must use their headlights during the day too.",
                "Turn on the headlight before setting off and keep it on.", "Pasal 107 ayat (2)"),
            new("Crossed the STOP line", "A STOP sign requires a complete stop before the stop line.",
                "Release the throttle, brake to 0 km/h, then check crossing traffic before moving.", "Pasal 106 ayat (4) huruf a–b"),
            new("Ran a red light", "Drivers must obey traffic signals.",
                "Stop before the white line and wait for green before continuing.", "Pasal 106 ayat (4) huruf c"),
            new("Main-road traffic is still passing", "When leaving a smaller road, yield to traffic on the main road.",
                "Hold the brake before the junction. Wait for traffic to pass and check that the road is clear.", "Pasal 113 ayat (1) huruf b"),
            new("Pedestrians are still crossing", "Pedestrians have priority at a crossing.",
                "Wait behind the crossing until pedestrians have cleared your path and no one is approaching it.", "Pasal 106 ayat (2) dan Pasal 131 ayat (2)"),
            new("Entered the oncoming lane", "Drive on the left side of the road.",
                "Return to the left of the center line and keep left on two-way roads.", "Pasal 108 ayat (1)"),
            new("Entered against a no-entry sign", "The no-entry sign prohibits access from this direction.",
                "Follow the straight arrows on the one-way road and avoid the branch with the red sign.", "Pasal 106 ayat (4) huruf a"),
            new("Changed lanes without indicating", "Check traffic and signal in the intended direction before changing lanes.",
                "Press E for the right indicator or Q for the left before changing lanes or moving sideways.", "Pasal 112 ayat (2)"),
            new("Lane-change gap is too small", "Check traffic ahead and behind before changing lanes.",
                "Wait for the car in your target lane to move away. Safe gaps in this simulation depend on vehicle speed.", "Pasal 112 ayat (2)"),
            new("Lane-change exercise is incomplete", "This exercise requires a safe move right and a return left before the road narrows.",
                "Signal right at the dashed line, then signal left and return before the markings end.", "Practice"),
            new("Exceeded the 30 km/h limit", "The posted speed limit applies on this road.",
                "Slow down before the 30 sign. Release the throttle or brake gently to stay below the limit.", "Pasal 106 ayat (4) huruf g dan Pasal 115 huruf a"),
            new("Answered the phone while driving", "Drivers must stay focused. Using a phone takes attention away from driving.",
                "Ignore calls while driving. Stop somewhere safe before using your phone.", "Pasal 106 ayat (1) dan Penjelasan"),
            new("Left the practice route", "Your vehicle left the road provided for this exercise.",
                "Follow the route markings and control your speed before steering.", "Practice"),
            new("Too close to another vehicle", "Leave enough space to brake if traffic ahead slows down.",
                "Keep your distance and brake early when approaching other vehicles.", "Safety advice"),
            new("Stopped in a no-stopping zone", "A no-stopping sign still applies when a passenger requests a pickup there.",
                "Continue to the pickup bay on the left beyond the restricted zone.", "Pasal 118 huruf a"),
            new("Pickup location is not safe yet", "In this mission, pick up the passenger after stopping in the marked bay.",
                "Signal left, enter the bay, stop beside the passenger, then press Pick up [F].", "Practice"),
            new("Passenger has not been picked up", "Pick up the passenger, then merge back into traffic safely.",
                "Stop in the pickup bay, press Pick up [F], then signal right and wait for a safe gap.", "Practice"),
            new("Turned without indicating", "Check traffic and signal in the intended direction before turning.",
                "Press Q before turning left or E before turning right. Yield to pedestrians until your turning path is clear.", "Pasal 112 ayat (1)"),
            new("Wrong lane for this direction", "Follow lane arrows and do not cross solid lane markings near a junction.",
                "Choose your lane before the solid line: left to turn left, middle to go straight, right to turn right.", "Pasal 106 ayat (4) huruf a–b"),
            new("Right-turn arrow is not green", "A green light for straight traffic does not permit a right turn at a junction with separate signals.",
                "Stop in the right lane. Wait for a green right arrow and check that pedestrians have cleared your turning path.", "Pasal 106 ayat (4) huruf c"),
            new("Car entered the motorcycle stop box", "The car crossed its stop line on red and entered the red box reserved for motorcycles.",
                "Cars stop before the red box. Motorcycles may use its front line but must still wait for the relevant signal.", "Pasal 106 ayat (4) huruf b–c"),
            new("Entered a closed railway crossing", "Stop when the warning sounds or the barriers begin to close, and give trains priority.",
                "Stop before the line. Wait for the train to pass, warning lights to go out, and barriers to open fully.", "Pasal 114"),
            new("Blocked an ambulance", "An ambulance carrying a patient has priority; this exercise uses its lights and siren.",
                "Locate the ambulance, signal left, and move into the left lane when safe. Do not block or follow it closely.", "Pasal 134 huruf b dan Pasal 135 ayat (1)"),
            new("Exceeded the 50 km/h limit", "The 50 km/h limit applies on this main road, including when an ambulance approaches.",
                "Ease off the throttle and give way calmly. Do not accelerate to get ahead of the ambulance.", "Pasal 106 ayat (4) huruf g dan Pasal 115 huruf a")
        };

        public bool HasSource => Citation.StartsWith("Pasal", StringComparison.Ordinal);
    }

    public sealed class RouteStage
    {
        public readonly string Title, Location, Instruction;
        public readonly float End, Heading;
        public readonly Vector2 Checkpoint;
        RouteStage(string title, string location, string instruction, float end)
        {
            Title = title; Location = location; Instruction = instruction; End = end;
            Checkpoint = new Vector2(LeftLane, end);
        }

        RouteStage(string title, string location, string instruction, Vector2 checkpoint, float heading = 0)
            : this(title, location, instruction, checkpoint.y)
        { Checkpoint = checkpoint; Heading = heading; }

        public static readonly RouteStage[] All =
        {
            new("Before you drive", "KENANGA GARAGE", "Check the driver and passenger safety equipment.", -12),
            new("Signs & signals", "JALAN KENANGA", "Stop completely at the STOP line before continuing.", 108),
            new("Right of way", "PASAR BUNGA", "Give pedestrians priority at the crossing.", 204),
            new("Directions & lanes", "JALAN MAWAR", "Follow the arrows. Signal and move into the right lane when safe.", 298),
            new("Speed & focus", "SCHOOL ZONE", "Slow down before the 30 km/h sign.", 382),
            new("Straight through the main road", "JALAN THAMRIN", "Choose the middle lane before the solid line. Wait for green and for pedestrians to clear.", new Vector2(-5.4f, 550)),
            new("Left turn & pedestrians", "THAMRIN – DIPONEGORO", "Choose the leftmost lane, press Q, then wait for green and a clear path through the crossing.", new Vector2(-70, 631), 270),
            new("Protected right turn", "DIPONEGORO – AHMAD YANI", "Choose the right lane before the solid line. Press E and wait for the green right arrow.", new Vector2(-181.8f, 720)),
            new("Railway crossing", "AHMAD YANI CROSSING", "Stop when the bell sounds. Wait for the train to pass and the barriers to open fully.", new Vector2(-185.4f, 850)),
            new("Yield to the ambulance", "JALAN RUMAH SAKIT", "Pass the minibus in the middle lane. When the siren sounds, signal and move left to give way.", new Vector2(-189, 1040))
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
        public bool CanPickup => IsMarket && !PassengerPickedUp && Speed < .28f
            && Position.x < -5 && Position.y >= Mission.PickupPoint - 5 && Position.y <= Mission.PickupPoint + 4;
        public int MistakeCount { get { int count = 0; foreach (int n in Save.mistakes) count += n; return count; } }
        public float StageEnd => Stage == Activity?.LastStage ? Activity.Finish : RouteStage.All[Mathf.Min(Stage, RouteStage.All.Length - 1)].End;
        public float CrossingZebra => IsSchool ? Mission.SchoolZebra : RouteStage.Zebra;
        public Vector2? PickupTraffic { get; private set; }
        public float Kph => Speed * 3.6f;
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
        public int LightPhase => lightTime < 0 || lightTime % 22 < 8 ? 0 : lightTime % 22 < 19 ? 2 : 1;

        float stopTime, lightTime, zebraTime, priorityTime, laneTime, wrongWay, overSpeed;
        float rightSignalAge, leftSignalAge, stoppedInRestriction, pickupTime, boardingTime;
        int lane;
        bool phoneResolved;
        readonly bool persistent;
        readonly RoadVehicleMotion motion = new();
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
                if (Speed > .28f || Position.x > -4.25f || Position.y < RouteStage.PhoneStart + 4)
                {
                    Fail(Rule.Phone);
                    return;
                }
            }
            PhoneRinging = false;
            phoneResolved = true;
        }

        public void Step(float dt, bool throttle, bool brake, float steer)
        {
            if (Stage == 0 || Finished || Failure.HasValue) return;
            ElapsedTime += dt;
            Throttle = throttle && !brake ? 1 : 0;
            // Boarding is a simulation state: the vehicle stays stopped while the passenger approaches.
            if (boardingTime > 0)
            {
                boardingTime = Mathf.Max(0, boardingTime - dt);
                Speed = 0;
                UpdateActors(dt);
                return;
            }

            if (Stage >= 5) { StepBoulevard(dt, throttle, brake, steer); return; }

            var oldPosition = Position;
            float oldHeading = Heading;
            var oldActors = (CrossTraffic, LaneTraffic, SpurTraffic, PickupTraffic);
            float oldFront = Front;
            float oldX = Position.x;
            float targetSpeed = Mode == VehicleMode.Car ? 16.1f : 16.7f;
            float speed = Speed, heading = Heading;
            if (UsesRouteSteering)
            {
                speed = brake ? Mathf.MoveTowards(speed, 0, 10.5f * dt)
                    : Mathf.MoveTowards(speed, throttle ? targetSpeed : 0, dt * (throttle ? (Mode == VehicleMode.Car ? 2.85f : 3.3f) : 1.7f));
                heading = steering.Step(Mode, speed, heading, steer, dt);
            }
            else motion.Step(dt, Mode, throttle, brake, steer, targetSpeed, ref speed, ref heading);
            Speed = speed; Heading = heading;
            Position += new Vector2(Mathf.Sin(Heading * Mathf.Deg2Rad), Mathf.Cos(Heading * Mathf.Deg2Rad)) * (Speed * dt);
            rightSignalAge = Indicator == 1 ? rightSignalAge + dt : 0;
            leftSignalAge = Indicator == -1 ? leftSignalAge + dt : 0;
            UpdateActors(dt);

            float z = Position.y;
            float x = Position.x;
            bool oneWay = z >= RouteStage.OneWayStart && z <= RouteStage.Merge;
            bool spur = z >= RouteStage.SpurStart && z <= RouteStage.SpurEnd;
            if (spur && x > 5.25f) Fail(Rule.NoEntry);
            bool bay = z >= RouteStage.PhoneStart + 4 && z <= RouteStage.PhoneEnd;
            bay |= IsMarket && z >= Mission.PickupStart && z <= Mission.PickupEnd;
            if (x < (bay ? -7.45f : -4.45f) || x > (spur ? 9.1f : 4.45f)) Fail(Rule.OffRoad);
            if (Mode == VehicleMode.Motorcycle && !Headlight && Kph >= 3) Fail(Rule.Headlight);
            if (!oneWay && x > .42f && Speed > 1)
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
                if (z >= Mission.NoStopStart && z <= Mission.NoStopEnd && Speed < .28f)
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
            if(Pedestrian.HasValue && PedestrianClearance.Touches(Pedestrian.Value,Position,Heading,Mode))
                Fail(Rule.Pedestrian);
            if (!Failure.HasValue && z >= StageEnd) Advance();
        }

        bool Crosses(float oldFront, float line) => oldFront < line && Front >= line;

        void CheckJunction(float oldFront, float center, bool signalled = false)
        {
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
                position, yaw, choice.Vehicle.HalfWidth, choice.Vehicle.HalfLength);

        void UpdateActors(float dt)
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
                float walkX = -5.6f + Mathf.Max(0, zebraTime - .65f) * 1.83f;
                Pedestrian = walkX < 5.6f ? new Vector2(walkX * PedestrianDirection, RouteStage.Zebra) : null;
                CrossDirection = Save.mistakes[(int)Rule.Junction] % 2 == 0 ? -1 : 1;
                CrossTraffic = CrossingCar(priorityTime, RouteStage.MainJunction, CrossDirection);
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
                float walkX = -5.6f + Mathf.Max(0, zebraTime - .65f) * 1.5f;
                Pedestrian = zebraTime >= 0 && walkX < 5.6f
                    ? new Vector2(walkX * PedestrianDirection, CrossingZebra) : null;
            }
            else if (IsSchool) Pedestrian = null;
            CrossTraffic = FollowEncounter(previousCross, CrossTraffic, Traffic.Cross, Vector2.right * CrossDirection, 6.2f, dt);
            LaneTraffic = FollowEncounter(previousLane, LaneTraffic, Traffic.Lane, Vector2.up, 5.1f, dt);
            SpurTraffic = FollowEncounter(previousSpur, SpurTraffic, Traffic.Spur, Vector2.down, 5, dt);
            PickupTraffic = FollowEncounter(previousPickup, PickupTraffic, Traffic.Pickup, Vector2.up, Save.variant % 2 == 0 ? 5.1f : 4.6f, dt);
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
