using System;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class DriveSafetyChecks
    {
        static void Require(bool ok, string message)
        { if (!ok) throw new InvalidOperationException("DRIVE_SAFETY: " + message); }
        public static void Run()
        {
            var state = new DriveSafetyState();
            var sample = new DriveSafetySample { SignedSpeed = 5, Heading = 180, AllowedForward = Vector2.up, DirectionKnown = true };
            for (int i = 0; i < 20; i++) state.Step(.02f, sample);
            Require(!state.Failure.HasValue, "Brief turn has a transition grace");
            float duration = state.WrongWayDuration;
            state.Step(0, sample);
            Require(state.WrongWayDuration == duration, "Pause freezes rule timers");
            for (int i = 0; i < 20; i++) state.Step(.02f, sample);
            Require(state.Failure == Rule.KeepLeft, "Sustained opposing travel fails");
            state.Reset(); sample.Heading = 90; sample.AllowedForward = Vector2.right;
            for (int i = 0; i < 60; i++) state.Step(.02f, sample);
            Require(!state.Failure.HasValue, "Curved local road tangent is lawful");
            sample.Heading = 180; sample.AllowedForward = Vector2.up; sample.AlternateForward = Vector2.down;
            for (int i = 0; i < 60; i++) state.Step(.02f, sample);
            Require(!state.Failure.HasValue, "A legal connector direction is accepted");
            state.Reset(); sample.AlternateForward = Vector2.zero; sample.Heading = 0;
            sample.SignedSpeed = -2.2f; sample.ReverseRequested = true;
            for (int i = 0; i < 60; i++) state.Step(.02f, sample);
            sample.ReverseRequested = false;
            for (int i = 0; i < 40; i++) state.Step(.02f, sample);
            Require(!state.Failure.HasValue, "Deliberate reverse and braking out of reverse are allowed");
            sample.VehicleContact = true; state.Step(.02f, sample);
            Require(state.Failure == Rule.Collision, "Reverse does not bypass contact");
            state.Reset(); sample.VehicleContact = false; sample.PriorityViolation = true; state.Step(.02f, sample);
            Require(state.Failure == Rule.Junction, "Confirmed unsafe priority entry fails");
            state.Reset(); sample.PriorityViolation = false; sample.PedestrianContact = true; state.Step(.02f, sample);
            Require(state.Failure == Rule.Pedestrian, "Confirmed pedestrian contact fails");
            state.Reset(); sample.PedestrianContact = false; sample.SignedSpeed = 40 / 3.6f; sample.PostedLimitKph = 30;
            for (int i = 0; i < 30; i++) state.Step(.02f, sample);
            Require(state.SpeedWarning && !state.Failure.HasValue, "Overspeed warns without failure");
            sample.SignedSpeed = 30 / 3.6f; state.Step(.02f, sample);
            Require(state.SpeedWarning, "Warning hysteresis avoids threshold flicker");
            sample.SignedSpeed = 29 / 3.6f; state.Step(.02f, sample);
            Require(!state.SpeedWarning, "Returning below limit clears warning");
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                var crossing = new[] { new DrivePedestrian(new Vector2(-3, 0), new Vector2(3, 0)) };
                Require(DriveSafetyState.PedestrianContact(Vector2.zero, Vector2.zero, 0, 0, mode, crossing), "Moving pedestrian swept across stationary vehicle " + mode);
                var clear = new[] { new DrivePedestrian(new Vector2(3, 5), new Vector2(3, 6)) };
                Require(!DriveSafetyState.PedestrianContact(Vector2.zero, Vector2.up, 0, 0, mode, clear), "Unrelated sidewalk walker remains clear " + mode);
                var rear = new[] { new DrivePedestrian(new Vector2(0, -3), new Vector2(0, -3)) };
                Require(DriveSafetyState.PedestrianContact(Vector2.zero, new Vector2(0, -4), 0, 0, mode, rear), "Reverse rear sweep catches pedestrian " + mode);
            }
            Debug.Log("DRIVE_SAFETY_CHECKS_PASSED direction, curves, transition, reverse, pause, contacts, priority and warning-only speed");
        }
    }
}
