using UnityEngine;

namespace JalanKita
{
    public static class DemoTutorialScenarios
    {
        // Zones cover the approach; speed determines when the cue actually appears.
        // Actor history remains valid after a slow driver has let the hazard clear.
        public static readonly TutorialScenario PasarBunga = new TutorialScenario(
            id: "pasar-bunga-right-of-way", mapId: "training-route/pasar-bunga",
            titleKey: "tutorial.demo.title", initialInstructionKey: "tutorial.action.start",
            completionSummaryKey: "tutorial.demo.summary",
            createJourney: mode => new Journey(mode, new JourneySave { next = 2 }, persistent: false),
            isComplete: run => run.Stage > 2,
            steps: new[]
            {
            new TutorialStep(
                id: "pedestrian-crossing", zone: new Rect(-50, RouteStage.Zebra - 22, 100, 22),
                triggerWhen: run => run.Stage == 2 && run.CrossingPedestrian(0).HasValue
                    && NearBrakePoint(run, RouteStage.Zebra - 2),
                ruleId: Rule.Pedestrian, action: TutorialAction.Brake,
                headingKey: "tutorial.pedestrian.heading", actionInstructionKey: "tutorial.action.brake",
                brakingInstructionKey: "tutorial.pedestrian.wait",
                waitInstructionKey: "tutorial.pedestrian.wait",
                completeWhen: run => run.PedestriansHaveCleared,
                successMessageKey: "tutorial.pedestrian.success"),
            new TutorialStep(
                id: "main-road-junction", zone: new Rect(-50, RouteStage.YieldLine - 22, 100, 22),
                triggerWhen: run => run.Stage == 2 && run.CrossedZebra && run.MainRoadEncounterStarted
                    && NearBrakePoint(run, RouteStage.YieldLine),
                ruleId: Rule.Junction, action: TutorialAction.Brake,
                headingKey: "tutorial.junction.heading", actionInstructionKey: "tutorial.action.brake",
                brakingInstructionKey: "tutorial.junction.stop", waitInstructionKey: "tutorial.junction.wait",
                completeWhen: run => !DrivingCoach.TrafficComing(run, RouteStage.MainJunction),
                successMessageKey: "tutorial.junction.success")
            });

        static bool NearBrakePoint(Journey run, float stopLine)
        {
            // This route brakes at 10.5 m/s². The prompt freezes the world; after
            // acknowledgement braking is automatic, so allow a short simulation-step
            // buffer plus 1.25 m of stopped clearance, not a human reaction delay.
            float stoppingDistance = run.Speed * run.Speed / (2 * 10.5f);
            float cueDistance = Mathf.Max(5.5f, stoppingDistance + run.Speed * .1f + 1.25f);
            float remaining = stopLine - run.Front;
            return remaining >= 0 && remaining <= cueDistance;
        }
    }
}
