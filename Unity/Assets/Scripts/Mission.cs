namespace JalanKita
{
    public enum MissionKind { SkillTest, Scenario }

    // Missions select encounters from the shared district; their scores never replace the full journey.
    public sealed class Mission
    {
        public readonly string Id, Title, Brief, Focus, Duration;
        public readonly MissionKind Kind;
        public readonly int FirstStage, LastStage;
        public readonly float Finish;
        public bool IsVillage => Id == "village";
        public bool IsSchool => Id == "school";
        public bool IsMarket => Id == "market";

        Mission(string id, MissionKind kind, string title, string brief, string focus, string duration,
            int firstStage, int lastStage, float finish)
        {
            Id = id; Kind = kind; Title = title; Brief = brief; Focus = focus; Duration = duration;
            FirstStage = firstStage; LastStage = lastStage; Finish = finish;
        }

        public static readonly Mission[] Tests =
        {
            new("preparation", MissionKind.SkillTest, "Before you drive", "Prepare safety equipment for the driver and passenger.",
                "Vehicle safety equipment", "1 minute", 0, 0, -12),
            new("signals", MissionKind.SkillTest, "Signs & signals", "Navigate a STOP sign and a signal-controlled junction safely.",
                "STOP / Right of way / Signals", "1–2 minutes", 1, 1, 108),
            new("priority", MissionKind.SkillTest, "Right of way", "Navigate a pedestrian crossing and main-road traffic.",
                "Pedestrians / Main-road traffic", "1–2 minutes", 2, 2, 204),
            new("lanes", MissionKind.SkillTest, "Directions & lanes", "Move right, then return left before the road narrows.",
                "Road direction / Indicators / Safe gaps", "1–2 minutes", 3, 3, 298),
            new("focus", MissionKind.SkillTest, "Speed & focus", "Obey the 30 km/h limit and handle a phone call safely.",
                "Speed / Concentration", "1–2 minutes", 4, 4, 382)
        };

        public static readonly Mission[] Scenarios =
        {
            new("village", MissionKind.Scenario, "Leaving the neighborhood",
                "Drive with a passenger to the local shop. Watch for a STOP sign and crossing traffic as you leave the neighborhood.",
                "Preparation / STOP / Right of way", "1–2 minutes", 0, 1, 56),
            new("school", MissionKind.Scenario, "Passing the school",
                "Drive past a school at dismissal time. Someone will cross, then your phone will ring.",
                "Speed / Pedestrians / Focus", "1–2 minutes", 4, 4, 382),
            new("market", MissionKind.Scenario, "Market pickup",
                "Your passenger requests a pickup in a no-stopping zone. Follow the one-way road to the pickup bay, then merge back safely.",
                "Road direction / Safe stopping / Indicators & gaps", "2–3 minutes", 3, 3, 298)
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
