using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace JalanKita
{
    public sealed class CityMapPreview : IDisposable
    {
        readonly GameObject root;
        readonly Camera camera;
        readonly List<Mesh> meshes = new();
        readonly List<Material> materials = new();
        readonly bool fog;
        readonly Light previousSun;
        readonly AmbientMode previousAmbientMode;
        readonly Color previousAmbientLight;
        Vector3 focus;
        float distance = 1400;
        public CityMapPreview(Transform parent, CityMapData map)
        {
            fog = RenderSettings.fog; RenderSettings.fog = false;
            previousSun = RenderSettings.sun;
            previousAmbientMode = RenderSettings.ambientMode;
            previousAmbientLight = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.52f,.57f,.61f);
            root = new GameObject("OSM city preview"); root.transform.SetParent(parent,false);
            try
            {
            camera = new GameObject("City overview camera").AddComponent<Camera>();
            camera.gameObject.AddComponent<AudioListener>();
            camera.transform.SetParent(root.transform,false); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f,.20f,.24f);
            camera.nearClipPlane = 1; camera.farClipPlane = 12000; camera.fieldOfView = 50;
            var sun = new GameObject("City sunlight").AddComponent<Light>();
            sun.transform.SetParent(root.transform,false); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48,-35,0); sun.intensity = 1.05f;
            sun.color = new Color(1f,.96f,.89f); RenderSettings.sun = sun;
            var ground = new Batch(this,"Ground",new Color(.40f,.49f,.39f));
            ground.Quad(new(-864,-.2f,-735),new(-864,-.2f,735),new(864,-.2f,735),new(864,-.2f,-735)); ground.Flush();
            var roads = new Batch(this,"Roads",new Color(.18f,.22f,.24f));
            foreach (var road in map.roads)
            {
                if (road.points == null || road.tunnel == "yes") continue;
                float width = road.highway switch { "motorway" or "trunk" => 13, "primary" => 10, "secondary" => 8,
                    "tertiary" => 7, "footway" or "path" or "steps" => 2, _ => 5 };
                float y = .05f;
                if (float.TryParse(road.layer,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float layer)) y += Mathf.Max(0,layer)*4;
                for (int i=1;i<road.points.Length;i++)
                {
                    Vector3 a=road.points[i-1].At(y), b=road.points[i].At(y);
                    if ((b-a).sqrMagnitude < .001f) continue;
                    Vector3 normal=Vector3.Cross((b-a).normalized,Vector3.up)*width*.5f;
                    roads.Polygon(Clip(new[] { a-normal,b-normal,b+normal,a+normal }));
                }
                // Round joins close the outer wedges at bends and source-way endpoints.
                foreach (var point in road.points)
                {
                    var join=new Vector3[8];
                    for (int n=0;n<join.Length;n++)
                    {
                        float angle=n*Mathf.PI*2/join.Length;
                        join[n]=point.At(y)+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*width*.5f;
                    }
                    roads.Polygon(Clip(join));
                }
            }
            roads.Flush();
            var facades = new[] {
                new Batch(this,"Ivory facades",new Color(.82f,.77f,.64f)),
                new Batch(this,"Sage facades",new Color(.56f,.66f,.57f)),
                new Batch(this,"Clay facades",new Color(.75f,.59f,.47f)),
                new Batch(this,"Concrete facades",new Color(.67f,.71f,.70f)) };
            var roofs = new Batch(this,"Terracotta roofs",new Color(.52f,.24f,.15f));
            var tallRoofs = new Batch(this,"Tower roofs",new Color(.37f,.43f,.45f));
            var roofTrim = new Batch(this,"Roof edge trim",new Color(.89f,.85f,.73f));
            var windows = new Batch(this,"Windows and shopfronts",new Color(.13f,.29f,.34f));
            foreach (var building in map.buildings)
            {
                if (building.points == null || building.points.Length < 3) continue;
                var outline=new List<Vector3>();
                foreach (var p in building.points) outline.Add(p.At(0));
                var clipped=Clip(outline.ToArray());
                if (clipped.Length < 3) continue;
                float signedArea=0;
                for (int i=0;i<clipped.Length;i++)
                {
                    var a=clipped[i]; var b=clipped[(i+1)%clipped.Length];
                    signedArea+=a.x*b.z-b.x*a.z;
                }
                if (signedArea<0) Array.Reverse(clipped);
                float height=Mathf.Clamp(building.height,1,1000);
                uint hash=2166136261;
                foreach (char c in building.osmId ?? "") hash=(hash^c)*16777619;
                var buildings=facades[(int)(hash%(uint)facades.Length)];
                var roof=new Vector3[clipped.Length];
                for (int i=0;i<clipped.Length;i++)
                {
                    var a=clipped[i]; var b=clipped[(i+1)%clipped.Length];
                    buildings.Quad(a,a+Vector3.up*height,b+Vector3.up*height,b);
                    roof[i]=a+Vector3.up*height;
                    // Illustrative facade detail, not surveyed window or storefront locations.
                    Vector3 edge=b-a;
                    float length=edge.magnitude;
                    if (length<3) continue;
                    Vector3 along=edge.normalized;
                    Vector3 offset=Vector3.Cross(Vector3.up,along)*.04f;
                    // Illustrative cornices make adjoining roof silhouettes legible at overview scale.
                    Vector3 top=Vector3.up*height;
                    Vector3 band=Vector3.up*Mathf.Min(.35f,height*.1f);
                    roofTrim.Quad(a+top-band+offset,a+top+offset,b+top+offset,b+top-band+offset);
                    int columns=Mathf.Min(24,Mathf.FloorToInt(length/3));
                    int floors=Mathf.Min(18,Mathf.FloorToInt(height/3));
                    for (int floor=0;floor<floors;floor++)
                        for (int col=0;col<columns;col++)
                        {
                            Vector3 center=a+along*(length*(col+.5f)/columns)+Vector3.up*((floor+.5f)*height/floors);
                            Vector3 half=along*Mathf.Min(.7f,length/columns*.3f);
                            Vector3 up=Vector3.up*.65f;
                            windows.Quad(center-half-up+offset,center-half+up+offset,center+half+up+offset,center+half-up+offset);
                        }
                }
                (height>18 ? tallRoofs : roofs).Polygon(roof);
            }
            foreach (var facade in facades) facade.Flush();
            roofs.Flush(); tallRoofs.Flush(); roofTrim.Flush(); windows.Flush(); MoveCamera();
            }
            catch { Dispose(); throw; }
        }
        public static Vector3[] Clip(Vector3[] input)
        {
            var polygon=new List<Vector3>(input);
            for (int edge=0;edge<4;edge++)
            {
                var output=new List<Vector3>();
                if (polygon.Count == 0) break;
                float Bound(Vector3 p) => edge switch { 0 => p.x+864, 1 => 864-p.x, 2 => p.z+735, _ => 735-p.z };
                var previous=polygon[polygon.Count-1]; float dp=Bound(previous);
                foreach (var current in polygon)
                {
                    float dc=Bound(current);
                    if ((dp>=0)!=(dc>=0)) output.Add(Vector3.Lerp(previous,current,dp/(dp-dc)));
                    if (dc>=0) output.Add(current);
                    previous=current; dp=dc;
                }
                polygon=output;
            }
            for (int i=polygon.Count-1;i>=0 && polygon.Count>1;i--)
                if ((polygon[i]-polygon[(i+polygon.Count-1)%polygon.Count]).sqrMagnitude<.0001f) polygon.RemoveAt(i);
            return polygon.ToArray();
        }
        public void Step(float dt)
        {
            float x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
            float z=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
            focus+=new Vector3(x,0,z)*distance*dt*.5f;
            focus.x=Mathf.Clamp(focus.x,-864,864); focus.z=Mathf.Clamp(focus.z,-735,735);
            if (Input.GetKey(KeyCode.Equals)||Input.GetKey(KeyCode.KeypadPlus)) Zoom(-dt);
            if (Input.GetKey(KeyCode.Minus)||Input.GetKey(KeyCode.KeypadMinus)) Zoom(dt);
            if (Input.GetKeyDown(KeyCode.R)) Reset();
            MoveCamera();
        }
        public void Zoom(float amount) { distance=Mathf.Clamp(distance*Mathf.Exp(amount),100,5000); MoveCamera(); }
        public void Reset() { focus=Vector3.zero; distance=1400; MoveCamera(); }
        void MoveCamera() { camera.transform.position=focus+new Vector3(0,.85f,-.65f)*distance; camera.transform.LookAt(focus); }
        public void Dispose()
        {
            if (root) { root.SetActive(false); { root.SetActive(false); UnityEngine.Object.Destroy(root); } }
            foreach (var mesh in meshes) UnityEngine.Object.Destroy(mesh);
            foreach (var material in materials) UnityEngine.Object.Destroy(material);
            RenderSettings.fog=fog;
            RenderSettings.sun=previousSun;
            RenderSettings.ambientMode=previousAmbientMode;
            RenderSettings.ambientLight=previousAmbientLight;
        }
        sealed class Batch
        {
            readonly CityMapPreview owner;
            readonly string name;
            readonly Material material;
            readonly List<Vector3> vertices=new();
            readonly List<int> indices=new();
            public Batch(CityMapPreview owner,string name,Color color)
            {
                this.owner=owner; this.name=name;
                material=new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color=color;
                material.SetFloat("_Cull",0); material.SetFloat("_Smoothness",.15f); owner.materials.Add(material);
            }
            public void Polygon(Vector3[] points)
            {
                var outline=new CityPoint[points.Length];
                for (int i=0;i<points.Length;i++) outline[i]=new CityPoint { x=points[i].x,z=points[i].z };
                var triangles=CityMapGeometry.Triangulate(outline);
                for (int i=0;i<triangles.Length;i+=3) Triangle(points[triangles[i]],points[triangles[i+1]],points[triangles[i+2]]);
            }
            void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                if (vertices.Count>59000) Flush();
                int n=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                indices.Add(n); indices.Add(n+1); indices.Add(n+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d) { Triangle(a,b,c); Triangle(a,c,d); }
            public void Flush()
            {
                if (vertices.Count==0) return;
                var mesh=new Mesh { name=name };
                owner.meshes.Add(mesh);
                mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var go=new GameObject(name); go.transform.SetParent(owner.root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.shadowCastingMode=ShadowCastingMode.Off;
                vertices.Clear(); indices.Clear();
            }
        }
    }
}
