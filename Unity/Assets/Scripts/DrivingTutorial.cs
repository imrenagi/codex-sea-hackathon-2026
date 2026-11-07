using System;
using System.Collections.Generic;
using UnityEngine;

namespace JalanKita
{
    public enum TutorialAction { Brake, SignalLeft, SignalRight }
    public enum TutorialSimulation { FreezeWorld, BrakeVehicle }
    public enum TutorialPhase { AwaitAction, Waiting, Ready }

    // Scenario data owns where/when a lesson applies. Rule references use the existing
    // catalog, including its regulation, citation and source URL; no legal text is copied.
    public sealed class TutorialStep
    {
        public readonly string Id, HeadingKey, ActionInstructionKey, BrakingInstructionKey, WaitInstructionKey, SuccessMessageKey;
        public readonly Rect Zone;
        public readonly Func<Journey, bool> TriggerWhen, CompleteWhen;
        public readonly Rule RuleId;
        public readonly TutorialAction Action;
        public readonly TutorialSimulation PromptSimulation, WaitSimulation;
        public Lesson RuleReference => Lesson.All[(int)RuleId];
        public KeyCode ActionKey => Action switch
        {
            TutorialAction.SignalLeft => KeyCode.Q,
            TutorialAction.SignalRight => KeyCode.E,
            _ => KeyCode.S
        };

        public TutorialStep(string id, Rect zone, Func<Journey, bool> triggerWhen,
            Rule ruleId, TutorialAction action, string headingKey, string actionInstructionKey,
            string brakingInstructionKey, string waitInstructionKey, Func<Journey, bool> completeWhen,
            string successMessageKey, TutorialSimulation promptSimulation = TutorialSimulation.FreezeWorld,
            TutorialSimulation waitSimulation = TutorialSimulation.BrakeVehicle)
        {
            Id = id; Zone = zone; TriggerWhen = triggerWhen; RuleId = ruleId; Action = action;
            HeadingKey = headingKey; ActionInstructionKey = actionInstructionKey; BrakingInstructionKey = brakingInstructionKey;
            WaitInstructionKey = waitInstructionKey; CompleteWhen = completeWhen; SuccessMessageKey = successMessageKey;
            PromptSimulation = promptSimulation; WaitSimulation = waitSimulation;
        }
    }

    public sealed class TutorialScenario
    {
        public readonly string Id, MapId, TitleKey, InitialInstructionKey, CompletionSummaryKey;
        public readonly Func<VehicleMode, Journey> CreateJourney;
        public readonly Func<Journey, bool> IsComplete;
        public readonly IReadOnlyList<TutorialStep> Steps;
        public readonly IViolationExampleProvider Examples;

        public TutorialScenario(string id, string mapId, string titleKey, string initialInstructionKey,
            string completionSummaryKey, Func<VehicleMode, Journey> createJourney,
            Func<Journey, bool> isComplete, IViolationExampleProvider examples = null, params TutorialStep[] steps)
        {
            Id = id; MapId = mapId; TitleKey = titleKey; InitialInstructionKey = initialInstructionKey;
            CompletionSummaryKey = completionSummaryKey; CreateJourney = createJourney; IsComplete = isComplete;
            Examples = examples ?? RouteViolationExamples.Instance;
            Steps = Array.AsReadOnly((TutorialStep[])steps.Clone());
        }
    }

    // One ordered step at a time. This runner never reads keyboard or UI state.
    public sealed class TutorialRunner
    {
        public TutorialScenario Scenario { get; }
        public TutorialStep Active { get; private set; }
        public TutorialPhase Phase { get; private set; }
        public int CompletedCount { get; private set; }
        public bool InitialInstructionDismissed { get; private set; }
        bool inputReleased;
        public bool IsActive => Active != null;
        public TutorialSimulation Simulation => Phase == TutorialPhase.AwaitAction
            ? Active.PromptSimulation : Active.WaitSimulation;

        public TutorialRunner(TutorialScenario scenario) { Scenario = scenario; }

        public bool TryBegin(Journey run)
        {
            if (IsActive || CompletedCount >= Scenario.Steps.Count) return false;
            var next = Scenario.Steps[CompletedCount];
            if (!next.Zone.Contains(run.Position) || !next.TriggerWhen(run)) return false;
            Active = next;
            Phase = TutorialPhase.AwaitAction;
            inputReleased = false;
            run.LearningControls = true;
            return true;
        }

        public void ResetInput() => inputReleased = false;

        public void MarkDrivingStarted() => InitialInstructionDismissed = true;

        bool FreshPress(bool held, bool pressed)
        {
            if (!held) { inputReleased = true; return false; }
            if (!inputReleased || !pressed) return false;
            inputReleased = false;
            return true;
        }

        public bool TryAction(Journey run, bool held, bool pressed)
        {
            if (!IsActive || Phase != TutorialPhase.AwaitAction || !FreshPress(held, pressed)) return false;
            AcceptAction(run);
            return true;
        }

        // Dismissing a prompt applies its safe action, then keeps the waiting guard.
        public void AcceptAction(Journey run)
        {
            if (!IsActive || Phase != TutorialPhase.AwaitAction) return;
            if (Active.Action == TutorialAction.SignalLeft && run.Indicator != -1) run.ToggleIndicator(-1);
            if (Active.Action == TutorialAction.SignalRight && run.Indicator != 1) run.ToggleIndicator(1);
            Phase = TutorialPhase.Waiting;
            inputReleased = false;
        }

        public void UpdateReadiness(Journey run)
        {
            if (!IsActive || Phase == TutorialPhase.AwaitAction) return;
            bool ready = run.Speed == 0 && Active.CompleteWhen(run);
            var next = ready ? TutorialPhase.Ready : TutorialPhase.Waiting;
            if (Phase != next) inputReleased = false;
            Phase = next;
        }

        public TutorialStep TryContinue(Journey run, bool held, bool pressed)
        {
            if (!IsActive || Phase != TutorialPhase.Ready || !FreshPress(held, pressed)) return null;
            var completed = Active;
            Active = null;
            CompletedCount++;
            run.LearningControls = false;
            return completed;
        }

        public string Instruction(Journey run) => GameText.Get(Phase == TutorialPhase.AwaitAction ? Active.ActionInstructionKey
            : Phase == TutorialPhase.Ready ? "tutorial.action.continue"
            : run.Speed > 0 ? Active.BrakingInstructionKey : Active.WaitInstructionKey);
    }
}
