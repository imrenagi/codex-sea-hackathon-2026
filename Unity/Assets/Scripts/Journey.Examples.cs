using UnityEngine;

namespace JalanKita
{
    public sealed partial class Journey
    {
        // Keep mode, mission, checkpoint, encounter variant and traffic seed, but
        // replay only the provider-selected maneuver from a detached save snapshot.
        internal Journey CreateSectionExample(Vector2 start)
        {
            var snapshot = JsonUtility.FromJson<JourneySave>(JsonUtility.ToJson(Save));
            snapshot.retryPending = false;
            var preview = new Journey(Mode, snapshot, persistent: false, mission: Activity, trafficSeed: Save.trafficSeed);
            preview.Position = start;
            preview.Heading = 0;
            if (Stage == 1 && start.y > RouteStage.StopLine)
            {
                preview.FullStop = true;
                preview.stopTime = 30;
            }
            if (Stage == 2 && start.y > CrossingZebra + 2) preview.zebraTime = 20;
            preview.UpdateActors(0);
            return preview;
        }
    }
}
