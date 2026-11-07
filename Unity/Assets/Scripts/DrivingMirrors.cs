using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JalanKita
{
    // Ujian SIM MirrorRig camera settings, with a centre rear view for either vehicle.
    public sealed class DrivingMirrors
    {
        public readonly Camera[] Cameras = new Camera[3];
        public readonly RenderTexture[] Textures = new RenderTexture[3];
        readonly Transform root;
        bool active, fresh;
        int turn;
        public DrivingMirrors(Transform parent)
        {
            root=new GameObject("Spion kendaraan").transform;
            root.SetParent(parent,false);
            for(int i=0;i<3;i++)
            {
                var go=new GameObject(new[] { "Spion kiri","Spion tengah","Spion kanan" }[i]);
                go.transform.SetParent(root,false);
                var cam=go.AddComponent<Camera>();
                var texture=new RenderTexture(i==1?384:256,i==1?128:160,24,RenderTextureFormat.ARGB32)
                    { name=go.name,antiAliasing=2 };
                texture.Create();
                cam.targetTexture=texture;
                cam.fieldOfView=i==1?18:24;
                cam.nearClipPlane=.05f;
                cam.farClipPlane=220;
                cam.depth=-10;
                cam.enabled=false;
                var cull=new float[32];
                for(int layer=0;layer<cull.Length;layer++) cull[layer]=110;
                cam.layerCullDistances=cull;
                var data=go.AddComponent<UniversalAdditionalCameraData>();
                data.renderShadows=false;
                data.renderPostProcessing=true;
                Cameras[i]=cam; Textures[i]=texture;
            }
        }
        public void Present(Journey run,bool visible,bool snap)
            => Present(run.Mode, run.Position, run.Heading, visible, snap);

        public void Present(VehicleMode mode, Vector2 position, float heading, bool visible, bool snap)
        {
            root.SetPositionAndRotation(new Vector3(position.x,0,position.y),Quaternion.Euler(0,heading,0));
            bool motorcycle=mode==VehicleMode.Motorcycle;
            for(int i=0;i<3;i++)
            {
                float side=i-1;
                Cameras[i].transform.localPosition=i==1 ? new Vector3(0,motorcycle?1.55f:1.4f,motorcycle?-1.3f:-2.3f)
                    : new Vector3(side*(motorcycle?.35f:1.1f),motorcycle?1.3f:1.05f,motorcycle?.34f:.8f);
                Cameras[i].transform.localRotation=Quaternion.Euler(3,180-side*(motorcycle?16:12),0);
            }
            fresh |= visible && (!active || snap);
            active=visible;
            turn=(turn+1)%3;
            for(int i=0;i<3;i++) Cameras[i].enabled=active && (fresh || i==turn);
            fresh=false;
        }
        public void Dispose()
        {
            for(int i=0;i<3;i++)
            {
                if(Cameras[i]) Cameras[i].targetTexture=null;
                if(Textures[i]) { Textures[i].Release(); Object.Destroy(Textures[i]); }
            }
        }
    }
}
