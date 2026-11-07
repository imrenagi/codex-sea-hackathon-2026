using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
namespace JalanKita.EditorTools
{
    [InitializeOnLoad] public static class HiMissionFlowChecks
    {
        const string Running = "RoadSense.HiMissionFlowChecks";
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static HiMissionFlowChecks() { if (SessionState.GetBool(Running, false)) EditorApplication.update += AwaitGame; }
        public static void Run()
        {
            HiObjectiveChecks.Run();
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            SessionState.SetBool(Running, true); EditorApplication.EnterPlaymode();
        }
        static object Field(JalanGame g, string n) => typeof(JalanGame).GetField(n, Private).GetValue(g);
        static void Set(JalanGame g, string n, object v) => typeof(JalanGame).GetField(n, Private).SetValue(g,v);
        static void Call(JalanGame g, string n, params object[] a) => typeof(JalanGame).GetMethod(n,Private).Invoke(g,a);
        static void Require(bool ok,string why) { if(!ok) throw new Exception("HI_FLOW: "+why); }
        static void Submit(Button b)
        {
            Require(b!=null,"Button exists"); b.Focus();
            using var e=NavigationSubmitEvent.GetPooled(); e.target=b; b.SendEvent(e);
        }
        static Button Button(VisualElement root,string text) => (root.Q("dialog") ?? root).Query<Button>().ToList().FirstOrDefault(b=>b.text==text);
        static void AwaitGame()
        {
            if(!EditorApplication.isPlaying) return;
            var game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();
            if(!game||Field(game,"journey")==null) return;
            EditorApplication.update-=AwaitGame; SessionState.SetBool(Running,false);
            try { Verify(game); Debug.Log("HI_MISSION_FLOW_PASSED catalogue/brief, actual drive completion, result/retry, pause, reverse, save isolation and listener lifecycle"); EditorApplication.Exit(0); }
            catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
        static void Verify(JalanGame game)
        {
            using var prefs=new IntegrationChecks.SavedPreferences();
            string[] keys={"RoadSense.Mission.HI.v1.Car","RoadSense.Mission.HI.v1.Motorcycle"};
            var originals=keys.Select(k=>PlayerPrefs.HasKey(k)?PlayerPrefs.GetString(k):null).ToArray();
            game.enabled=false;
            try
            {
                var root=game.GetComponent<UIDocument>().rootVisualElement;
                foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))
                {
                    Set(game,"mode",mode); Call(game,"ShowMenu");
                    string fullKey=JourneySave.Key(mode), fullSave=PlayerPrefs.GetString(fullKey);
                    string[] missions=Mission.Scenarios.Select(m=>PlayerPrefs.GetString(JourneySave.Key(mode,m))).ToArray();
                    Call(game,"ShowCatalogue",MissionKind.Scenario);
                    var card=root.Query<VisualElement>(className:"mission-card").ToList().Single(c=>c.Query<Label>().ToList().Any(l=>l.text==GameText.Get("hi.title")));
                    Submit(Button(card,GameText.Get("text.select_2a7802"))); Require(Field(game,"page").ToString()=="Brief","Catalogue opens existing briefing");
                    Submit(Button(root,GameText.Get("text.start"))); Require(Field(game,"page").ToString()=="Jaksa"&&(bool)Field(game,"hiActive"),"Brief starts HI mission");
                    var drive=(LandmarkDrive)Field(game,"hi"); var objective=(HiRoundaboutObjective)Field(game,"hiObjective");
                    Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.isActiveAndEnabled)==1,"One active listener in HI");
                    Set(game,"inputReleaseRequired",false);
                    Call(game,"ShowJaksaPause"); var position=drive.Position; float time=drive.Elapsed;
                    Call(game,"FixedUpdate"); Require(drive.Position==position&&drive.Elapsed==time,"Pause freezes pose/time");
                    Submit(Button(root,GameText.Get("text.continue"))); Set(game,"inputReleaseRequired",false);
                    drive.TrafficEnabled=false;
                    for(int i=0;i<60;i++) Call(game,"StepHi",.02f,false,false,0f,true);
                    Require(drive.Speed<-.1f,"S dispatcher supports signed reverse");
                    Call(game,"ResetExplore"); Require(drive.Speed==0&&objective.Stage==HiRoundaboutObjective.Phase.Approach,"Retry resets motion/objective");
                    drive.TrafficEnabled=false;
                    var path=drive.Environment.Network.TrafficPaths[0];
                    for(int i=0;i<16000&&!objective.Complete&&!objective.Failed;i++)
                    {
                        float s=path.Nearest(drive.Position,out _); var target=path.At(s+9)-drive.Position;
                        float heading=Mathf.Atan2(target.x,target.y)*Mathf.Rad2Deg;
                        float steer=RoadVehicleMotion.TrackLane(mode,drive.Speed,drive.Heading,0,0,heading);
                        Call(game,"StepHi",.02f,true,false,steer,false);
                    }
                    Require(objective.Complete,"Actual controller reaches result "+mode+" pose="+drive.Position+" stage="+objective.Stage+" reason="+objective.FailureReason);
                    Require(Field(game,"page").ToString()=="JaksaPause"&&root.Query<Label>().ToList().Any(l=>l.text==GameText.Get("hi.completed")),"Completion result shown");
                    Require(PlayerPrefs.GetString(fullKey)==fullSave,"Main progress unchanged");
                    for(int i=0;i<missions.Length;i++) Require(PlayerPrefs.GetString(JourneySave.Key(mode,Mission.Scenarios[i]))==missions[i],"Other scenario progress unchanged");
                    Submit(Button(root,GameText.Get("hi.retry"))); Require(objective.Stage==HiRoundaboutObjective.Phase.Approach&&drive.Speed==0,"Result retry works");
                    Call(game,"ShowJaksaPause"); Submit(Button(root,GameText.Get("hi.choose")));
                    Require(!(bool)Field(game,"hiActive")&&Field(game,"page").ToString()=="Catalogue","Result returns to catalogue");
                    Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.isActiveAndEnabled)==1,"One active listener after leaving HI");
                    Debug.Log("HI_FLOW_MODE_PASS "+mode);
                }
                Set(game,"mode",VehicleMode.Car); Call(game,"StartHiMission");
                var live=(LandmarkDrive)Field(game,"hi"); var task=(HiRoundaboutObjective)Field(game,"hiObjective");
                var route=live.Environment.Network.TrafficPaths[0]; var entry=live.Environment.Network.Branches[0].Route;
                float gate=(float)typeof(HiRoundaboutObjective).GetField("entryGate",Private).GetValue(task);
                int yieldingFrames=0;
                for(int i=0;i<16000&&!task.Complete&&!task.Failed;i++)
                {
                    float s=route.Nearest(live.Position,out _); var target=route.At(s+9)-live.Position;
                    float heading=Mathf.Atan2(target.x,target.y)*Mathf.Rad2Deg;
                    bool wait=task.Stage==HiRoundaboutObjective.Phase.Yield&&task.Conflict&&gate-entry.Nearest(live.Position,out _)<19;
                    if(wait)yieldingFrames++;
                    Call(game,"StepHi",.02f,!wait,wait,RoadVehicleMotion.TrackLane(VehicleMode.Car,live.Speed,live.Heading,0,0,heading),false);
                }
                if(!task.Complete)
                {
                    Debug.Log("HI_STALL player "+live.Position+" yaw="+live.Heading+" speed="+live.Speed+" curb="+live.CurbContact+" traffic="+live.TrafficContact);
                    for(int i=0;i<live.Traffic.Length;i++)
                    {
                        var a=live.Traffic[i]; var next=live.Environment.Route.At(a.S+.1f,a.Lane); float yaw=live.Environment.Route.Heading(a.S+.1f);
                        Debug.Log("HI_RING "+i+" p="+a.Position+" yaw="+a.Heading+" speed="+a.Speed+" lane="+a.Lane+" size="+a.Choice.Vehicle.HalfWidth+","+a.Choice.Vehicle.HalfLength+" accessLimit="+live.AccessTraffic.SafeSpeed(a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,7));
                        for(int j=0;j<live.AccessTraffic.Actors.Length;j++) { var b=live.AccessTraffic.Actors[j]; if(TrafficFootprint.Overlaps(next,yaw,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,b.Position,b.Heading,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,.16f)) Debug.Log("HI_BLOCK ring"+i+" -> access"+j); }
                    }
                    for(int i=0;i<live.AccessTraffic.Actors.Length;i++)
                    {
                        var a=live.AccessTraffic.Actors[i]; var next=a.Path.At(a.S+.1f);float yaw=a.Path.Heading(a.S+.1f);
                        Debug.Log("HI_ACCESS "+i+" p="+a.Position+" yaw="+a.Heading+" speed="+a.Speed+" size="+a.Choice.Vehicle.HalfWidth+","+a.Choice.Vehicle.HalfLength+" s="+a.S);
                        for(int j=0;j<live.Traffic.Length;j++) { var b=live.Traffic[j]; float limit=LandmarkAccessTraffic.AheadLimit(a.Position,a.Heading,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,b.Position,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,7); if(limit<.1f)Debug.Log("HI_LIMIT access"+i+" -> ring"+j); if(TrafficFootprint.Overlaps(next,yaw,a.Choice.Vehicle.HalfWidth,a.Choice.Vehicle.HalfLength,b.Position,b.Heading,b.Choice.Vehicle.HalfWidth,b.Choice.Vehicle.HalfLength,.16f))Debug.Log("HI_BLOCK access"+i+" -> ring"+j); }
                    }
                }
                Require(task.Complete,"Live traffic safe mission completes: "+task.Stage+" "+task.FailureReason+" at "+live.Position+" t="+live.Elapsed);
                Debug.Log("HI_LIVE_TRAFFIC_PASS elapsed="+live.Elapsed+" yieldingFrames="+yieldingFrames);

            }
            finally
            {
                Call(game,"ShowMenu");
                for(int i=0;i<keys.Length;i++) if(originals[i]==null) PlayerPrefs.DeleteKey(keys[i]);else PlayerPrefs.SetString(keys[i],originals[i]);
                PlayerPrefs.Save(); game.enabled=true;
            }
        }
    }
}
