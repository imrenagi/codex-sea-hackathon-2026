using System.Collections.Generic;
using UnityEngine;

namespace JalanKita
{
    // These exact positions drive both drawing and contact checks; no renderer or wall clock queries.
    public static class TrainingPedestrianPaths
    {
        readonly struct Path
        {
            public readonly int Group;
            public readonly Vector2 Start, End;
            public readonly float Radius, Facing;
            public Path(int group, float x, float z, float endX, float endZ, float height, float facing)
            { Group = group; Start = new(x, z); End = new(endX, endZ); Radius = .3f * height; Facing = facing; }
        }
        static readonly Path[] Paths = {
            new(0, -9.5f, 52, -9.5f, 52, 1, 90),
            new(0, -6.6f, 51.4f, -6.6f, 51.4f, 1, -90),
            new(0, -5.8f, 6, -5.8f, 6, 1, 90),
            new(0, 5.65f, 9, 5.65f, 25, .97f, 180),
            new(0, -5.7f, 43, -5.7f, 48, 1.03f, 0),
            new(0, 6.5f, 17, 6.5f, 17, 1, -90),
            new(1, -6.35f, 310, -6.35f, 319.5f, .79f, 0),
            new(1, -7.05f, 320.2f, -7.05f, 320.2f, .8f, 90),
            new(1, -6.5f, 321.5f, -6.5f, 321.5f, .76f, 65),
            new(1, -7.3f, 316, -7.3f, 316, 1, 90),
            new(1, -6.65f, 326.8f, -6.65f, 326.8f, .98f, 75),
            new(1, -7.4f, 327.1f, -7.4f, 327.1f, .77f, 85),
            new(1, 5.8f, 325.5f, 5.8f, 325.5f, .8f, -90),
            new(1, 6.65f, 326, 6.65f, 326, 1.02f, -90),
            new(1, 5.65f, 307, 5.65f, 319, 1, 0),
            new(2, -10.9f, 255, -10.9f, 255, 1.02f, 90),
            new(2, -10.9f, 273, -10.9f, 273, .98f, 90),
            new(2, -8.8f, 254.6f, -8.8f, 254.6f, .96f, -90),
            new(2, -8.15f, 250.8f, -8.15f, 260.3f, 1.01f, 0),
            new(2, -8.9f, 272.5f, -8.9f, 272.5f, 1.03f, -90),
            new(2, -8.15f, 268.4f, -8.15f, 277, .99f, 180),
            new(2, -8.8f, 256.1f, -8.8f, 256.1f, .76f, -75),
        };
        public static int Count => Paths.Length;
        public static Vector2 WaitingPassenger => new(-8.12f, Mission.PickupPoint);
        public static float Offset(int index) => index * 1.71f;
        public static void Sample(int index, float time, out Vector2 position, out float heading, out float walking)
        {
            var path = Paths[index];
            var delta = path.End - path.Start;
            float distance = delta.magnitude, pace = .7f + index % 4 * .11f;
            position = path.Start; heading = path.Facing; walking = 0;
            if (distance <= .1f) return;
            float duration = distance / pace;
            const float wait = 2.5f;
            float cycle = Mathf.Repeat(time + Offset(index), 2 * (duration + wait));
            bool outward = cycle < duration + wait;
            float part = outward ? cycle : cycle - duration - wait;
            float u = Mathf.Clamp01(part / duration);
            position = Vector2.Lerp(path.Start, path.End, outward ? u : 1 - u);
            heading = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg + (outward ? 0 : 180);
            if (part < duration) walking = pace / 1.35f;
            else heading += 180 * Mathf.SmoothStep(0, 1, (part - duration) / wait);
        }
        public static void Append(Journey run, float previousTime, float time, List<DrivePedestrian> result)
        {
            int group = run.Activity?.IsVillage == true ? 0 : run.IsSchool ? 1 : run.IsMarket ? 2 : -1;
            for (int i = 0; i < Paths.Length; i++)
            {
                if (Paths[i].Group != group) continue;
                Sample(i, previousTime, out var before, out _, out _);
                Sample(i, time, out var after, out _, out _);
                result.Add(new DrivePedestrian(before, after, Paths[i].Radius));
            }
            // Only the intentional boarding/attached passenger is exempt. A waiting person is physical.
            if (run.IsMarket && !run.PassengerPickedUp)
                result.Add(new DrivePedestrian(WaitingPassenger, WaitingPassenger));
        }
    }
}
