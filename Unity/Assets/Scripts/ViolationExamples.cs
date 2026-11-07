using System;
using UnityEngine;

namespace JalanKita
{
    public interface IViolationExampleProvider
    {
        bool TryGetExample(Journey failed, out ViolationExample example);
    }

    public sealed class ViolationExample
    {
        public readonly string SectionId;
        public readonly Rule RuleId;
        public readonly Func<Journey, Journey> CreateSnapshot;
        public readonly Action<Journey, float> Demonstrate;
        public readonly Func<Journey, bool> IsComplete;
        public Lesson RuleReference => Lesson.All[(int)RuleId];

        public ViolationExample(string sectionId, Rule ruleId, Func<Journey, Journey> createSnapshot,
            Action<Journey, float> demonstrate, Func<Journey, bool> isComplete)
        {
            SectionId = sectionId; RuleId = ruleId; CreateSnapshot = createSnapshot;
            Demonstrate = demonstrate; IsComplete = isComplete;
        }
    }

    // Retain the exact failed journey as return/retry context. Only the detached,
    // nonpersistent snapshot is simulated; watching never edits the learner's save.
    public sealed class ViolationExampleSession
    {
        public Journey FailedJourney { get; }
        public Journey Preview { get; }
        public ViolationExample Definition { get; }
        public ViolationExampleSession(Journey failed, ViolationExample definition)
        {
            FailedJourney = failed; Definition = definition;
            Preview = definition.CreateSnapshot(failed);
        }
        public void Step(float dt) => Definition.Demonstrate(Preview, dt);
        public bool Finished => Preview.Failure.HasValue || Definition.IsComplete(Preview);
    }

    public sealed class RouteViolationExamples : IViolationExampleProvider
    {
        public static readonly RouteViolationExamples Instance = new RouteViolationExamples();

        public bool TryGetExample(Journey failed, out ViolationExample example)
        {
            example = null;
            if (!failed.Failure.HasValue) return false;
            var rule = failed.Failure.Value;
            if (rule == Rule.Pedestrian && (failed.Stage == 2 || failed.Stage == 4 && failed.IsSchool))
            {
                float crossing = failed.CrossingZebra;
                example = Section("pedestrian-crossing", rule, crossing - 19, crossing + 4);
            }
            else if (rule == Rule.Junction && failed.Stage == 2)
                example = Section("main-road-junction", rule, RouteStage.YieldLine - 20, RouteStage.MainJunction + 6);
            else if ((rule == Rule.Stop || rule == Rule.Junction) && failed.Stage == 1)
                example = Section("stop-junction", rule, RouteStage.StopLine - 22, RouteStage.StopJunction + 6);
            else if (rule == Rule.Signal && failed.Stage == 1)
                example = Section("traffic-light-junction", rule, RouteStage.LightLine - 22, RouteStage.LightJunction + 6);
            return example != null;
        }

        static ViolationExample Section(string id, Rule rule, float start, float end) => new ViolationExample(
            id, rule, source => source.CreateSectionExample(new Vector2(RouteStage.LeftLane, start)),
            DrivingCoach.Step, run => run.Front > end);
    }
}
