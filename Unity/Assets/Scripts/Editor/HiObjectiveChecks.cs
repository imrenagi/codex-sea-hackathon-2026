using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class HiObjectiveChecks
    {
        static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException("HI_OBJECTIVE: " + message); }
        static void Pose(LandmarkDrive drive, Vector2 point, float heading, float speed)
        {
            typeof(LandmarkDrive).GetProperty("Position").SetValue(drive, point);
            typeof(LandmarkDrive).GetProperty("Heading").SetValue(drive, heading);
            typeof(LandmarkDrive).GetProperty("Speed").SetValue(drive, speed);
        }
        public static void Run()
        {
            using var saved = new IntegrationChecks.SavedPreferences();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var host = new GameObject("HI objective regression");
            var drive = new LandmarkDrive(host.transform, JakartaPlace.BundaranHI, forwardMissionTraffic: true);
            var path = drive.Environment.Network.TrafficPaths[0];
            foreach(var through in new[]{drive.Environment.Network.TrafficPaths[0]})
            for(float s=5;s<through.Length-5;s+=.5f)
            {
                var point=through.At(s);float yaw=through.Heading(s)*Mathf.Deg2Rad;
                var forward=new Vector2(Mathf.Sin(yaw),Mathf.Cos(yaw));var right=new Vector2(forward.y,-forward.x);
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    Require(drive.Environment.Network.ContainsPoint(point+right*(x*.98f)+forward*(z*1.95f),.1f),"Through-route body stays in road union s="+s+" point="+point);
            }

            foreach (VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
            {
                drive.Reset(mode); drive.TrafficEnabled = false;
                var objective = new HiRoundaboutObjective(drive);
                bool entered = false, exited = false;
                for (float s = 16; s < path.Length - 2 && !objective.Complete && !objective.Failed; s += .5f)
                {
                    Pose(drive, path.At(s), path.Heading(s), 5);
                    objective.Step(.1f);
                    entered |= objective.Stage == HiRoundaboutObjective.Phase.Circulate;
                    exited |= objective.Stage == HiRoundaboutObjective.Phase.Exit;
                }
                Require(entered && exited && objective.Complete, "Safe ordered trajectory " + mode + ": " + objective.Stage + " " + objective.FailureReason);

                drive.Reset(mode); drive.TrafficEnabled = true; objective.Reset();
                float merge = (float)typeof(HiRoundaboutObjective).GetField("mergeS", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(objective);
                var ring = drive.Environment.Route;
                foreach (var actor in drive.Traffic)
                { actor.S = merge + 100; actor.Position = ring.At(actor.S, actor.Lane); }
                foreach (var actor in drive.AccessTraffic.Actors) actor.Position = new Vector2(500, 500);
                var conflict = drive.Traffic[0];
                conflict.S = merge - 5; conflict.Lane = -5.15f; conflict.Speed = 6;
                conflict.Position = ring.At(conflict.S, conflict.Lane);
                objective.Reset();
                for (float s = 16; s < path.Length - 2 && !objective.Failed; s += .5f)
                { Pose(drive, path.At(s), path.Heading(s), 5); objective.Step(.1f); }
                Require(objective.Failed && !objective.Complete && objective.FailureReason == GameText.Get("hi.unsafe_entry"), "Actual circulating conflict blocks successful entry " + mode);

                drive.Reset(mode); drive.TrafficEnabled = false; objective.Reset();
                Require(!objective.Failed && !objective.Complete && objective.Stage == HiRoundaboutObjective.Phase.Approach, "Retry clears failure and stage");
                Pose(drive, path.At(path.Length - 2), path.Heading(path.Length - 2), -5);
                objective.Reset();
                for (float s = path.Length - 2; s > 16; s -= .5f)
                { Pose(drive, path.At(s), path.Heading(s), -5); objective.Step(.1f); }
                Require(!objective.Complete && objective.Stage != HiRoundaboutObjective.Phase.Exit, "Reverse traversal cannot complete or skip entry " + mode);
                var stage = objective.Stage;
                objective.Step(0);
                Require(objective.Stage == stage, "Paused objective unchanged");
                var forwardContact=new Vector2(Mathf.Sin(drive.Heading*Mathf.Deg2Rad),Mathf.Cos(drive.Heading*Mathf.Deg2Rad));
                drive.Traffic[0].Position=drive.Position+forwardContact*5;
                drive.Traffic[0].Heading=drive.Heading;
                Require((bool)typeof(LandmarkDrive).GetMethod("TrafficBlocks",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(drive,new object[]{drive.Position+forwardContact*10,drive.Heading}),"Circulating vehicle sweep still blocks contact");
                drive.AccessTraffic.Actors[0].Position=drive.Position+forwardContact*5;
                drive.AccessTraffic.Actors[0].Heading=drive.Heading;
                Require(drive.AccessTraffic.BlocksPath(drive.Position,drive.Position+forwardContact*10,drive.Heading,drive.Heading,TrafficFootprint.PlayerHalfWidth(mode),TrafficFootprint.PlayerHalfLength(mode)),"Access traffic sweep still blocks contact");
                Debug.Log("HI_OBJECTIVE_PASS " + mode + " safe trajectory, actual unsafe entry, reset and reverse shortcut");
            }
            drive.Mirrors.Dispose(); UnityEngine.Object.DestroyImmediate(host);
            Debug.Log("HI_OBJECTIVE_ALL_PASSED");
        }
    }
}
