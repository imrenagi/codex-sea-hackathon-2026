using System;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class RoadBoundaryChecks
    {
        static void Require(bool ok, string message)
        { if (!ok) throw new InvalidOperationException("ROAD_BOUNDARY: " + message); }

        public static void Run()
        {
            foreach (var vehicle in VehicleCatalogue.All)
            {
                Check(vehicle.Id, vehicle.Mode, p => new RoadBoundary.Frame(new Vector2(0, p.y), Vector2.up, 3.25f),
                    new Vector2(0, 30));
                Check(vehicle.Id, vehicle.Mode, p =>
                {
                    const float slope = .38f;
                    var forward = new Vector2(slope, 1).normalized;
                    return new RoadBoundary.Frame(new Vector2(p.y * slope, p.y), forward, 3.25f * forward.y);
                }, new Vector2(11.4f, 30));
                // Curved road, both its inner and outer boundaries.
                Check(vehicle.Id, vehicle.Mode, p =>
                {
                    var radial = p.normalized;
                    return new RoadBoundary.Frame(radial * 35, new Vector2(-radial.y, radial.x), 3.25f);
                }, new Vector2(35, 0));
                // A bend changes its tangent within a vehicle's length.
                Check(vehicle.Id, vehicle.Mode, p =>
                {
                    float x = Mathf.Sin(p.y * .07f) * 3;
                    var forward = new Vector2(Mathf.Cos(p.y * .07f) * .21f, 1).normalized;
                    return new RoadBoundary.Frame(new Vector2(x, p.y), forward, 3.25f * forward.y);
                }, new Vector2(Mathf.Sin(2.1f) * 3, 30));
            }
            Debug.Log("ROAD_BOUNDARY_PASSED seven selections, straight/sloped/curved/bending roads, both edges, nose-on/glancing contact, containment, steering recovery and no stationary spin.");
        }

        static void Check(string id, VehicleMode mode, Func<Vector2, RoadBoundary.Frame> road, Vector2 start)
        {
            foreach (int side in new[] { -1, 1 })
            foreach (float approach in new[] { 35f, 85f, 90f })
            {
                var frame = road(start);
                float heading = frame.Heading + side * approach;
                var position = RoadBoundary.Fit(road, mode, start + frame.Right * side * 3, heading);
                Require(RoadBoundary.Contains(road, mode, position, heading), id + " edge start must fit");
                var original = position;
                var motion = new RoadVehicleMotion();
                float speed = 0;
                bool touched = false;
                // Hold into the edge, then steer back towards the open road.
                for (int i = 0; i < 220; i++)
                {
                    motion.Step(.02f, mode, true, false, i < 90 ? side : -side * .35f, 8.33f, ref speed, ref heading);
                    position = RoadBoundary.Move(road, mode, position, .02f, ref speed, ref heading, out bool contact);
                    touched |= contact;
                    Require(RoadBoundary.Contains(road, mode, position, heading), id + " footprint left road at " + i);
                }
                Require(touched && Vector2.Distance(position, original) > 2 && speed > .5f,
                    id + " stuck at edge: " + position + " speed=" + speed);

                speed = 0;
                var before = position; float yaw = heading;
                for (int i = 0; i < 30; i++)
                {
                    motion.Step(.02f, mode, false, true, side, 8.33f, ref speed, ref heading);
                    position = RoadBoundary.Move(road, mode, position, .02f, ref speed, ref heading, out _);
                }
                Require(Vector2.Distance(before, position) < .003f && Mathf.Abs(Mathf.DeltaAngle(yaw, heading)) < .001f,
                    id + " stationary curb contact changed pose");
            }
        }
    }
}
