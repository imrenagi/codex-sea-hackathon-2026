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
        public bool PedestrianBlocksPath => Pedestrian.HasValue && PedestrianClearance.Blocks(
            Pedestrian.Value,Vector2.right*PedestrianDirection,new Vector2(Position.x,Pedestrian.Value.y),Vector2.right,4.3f);

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
