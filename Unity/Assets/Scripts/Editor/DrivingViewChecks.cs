using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UjianSim.Game;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class DrivingViewChecks
    {
        const string Running="JalanKita.DrivingViewChecks";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static JalanGame game;
        static Journey run;
        static District district;
        static SoundEngine sound;
        static double began;
        static bool ready, hadMute;
        static int previousMute;
        static readonly float[] output=new float[2048];
        static readonly float[] listenerOutput=new float[2048];
        static DrivingViewChecks() { if(SessionState.GetBool(Running,false)) EditorApplication.update+=Poll; }
        public static void Run()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Run without -nographics or -batchmode to verify real mirror/audio output.");
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            SessionState.SetBool(Running,true);
            EditorApplication.EnterPlaymode();
        }
        static void Poll()
        {
            if(!EditorApplication.isPlaying) return;
            try
            {
                if(!ready)
                {
                    game=UnityEngine.Object.FindFirstObjectByType<JalanGame>();
                    if(!game || typeof(JalanGame).GetField("journey",Private).GetValue(game)==null) return;
                    game.enabled=false;
                    run=new Journey(VehicleMode.Car,new JourneySave { next=9 },persistent:false);
                    for(int i=0;i<10000 && !run.Failure.HasValue;i++)
                    {
                        DrivingCoach.Step(run,.02f);
                        if(run.Ambulance.HasValue && run.AmbulanceGap<40) break;
                    }
                    if(!run.Ambulance.HasValue || run.Failure.HasValue) throw new InvalidOperationException("Ambulance preview failed");
                    typeof(JalanGame).GetField("journey",Private).SetValue(game,run);
                    var page=typeof(JalanGame).GetNestedType("Page",BindingFlags.NonPublic);
                    typeof(JalanGame).GetMethod("SetPage",Private).Invoke(game,new[] { Enum.Parse(page,"Drive") });
                    district=(District)typeof(JalanGame).GetField("district",Private).GetValue(game);
                    sound=(SoundEngine)typeof(JalanGame).GetField("sound",Private).GetValue(game);
                    hadMute=PlayerPrefs.HasKey("jalan-kita:muted"); previousMute=PlayerPrefs.GetInt("jalan-kita:muted");
                    sound.SetMuted(false);
                    district.Snap();
                    began=EditorApplication.timeSinceStartup;
                    ready=true;
                }
                district.Present(run,false,.02f,true);
                sound.UpdateFrame(.02f,new SoundFrame { Motorcycle=false, Speed=run.Speed, Throttle=0,
                    SirenDist=Mathf.Abs(run.AmbulanceGap), BellDist=float.PositiveInfinity });
                typeof(JalanGame).GetMethod("RefreshHud",Private).Invoke(game,null);
                sound.GetComponent<AudioSource>().GetOutputData(output,0);
                AudioListener.GetOutputData(listenerOutput,0);
                if(EditorApplication.timeSinceStartup-began<3) return;
                Directory.CreateDirectory("Builds/view-review");
                double energy=0; foreach(float sample in output) energy+=sample*sample;
                double rms=Math.Sqrt(energy/output.Length);
                double listenerEnergy=0; foreach(float sample in listenerOutput) listenerEnergy+=sample*sample;
                double listenerRms=Math.Sqrt(listenerEnergy/listenerOutput.Length);
                Debug.Log($"Live audio: frames={sound.RenderedFrames}, RMS={rms:F4}, listenerRMS={listenerRms:F4}, peak={sound.OutputPeak}, muted={sound.Muted}, listenerVolume={AudioListener.volume}, paused={AudioListener.pause}, playing={sound.GetComponent<AudioSource>().isPlaying}, virtual={sound.GetComponent<AudioSource>().isVirtual}");
                if(sound.RenderedFrames==0 || listenerRms<.015) throw new InvalidOperationException("Live ambulance audio output is silent or too quiet");
                foreach(var texture in district.Mirrors.Textures)
                {
                    var before=RenderTexture.active;
                    RenderTexture.active=texture;
                    var image=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); image.Apply();
                    var pixels=image.GetPixels(); float min=1,max=0;
                    foreach(var color in pixels) { min=Mathf.Min(min,color.grayscale); max=Mathf.Max(max,color.grayscale); }
                    if(max-min<.05f) throw new InvalidOperationException("Mirror rendered a blank image: "+texture.name);
                    File.WriteAllBytes("Builds/view-review/"+texture.name+".png",image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image); RenderTexture.active=before;
                }
                File.WriteAllText("Builds/view-review/result.txt",$"PASS: three live mirror images; real audio callback frames={sound.RenderedFrames}; listener ambulance RMS={listenerRms:F4}");
                Finish(0);
            }
            catch(Exception error) { Debug.LogException(error); Finish(1); }
        }
        static void Finish(int code)
        {
            if(ready)
            {
                if(hadMute) PlayerPrefs.SetInt("jalan-kita:muted",previousMute); else PlayerPrefs.DeleteKey("jalan-kita:muted");
                PlayerPrefs.Save();
            }
            SessionState.SetBool(Running,false);
            EditorApplication.update-=Poll;
            EditorApplication.Exit(code);
        }
    }
}
