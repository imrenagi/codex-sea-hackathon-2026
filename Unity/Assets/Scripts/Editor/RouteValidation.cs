using System;
using System.IO;
using UnityEditor;

namespace JalanKita.EditorTools
{
    // Allows the open editor to run validation without closing the user's scene.
    [InitializeOnLoad]
    public static class RouteValidation
    {
        static RouteValidation() => EditorApplication.update += Poll;
        static void Poll()
        {
            const string request = "Temp/validate-route";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isPlaying) return;
            File.Delete(request);
            try
            {
                JourneyChecks.Run();
                File.WriteAllText("Temp/route-validation.txt", "PASS: route, failures, retry poses and save migration");
            }
            catch (Exception error)
            {
                File.WriteAllText("Temp/route-validation.txt", error.ToString());
                UnityEngine.Debug.LogException(error);
            }
        }
    }
}
