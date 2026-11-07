using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
namespace JalanKita.EditorTools
{
    [InitializeOnLoad] public static class HiSelectionChecks
    {
        const string Running="RoadSense.HiSelectionChecks";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static HiSelectionChecks(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Await;}
        public static void Run(){EditorSceneManager.OpenScene(ProjectSetup.ScenePath);SessionState.SetBool(Running,true);EditorApplication.EnterPlaymode();}
        static object Field(JalanGame g,string name)=>typeof(JalanGame).GetField(name,Private).GetValue(g);
        static void Call(JalanGame g,string name,params object[] args)=>typeof(JalanGame).GetMethods(Private).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(g,args);
        static void Require(bool ok,string message){if(!ok)throw new Exception("HI_SELECTION: "+message);}
        static void Submit(Button b){Require(b!=null,"Button exists");b.Focus();using var e=NavigationSubmitEvent.GetPooled();e.target=b;b.SendEvent(e);}
        static void Await()
        {
            if(!EditorApplication.isPlaying)return;
            var game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();if(!game||Field(game,"journey")==null)return;
            EditorApplication.update-=Await;SessionState.SetBool(Running,false);
            try{Verify(game);Debug.Log("HI_SELECTION_PASSED actual new Play/catalogue cards, detail launch, briefing, drive, pause/return, save and listener isolation");EditorApplication.Exit(0);}
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }
        static void Verify(JalanGame game)
        {
            using var preferences=new IntegrationChecks.SavedPreferences();
            string[] keys={"RoadSense.Mission.HI.v1.Car","RoadSense.Mission.HI.v1.Motorcycle"};
            var saved=keys.Select(k=>PlayerPrefs.HasKey(k)?PlayerPrefs.GetString(k):null).ToArray();
            game.enabled=false;
            try
            {
                var root=game.GetComponent<UIDocument>().rootVisualElement;
                foreach(VehicleMode mode in Enum.GetValues(typeof(VehicleMode)))foreach(bool catalogue in new[]{false,true})
                {
                    typeof(JalanGame).GetField("mode",Private).SetValue(game,mode);
                    string original=PlayerPrefs.GetString(JourneySave.Key(mode));
                    Call(game,"ShowMenu");if(catalogue)Call(game,"ShowCatalogue",MissionKind.Scenario);else Call(game,"ShowPlayChooser");
                    var card=root.Query<Button>(className:"selection-card").ToList().Single(c=>c.tooltip==GameText.Get("hi.title"));
                    Require(card.focusable&&card.enabledSelf,"HI card participates in new keyboard selector");Submit(card);
                    Require(root.Q<Label>(className:"selection-detail-title").text==GameText.Get("hi.title"),"Selection detail names playable HI mission");
                    Submit(root.Q<Button>(className:"selection-start"));Require(Field(game,"page").ToString()=="Brief","Selection opens existing briefing");
                    Submit(root.Q("dialog").Query<Button>().ToList().Single(b=>b.text==GameText.Get("text.start")));
                    Require((bool)Field(game,"hiActive")&&Field(game,"page").ToString()=="Jaksa","Brief starts actual HI controller");
                    Call(game,"StepHi",.02f,true,false,0f,false);
                    var drive=(LandmarkDrive)Field(game,"hi");
                    drive.Safety.Step(.02f,new DriveSafetySample{VehicleContact=true});
                    Call(game,"ShowJaksaPause");
                    Require(root.Q<Label>(className:"stamp")?.text==GameText.Get("hi.try_again"),"Common safety failure uses restored failure banner");
                    Require(!root.Q("dialog").Query<Button>().ToList().Any(b=>b.text==GameText.Get("text.continue")),"Failed attempt cannot resume without retry");
                    Submit(root.Q("dialog").Query<Button>().ToList().Single(b=>b.text==GameText.Get("hi.retry")));
                    Require(!drive.Safety.Failure.HasValue&&Field(game,"page").ToString()=="Jaksa","Actual retry resets safety and resumes driving");
                    Call(game,"ShowJaksaPause");
                    Submit(root.Q("dialog").Query<Button>().ToList().Single(b=>b.text==GameText.Get("hi.choose")));
                    Require(!(bool)Field(game,"hiActive")&&Field(game,"page").ToString()=="Catalogue","Pause returns to new mission selector");
                    Require(PlayerPrefs.GetString(JourneySave.Key(mode))==original,"Main journey unchanged");
                    Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.isActiveAndEnabled)==1,"One listener after return");
                }
            }
            finally
            {
                Call(game,"ShowMenu");for(int i=0;i<keys.Length;i++)if(saved[i]==null)PlayerPrefs.DeleteKey(keys[i]);else PlayerPrefs.SetString(keys[i],saved[i]);
                PlayerPrefs.Save();game.enabled=true;
            }
        }
    }
}
