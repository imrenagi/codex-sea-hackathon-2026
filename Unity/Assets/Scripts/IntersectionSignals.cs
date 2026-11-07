using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UjianSim.Game;

namespace JalanKita
{
    public sealed class IntersectionSignals
    {
        public readonly Camera Camera;
        public readonly RenderTexture Texture, ArrowTexture;
        public readonly Camera ArrowCamera;
        readonly Dictionary<int,(Vector3 main,Vector3 arrow,Vector3 forward)> targets=new();
        readonly List<Mesh> enlargedLenses=new();
        public bool Visible { get; private set; }
        public bool HasArrow { get; private set; }
        public int MainPhase { get; private set; }
        public int ArrowPhase { get; private set; }
        public static readonly Color[] Colors={new(1,.04f,.025f),new(1,.6f,.015f),new(.02f,1,.16f)};
        public IntersectionSignals(Transform parent)
        {
            Camera=new GameObject("Close-up lampu persimpangan").AddComponent<Camera>();
            Camera.transform.SetParent(parent,false);
            Texture=new RenderTexture(256,256,24) { name="Lampu persimpangan",antiAliasing=2 };
            Texture.Create(); Camera.targetTexture=Texture;
            Camera.orthographic=true; Camera.nearClipPlane=.1f; Camera.farClipPlane=18;
            Camera.depth=-9; Camera.enabled=false;
            Camera.clearFlags=CameraClearFlags.SolidColor; Camera.backgroundColor=new Color(.08f,.1f,.12f);
            var data=Camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderShadows=false; data.renderPostProcessing=false;
            ArrowCamera=Object.Instantiate(Camera, parent);
            ArrowCamera.name="Close-up panah kanan";
            ArrowTexture=new RenderTexture(256,256,24) { name="Panah kanan",antiAliasing=2 };
            ArrowTexture.Create(); ArrowCamera.targetTexture=ArrowTexture;
        }
        public void Register(int stage,Transform model,Vector3 forward)
        {
            Vector3 Center(string prefix)
            {
                Bounds bounds=default; bool found=false;
                foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    if(!filter.name.StartsWith(prefix)) continue;
                    foreach(var v in filter.sharedMesh.vertices)
                    {
                        var p=filter.transform.TransformPoint(v);
                        if(p.y<4) continue;
                        if(!found) { bounds=new Bounds(p,Vector3.zero); found=true; } else bounds.Encapsulate(p);
                    }
                }
                return bounds.center;
            }
            targets[stage]=(Center("Light_"),Center("Arrow_"),forward);
        }
        public static void Light(Material material,int color,bool on)
        {
            if(!material) return;
            material.SetColor("_BaseColor",on?Colors[color]:new Color(.012f,.016f,.02f));
            material.SetFloat("_Smoothness",0);
            material.EnableKeyword("_EMISSION");
            // Saturated lenses remain distinct in daylight without blooming to white.
            material.SetColor("_EmissionColor",on?Colors[color]*.8f:Color.black);
        }
        public void EnlargeLenses(Transform model)
        {
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.name.StartsWith("Light_") && !filter.name.StartsWith("Arrow_")) continue;
                var mesh=Object.Instantiate(filter.sharedMesh);
                mesh.name=filter.sharedMesh.name+" readable lenses";
                var vertices=mesh.vertices;
                // Joined FBX meshes contain both the overhead and pole lenses.
                for(int group=0;group<2;group++)
                {
                    Bounds bounds=default; bool found=false;
                    foreach(var v in vertices)
                    {
                        var p=filter.transform.TransformPoint(v);
                        if((p.y>4?1:0)!=group) continue;
                        if(!found) { bounds=new Bounds(p,Vector3.zero); found=true; } else bounds.Encapsulate(p);
                    }
                    if(!found) continue;
                    for(int i=0;i<vertices.Length;i++)
                    {
                        var p=filter.transform.TransformPoint(vertices[i]);
                        if((p.y>4?1:0)!=group) continue;
                        p=bounds.center+(p-bounds.center)*1.4f;
                        vertices[i]=filter.transform.InverseTransformPoint(p);
                    }
                }
                mesh.vertices=vertices; mesh.RecalculateBounds();
                filter.sharedMesh=mesh; enlargedLenses.Add(mesh);
            }
        }
        public void Present(Journey run,bool driving)
        {
            int stage=run.Stage;
            Visible=driving && targets.ContainsKey(stage);
            if(stage==1) Visible &= run.Position.y>RouteStage.StopJunction+7 && run.Position.y<RouteStage.LightJunction+9;
            else if(stage>=5&&stage<=7)
                Visible &= Vector2.Distance(run.Position,Boulevard.Center(stage))<110
                    && Vector2.Dot(run.Position-Boulevard.Center(stage),Boulevard.Exit(stage))<22;
            HasArrow=stage>=5 && stage<=7;
            Camera.enabled=Visible; ArrowCamera.enabled=Visible && HasArrow;
            if(!Visible) return;
            MainPhase=HasArrow?run.CitySignal(stage,0):run.LightPhase;
            ArrowPhase=stage==7?run.ArrowPhase:0;
            var (main,arrow,forward)=targets[stage];
            Camera.orthographicSize=ArrowCamera.orthographicSize=.68f;
            Camera.transform.SetPositionAndRotation(main-forward*8,Quaternion.LookRotation(forward));
            ArrowCamera.transform.SetPositionAndRotation(arrow-forward*8,Quaternion.LookRotation(forward));
        }
        public void Dispose()
        {
            Camera.targetTexture=null; Texture.Release(); Object.Destroy(Texture);
            ArrowCamera.targetTexture=null; ArrowTexture.Release(); Object.Destroy(ArrowTexture);
            foreach(var mesh in enlargedLenses) Object.Destroy(mesh);
        }
    }
}
