using System;
using UnityEngine;

namespace JalanKita
{
    // A transient HI exercise. Driving, collision response and all saved journeys
    // remain owned by their existing controllers.
    public sealed class HiRoundaboutObjective
    {
        public enum Phase { Approach, Yield, Circulate, Exit, Complete, Failed }
        readonly LandmarkDrive drive;
        readonly LandmarkRoadNetwork network;
        readonly LandmarkRoute entry, exit, ring;
        readonly float entryGate, mergeS, exitRingS, requiredRingTravel;
        Vector2 previousPosition;
        float previousHeading, previousRingS, ringTravel;
        bool previousConflict;
        public Phase Stage { get; private set; }
        public bool Complete => Stage == Phase.Complete;
        public bool Failed => Stage == Phase.Failed;
        public bool Conflict { get; private set; }
        public bool Yielded { get; private set; }
        public string FailureReason { get; private set; }
        public string Title => Stage switch
        {
            Phase.Approach => "Dekati Bundaran HI",
            Phase.Yield => "Beri jalan sebelum masuk",
            Phase.Circulate => "Ikuti bundaran menuju Thamrin",
            Phase.Exit => "Keluar menuju Thamrin",
            Phase.Complete => "Perjalanan HI selesai",
            _ => "Coba lagi dari pendekatan"
        };
        public string Instruction => Failed ? FailureReason : Stage switch
        {
            Phase.Approach => "Ikuti lajur kiri dari Sudirman. Kurangi kecepatan menjelang bundaran.",
            Phase.Yield => Conflict ? "Ada arus di bundaran. Tunggu sebelum masuk; lihat kanan dengan X."
                : "Celah terbuka. Periksa kanan lalu masuk mengikuti arah bundaran.",
            Phase.Circulate => "Ikuti arus searah jarum jam. Bersiap keluar ke Thamrin; gunakan sein kiri.",
            Phase.Exit => "Lanjutkan di lajur keluar Thamrin hingga tujuan tercapai.",
            _ => "Pendekatan, masuk bundaran, dan keluar menuju Thamrin selesai."
        };

        public HiRoundaboutObjective(LandmarkDrive drive)
        {
            this.drive = drive ?? throw new ArgumentNullException(nameof(drive));
            network = drive.Environment.Network;
            if (!network.HasAccess || network.Branches.Length < 2)
                throw new ArgumentException("HI objective requires the connected HI road network.");
            entry = network.Branches[0].Route;
            exit = network.Branches[1].Route;
            ring = network.Ring;
            // Locate the actual outer edge of the circulating carriageway instead
            // of treating the approach's overlapping final knot as a stop line.
            entryGate = entry.Length - 12;
            for (float s = 16; s < entry.Length; s += .5f)
            {
                ring.Nearest(entry.At(s), out float lateral);
                if (Mathf.Abs(lateral) > network.RingWidth * .5f) continue;
                entryGate = s;
                break;
            }
            mergeS = ring.Nearest(entry.At(entryGate), out _);
            exitRingS = ring.Nearest(exit.At(0), out _);
            requiredRingTravel = Mathf.Max(8, Mathf.Repeat(exitRingS - mergeS, ring.Length) - 14);
            Reset();
        }

        public void Reset()
        {
            Stage = Phase.Approach;
            FailureReason = null;
            Yielded = false;
            ringTravel = 0;
            previousPosition = drive.Position;
            previousHeading = drive.Heading;
            previousRingS = ring.Nearest(drive.Position, out _);
            Conflict = previousConflict = HasConflict();
        }

        public void Step(float dt)
        {
            if (dt <= 0 || Complete || Failed) return;
            var position = drive.Position;
            float moved = Vector2.Distance(previousPosition, position);
            Conflict = HasConflict();
            float entryS = entry.Nearest(position, out float entryLane);
            float ringS = ring.Nearest(position, out float ringLane);
            bool nearEntry = Mathf.Abs(entryLane) <= network.Branches[0].Width * .5f + 1;
            if (Stage == Phase.Approach && nearEntry && entryS >= entryGate - 38)
                Stage = Phase.Yield;
            if (Stage == Phase.Yield)
            {
                if (Conflict && Mathf.Abs(drive.Speed) < .6f && entryS < entryGate) Yielded = true;
                if (Crossed(entry, entryGate, network.Branches[0].Width, true))
                {
                    if (Conflict || previousConflict)
                        Fail("Masuk terlalu dekat dengan arus bundaran. Tunggu celah aman, lalu coba lagi.");
                    else
                    {
                        Stage = Phase.Circulate;
                        previousRingS = ringS;
                        ringTravel = 0;
                    }
                }
            }
            else if (Stage == Phase.Circulate)
            {
                float delta = Mathf.Repeat(ringS - previousRingS + ring.Length * .5f, ring.Length) - ring.Length * .5f;
                if (Mathf.Abs(ringLane) <= network.RingWidth * .5f + 2 && Mathf.Abs(delta) <= moved + 2)
                    ringTravel += delta; // Reversing removes progress; oscillation cannot farm a lap.
                if (Crossed(exit, 22, network.Branches[1].Width, false))
                {
                    if (ringTravel < requiredRingTravel)
                        Fail("Ikuti bundaran searah jarum jam sebelum keluar menuju Thamrin.");
                    else Stage = Phase.Exit;
                }
            }
            else if (Stage == Phase.Exit && Crossed(exit, 70, network.Branches[1].Width, false))
                Stage = Phase.Complete;
            previousPosition = position;
            previousHeading = drive.Heading;
            previousRingS = ringS;
            previousConflict = Conflict;
        }

        bool Crossed(LandmarkRoute route, float s, float width, bool nose)
        {
            var center = route.At(s);
            float angle = route.Heading(s) * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            var right = new Vector2(forward.y, -forward.x);
            Vector2 Point(Vector2 p, float heading) => nose ? p +
                new Vector2(Mathf.Sin(heading * Mathf.Deg2Rad), Mathf.Cos(heading * Mathf.Deg2Rad))
                * TrafficFootprint.PlayerHalfLength(drive.Mode) : p;
            var from = Point(previousPosition, previousHeading);
            var to = Point(drive.Position, drive.Heading);
            float a = Vector2.Dot(from - center, forward), b = Vector2.Dot(to - center, forward);
            if (a >= 0 || b < 0 || drive.Speed <= .05f || Vector2.Dot(drive.Position - previousPosition, forward) <= 0) return false;
            var crossing = Vector2.Lerp(from, to, -a / (b - a));
            return Mathf.Abs(Vector2.Dot(crossing - center, right)) <= width * .5f;
        }

        bool HasConflict()
        {
            if (!drive.TrafficEnabled) return false;
            bool ConflictAt(Vector2 p, float speed, float halfLength)
            {
                float s = ring.Nearest(p, out float lateral);
                // The approach joins the outer lane. Inner-lane traffic whose
                // path does not meet this merge is not an invented obstruction.
                if (Mathf.Abs(lateral + 4.9f) > 3.6f) return false;
                float ahead = Mathf.Repeat(mergeS - s, ring.Length);
                float behind = Mathf.Repeat(s - mergeS, ring.Length);
                float body = halfLength + TrafficFootprint.PlayerHalfLength(drive.Mode);
                return ahead < Mathf.Max(12, Mathf.Max(0, speed) * 3.5f) + body || behind < body + 3;
            }
            foreach (var actor in drive.Traffic)
                if (ConflictAt(actor.Position, actor.Speed, actor.Choice.Vehicle.HalfLength)) return true;
            foreach (var actor in drive.AccessTraffic.Actors)
            {
                ring.Nearest(actor.Position, out float lateral);
                if (Mathf.Abs(lateral) <= network.RingWidth * .5f &&
                    ConflictAt(actor.Position, actor.Speed, actor.Choice.Vehicle.HalfLength)) return true;
            }
            return false;
        }

        void Fail(string reason) { FailureReason = reason; Stage = Phase.Failed; }
    }
}
