using System.Collections.Generic;
using UnityEngine;

namespace JalanKita
{
    public sealed partial class Journey
    {
        public DriveSafetyState Safety { get; } = new();
        public float PostedLimitKph => Stage >= 5 ? 50 : Stage == 4 && Front >= RouteStage.SchoolLimit ? 30 : 0;
        readonly List<DrivePedestrian> safetyPedestrians = new(32);
        public IReadOnlyList<DrivePedestrian> PedestrianSnapshots => safetyPedestrians;
        readonly Vector2?[] safetyCrossingBefore = new Vector2?[CrossingPedestrianCount];
        Vector2? safetyBoulevardBefore;
        float safetyTimeBefore;
        bool safetyReverse;

        void BeginSafetyStep(bool reverse)
        {
            safetyReverse = reverse;
            safetyTimeBefore = ElapsedTime;
            safetyBoulevardBefore = BoulevardPedestrian;
            for (int i = 0; i < CrossingPedestrianCount; i++) safetyCrossingBefore[i] = crossingPedestrians[i];
        }

        void StepCommonSafety(float dt)
        {
            safetyPedestrians.Clear();
            for (int i = 0; i < CrossingPedestrianCount; i++)
                AddSafetyPedestrian(safetyCrossingBefore[i], crossingPedestrians[i]);
            AddSafetyPedestrian(safetyBoulevardBefore, BoulevardPedestrian);
            TrainingPedestrianPaths.Append(this, safetyTimeBefore, ElapsedTime, safetyPedestrians);
            bool contact = DriveSafetyState.PedestrianContact(motionFrom, Position, motionHeading, Heading, Mode, safetyPedestrians);
            Vector2 forward = Vector2.up, alternate = Vector2.zero;
            if (Stage >= 5 && Stage <= 7)
            {
                var local = Boulevard.Local(Stage, Position);
                var exit = Boulevard.Exit(Stage);
                float progress = Vector2.Dot(Position - Boulevard.Center(Stage), exit);
                if (local.y < -17) forward = Boulevard.Forward(Stage);
                else if (progress > 17) forward = exit;
                else { forward = Boulevard.Forward(Stage); alternate = exit; }
            }
            var failure = Safety.Step(dt, new DriveSafetySample {
                SignedSpeed = Speed, Heading = Heading, PostedLimitKph = PostedLimitKph,
                AllowedForward = forward, AlternateForward = alternate,
                DirectionKnown = !LearningControls, ReverseRequested = safetyReverse,
                PedestrianContact = !LearningControls && contact,
                VehicleContact = Failure == Rule.Collision, PriorityViolation = Failure == Rule.Junction
            });
            if (!LearningControls && failure.HasValue) Fail(failure.Value);
        }

        void AddSafetyPedestrian(Vector2? before, Vector2? after)
        {
            if (!before.HasValue && !after.HasValue) return;
            safetyPedestrians.Add(new DrivePedestrian(before ?? after.Value, after ?? before.Value));
        }
    }
}
