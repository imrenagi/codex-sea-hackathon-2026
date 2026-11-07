using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UjianSim.Game;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class SignalChecks
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Running="JalanKita.SignalChecks";
        static JalanGame game;
        static District district;
        static Journey run;
        static int frame,shot;
        static double began;
        static bool captured;
        static readonly string[] Shots={"right-red","right-green","straight-red","left-green","small-red","law-details","law-details-scrolled"};
        static SignalChecks() { if(SessionState.GetBool(Running,false)) EditorApplication.update+=Poll; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            SessionState.SetBool(Running,true); EditorApplication.EnterPlaymode();
        }
        public static void Verify(JalanGame target)
        {
            var view=(District)typeof(JalanGame).GetField("district",Private).GetValue(target);
            foreach(Rule rule in Enum.GetValues(typeof(Rule)))
                if(Lesson.All[(int)rule].HasSource)
                {
                    var law=LawDetails.For(rule);
                    Require(law.Meaning.Length>50 && law.Sanction.Length>30,"Law explanation and sanction: "+rule);
                    Require(!law.Sanction.Contains("kurungan"),"No obsolete confinement penalty");
                }
            Require(LawDetails.For(Rule.RightArrow).Sanction.Contains("500.000"),"Signal maximum fine");
            foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if(go.name!="TrafficLightArrow") continue;
                var arrow=Models.Find(go,"Arrow_Red").GetComponent<MeshFilter>();
                Vector3 right=-go.right; // Mirrored rig: approach heading is root yaw minus 180.
                float minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
                foreach(var v in arrow.sharedMesh.vertices) { var p=arrow.transform.TransformPoint(v); if(p.y<4) continue; minY=Mathf.Min(minY,p.y); maxY=Mathf.Max(maxY,p.y); }
                float min=float.PositiveInfinity,max=float.NegativeInfinity,stem=0; int count=0;
                foreach(var v in arrow.sharedMesh.vertices)
                {
                    var p=arrow.transform.TransformPoint(v); if(p.y<4) continue;
                    float x=Vector3.Dot(p,right); min=Mathf.Min(min,x); max=Mathf.Max(max,x);
                    if(p.y<minY+(maxY-minY)*.2f) { stem+=x; count++; }
                }
                stem/=count;
                Require(max-stem>(stem-min)*2,"Arrow points to driver's right: "+go.position);
            }
            foreach(int stage in new[] {1,5,6,7}) foreach(int phase in new[] {0,1,2})
            {
                var journey=new Journey(VehicleMode.Car,new JourneySave { next=stage },persistent:false,trafficSeed:1);
                typeof(Journey).GetProperty("Position").SetValue(journey,stage==1?new Vector2(-2,72):Boulevard.World(stage,Boulevard.Lane(stage),-32));
                typeof(Journey).GetField(stage==1?"lightTime":"boulevardTime",Private).SetValue(journey,
                    stage==1 ? phase==0?0f:phase==1?20f:10f : phase==0?0f:phase==1?(stage==7?10f:30f):stage==7?6f:18f);
                view.Present(journey,false,.02f,true);
                Require(view.Signals.Visible && view.Signals.Camera.enabled,"Signal close-up visible: "+stage);
                Require(view.Signals.MainPhase==phase && view.Signals.MainPhase==(stage==1?journey.LightPhase:journey.MainPhase),"Main signal state matches simulation");
                if(stage==7) Require(view.Signals.MainPhase!=2 || view.Signals.ArrowPhase==0,"Straight green is not right-turn green");
                view.Present(journey,false,.02f,false);
                Require(!view.Signals.Camera.enabled,"Signal camera pauses with game");
            }
        }
        static void Poll()
        {
            if(!EditorApplication.isPlaying) return;
            try
            {
                if(!game)
                {
                    game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();
                    if(!game || typeof(JalanGame).GetField("journey",Private).GetValue(game)==null) { game=null; return; }
                    game.enabled=false; Verify(game);
                    district=(District)typeof(JalanGame).GetField("district",Private).GetValue(game);
                    Directory.CreateDirectory("Builds/signal-review");
                }
                if(frame==0)
                {
                    began=EditorApplication.timeSinceStartup; captured=false;
                    int stage=shot<2||shot>=5?7:shot==2?5:shot==3?6:1;
                    run=new Journey(VehicleMode.Car,new JourneySave { next=stage },persistent:false,trafficSeed:1);
                    typeof(Journey).GetProperty("Position").SetValue(run,stage==1?new Vector2(-2,72):Boulevard.World(stage,Boulevard.Lane(stage),-32));
                    typeof(Journey).GetField(stage==1?"lightTime":"boulevardTime",Private).SetValue(run,shot==0?6f:shot==1?16f:shot==3?4f:0f);
                    typeof(JalanGame).GetField("journey",Private).SetValue(game,run);
                    var page=typeof(JalanGame).GetNestedType("Page",BindingFlags.NonPublic);
                    typeof(JalanGame).GetMethod("SetPage",Private).Invoke(game,new[] {Enum.Parse(page,"Drive")});
                    district.Snap();
                    if(shot>=5)
                    {
                        typeof(JalanGame).GetMethod("ShowLesson",Private).Invoke(game,new object[] { Rule.RightArrow });
                        var root=game.GetComponent<UIDocument>().rootVisualElement;
                        var folds=root.Query<VisualElement>(className:"fold").ToList();
                        folds[1].AddToClassList("open");
                        folds[1].Q<Label>(className:"fold-sign").text="-";
                    }
                }
                district.Present(run,false,.02f,shot<5);
                if(shot<5) typeof(JalanGame).GetMethod("RefreshHud",Private).Invoke(game,null);
                if(shot==6 && EditorApplication.timeSinceStartup-began>1)
                    game.GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>().scrollOffset=new Vector2(0,10000);
                frame++;
                if(!captured && EditorApplication.timeSinceStartup-began>2)
                {
                    captured=true;
                    if(shot<5) foreach(var texture in new[] { district.Signals.Texture,district.Signals.ArrowTexture })
                    {
                        if(texture==district.Signals.ArrowTexture && !district.Signals.HasArrow) continue;
                        RenderTexture.active=texture;
                        var tex=new Texture2D(256,256,TextureFormat.RGB24,false);
                        tex.ReadPixels(new Rect(0,0,256,256),0,0); tex.Apply();
                        int phase=texture==district.Signals.Texture?district.Signals.MainPhase:district.Signals.ArrowPhase;
                        int colored=0;
                        foreach(var c in tex.GetPixels()) if(phase==0?c.r>.6f&&c.r>c.g*1.8f:c.g>.6f&&c.g>c.r*1.8f) colored++;
                        Require(colored>40,"Active signal colour visible in close-up: "+Shots[shot]+" / "+texture.name);
                        File.WriteAllBytes("Builds/signal-review/"+Shots[shot]+"-"+texture.name+"-zoom.png",tex.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(tex); RenderTexture.active=null;
                    }
                    if(shot>=5)
                    {
                        var root=game.GetComponent<UIDocument>().rootVisualElement;
                        var scroll=root.Q<ScrollView>();
                        var actions=root.Q<VisualElement>(className:"sheet-actions");
                        Require(scroll.contentViewport.worldBound.yMax<=actions.worldBound.yMin+1,"Law scroll viewport stays above result buttons");
                        Require(scroll.verticalScroller.highValue>0,"Expanded legal details can scroll");
                    }
                    ScreenCapture.CaptureScreenshot("Builds/signal-review/"+Shots[shot]+"-screen.png");
                }
                if(EditorApplication.timeSinceStartup-began<3) return;
                frame=0; shot++;
                if(shot<Shots.Length) return;
                Debug.Log("Signal checks passed: laws, right-facing glyphs, phases, live close-ups and HUD captures.");
                Finish(0);
            }
            catch(Exception e) { Debug.LogException(e); Finish(1); }
        }
        static void Finish(int code) { SessionState.SetBool(Running,false); EditorApplication.update-=Poll; EditorApplication.Exit(code); }
        static void Require(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }
    }
}
