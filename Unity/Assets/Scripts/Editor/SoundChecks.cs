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
            var parent = new GameObject("Audio validation");
            try
            {
                var sound = SoundEngine.Create(parent.transform);
                sound.GetComponent<AudioSource>().Stop();
                sound.SetMuted(false);
                sound.SetVolume(1f);
                var render = typeof(SoundEngine).GetMethod("OnAudioFilterRead", BindingFlags.Instance|BindingFlags.NonPublic);
                var samples = new float[9600];
                foreach (bool motorcycle in new[] { false, true })
                {
                    sound.UpdateFrame(.02f, new SoundFrame { Motorcycle=motorcycle, Speed=10, Throttle=1,
                        Horn=true, Ringing=true, SirenDist=20, BellDist=30 });
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
                // The ambulance spawns 70 m behind: it must be clearly audible there,
                // independently of engine/phone/horn tones from the previous mix.
                sound.UpdateFrame(.02f,new SoundFrame { SirenDist=70, BellDist=float.PositiveInfinity });
                for(int i=0;i<30;i++) render.Invoke(sound,new object[] { samples,2 });
                double energy=0;
                foreach(float value in samples) energy+=value*value;
                if(Math.Sqrt(energy/samples.Length)<.035) throw new InvalidOperationException("Approaching ambulance siren is too quiet at 70 m");
                sound.SetMuted(true);
                render.Invoke(sound, new object[] { samples, 2 });
                if (Mathf.Abs(samples[^1])>.0001f) throw new InvalidOperationException("Mute did not silence output");
                Debug.Log("Sound checks passed: car/motor driving mix, finite samples and mute.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                if (had) PlayerPrefs.SetInt(key, previous); else PlayerPrefs.DeleteKey(key);
                if (hadVolume) PlayerPrefs.SetFloat(volumeKey, previousVolume); else PlayerPrefs.DeleteKey(volumeKey);
                PlayerPrefs.Save();
            }
        }
    }
}
