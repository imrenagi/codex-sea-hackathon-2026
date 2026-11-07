using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JalanKita.EditorTools
{
    public static class BrandChecks
    {
        public static void Run()
        {
            string destination = Path.Combine(Path.GetTempPath(), "roadsense-brand-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(destination);
            try
            {
                string index = Path.Combine(destination, "index.html");
                const string body = "<body>keep Unity loader and game</body>";
                File.WriteAllText(index, "<html><head></head>" + body + "</html>");
                ProjectSetup.ApplyWebBranding(destination);
                string html = File.ReadAllText(index);
                Require(html.Contains(body) && html.Contains("apple-touch-icon.png") && html.Contains("favicon-32.png"), "HTML links preserve loader/body");
                string exports = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Brand/exports"));
                foreach (string name in new[] { "favicon.ico", "favicon-16.png", "favicon-32.png", "favicon-48.png", "apple-touch-icon.png", "TemplateData/favicon.ico" })
                    Require(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(destination, name))) ==
                        Convert.ToBase64String(File.ReadAllBytes(Path.Combine(exports, Path.GetFileName(name)))), "Exact icon export " + name);
                File.WriteAllText(index, "invalid template");
                bool rejected = false;
                try { ProjectSetup.ApplyWebBranding(destination); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected, "Reject template without head");
                Require(PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone) == "id.jalankita.hackathon", "Original save bundle ID");
                Require(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Brand/RoadSenseIcon.png"), "Imported build icon");
                Debug.Log("RoadSense branding checks PASS");
            }
            finally { Directory.Delete(destination, true); }
        }

        static void Require(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Branding: " + label);
        }
    }
}
