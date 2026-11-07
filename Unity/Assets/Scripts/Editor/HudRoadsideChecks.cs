using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    [InitializeOnLoad]
    public static class HudRoadsideChecks
    {
        const string Running="RoadSense.HudRoadsideChecks";
        const string Output="Builds/HudRoadsideChecks";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly Vector2Int[] Sizes={new(1280,720),new(1920,1080),new(800,600)};
        static readonly string[] Notices={
            "LATIHAN MISI 04 · Progres perjalanan tersimpan",
            "LATIHAN MISI 04 · PROGRES PERJALANAN TETAP TERSIMPAN",
            "Mengerem. Tunggu sampai orang selesai menyeberang.",
            "Bagus! Lanjut berkendara. Aturan normal sekarang aktif.",
            "Lepaskan tombol, lalu mulai berkendara. F1: panduan",
            "Latihan selesai. Kembali ke awal; aturan normal aktif."};
        static JalanGame game;
        static UIDocument document;
        static PanelSettings originalPanel, capturePanel;
        static RenderTexture frame;
        static IntegrationChecks.SavedPreferences preferences;
        static int sizeIndex, noticeIndex, frames;
        static double deadline, nextCheck;
        static readonly List<string> evidence=new();
        static HudRoadsideChecks(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Poll;}
        static object Field(object target,string name)=>target.GetType().GetField(name,Private).GetValue(target);
        static void Call(string name,params object[] args)=>typeof(JalanGame).GetMethod(name,Private).Invoke(game,args);
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException("HUD_ROADSIDE: "+message);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            SessionState.SetBool(Running,true);EditorApplication.EnterPlaymode();
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
                    game.enabled=false;preferences=new IntegrationChecks.SavedPreferences();
                    Directory.CreateDirectory(Output);VerifyRoadside();
                    document=game.GetComponent<UIDocument>();originalPanel=document.panelSettings;
                    capturePanel=UnityEngine.Object.Instantiate(originalPanel);document.panelSettings=capturePanel;
                    Call("JumpToMission",3);Call("RefreshHud");
                    var district=(District)Field(game,"district");district.Snap();district.Present((Journey)Field(game,"journey"),false,0);
                    deadline=EditorApplication.timeSinceStartup+60;SetSize();
                }
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("HUD render layout timed out");
                if(++frames<8 || EditorApplication.timeSinceStartup<nextCheck)return;
                frames=0;
                var root=document.rootVisualElement;
                root.panel.GetType().GetMethod("ValidateLayout",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(root.panel,null);
                var toast=root.Q<Label>("toast");
                Fits(toast,"toast");
                if(noticeIndex%2==0)Fits(root.Q<Label>("hudHint"),"hudHint");
                else Fits(root.Q<Label>("cueCopy"),"cueCopy");
                if(noticeIndex<2)SaveFrame($"badge-{Sizes[sizeIndex].x}x{Sizes[sizeIndex].y}-{noticeIndex}.png");
                if(++noticeIndex<Notices.Length){SetNotice();return;}
                noticeIndex=0;
                if(++sizeIndex<Sizes.Length){SetSize();return;}
                File.WriteAllLines(Output+"/validation.txt",evidence);
                Debug.Log("HUD_ROADSIDE_CHECKS_PASSED: 18 toast layouts plus HUD hints/cues at1280x720,1920x1080,800x600; market tree/pole/cables hide and restore; all active low tree/pole/lamp vertices clear road/market/spur corridors.");
                Finish(0);
            }
            catch(Exception error){Debug.LogException(error);Finish(1);}
        }
        static void SetSize()
        {
            if(frame){Camera.main.targetTexture=null;capturePanel.targetTexture=null;frame.Release();UnityEngine.Object.DestroyImmediate(frame);}
            var size=Sizes[sizeIndex];frame=new RenderTexture(size.x,size.y,24){name="HUD validation "+size};frame.Create();
            capturePanel.targetTexture=frame;Camera.main.targetTexture=frame;
            SetNotice();
        }
        static void SetNotice()
        {
            nextCheck=EditorApplication.timeSinceStartup+.25;
            var root=document.rootVisualElement;
            var toast=root.Q<Label>("toast");toast.text=Notices[noticeIndex];toast.RemoveFromClassList("hidden");
            var hint=root.Q<Label>("hudHint");hint.text="Gas W / ↑ · S / ↓ rem lalu mundur · Spasi rem saja · F1 panduan";hint.EnableInClassList("hidden",noticeIndex%2!=0);
            root.Q<Label>("cueKeys").text="SPASI / S";
            root.Q<Label>("cueCopy").text="Rem sebelum area berhenti dan tunggu sampai jalur aman.";
            root.Q("progressiveCue").EnableInClassList("hidden",noticeIndex%2==0);
        }
        static void Fits(Label label,string name)
        {
            var available=label.contentRect;
            var required=label.MeasureTextSize(label.text,available.width,VisualElement.MeasureMode.Exactly,0,VisualElement.MeasureMode.Undefined);
            Require(available.width>50 && available.height>=required.y-1,$"{name} clipped: content={available}, measured={required}, text={label.text}");
            var screen=document.rootVisualElement.worldBound;var bounds=label.worldBound;
            Require(bounds.xMin>=screen.xMin-1 && bounds.xMax<=screen.xMax+1 && bounds.yMin>=screen.yMin-1 && bounds.yMax<=screen.yMax+1,
                name+" outside screen: "+bounds+" / "+screen);
            evidence.Add($"{Sizes[sizeIndex]} {name}: bounds={bounds} content={available} measured={required} text={label.text}");
        }
        static void SaveFrame(string name)
        {
            var old=RenderTexture.active;RenderTexture.active=frame;
            var image=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);image.Apply();
            File.WriteAllBytes(Output+"/"+name,image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
        }
        static void VerifyRoadside()
        {
            var district=(District)Field(game,"district");
            var curb=(GameObject)Field(district,"marketCurb");
            var tree=curb.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Tree_Shade_1" && t.parent==curb.transform);
            var pole=curb.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="PowerPole" && t.parent==curb.transform);
            Require(Vector3.Distance(tree.position,new(-6.1f,.13f,265))<.01f,"Original market tree remains at normal sidewalk position");
            Require(Vector3.Distance(pole.position,new(-7.1f,.13f,270))<.01f,"Original market pole preserved");
            var wires=curb.GetComponentsInChildren<LineRenderer>(true);
            Require(wires.Length==4,"Both incoming/outgoing pole cable pairs follow market visibility");
            var normal=new Journey(VehicleMode.Car,new JourneySave{next=3},false,trafficSeed:173);
            district.Present(normal,false,0);
            Require(tree.gameObject.activeInHierarchy&&pole.gameObject.activeInHierarchy&&wires.All(w=>w.gameObject.activeInHierarchy),"Normal-route roadside scenery remains visible");
            CheckCorridors(false);
            var market=new Journey(VehicleMode.Car,Mission.Scenarios[2].NewSave(),false,Mission.Scenarios[2],173);
            typeof(Journey).GetProperty("Position").SetValue(market,new Vector2(-5.8f,264));
            VehicleCatalogue.Select(VehicleCatalogue.All.First(v=>v.Id=="sedan"));
            district.Snap();district.Present(market,false,0);
            Require(!tree.gameObject.activeInHierarchy&&!pole.gameObject.activeInHierarchy&&wires.All(w=>!w.gameObject.activeInHierarchy),"Pickup bay hides tree, pole and all connected wires");
            CheckCorridors(true);
            CaptureRoad("market-bay.png",new Vector3(-5.8f,4.2f,255),new Vector3(-5.8f,1.5f,268));
            CaptureRoad("spur-clearance.png",new Vector3(7.2f,4,247),new Vector3(7.2f,1.5f,267));
            district.Present(normal,false,0);
            Require(tree.gameObject.activeInHierarchy&&pole.gameObject.activeInHierarchy&&wires.All(w=>w.gameObject.activeInHierarchy),"Leaving market restores all normal-route props");
            evidence.Add("Tree(-6.1,.13,265), pole(-7.1,.13,270),4 cable spans: normal visible / market hidden / normal restored.");
            evidence.Add("Market render: player(-5.8,264),camera(-5.8,4.2,255),target(-5.8,1.5,268). Spur camera(7.2,4,247),target(7.2,1.5,267).");
        }
        static void CheckCorridors(bool market)
        {
            var training=(GameObject)Field(game,"trainingRoot");int count=0,points=0;
            foreach(var node in training.GetComponentsInChildren<Transform>())
            {
                if(!(node.name.StartsWith("Tree_")||node.name=="PowerPole"||node.name=="StreetLamp"||node.name=="Kenanga branching street tree"))continue;
                if(node.parent&&(node.parent.name.StartsWith("Tree_")||node.parent.name==node.name))continue;
                count++;
                foreach(var renderer in node.GetComponentsInChildren<MeshRenderer>())
                {
                    var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                    var mesh=filter.sharedMesh;var vertices=mesh.vertices;
                    for(int slot=0;slot<renderer.sharedMaterials.Length;slot++)
                    {
                        int submesh=renderer.isPartOfStaticBatch?renderer.subMeshStartIndex+slot:slot;
                        foreach(int i in mesh.GetIndices(submesh).Distinct())
                        {
                            var p=renderer.localToWorldMatrix.MultiplyPoint3x4(vertices[i]);if(p.y<.05f||p.y>2.6f)continue;
                            points++;
                            bool main=Mathf.Abs(p.x)<4.75f&&p.z>-28&&p.z<382;
                            bool spur=p.x>4.675f&&p.x<9.025f&&p.z>=RouteStage.SpurStart&&p.z<=RouteStage.SpurEnd+1;
                            bool bay=market&&p.x>-7.85f&&p.x<-4.45f&&p.z>=Mission.PickupStart&&p.z<=Mission.PickupEnd;
                            Require(!main&&!spur&&!bay,$"Active {node.name} low geometry blocks road at{p} (market={market})");
                        }
                    }
                }
            }
            evidence.Add($"Corridor check market={market}: {count} roadside prop roots, {points} low vertices, no active road intrusion.");
        }
        static void CaptureRoad(string name,Vector3 position,Vector3 target)
        {
            var camera=Camera.main;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var texture=new RenderTexture(1280,720,24);texture.Create();camera.targetTexture=texture;
            camera.transform.position=position;camera.transform.LookAt(target);
            bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            for(int i=0;i<3;i++)camera.Render();
            var old=RenderTexture.active;RenderTexture.active=texture;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name,image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);ShaderUtil.allowAsyncCompilation=async;
        }
        static void Finish(int code)
        {
            preferences?.Dispose();
            if(document)document.panelSettings=originalPanel;
            if(Camera.main)Camera.main.targetTexture=null;
            if(frame){frame.Release();UnityEngine.Object.DestroyImmediate(frame);}
            if(capturePanel)UnityEngine.Object.DestroyImmediate(capturePanel);
            SessionState.SetBool(Running,false);EditorApplication.update-=Poll;EditorApplication.Exit(code);
        }
    }
}
