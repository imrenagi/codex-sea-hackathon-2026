using System.Collections.Generic;
using UnityEngine;

namespace JalanKita
{
    public struct DriveSafetySample
    {
        public float SignedSpeed, Heading, PostedLimitKph;
        public Vector2 AllowedForward, AlternateForward;
        public bool DirectionKnown, ReverseRequested;
        // Confirmed contacts/entry violations supplied by the map. Proximity,
        // safe following speed and a curb correction are not collision facts.
        public bool VehicleContact, PedestrianContact, PriorityViolation;
    }

    public readonly struct DrivePedestrian
    {
        public readonly Vector2 PreviousPosition, Position;
        public readonly float Radius;
        public DrivePedestrian(Vector2 previousPosition, Vector2 position, float radius = .3f)
        { PreviousPosition = previousPosition; Position = position; Radius = Mathf.Max(.05f, radius); }
    }

    // Pure per-attempt rule state: no scene, UI, random seed or saved-progress ownership.
    public sealed class DriveSafetyState
    {
        public Rule? Failure { get; private set; }
        public float WrongWayDuration { get; private set; }
        public float OverspeedDuration { get; private set; }
        public bool SpeedWarning { get; private set; }
        bool reversing;

        public void Reset()
        {
            Failure = null;
            WrongWayDuration = OverspeedDuration = 0;
            SpeedWarning = reversing = false;
        }

        public Rule? Step(float dt, DriveSafetySample sample)
        {
            if (dt <= 0 || Failure.HasValue) return Failure;
            // Legal speed feedback is independent of both physical avoidance and
            // failure. Overspeed is deliberately warning-only.
            float kph = Mathf.Abs(sample.SignedSpeed) * 3.6f;
            if (sample.PostedLimitKph <= 0)
            { SpeedWarning = false; OverspeedDuration = 0; }
            else if (kph > sample.PostedLimitKph + .75f)
            {
                OverspeedDuration += dt;
                if (OverspeedDuration >= .35f) SpeedWarning = true;
            }
            else
            {
                OverspeedDuration = 0;
                if (kph < sample.PostedLimitKph - .5f) SpeedWarning = false;
            }

            if (sample.PedestrianContact) return Failure = Rule.Pedestrian;
            if (sample.PriorityViolation) return Failure = Rule.Junction;
            if (sample.VehicleContact) return Failure = Rule.Collision;

            if (sample.ReverseRequested && sample.SignedSpeed <= 0) reversing = true;
            if (sample.SignedSpeed > .05f) reversing = false;
            // Retain the exemption while a deliberately requested reverse coasts
            // to rest or W brakes it. Contact checks above remain active throughout.
            bool reverseManeuver = reversing && sample.SignedSpeed <= 0 && sample.SignedSpeed >= -2.35f;
            if (!sample.DirectionKnown || sample.AllowedForward.sqrMagnitude < .01f
                || Mathf.Abs(sample.SignedSpeed) < 1 || reverseManeuver)
            { WrongWayDuration = 0; return null; }
            float angle = sample.Heading * Mathf.Deg2Rad;
            var travel = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * Mathf.Sign(sample.SignedSpeed);
            bool opposed = Vector2.Dot(travel, sample.AllowedForward.normalized) < -.25f;
            if (sample.AlternateForward.sqrMagnitude >= .01f)
                opposed &= Vector2.Dot(travel, sample.AlternateForward.normalized) < -.25f;
            WrongWayDuration = opposed ? WrongWayDuration + dt : 0;
            if (WrongWayDuration >= .65f) Failure = Rule.KeepLeft;
            return Failure;
        }

        public static bool PedestrianContact(Vector2 from, Vector2 to, float fromHeading, float toHeading,
            VehicleMode mode, IReadOnlyList<DrivePedestrian> pedestrians)
        {
            if (pedestrians == null) return false;
            foreach (var person in pedestrians)
                if (TrafficFootprint.Sweeps(from, to, fromHeading, toHeading,
                    TrafficFootprint.PlayerHalfWidth(mode), TrafficFootprint.PlayerHalfLength(mode),
                    person.PreviousPosition, person.Position, 0, 0, person.Radius, person.Radius, 0)) return true;
            return false;
        }
    }
}
