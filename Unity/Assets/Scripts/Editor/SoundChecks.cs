using System;
using System.Reflection;
using UnityEngine;
using UjianSim.Game;

namespace JalanKita.EditorTools
{
    public static class SoundChecks
    {
        public static void Run()
        {
            const string key = "jalan-kita:muted";
            bool had = PlayerPrefs.HasKey(key);
            int previous = PlayerPrefs.GetInt(key);
            const string volumeKey = "jalan-kita:volume";
            bool hadVolume = PlayerPrefs.HasKey(volumeKey);
            float previousVolume = PlayerPrefs.GetFloat(volumeKey);
            const string ambienceKey = "jalan-kita:ambience";
            bool hadAmbience = PlayerPrefs.HasKey(ambienceKey);
            int previousAmbience = PlayerPrefs.GetInt(ambienceKey);
            var parent = new GameObject("Audio validation");
            try
            {
                var sound = SoundEngine.Create(parent.transform);
                sound.GetComponent<AudioSource>().Stop();
                sound.SetMuted(false);
                sound.SetVolume(.35f);
                sound.SetAmbience(false);
                var restored = SoundEngine.Create(parent.transform);
                if (Mathf.Abs(restored.Volume - .35f) > .001f || restored.AmbienceEnabled || restored.Muted)
                    throw new InvalidOperationException("Audio settings did not restore independently");
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
                sound.SetVolume(float.NaN);
                if (Mathf.Abs(sound.Volume - .35f) > .001f) throw new InvalidOperationException("Nonfinite volume accepted");
                sound.SetVolume(2f);
                if (sound.Volume != 1f) throw new InvalidOperationException("Volume did not clamp");
                var render = typeof(SoundEngine).GetMethod("OnAudioFilterRead", BindingFlags.Instance|BindingFlags.NonPublic);
                var samples = new float[9600];
                foreach (bool motorcycle in new[] { false, true })
                {
                    sound.UpdateFrame(.02f, new SoundFrame { Motorcycle=motorcycle, Speed=10, Throttle=1,
                        Horn=true, Ringing=true, SirenDist=20, BellDist=30, Rain=true });
                    float Target(string field) => (float)typeof(SoundEngine).GetField(field, BindingFlags.Instance|BindingFlags.NonPublic).GetValue(sound);
                    if (Target("tWind") != 0 || Target("tRain") != 0 || Target("tHornGain") <= 0 || Target("tSirenGain") <= 0)
                        throw new InvalidOperationException("Ambience preference suppressed critical warning audio or retained ambience");
                    sound.RetryCue();
                    sound.Tick(true);
                    sound.Chime(false);
                    render.Invoke(sound, new object[] { samples, 2 });
                    float peak=0;
                    foreach (float value in samples)
                    {
                        if (float.IsNaN(value) || float.IsInfinity(value) || Mathf.Abs(value)>1)
                            throw new InvalidOperationException("Invalid audio sample");
                        peak=Mathf.Max(peak,Mathf.Abs(value));
                    }
                    if (peak<.001f) throw new InvalidOperationException("Driving mix is silent");
                }
                sound.SetMuted(true);
                render.Invoke(sound, new object[] { samples, 2 });
                if (Mathf.Abs(samples[^1])>.0001f) throw new InvalidOperationException("Mute did not silence output");
                Debug.Log("Sound checks passed: car/motor mix, finite samples, mute, persisted/clamped volume, ambience toggle and critical warning audio.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                if (had) PlayerPrefs.SetInt(key, previous); else PlayerPrefs.DeleteKey(key);
                if (hadVolume) PlayerPrefs.SetFloat(volumeKey, previousVolume); else PlayerPrefs.DeleteKey(volumeKey);
                if (hadAmbience) PlayerPrefs.SetInt(ambienceKey, previousAmbience); else PlayerPrefs.DeleteKey(ambienceKey);
                PlayerPrefs.Save();
            }
        }
    }
}
