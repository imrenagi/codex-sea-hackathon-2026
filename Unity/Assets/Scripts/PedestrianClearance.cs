using UnityEngine;

namespace JalanKita
{
    public static class PedestrianClearance
    {
        // Ujian SIM's InPath policy: one metre beyond the lane, and one incoming
        // lane of anticipation. These distances are simulation safety margins.
        public static bool Blocks(Vector2 pedestrian,Vector2 walking,Vector2 crossingPoint,Vector2 crossingAxis,float laneWidth)
        {
            float offset=Vector2.Dot(pedestrian-crossingPoint,crossingAxis);
            float reach=laneWidth*.5f+1;
            if(Mathf.Abs(offset)<=reach) return true;
            bool approaching=offset*Vector2.Dot(walking,crossingAxis)<0;
            return approaching && Mathf.Abs(offset)<=reach+laneWidth;
        }

        public static bool Touches(Vector2 pedestrian,Vector2 vehicle,float heading,VehicleMode mode)
        {
            var forward=new Vector2(Mathf.Sin(heading*Mathf.Deg2Rad),Mathf.Cos(heading*Mathf.Deg2Rad));
            var delta=pedestrian-vehicle;
            return Mathf.Abs(Vector2.Dot(delta,forward))<(mode==VehicleMode.Car?2.1f:1)+.35f
                && Mathf.Abs(Vector2.Dot(delta,new Vector2(forward.y,-forward.x)))<(mode==VehicleMode.Car?.85f:.35f)+.35f;
        }
    }

    public sealed partial class Journey
    {
        public const int CrossingPedestrianCount = 3;
        const float CrossingStartX = -5.9f, CrossingEndX = 5.9f;
        readonly Vector2?[] crossingPedestrians = new Vector2?[CrossingPedestrianCount];
        readonly float[] crossingWalking = new float[CrossingPedestrianCount];
        readonly float[] crossingGaitTime = new float[CrossingPedestrianCount];

        // Rendered people outlive the crossing hazard: they wait on the far sidewalk
        // instead of vanishing the moment their centre clears the road.
        public Vector2? CrossingPedestrian(int index) => crossingPedestrians[index];
        public float CrossingPedestrianWalking(int index) => crossingWalking[index];
        public float CrossingPedestrianGaitTime(int index) => crossingGaitTime[index];
        public bool PedestriansHaveCleared => zebraTime >= 0 && !Pedestrian.HasValue;

        void UpdateCrossingPedestrians(float crossingZ, float pace)
        {
            Pedestrian = null;
            for (int i = 0; i < CrossingPedestrianCount; i++)
            {
                float walkingTime = Mathf.Max(0, zebraTime - .2f - i * .4f);
                float personalPace = pace * (i == 0 ? .96f : i == 1 ? 1.045f : 1f);
                float phase = (trafficSeed & 255) * .02454f + i * 2.17f;
                // Integrate gentle pace changes analytically: deterministic across frame
                // rates, with strictly forward speed (at least 91% of personal pace).
                float distance = personalPace * (walkingTime
                    + .065f / .95f * (Mathf.Cos(phase) - Mathf.Cos(.95f * walkingTime + phase))
                    + .025f / 1.7f * (Mathf.Cos(phase * .63f) - Mathf.Cos(1.7f * walkingTime + phase * .63f)));
                distance = Mathf.Clamp(distance, 0, CrossingEndX - CrossingStartX);
                float progress = distance / (CrossingEndX - CrossingStartX);
                float x = CrossingStartX + distance;
                // A few centimetres of actual path variation, tapered at both curbs.
                float drift = .045f * (Mathf.Sin(.85f * walkingTime + phase) - Mathf.Sin(phase))
                    * Mathf.Sin(progress * Mathf.PI);
                crossingPedestrians[i] = new Vector2(x * PedestrianDirection, crossingZ + (i - 1) * .8f + drift);
                crossingGaitTime[i] = distance / 1.65f + i * .57f;
                // Ease into the stride and settle at the curb rather than walking in place.
                crossingWalking[i] = Mathf.SmoothStep(0, 1, walkingTime / .18f)
                    * Mathf.Clamp01((CrossingEndX - x) / .45f);
                if (x < CrossingEndX && !Pedestrian.HasValue) Pedestrian = crossingPedestrians[i];
            }
        }

        void ClearCrossingPedestrians()
        {
            Pedestrian = null;
            for (int i = 0; i < CrossingPedestrianCount; i++)
            {
                crossingPedestrians[i] = null;
                crossingWalking[i] = crossingGaitTime[i] = 0;
            }
        }

        public bool PedestrianBlocksPath
        {
            get
            {
                for (int i = 0; i < CrossingPedestrianCount; i++)
                {
                    var person = crossingPedestrians[i];
                    if (person.HasValue && PedestrianClearance.Blocks(person.Value,
                        Vector2.right * PedestrianDirection, new Vector2(Position.x, person.Value.y), Vector2.right, 4.3f))
                        return true;
                }
                return false;
            }
        }

        static readonly Vector2?[] boulevardCrossingPoints=new Vector2?[3];
        public Vector2 BoulevardCrossingPoint
        {
            get
            {
                if(Stage==5) return Boulevard.World(Stage,Boulevard.Local(Stage,Position).x,-14);
                if(Stage<5 || Stage>7) return Position;
                var exit=Boulevard.Exit(Stage);
                var forward=new Vector2(Mathf.Sin(Heading*Mathf.Deg2Rad),Mathf.Cos(Heading*Mathf.Deg2Rad));
                float progress=Vector2.Dot(Position-Boulevard.Center(Stage),exit);
                if(progress>8 && Vector2.Dot(forward,exit)>.8f)
                    return Position+forward*((14-progress)/Vector2.Dot(forward,exit));
                if(!boulevardCrossingPoints[Stage-5].HasValue)
                {
                    var path=Boulevard.Path(Stage);
                    for(int i=1;i<path.Length;i++)
                    {
                        float a=Vector2.Dot(path[i-1]-Boulevard.Center(Stage),exit);
                        float b=Vector2.Dot(path[i]-Boulevard.Center(Stage),exit);
                        if(a<=14 && b>=14 && b>a)
                        { boulevardCrossingPoints[Stage-5]=Vector2.Lerp(path[i-1],path[i],(14-a)/(b-a)); break; }
                    }
                }
                return boulevardCrossingPoints[Stage-5] ?? Boulevard.Center(Stage)+exit*14;
            }
        }
        public bool BoulevardPedestrianBlocksPath
        {
            get
            {
                if(!BoulevardPedestrian.HasValue || Stage<5 || Stage>7) return false;
                var axis=Stage==5?Boulevard.Right(Stage):Boulevard.Forward(Stage);
                return PedestrianClearance.Blocks(BoulevardPedestrian.Value,axis,BoulevardCrossingPoint,axis,Boulevard.LaneWidth);
            }
        }
    }
}
