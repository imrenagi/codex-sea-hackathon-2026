using UnityEngine;

namespace JalanKita
{
    // Posted legal guidance; this sign never controls vehicle propulsion.
    public static class RoadSpeedSign
    {
        static Material red, white, steel;
        static Material Surface(string name, Color color)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            material.SetColor("_BaseColor",color);return material;
        }
        static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Quaternion rotation,Material material)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cylinder);part.name=name;part.transform.SetParent(parent,false);
            part.transform.localPosition=position;part.transform.localScale=scale;part.transform.localRotation=rotation;
            part.GetComponent<MeshRenderer>().sharedMaterial=material;
            if(Application.isPlaying)Object.Destroy(part.GetComponent<Collider>());else Object.DestroyImmediate(part.GetComponent<Collider>());
        }
        public static void Mark(Transform parent,Vector3 position,float roadHeading,int limit)
        {
            var label=new GameObject("Painted speed "+limit+" km/h");label.transform.SetParent(parent,false);
            label.transform.SetPositionAndRotation(position,Quaternion.Euler(0,roadHeading,0)*Quaternion.Euler(90,0,0));
            var text=label.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=limit.ToString();text.fontSize=96;text.characterSize=.4f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.9f,.9f,.84f);
            label.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        }
        public static GameObject Create(Transform parent,Vector3 position,float roadHeading,int limit)
        {
            red??=Surface("Posted speed red",new Color(.75f,.025f,.03f));
            white??=Surface("Posted speed white",new Color(.96f,.96f,.92f));
            steel??=Surface("Posted speed pole",new Color(.37f,.39f,.4f));
            var root=new GameObject("Posted speed "+limit+" km/h");root.transform.SetParent(parent,false);
            root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,roadHeading,0));
            Part(root.transform,"Pole",new Vector3(0,1.15f,0),new Vector3(.06f,1.15f,.06f),Quaternion.identity,steel);
            Part(root.transform,"Red border",new Vector3(0,2.15f,0),new Vector3(.95f,.035f,.95f),Quaternion.Euler(90,0,0),red);
            Part(root.transform,"White face",new Vector3(0,2.15f,-.042f),new Vector3(.77f,.012f,.77f),Quaternion.Euler(90,0,0),white);
            var label=new GameObject("Speed limit number");label.transform.SetParent(root.transform,false);
            label.transform.localPosition=new Vector3(0,2.15f,-.06f);
            var text=label.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=limit.ToString();text.fontSize=64;text.characterSize=.12f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.black;
            label.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            return root;
        }
    }
}
