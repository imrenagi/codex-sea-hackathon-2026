using System;
using UnityEngine;

namespace JalanKita
{
    // One deterministic position source for both visible people and simulation
    // contacts. Call Fill methods once per simulation step with the same clocks
    // used to draw; Present never advances a person's simulation clock.
    public static class PedestrianPaths
    {
        static readonly Vector2[] SopoSpots =
        {
            new(-4.7f,34), new(4.8f,87), new(-4.9f,164), new(4.6f,219)
        };
        public const int SopoCount = 4;
        public static Vector2 SopoPosition(JaksaStreet street, int index)
        {
            var spot = SopoSpots[index];
            return new Vector2(street.RoadX(spot.y) + spot.x, spot.y);
        }
        public static float SopoHeading(int index) => SopoSpots[index].x < 0 ? 90 : 270;
        public static void FillSopo(JaksaStreet street, DrivePedestrian[] buffer)
        {
            if (buffer.Length != SopoCount) throw new ArgumentException("Sopo pedestrian buffer must contain four people.");
            for (int i = 0; i < buffer.Length; i++)
            { var p = SopoPosition(street, i); buffer[i] = new DrivePedestrian(p, p); }
        }

        public static float HiRingDistance(LandmarkRoute route, int index, int count, float time)
            => index * route.Length / count + Mathf.Sin(time * .075f + index) * 7;
        public static Vector2 HiRingPosition(LandmarkRoute route, int index, int count, float time)
            => route.At(HiRingDistance(route, index, count, time), index % 3 == 0 ? 9.6f : -9.6f);
        public static float HiRingHeading(LandmarkRoute route, int index, int count, float time)
            => route.Heading(HiRingDistance(route, index, count, time)) + (Mathf.Cos(time * .075f + index) < 0 ? 180 : 0);
        public static void FillHiRing(LandmarkRoute route, DrivePedestrian[] buffer, float previousTime, float currentTime)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = new DrivePedestrian(HiRingPosition(route, i, buffer.Length, previousTime),
                    HiRingPosition(route, i, buffer.Length, currentTime), i % 7 == 0 ? .24f : .3f);
        }

        public static float HiAccessDistance(int index, float time)
            => 45 + (index / 4) * 54 + Mathf.Sin(time * .08f + index) * 7;
        public static Vector2 HiAccessPosition(LandmarkRoadNetwork network, int index, float time)
        {
            var branch = network.Branches[index % 4];
            return branch.Route.At(HiAccessDistance(index, time), -branch.Width * .5f - 1.2f);
        }
        public static float HiAccessHeading(LandmarkRoadNetwork network, int index, float time)
            => network.Branches[index % 4].Route.Heading(HiAccessDistance(index, time)) + (Mathf.Cos(time * .08f + index) < 0 ? 180 : 0);
        public static void FillHiAccess(LandmarkRoadNetwork network, DrivePedestrian[] buffer, float previousTime, float currentTime)
        {
            if (buffer.Length > 0 && network.Branches.Length != 4)
                throw new ArgumentException("HI access pedestrians require the four mapped branches.");
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = new DrivePedestrian(HiAccessPosition(network, i, previousTime), HiAccessPosition(network, i, currentTime));
        }
    }
}
