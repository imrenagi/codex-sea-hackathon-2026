using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JalanKita
{
    // One low-resolution planar capture is shared by all coplanar pools in an area.
    // Time comes from the driving simulation, so water and spray freeze with pause.
    public sealed class LandmarkWater : MonoBehaviour
    {
        readonly List<Material> surfaces=new();
        readonly List<Vector3> nozzles=new();
        readonly List<Vector3> velocities=new();
        readonly List<Mesh> ownedMeshes=new();
        readonly List<Vector3> poolCenters=new();
        Camera reflection;
        RenderTexture reflectionTexture;
        Material jetMaterial, sprayMaterial;
        ParticleSystem spray;
        ParticleSystem.Particle[] drops;
        AudioSource fountainSound;
        AudioClip fountainClip;
        bool audioPlaying;
        float height, lastCapture=-100;
        Vector3 lastEye;
        Quaternion lastRotation;
        readonly Plane[] frustum=new Plane[6];
        public float SimulationTime {get;private set;}
        public int ReflectionFrames {get;private set;}
        public int PoolCount=>surfaces.Count;
        public int DropletCount=>drops?.Length??0;

        public void AddPool(Vector3 center,float radius,bool large)
        {
            poolCenters.Add(center);
            height=center.y;
            var mat=new Material(Resources.Load<Shader>("JakartaLandmarks/Shaders/ReflectiveWater")){name="Animated pool water"};
            mat.SetVector("_Pool",new Vector4(center.x,center.z,radius,0));mat.SetFloat("_ImpactRadius",large?28:4);
            surfaces.Add(mat);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            vertices.Add(center);uv.Add(Vector2.zero);
            const int segments=192;
            for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add(center+new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius));uv.Add(new Vector2(Mathf.Sin(a),Mathf.Cos(a)));}
            for(int i=1;i<=segments;i++){triangles.Add(0);triangles.Add(i);triangles.Add(i+1);}
            MeshObject("Reflective pool",vertices,uv,triangles,mat);
            if(!jetMaterial)
            {
                jetMaterial=new Material(Resources.Load<Shader>("JakartaLandmarks/Shaders/Fountain")){name="Flowing fountain streams"};
                sprayMaterial=new Material(jetMaterial){name="Soft fountain droplets"};sprayMaterial.SetFloat("_Droplet",1);sprayMaterial.SetColor("_BaseColor",new Color(.88f,.95f,1,.58f));
            }
            vertices.Clear();uv.Clear();triangles.Clear();
            int jets=large?64:18;
            for(int i=0;i<jets;i++)
            {
                float a=i*Mathf.PI*2/jets;var radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                var start=center+radial*(large?36:8)+Vector3.up*.07f;
                var velocity=-radial*(large?5.1f:2.9f)+Vector3.up*(large?7.7f:6.6f);
                nozzles.Add(start);velocities.Add(velocity);
                float duration=velocity.y*2/9.81f;
                const int steps=24,sides=6;
                for(int k=0;k<=steps;k++)
                {
                    float t=k/(float)steps*duration;
                    var p=start+velocity*t-Vector3.up*(4.905f*t*t);
                    var direction=(velocity-Vector3.up*(9.81f*t)).normalized;
                    var tangent=Vector3.Cross(direction,Vector3.right).normalized;var bitangent=Vector3.Cross(direction,tangent);
                    float width=Mathf.Lerp(.055f,.035f,k/(float)steps);
                    for(int j=0;j<=sides;j++)
                    {
                        float angle=j*Mathf.PI*2/sides;vertices.Add(p+(tangent*Mathf.Sin(angle)+bitangent*Mathf.Cos(angle))*width);uv.Add(new Vector2(j/(float)sides,k/(float)steps));
                        if(k==steps||j==sides)continue;
                        int n=vertices.Count-1;triangles.Add(n);triangles.Add(n+sides+1);triangles.Add(n+1);triangles.Add(n+1);triangles.Add(n+sides+1);triangles.Add(n+sides+2);
                    }
                }
            }
            MeshObject("Parabolic fountain jets",vertices,uv,triangles,jetMaterial);
        }
        void MeshObject(string label,List<Vector3> vertices,List<Vector2> uv,List<int> triangles,Material material)
        {
            var go=new GameObject(label);go.layer=4;go.transform.SetParent(transform,false);
            var mesh=new Mesh{name=label};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        public void Complete()
        {
            var go=new GameObject("Fountain spray");go.layer=4;go.transform.SetParent(transform,false);spray=go.AddComponent<ParticleSystem>();
            var main=spray.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=nozzles.Count*12;
            var emission=spray.emission;emission.enabled=false;var shape=spray.shape;shape.enabled=false;
            spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);drops=new ParticleSystem.Particle[nozzles.Count*12];
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=sprayMaterial;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
            reflection=new GameObject("Pool reflection camera").AddComponent<Camera>();reflection.transform.SetParent(transform,false);reflection.enabled=false;
            reflectionTexture=new RenderTexture(640,360,24,RenderTextureFormat.ARGBHalf){name="Jakarta water reflection",useMipMap=false};reflectionTexture.Create();
            var data=reflection.GetUniversalAdditionalCameraData();data.renderShadows=false;data.renderPostProcessing=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
            foreach(var mat in surfaces)mat.SetTexture("_ReflectionTex",reflectionTexture);
            if(Application.isPlaying)
            {
                // Original deterministic filtered noise; no downloaded audio asset.
                const int samples=88200;var dataSamples=new float[samples];var random=new System.Random(1962);float low=0;
                for(int i=0;i<samples;i++){float white=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,white,.12f);dataSamples[i]=(white*.1f+low*.8f)*(.72f+.12f*Mathf.Sin(i*Mathf.PI*8/samples));}
                const int fade=128;
                for(int i=0;i<fade;i++){float envelope=i/(float)fade;dataSamples[i]*=envelope;dataSamples[samples-1-i]*=envelope;}
                fountainClip=AudioClip.Create("Fountain water ambience",samples,1,22050,false);fountainClip.SetData(dataSamples,0);
                fountainSound=new GameObject("Spatial fountain audio").AddComponent<AudioSource>();fountainSound.transform.SetParent(transform,false);
                fountainSound.clip=fountainClip;fountainSound.loop=true;fountainSound.playOnAwake=false;fountainSound.spatialBlend=1;fountainSound.dopplerLevel=0;
                fountainSound.rolloffMode=AudioRolloffMode.Linear;fountainSound.minDistance=18;fountainSound.maxDistance=160;fountainSound.volume=0;
            }
        }
        public void SetAudio(float volume,bool playing)
        {
            if(!fountainSound)return;
            fountainSound.volume=Mathf.Clamp01(volume)*.18f;
            if(playing&&!audioPlaying){if(fountainSound.timeSamples>0)fountainSound.UnPause();else fountainSound.Play();}
            if(!playing&&audioPlaying)fountainSound.Pause();
            audioPlaying=playing;
        }
        public void Present(Camera source,float time,bool forceReflection=false)
        {
            SimulationTime=time;
            if(fountainSound)
            {
                Vector3 nearest=poolCenters[0];foreach(var pool in poolCenters)if((pool-source.transform.position).sqrMagnitude<(nearest-source.transform.position).sqrMagnitude)nearest=pool;
                fountainSound.transform.position=nearest+Vector3.up*1.5f;
            }
            foreach(var mat in surfaces)mat.SetFloat("_WaterTime",time);
            jetMaterial.SetFloat("_WaterTime",time);sprayMaterial.SetFloat("_WaterTime",time);
            for(int i=0;i<drops.Length;i++)
            {
                int nozzle=i/12,j=i%12;float life=velocities[nozzle].y*2/9.81f;float age=Mathf.Repeat(time+j*life/12+nozzle*.137f,life);
                float spread=Mathf.Max(0,age/life-.5f)*.32f;
                var offset=new Vector3(Mathf.Sin(i*12.4f+age*4),.2f*Mathf.Sin(i*3.7f),Mathf.Cos(i*8.3f+age*3))*spread;
                drops[i].position=nozzles[nozzle]+velocities[nozzle]*age-Vector3.up*(4.905f*age*age)+offset;
                drops[i].startColor=Color.white;drops[i].startSize=.055f+age/life*.09f;drops[i].startLifetime=2;drops[i].remainingLifetime=1;
            }
            spray.SetParticles(drops,drops.Length);
            if(!forceReflection)
            {
                GeometryUtility.CalculateFrustumPlanes(source,frustum);
                bool visible=false;
                for(int i=0;i<poolCenters.Count;i++)
                {
                    float radius=surfaces[i].GetVector("_Pool").z;
                    visible|=GeometryUtility.TestPlanesAABB(frustum,new Bounds(poolCenters[i],new Vector3(radius*2,8,radius*2)));
                }
                if(!visible)return;
            }
            bool changed=Vector3.Distance(lastEye,source.transform.position)>.08f||Quaternion.Angle(lastRotation,source.transform.rotation)>.15f;
            if(!forceReflection&&(!changed||time-lastCapture<.09f)&&ReflectionFrames>0)return;
            lastCapture=time;lastEye=source.transform.position;lastRotation=source.transform.rotation;
            reflection.CopyFrom(source);reflection.enabled=false;reflection.targetTexture=reflectionTexture;reflection.cullingMask=source.cullingMask&~(1<<4);
            reflection.allowMSAA=false;reflection.farClipPlane=Mathf.Min(source.farClipPlane,850);reflection.useOcclusionCulling=false;
            var mirror=Matrix4x4.identity;mirror.m11=-1;mirror.m13=height*2;
            reflection.transform.position=mirror.MultiplyPoint(source.transform.position);
            var euler=source.transform.eulerAngles;reflection.transform.eulerAngles=new Vector3(-euler.x,euler.y,-euler.z);
            reflection.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
            var point=reflection.worldToCameraMatrix.MultiplyPoint(new Vector3(0,height+.025f,0));
            var normal=reflection.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            reflection.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(point,normal)));
            bool invert=GL.invertCulling;
            try
            {
                GL.invertCulling=!invert;
                RenderPipeline.SubmitRenderRequest(reflection,new UniversalRenderPipeline.SingleCameraRequest{destination=reflectionTexture});
                var vp=GL.GetGPUProjectionMatrix(reflection.projectionMatrix,true)*reflection.worldToCameraMatrix;
                foreach(var mat in surfaces){mat.SetMatrix("_ReflectionVP",vp);mat.SetFloat("_HasReflection",1);}
                ReflectionFrames++;
            }
            finally{GL.invertCulling=invert;}
        }
        void OnDestroy()
        {
            if(reflectionTexture){reflectionTexture.Release();Destroy(reflectionTexture);}
            foreach(var material in surfaces)if(material)Destroy(material);
            if(jetMaterial)Destroy(jetMaterial);if(sprayMaterial)Destroy(sprayMaterial);
            foreach(var mesh in ownedMeshes)if(mesh)Destroy(mesh);
            if(fountainClip)Destroy(fountainClip);
        }
        void OnDisable(){if(fountainSound)fountainSound.Stop();audioPlaying=false;}
    }
}
