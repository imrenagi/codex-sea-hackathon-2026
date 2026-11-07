using System;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class SteeringChecks
    {
        public static void Run()
        {
            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                var right = new VehicleSteering();
                var left = new VehicleSteering();
                float a = 0, b = 0;
                for (int i = 0; i < 50; i++)
                {
                    a = right.Step(mode, 10, a, 1, .02f);
                    b = left.Step(mode, 10, b, -1, .02f);
                }
                Require(Mathf.Abs(Mathf.DeltaAngle(a, -b)) < .001f, "Symmetric steering");
                float expected = mode == VehicleMode.Car ? 1.305694f : 1.847648f;
                Require(Mathf.Abs(right.YawRate - expected) < .002f, "Ujian SIM reference yaw at 10 m/s");
                for (int i = 0; i < 12; i++) a = right.Step(mode, 10, a, 0, .02f);
                float held = a;
                for (int i = 0; i < 50; i++) a = right.Step(mode, 10, a, 0, .02f);
                Require(Mathf.Abs(Mathf.DeltaAngle(held,a)) < .001f && Mathf.Abs(Mathf.DeltaAngle(a,0)) > 10,
                    "Release centers the wheels without snapping the vehicle north");
                a = right.Step(mode, 0, a, 1, 1);
                Require(right.YawRate == 0 && a == held, "Stationary steering must not rotate the body");
                right.Reset();
                right.Step(mode, 10, 0, 1, .02f);
                Require(Mathf.Abs(right.Input-.06f) < .0001f, "Source keyboard ramp");
            }
        }

        static void Require(bool valid, string reason)
        { if (!valid) throw new InvalidOperationException(reason); }
    }
}
