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
                VerifyCrossing(parent.transform, render);
                Debug.Log("Sound checks passed: car/motor mix, finite samples, mute, persisted/clamped volume, ambience toggle, critical warnings and sustained two-tone crossing alarm lifecycle.");
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

        static void VerifyCrossing(Transform parent, MethodInfo render)
        {
            var sound = SoundEngine.Create(parent);
            sound.GetComponent<AudioSource>().Stop();
            sound.SetMuted(false);
            sound.SetVolume(1);
            sound.SetAmbience(false);
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            float[] Capture(float distance, float seconds)
            {
                sound.UpdateFrame(.02f, new SoundFrame { BellDist = distance, SirenDist = float.PositiveInfinity });
                var pcm = new float[(int)(rate * seconds)];
                render.Invoke(sound, new object[] { pcm, 1 });
                return pcm;
            }
            double Rms(float[] pcm, float from, float to)
            {
                int first = (int)(from * rate), last = (int)(to * rate);
                double energy = 0;
                for (int i = first; i < last; i++) energy += pcm[i] * pcm[i];
                return Math.Sqrt(energy / (last - first));
            }
            double Magnitude(float[] pcm, float from, double frequency)
            {
                int first = (int)(from * rate), count = rate / 10;
                double real = 0, imaginary = 0;
                for (int i = 0; i < count; i++)
                {
                    double phase = 2 * Math.PI * frequency * i / rate;
                    real += pcm[first + i] * Math.Cos(phase);
                    imaginary += pcm[first + i] * Math.Sin(phase);
                }
                return Math.Sqrt(real * real + imaginary * imaginary) / count;
            }
            void Require(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException("Crossing alarm: " + message);
            }

            // Capture several loops with no intervening game frames: rhythm must use the audio clock.
            var near = Capture(0, 3.1f);
            foreach (float at in new[] { .15f, 1.63f, 2.99f })
                Require(Magnitude(near, at, 755) > 8 * Magnitude(near, at, 551), "high note missing or cadence drifted");
            foreach (float at in new[] { .9f, 2.38f })
                Require(Magnitude(near, at, 551) > 8 * Magnitude(near, at, 755), "low note missing or cadence drifted");
            Require(Rms(near, .5f, .65f) > .8 * Rms(near, .1f, .25f), "alarm decays like a bell instead of sustaining");
            Require(Rms(near, .1f, .25f) > .05, "nearby warning too quiet with ambience disabled");
            foreach (float distance in new[] { float.PositiveInfinity, float.NaN, 200f, 300f })
                Require(Rms(Capture(distance, .15f), .1f, .14f) < .000001, "inactive/out-of-range warning did not stop");

            var far = Capture(100, .4f);
            double nearRms = Rms(near, .1f, .25f), farRms = Rms(far, .1f, .25f);
            Require(farRms > nearRms * .48 && farRms < nearRms * .52, "distance attenuation changed");
            sound.SetMuted(true);
            Require(Rms(Capture(0, .2f), .1f, .19f) < .000001, "mute left the crossing audible");
            sound.SetMuted(false);
            sound.SetVolume(.25f);
            Capture(float.PositiveInfinity, .15f);
            var quiet = Capture(0, .4f);
            Require(Rms(quiet, .1f, .25f) < nearRms * .26 && Rms(quiet, .1f, .25f) > nearRms * .24,
                "master volume does not scale the warning");
            Require(Magnitude(quiet, .15f, 755) > 8 * Magnitude(quiet, .15f, 551), "warning restart did not begin at the high note");
        }
    }
}
