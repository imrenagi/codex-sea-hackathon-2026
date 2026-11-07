using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace JalanKita
{
    // Keep localization independent of save keys, model IDs and gameplay state.
    // Choose a locale before creating a screen; content definitions resolve their keys on access.
    public static class GameText
    {
        public const string DefaultLocale = "en";
        public static string Locale { get; private set; } = DefaultLocale;
        public static event Action LocaleChanged;

        [Serializable] sealed class Catalogue { public Entry[] entries; }
        [Serializable] sealed class Entry { public string key, value; }
        static Dictionary<string, string> english;
        static Dictionary<string, string> selected;
        static CultureInfo culture = CultureInfo.GetCultureInfo(DefaultLocale);

        static Dictionary<string, string> Load(string locale)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var asset = Resources.Load<TextAsset>("Localization/" + locale);
            if (!asset) return result;
            try
            {
                var catalogue = JsonUtility.FromJson<Catalogue>(asset.text);
                if (catalogue?.entries != null)
                    foreach (var entry in catalogue.entries)
                        if (entry != null && !string.IsNullOrEmpty(entry.key) && entry.value != null)
                            result[entry.key] = entry.value;
            }
            catch (ArgumentException error)
            {
                Debug.LogWarning("Invalid localization catalogue " + locale + ": " + error.Message);
            }
            return result;
        }

        static void EnsureLoaded()
        {
            if (english != null) return;
            english = Load(DefaultLocale);
            selected = english;
        }

        // Missing regional entries fall back to the base language, then English.
        // Only letters, digits and hyphens are accepted as resource path components.
        public static void SetLocale(string locale)
        {
            locale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.Trim().ToLowerInvariant().Replace('_', '-');
            foreach (char character in locale)
                if (!(character >= 'a' && character <= 'z') && !(character >= '0' && character <= '9') && character != '-')
                    throw new ArgumentException("Invalid locale identifier", nameof(locale));
            EnsureLoaded();
            if (locale == Locale) return;
            var next = new Dictionary<string, string>(StringComparer.Ordinal);
            int separator = locale.IndexOf('-');
            if (separator > 0)
                foreach (var entry in Load(locale.Substring(0, separator))) next[entry.Key] = entry.Value;
            foreach (var entry in Load(locale)) next[entry.Key] = entry.Value;
            Locale = locale;
            selected = locale == DefaultLocale ? english : next;
            try { culture = CultureInfo.GetCultureInfo(locale); }
            catch (CultureNotFoundException) { culture = CultureInfo.GetCultureInfo(DefaultLocale); }
            LocaleChanged?.Invoke();
        }

        public static string Get(string key, params object[] args)
        {
            if (key == null) return string.Empty;
            EnsureLoaded();
            if (!selected.TryGetValue(key, out var text) && !english.TryGetValue(key, out text)) text = key;
            if (args == null || args.Length == 0) return text;
            try { return string.Format(culture, text, args); }
            catch (FormatException)
            {
                // A malformed translation must not break a driving or feedback screen.
                if (english.TryGetValue(key, out var fallback))
                    try { return string.Format(CultureInfo.GetCultureInfo(DefaultLocale), fallback, args); }
                    catch (FormatException) { return fallback; }
                return text;
            }
        }

        // UXML keeps keys in classes so its text and tooltips share the same catalogue.
        // Reapply after SetLocale when rebuilding an already-created UI tree.
        public static void Apply(VisualElement root)
        {
            if (root == null) return;
            ApplyElement(root);
            root.Query<VisualElement>().ForEach(ApplyElement);
        }

        static void ApplyElement(VisualElement element)
        {
            const string textPrefix = "i18n-text--", tooltipPrefix = "i18n-tooltip--";
            foreach (var styleClass in element.GetClasses())
            {
                if (styleClass.StartsWith(textPrefix, StringComparison.Ordinal) && element is TextElement text)
                    text.text = Get(styleClass.Substring(textPrefix.Length));
                else if (styleClass.StartsWith(tooltipPrefix, StringComparison.Ordinal))
                    element.tooltip = Get(styleClass.Substring(tooltipPrefix.Length));
            }
        }
    }
}
