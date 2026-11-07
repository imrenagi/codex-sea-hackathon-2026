using System.Collections.Generic;
using UnityEngine;

namespace UjianSim.Game
{
    /// <summary>Instantiates the Blender models in Resources/Models with library materials applied.</summary>
    public static class Models
    {
        static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();

        public static GameObject Prefab(string name)
        {
            if (!Cache.TryGetValue(name, out var go))
            {
                go = Resources.Load<GameObject>("Models/" + name);
                if (go == null) Debug.LogError($"Missing model Resources/Models/{name}");
                Cache[name] = go;
            }
            return go;
        }

        /// <summary>A new instance under parent. Its own lamp/paint materials come back in `materials`.</summary>
        public static GameObject Spawn(string name, Transform parent, out Dictionary<string, Material> materials,
            bool shadows = true)
        {
            var prefab = Prefab(name);
            var go = prefab != null ? Object.Instantiate(prefab, parent) : new GameObject(name + " (missing)");
            go.transform.SetParent(parent, false);
            go.name = name;
            materials = MaterialLibrary.Apply(go, shadows);
            return go;
        }

        public static GameObject Spawn(string name, Transform parent, bool shadows = true) =>
            Spawn(name, parent, out _, shadows);

        /// <summary>Destroy that also works in edit mode (the headless playtest and capture tools run there).</summary>
        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        /// <summary>Give a renderer its own copies of its materials (without Renderer.materials, which leaks in edit mode).</summary>
        public static void OwnMaterials(Renderer r)
        {
            if (r == null) return;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) if (mats[i] != null) mats[i] = new Material(mats[i]) { name = mats[i].name };
            r.sharedMaterials = mats;
        }

        /// <summary>First descendant whose name starts with the given part name (FBX may append suffixes).</summary>
        public static Transform Find(Transform root, string part)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == part || t.name.StartsWith(part + ".")) return t;
            return null;
        }
    }
}
