using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita.EditorTools
{
    // Focused layout regression; does not start driving or read/write learner preferences.
    public static class MenuFocusChecks
    {
        static EditorWindow window;
        static GameObject host;
        static ScrollView scroll;
        static Button first, last;
        static int phase, frames, clicks;
        static double deadline;

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this isolated check in batch mode.");
            window = ScriptableObject.CreateInstance<MenuFocusCheckWindow>();
            window.position = new Rect(0, 0, 1280, 720);
            window.Show();
            window.rootVisualElement.style.width = 1280;
            window.rootVisualElement.style.height = 720;
            window.rootVisualElement.panel.visualTree.style.width = 1280;
            window.rootVisualElement.panel.visualTree.style.height = 720;
            window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("UI/Jalan"));
            host = new GameObject("Menu focus check") { hideFlags = HideFlags.HideAndDontSave };
            host.SetActive(false);
            var game = host.AddComponent<JalanGame>();
            var dialog = new VisualElement();
            dialog.style.width = 440;
            dialog.style.height = 400;
            window.rootVisualElement.Add(dialog);
            typeof(JalanGame).GetField("dialog", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, dialog);
            scroll = (ScrollView)typeof(JalanGame).GetMethod("MenuBody", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            scroll.style.height = 400;
            scroll.style.flexShrink = 1;
            for (int i = 0; i < 20; i++)
            {
                var button = new Button(() => clicks++) { text = i == 19 ? "Pratinjau peta – belum bisa dimainkan" : "Menu " + i };
                button.style.height = 42;
                button.style.flexShrink = 0;
                scroll.Add(button);
                if (i == 0) first = button;
                last = button;
            }
            deadline = EditorApplication.timeSinceStartup + 20;
            phase = frames = clicks = 0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new InvalidOperationException("Menu focus check timed out.");
                var panel = window.rootVisualElement.panel;
                panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(panel, null);
                var scheduling = panel.GetType().GetMethod("TickSchedulingUpdaters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Require(scheduling != null, "Headless panel scheduler is available");
                scheduling.Invoke(panel, null);
                if (float.IsNaN(scroll.contentViewport.worldBound.height) || float.IsNaN(last.worldBound.height)) return;
                if (scroll.contentViewport.layout.height <= 1 || last.layout.height <= 1) return;
                if (++frames < 4) return;
                frames = 0;
                switch (phase++)
                {
                    case 0:
                        Require(last.worldBound.yMin > scroll.contentViewport.worldBound.yMax, "Last control starts below viewport: target=" + last.worldBound + " viewport=" + scroll.contentViewport.worldBound + " offset=" + scroll.scrollOffset);
                        last.Focus();
                        break;
                    case 1:
                        Require(scroll.focusController.focusedElement == last, "Focused last menu control before reveal");
                        Visible(last, "1280x720 lower menu control");
                        Require(scroll.scrollOffset.y > 0 && clicks == 0, "Focus scrolls without activating control");
                        first.Focus();
                        break;
                    case 2:
                        Visible(first, "Reverse keyboard navigation");
                        last.Focus(); first.Focus();
                        break;
                    case 3:
                        Visible(first, "Stale queued focus cannot scroll away from current control");
                        window.position = new Rect(0, 0, 580, 336);
                        window.rootVisualElement.style.width = 580;
                        window.rootVisualElement.style.height = 336;
                        panel.visualTree.style.width = 580;
                        panel.visualTree.style.height = 336;
                        scroll.parent.style.height = 180;
                        scroll.style.height = 180;
                        break;
                    case 4:
                        last.Focus();
                        break;
                    case 5:
                        Visible(last, "580x336 lower menu control");
                        Require(scroll.focusController.focusedElement == last && clicks == 0, "Focus is preserved and never clicks");
                        using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target = last; last.SendEvent(submit); }
                        Require(clicks == 1, "Enter activation remains available");
                        Debug.Log("MENU_FOCUS_CHECKS_PASSED: lower control reveal at1280x720 and580x336, reverse navigation, stale focus, no click, Enter activation.");
                        Finish(0);
                        break;
                }
            }
            catch (Exception error) { Debug.LogException(error); Finish(1); }
        }

        static void Visible(VisualElement target, string label)
        {
            var viewport = scroll.contentViewport.worldBound;
            Require(target.worldBound.yMin >= viewport.yMin - 1 && target.worldBound.yMax <= viewport.yMax + 1,
                label + " is fully visible: target=" + target.worldBound + " viewport=" + viewport + " offset=" + scroll.scrollOffset);
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            if (window) window.Close();
            if (host) UnityEngine.Object.DestroyImmediate(host);
            EditorApplication.Exit(code);
        }
    }
    sealed class MenuFocusCheckWindow : EditorWindow { }
}
