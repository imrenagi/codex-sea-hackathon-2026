using UnityEngine;

namespace JalanKita
{
    public static class DemoTutorialScenarios
    {
        // Both zones begin where Journey starts the corresponding actor simulation.
        // Braking from either edge leaves room before the zebra/yield line at top speed.
        public static readonly TutorialScenario PasarBunga = new TutorialScenario(
            id: "pasar-bunga-right-of-way", mapId: "training-route/pasar-bunga",
            titleKey: "tutorial.demo.title", initialInstructionKey: "tutorial.action.start",
            completionSummaryKey: "tutorial.demo.summary",
            createJourney: mode => new Journey(mode, new JourneySave { next = 2 }, persistent: false),
            isComplete: run => run.Stage > 2,
            steps: new[]
            {
            new TutorialStep(
                id: "pedestrian-crossing", zone: new Rect(-50, RouteStage.Zebra - 19, 100, 19),
                triggerWhen: run => run.Stage == 2 && run.Pedestrian.HasValue,
                ruleId: Rule.Pedestrian, action: TutorialAction.Brake,
                headingKey: "tutorial.pedestrian.heading", actionInstructionKey: "tutorial.action.brake",
                brakingInstructionKey: "tutorial.pedestrian.wait",
                waitInstructionKey: "tutorial.pedestrian.wait",
                completeWhen: run => !run.Pedestrian.HasValue,
                successMessageKey: "tutorial.pedestrian.success"),
            new TutorialStep(
                id: "main-road-junction", zone: new Rect(-50, RouteStage.YieldLine - 20, 100, 20),
                triggerWhen: run => run.Stage == 2 && run.CrossedZebra,
                ruleId: Rule.Junction, action: TutorialAction.Brake,
                headingKey: "tutorial.junction.heading", actionInstructionKey: "tutorial.action.brake",
                brakingInstructionKey: "tutorial.junction.stop", waitInstructionKey: "tutorial.junction.wait",
                completeWhen: run => !DrivingCoach.TrafficComing(run, RouteStage.MainJunction),
                successMessageKey: "tutorial.junction.success")
            });
    }
}
