using UnityEngine;

namespace UjianSim.Game
{
    /// <summary>Procedural surface textures (no image files): asphalt, paving, grass, ballast.</summary>
    public static class Textures
    {
        static float Fbm(float x, float y, int octaves = 4)
        {
            float s = 0, a = 0.5f, f = 1;
            for (int i = 0; i < octaves; i++) { s += a * Mathf.PerlinNoise(x * f + 31.7f * i, y * f + 17.3f * i); a *= 0.5f; f *= 2.03f; }
            return s;
        }

        static Texture2D Make(string name, int size, System.Func<float, float, Color> px)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
            var data = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    data[y * size + x] = px((float)x / size, (float)y / size);
            t.SetPixels(data);
            t.Apply(true);
            return t;
        }

        /// <summary>Tileable noise: sample a torus so edges match.</summary>
        static float Tile(float u, float v, float scale, int octaves = 4)
        {
            float a = u * Mathf.PI * 2, b = v * Mathf.PI * 2;
            float r = scale / (2 * Mathf.PI);
            return Fbm(Mathf.Cos(a) * r + 50 + Mathf.Sin(b) * r * 0.37f, Mathf.Sin(a) * r + 50 + Mathf.Cos(b) * r, octaves);
        }

        public static Texture2D Asphalt() => Make("Asphalt", 512, (u, v) =>
        {
            float patches = Tile(u, v, 6f, 3);
            float grain = Random.value;
            float g = 0.26f + 0.12f * patches + 0.07f * (grain - 0.5f);
            return new Color(g, g * 1.01f, g * 1.04f, 1);
        });

        public static Texture2D Paving() => Make("Paving", 256, (u, v) =>
        {
            float fu = (u * 4) % 1f, fv = (v * 4) % 1f;
            bool joint = fu < 0.04f || fv < 0.04f;
            float stain = Tile(u, v, 4f, 3);
            float g = (0.74f + 0.14f * stain) * (joint ? 0.78f : 1f);
            return new Color(g * 1.02f, g * 0.98f, g * 0.91f, 1);
        });

        public static Texture2D Grass() => Make("Grass", 512, (u, v) =>
        {
            float n = Tile(u, v, 8f, 5);
            float dry = Mathf.SmoothStep(0.45f, 0.7f, Tile(u + 0.3f, v, 3f, 3));
            var green = new Color(0.22f, 0.36f, 0.13f);
            var brown = new Color(0.42f, 0.36f, 0.22f);
            var c = Color.Lerp(green, brown, dry * 0.7f) * (0.75f + 0.5f * n);
            c.a = 1;
            return c;
        });

        public static Texture2D Ballast() => Make("Ballast", 256, (u, v) =>
        {
            float n = Tile(u, v, 24f, 2);
            float g = 0.35f + 0.3f * n + 0.08f * (Random.value - 0.5f);
            return new Color(g, g * 0.97f, g * 0.93f, 1);
        });
    }
}
