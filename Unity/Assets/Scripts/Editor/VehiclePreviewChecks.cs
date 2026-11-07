using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class VehiclePreviewChecks
    {
        const string Running="JalanKita.VehiclePreviewChecks";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static JalanGame game;
        static IntegrationChecks.SavedPreferences preferences;
        static double began;
        static int shot;
        static bool captured;
        static VehiclePreviewChecks() { if(SessionState.GetBool(Running,false)) EditorApplication.update+=Poll; }
        static void Require(bool ok,string message) { if(!ok) throw new InvalidOperationException("PREVIEW: "+message); }
        static object Field(JalanGame game,string name)=>typeof(JalanGame).GetField(name,Private).GetValue(game);
        static void Call(JalanGame game,string name,params object[] args)=>typeof(JalanGame).GetMethod(name,Private).Invoke(game,args);
        static void Click(Button button) { using var e=NavigationSubmitEvent.GetPooled(); e.target=button; button.SendEvent(e); }
        public static void Verify(JalanGame game)
        {
            using var saved=new IntegrationChecks.SavedPreferences();
            var root=game.GetComponent<UIDocument>().rootVisualElement;
            foreach(var definition in VehicleCatalogue.All)
            {
                Call(game,"ShowPlayChooser");
                Call(game,"SelectMode",definition.Mode);
                Click(root.Query<Button>().ToList().Find(b=>b.text=="Lihat kendaraan 3D"));
                Click(root.Q<Button>("vehicle-"+definition.Id));
                var preview=(VehiclePreview)Field(game,"vehiclePreview");
                Require(preview.Definition==definition && VehicleCatalogue.Selected(definition.Mode)==definition,"Selecting model updates render and gameplay selection");
                float angle=preview.Angle;
                preview.AutoRotate=true; preview.Present(definition.Mode,.1f);
                Require(Mathf.Abs(Mathf.DeltaAngle(angle,preview.Angle))>1,"Automatic rotation advances");
                root.Q<Slider>("vehicleAngle").value=270;
                Require(!preview.AutoRotate && Mathf.Abs(preview.Angle-270)<.01f,"Manual slider controls full turn and pauses auto");
                preview.Rotate(180); Require(Mathf.Abs(preview.Angle-90)<.01f,"Manual rotation wraps 360 degrees");
                var image=root.Q<Image>("vehiclePreview");
                using(var key=KeyDownEvent.GetPooled(new Event {type=EventType.KeyDown,keyCode=KeyCode.RightArrow}))
                { key.target=image; image.SendEvent(key); }
                Require(Mathf.Abs(preview.Angle-105)<.01f,"Keyboard rotation is available");
                image.Focus();
                foreach(var direction in new[]{Vector2.left,Vector2.right})
                    using(var navigation=NavigationMoveEvent.GetPooled(direction))
                    {
                        navigation.target=image; image.SendEvent(navigation);
                        Require(navigation.isPropagationStopped && root.focusController.focusedElement==image,
                            "Image arrow navigation retains focus for repeated rotation");
                    }
                float beforeDrag=preview.Angle;
                using(var down=PointerDownEvent.GetPooled(new Event {type=EventType.MouseDown,button=0,mousePosition=new Vector2(100,100)}))
                { down.target=image; image.SendEvent(down); }
                using(var move=PointerMoveEvent.GetPooled(new Event {type=EventType.MouseDrag,button=0,mousePosition=new Vector2(200,100)}))
                { move.target=image; image.SendEvent(move); }
                using(var up=PointerUpEvent.GetPooled(new Event {type=EventType.MouseUp,button=0,mousePosition=new Vector2(200,100)}))
                { up.target=image; image.SendEvent(up); }
                Require(Mathf.Abs(Mathf.DeltaAngle(beforeDrag,preview.Angle)-60)<.1f,"Pointer drag rotates the vehicle and releases capture");
                Click(root.Q<Button>("vehicleAutoRotate")); Require(preview.AutoRotate,"Auto rotation resumes");
                Require(preview.Camera.targetTexture==image.image && preview.Texture.IsCreated(),"Live render texture is bound");
            }
            Call(game,"ShowMissionBrief",Mission.Scenarios[1]);
            Require(!root.Q("vehicleShowroom").ClassListContains("hidden"),"Briefing includes vehicle selection");
            Call(game,"StartMission",Mission.Scenarios[1],null,false);
            Require(!((VehiclePreview)Field(game,"vehiclePreview")).Camera.enabled,"Preview camera stops while driving");
            Call(game,"BackToMenu");
        }
        public static void Run()
        {
            NpcRuleChecks.Run();
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            SessionState.SetBool(Running,true); EditorApplication.EnterPlaymode();
        }
        static void Poll()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(!game)
                {
                    game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();
                    if(!game||Field(game,"journey")==null){game=null;return;}
                    game.enabled=false; preferences=new IntegrationChecks.SavedPreferences(); Verify(game);
                    Directory.CreateDirectory("Builds/vehicle-preview-review");
                    began=EditorApplication.timeSinceStartup;
                }
                var def=VehicleCatalogue.All[Mathf.Min(shot,VehicleCatalogue.All.Length-1)];
                if(!captured && EditorApplication.timeSinceStartup-began<.2)
                {
                    Call(game,"ShowPlayChooser");
                    Call(game,"SelectMode",def.Mode); VehicleCatalogue.Select(def);
                    var chooserRoot=game.GetComponent<UIDocument>().rootVisualElement;
                    Click(chooserRoot.Query<Button>().ToList().Find(b=>b.text=="Lihat kendaraan 3D"));
                    if(shot==7) Call(game,"ShowMissionBrief",Mission.Scenarios[1]);
                    if(shot==8) Call(game,"ShowPreflight");
                }
                Call(game,"RefreshVehiclePreview",.02f);
                if(!captured && EditorApplication.timeSinceStartup-began>2)
                {
                    var root=game.GetComponent<UIDocument>().rootVisualElement;
                    var image=root.Q<Image>("vehiclePreview");
                    var panel=root.Q("vehicleShowroom");
                    Require(panel.worldBound.xMin>=root.Q("dialog").worldBound.xMax,"Preview sits on right of menu");
                    Require(panel.worldBound.xMax<=root.worldBound.xMax && panel.worldBound.yMax<=root.worldBound.yMax,"Preview fits screen");
                    Require(image.worldBound.height>100,"Vehicle view has useful height");
                    var preview=(VehiclePreview)Field(game,"vehiclePreview");
                    RenderTexture.active=preview.Texture;
                    var tex=new Texture2D(768,768,TextureFormat.RGB24,false);
                    tex.ReadPixels(new Rect(0,0,768,768),0,0); tex.Apply();
                    int visible=0;foreach(var c in tex.GetPixels()) if(c.maxColorComponent>.3f)visible++;
                    Require(visible>3000,"Vehicle renders visibly: "+def.Id);
                    File.WriteAllBytes("Builds/vehicle-preview-review/"+shot+"-model.png",tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=null;
                    ScreenCapture.CaptureScreenshot("Builds/vehicle-preview-review/"+shot+"-screen.png");captured=true;
                }
                if(EditorApplication.timeSinceStartup-began<3)return;
                shot++;captured=false;began=EditorApplication.timeSinceStartup;
                if(shot<9)return;
                Debug.Log("VEHICLE_PREVIEW_CHECKS_PASSED all seven vehicles, auto/manual/keyboard rotation, selection persistence, briefing, preflight and camera lifecycle.");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code){preferences?.Dispose();SessionState.SetBool(Running,false);EditorApplication.update-=Poll;EditorApplication.Exit(code);}
    }
}
