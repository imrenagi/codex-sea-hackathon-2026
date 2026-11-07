using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class JakartaIntegrationChecks
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        const string Running="JalanKita.JakartaIntegrationChecks";
        static JakartaIntegrationChecks(){if(SessionState.GetBool(Running,false))EditorApplication.update+=AwaitGame;}
        public static void Run()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            SessionState.SetBool(Running,true);EditorApplication.EnterPlaymode();
        }
        static void AwaitGame()
        {
            if(!EditorApplication.isPlaying)return;
            var game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();if(!game||Field(game,"journey")==null)return;
            EditorApplication.update-=AwaitGame;SessionState.SetBool(Running,false);
            try{game.enabled=false;VerifyUI(game);VerifyTeardown(game);EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void VerifyTeardown(JalanGame game)
        {
            var scenes=(LandmarkDrive[])Field(game,"landmarkScenes");
            var textures=scenes.Where(scene=>scene!=null).SelectMany(scene=>scene.Mirrors.Textures).ToArray();
            Require(textures.Length==6&&textures.All(texture=>texture.IsCreated()),"Both cached scenes own three live mirror textures before teardown");
            Call(game,"OnDestroy");
            Require(textures.All(texture=>!texture||!texture.IsCreated()),"Scene teardown releases all six landmark mirror textures");
            Require(scenes.All(scene=>scene.Mirrors.Cameras.All(camera=>camera.targetTexture==null)),"Scene teardown unbinds mirror cameras");
            Debug.Log("JAKARTA_TEARDOWN_PASS six cached mirror targets released and camera bindings removed.");
        }
        static object Field(JalanGame game,string name)=>typeof(JalanGame).GetField(name,Private).GetValue(game);
        static void Call(JalanGame game,string name,params object[] args)=>typeof(JalanGame).GetMethod(name,Private).Invoke(game,args);
        static void Require(bool valid,string message){if(!valid)throw new InvalidOperationException("JAKARTA INTEGRATION: "+message);}
        static void ActiveScene(JalanGame game,string label)
        {
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled)==1,label+": exactly one listener");
            Require(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled&&x.targetTexture==null)==1,label+": exactly one main camera");
            Require(RenderSettings.sun&&RenderSettings.sun.isActiveAndEnabled,label+": active daylight");
        }
        public static void VerifyUI(JalanGame game)
        {
            using var preferences=new IntegrationChecks.SavedPreferences();
            var root=game.GetComponent<UIDocument>().rootVisualElement;
            foreach(var mode in new[]{VehicleMode.Car,VehicleMode.Motorcycle})
            {
                typeof(JalanGame).GetField("mode",Private).SetValue(game,mode);
                var choice=VehicleCatalogue.All.Last(x=>x.Mode==mode);VehicleCatalogue.Select(choice);
                var save=new JourneySave{next=4,before=2,trafficSeed=791};save.Store(mode);
                string key=JourneySave.Key(mode),original=PlayerPrefs.GetString(key);
                Call(game,"ShowMenu");
                Require(root.Query<Button>().ToList().Any(x=>x.text.Contains("Sopo Del / HI / Monas")),"Main menu location entry");
                Call(game,"ShowLandmarks");
                Require(root.Query<Button>().ToList().Count(x=>x.text.StartsWith("Sopo Del")||x.text.StartsWith("Bundaran HI")||x.text.StartsWith("Monas"))==3,"Three playable locations");
                Call(game,"StartJaksa");ActiveScene(game,"Sopo Del");
                Call(game,"ShowJaksaPause");Call(game,"ShowLandmarks");ActiveScene(game,"Location picker");
                foreach(var place in new[]{JakartaPlace.BundaranHI,JakartaPlace.Monas})
                {
                    Call(game,"StartLandmark",place);Call(game,"LateUpdate");
                    var drive=(LandmarkDrive)Field(game,"landmark");
                    ActiveScene(game,place.ToString());
                    var pause=root.Q<Button>("progress");pause.Focus();
                    using(var move=NavigationMoveEvent.GetPooled(Vector2.up))
                    {
                        move.target=pause;pause.SendEvent(move);Require(move.isImmediatePropagationStopped,"Landmark W/S cannot navigate HUD");
                    }
                    using(var spaceEvent=KeyDownEvent.GetPooled(new Event{type=EventType.KeyDown,keyCode=KeyCode.Space,character=' '}))
                    {
                        spaceEvent.target=pause;pause.SendEvent(spaceEvent);Require(spaceEvent.isImmediatePropagationStopped&&Field(game,"page").ToString()=="Landmark","Landmark Space remains brake with focused HUD");
                    }
                    root.focusController.focusedElement?.Blur();
                    Require(drive.Mode==mode&&VehicleCatalogue.Selected(mode).Id==choice.Id,"Vehicle selection preserved");
                    Require(drive.Environment.Root.GetComponentsInChildren<Transform>(true).Any(t=>t.name==choice.Label),"Selected model appears");
                    Require(!root.Q("hud").ClassListContains("hidden")&&root.Q("overlay").ClassListContains("hidden"),"Drive HUD visible");
                    var mirrors=root.Q("mirrors").Query<Image>().ToList();
                    Require(mirrors.Select((m,i)=>m.image==drive.Mirrors.Textures[i]).All(x=>x),"Landmark mirrors bound");
                    for(int i=0;i<80;i++)drive.Step(.02f,true,false,0);
                    drive.Present(.02f);float time=drive.Elapsed;var position=drive.Position;int renders=drive.Environment.Water.ReflectionFrames;
                    var trafficPositions=drive.Traffic.Select(a=>a.Position).ToArray();
                    if(place==JakartaPlace.BundaranHI)Call(game,"OnApplicationFocus",false);else Call(game,"OnApplicationPause",true);
                    Require(Field(game,"page").ToString()=="LandmarkPause","Focus/application pause opens landmark pause");
                    Call(game,"OnApplicationFocus",true);Call(game,"OnApplicationPause",false);
                    Require(Field(game,"page").ToString()=="LandmarkPause","Returning focus waits for explicit resume");
                    Call(game,"FixedUpdate");Call(game,"LateUpdate");
                    Require(drive.Traffic.Select((actor,i)=>actor.Position==trafficPositions[i]).All(x=>x),"Focus loss freezes traffic positions");
                    Require(drive.Elapsed==time&&drive.Position==position&&drive.Environment.Water.SimulationTime==time,"Pause freezes traffic, player and water");
                    Require(drive.Environment.Water.ReflectionFrames==renders,"Stationary paused water does not recapture");
                    Require(drive.Mirrors.Cameras.All(c=>!c.enabled),"Paused mirrors stop rendering");
                    Require(drive.Environment.Water.GetComponentsInChildren<AudioSource>().All(a=>!a.isPlaying),"Fountain audio pauses");
                    Call(game,"JumpToMission",0);Require(ReferenceEquals(drive,Field(game,"landmark")),"Landmark step cannot jump to training");
                    Call(game,"ShowLandmarks");Require(!drive.Environment.Root.activeSelf,"Previous area disabled");
                    ActiveScene(game,"Picker after "+place);
                    Call(game,"StartLandmark",place);Require(ReferenceEquals(drive,Field(game,"landmark")),"Cached area reused");
                    Call(game,"ShowMenu");ActiveScene(game,"Menu after "+place);
                    Require(PlayerPrefs.GetString(key)==original,"Exploration keeps journey save byte-for-byte");
                }
                Call(game,"OpenCityMap",0);ActiveScene(game,"Map after landmark");
                Call(game,"ShowMenu");Call(game,"Resume");
                Require(((Journey)Field(game,"journey")).Stage==4,"Journey still resumes");
            }
            Call(game,"ShowMenu");Call(game,"LateUpdate");
            foreach(string shader in new[]{"ReflectiveWater","Fountain"})Require(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("JakartaLandmarks/Shaders/"+shader)),"Shader compiles: "+shader);
            Debug.Log("JAKARTA_INTEGRATION_PASS three destinations; vehicle selections; scenes, mirrors, lights and audio lifecycle; water pause; cache reuse; immutable saves and resume.");
        }
    }
}
