using System;
using System.Collections.Generic;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace UjianSim.Game
{
    /// <summary>What the sound engine needs from one frame. Filled by GameController from the simulation.</summary>
    public struct SoundFrame
    {
        /// <summary>m/s</summary>
        public float Speed;
        public float Throttle;
        /// <summary>Null behind the menu: the engine fades out.</summary>
        public bool? Motorcycle;
        public bool Ringing;
        public bool Horn;
        /// <summary>Metres to the nearest emergency vehicle with its siren on (infinity when none).</summary>
        public float SirenDist;
        /// <summary>Metres to the nearest level crossing whose bell is ringing (infinity when none).</summary>
        public float BellDist;
        public bool Rain;
        public bool DecorativeAmbienceSuppressed;
    }

    /// <summary>
    /// Synthesized sound only, no audio files: a port of web-threejs/src/audio/AudioEngine.ts. The mix is written
    /// sample by sample in OnAudioFilterRead on the audio thread; the main thread only sets targets and queues tones.
    /// WebGL players have no OnAudioFilterRead, so there the same targets and tones go to the Web Audio graph in
    /// Plugins/WebGL/UjianSimAudio.jslib instead.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundEngine : MonoBehaviour
    {
        /// <summary>The values are the wave codes UjianSimAudio.jslib uses.</summary>
        enum Wave { Sine = 0, Square = 1, Saw = 2, Triangle = 3 }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void UjianAudio_Init(float[] crossingLoop, int sampleCount, int sampleRate);
        [DllImport("__Internal")] static extern void UjianAudio_Dispose();
        [DllImport("__Internal")] static extern void UjianAudio_Set(float master, float engineFreq, float engineCut, float engineGain,
            float sirenFreq, float sirenGain, float hornA, float hornB, float hornGain, float wind, float rain, float crossingGain);
        [DllImport("__Internal")] static extern void UjianAudio_Tone(double freq, double delay, double dur, int wave, double volume, double slideTo);
#endif

        /// <summary>A short enveloped tone, like the web build's tone(): 10 ms attack, exponential decay.</summary>
        class Voice
        {
            public Wave Wave;
            public double Freq, SlideTo, Phase, Volume;
            public double Start, Dur;
        }

        const string PrefKey = "jalan-kita:muted";
        const string VolumeKey = "jalan-kita:volume";
        const string AmbienceKey = "jalan-kita:ambience";

        readonly object gate = new object();
        readonly List<Voice> voices = new List<Voice>();
        readonly System.Random random = new System.Random();

        AudioClip silentClip;
        float[] crossingLoop;
        int crossingCursor;
        bool crossingWasActive;
        int rate = 48000;
        /// <summary>Audio-thread clock in seconds; tones are scheduled against it.</summary>
        double now;

        // Targets written by the main thread, smoothed per sample on the audio thread.
        volatile float tMaster, tEngineFreq, tEngineCut, tEngineGain, tSirenFreq, tSirenGain, tHornA, tHornB, tHornGain, tWind, tRain, tCrossingGain;
        float master, engineFreq = 40f, engineCut = 300f, engineGain, sirenFreq = 650f, sirenGain, hornA = 410f, hornB = 500f, hornGain, wind, rain;
        float crossingGain;

        // Oscillator phases and filter states (audio thread only).
        double pEngine, pSub, pSiren, pHornA, pHornB;
        float lpEngine1, lpEngine2, bpWindLow, bpWindBand, lpHorn, lpRain, hpRain;

        float ringTimer, sirenPhase, ambienceTimer = 2f;
        float ambienceStartupQuiet = 8f;

        public bool Muted { get; private set; }
        public float Volume { get; private set; } = 1f;
        public bool AmbienceEnabled { get; private set; } = true;
        long renderedFrames;
        public long RenderedFrames => System.Threading.Interlocked.Read(ref renderedFrames);
        volatile float outputPeak;
        public float OutputPeak => outputPeak;

        public static SoundEngine Create(Transform parent)
        {
            var go = new GameObject("Sound");
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            // A silent looping clip keeps the source playing so OnAudioFilterRead is called.
            int sr = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            source.clip = AudioClip.Create("silence", sr, 1, sr, false);
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.volume = 1f;
            source.playOnAwake = false;
            var engine = go.AddComponent<SoundEngine>();
            engine.silentClip = source.clip;
            engine.rate = sr;
            engine.crossingLoop = CreateCrossingLoop(sr);
            engine.Muted = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            engine.AmbienceEnabled = PlayerPrefs.GetInt(AmbienceKey, 1) != 0;
            float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            engine.Volume = float.IsNaN(volume) || float.IsInfinity(volume) ? 1f : Mathf.Clamp01(volume);
            engine.tMaster = engine.Muted ? 0f : 0.7f * engine.Volume;
#if UNITY_WEBGL && !UNITY_EDITOR
            UjianAudio_Init(engine.crossingLoop, engine.crossingLoop.Length, sr);
            engine.PushTargets();
#else
            if (Application.isPlaying) source.Play();
#endif
            return engine;
        }

        void OnDestroy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UjianAudio_Dispose();
#endif
            if (silentClip)
            {
                if (Application.isPlaying) Destroy(silentClip);
                else DestroyImmediate(silentClip);
            }
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            tMaster = muted ? 0f : 0.7f * Volume;
            PlayerPrefs.SetInt(PrefKey, muted ? 1 : 0);
            PushTargets();
        }

        public void SetVolume(float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume)) return;
            Volume = Mathf.Clamp01(volume);
            tMaster = Muted ? 0f : 0.7f * Volume;
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PushTargets();
        }

        public void SetAmbience(bool enabled)
        {
            AmbienceEnabled = enabled;
            PlayerPrefs.SetInt(AmbienceKey, enabled ? 1 : 0);
            if (!enabled) { tWind = tRain = 0f; ambienceTimer = 2f; }
            PushTargets();
        }

        /// <summary>Hands the targets to the Web Audio graph in WebGL players; the desktop mix reads them itself.</summary>
        void PushTargets()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UjianAudio_Set(tMaster, tEngineFreq, tEngineCut, tEngineGain, tSirenFreq, tSirenGain, tHornA, tHornB, tHornGain, tWind, tRain, tCrossingGain);
#endif
        }

        // Original synthesis inspired by the alternating alarm at 0:21–0:42 in
        // https://www.youtube.com/watch?v=4Kpb0c4lvbo (no recording is bundled).
        // Both back ends consume these same PCM samples, so timing is independent of frame rate.
        static float[] CreateCrossingLoop(int sampleRate)
        {
            int noteSamples = (int)Math.Round(sampleRate * .74);
            var samples = new float[noteSamples * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                bool high = i < noteSamples;
                int local = i % noteSamples;
                double t = (double)local / sampleRate;
                double phase = 2 * Math.PI * (high ? 755 : 551) * t;
                double tone = high
                    ? Math.Sin(phase) + .045 * Math.Sin(3 * phase) + .006 * Math.Sin(5 * phase)
                    : .72 * (Math.Sin(phase) + .15 * Math.Sin(2 * phase) + .32 * Math.Sin(3 * phase) + .04 * Math.Sin(5 * phase));
                // Short fades avoid clicks at the note changes and loop boundary; the body sustains.
                double envelope = Math.Min(1, Math.Min(t / .005, (double)(noteSamples - 1 - local) / sampleRate / .008));
                samples[i] = (float)(.8 * tone * envelope);
            }
            return samples;
        }

        // ------------------------------------------------------------------------------- main thread

        void Tone(double freq, double delay, double dur, Wave wave, double volume, double slideTo = 0)
        {
            if (Muted) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            UjianAudio_Tone(freq, delay, dur, (int)wave, volume, slideTo);
            return;
#endif
            lock (gate) voices.Add(new Voice { Wave = wave, Freq = freq, SlideTo = slideTo, Volume = volume, Start = now + delay, Dur = dur });
        }

        /// <summary>Indicator relay click, called when the blink clock flips while an indicator is on.</summary>
        public void Tick(bool on) => Tone(on ? 1900 : 1500, 0, 0.03, Wave.Square, 0.05);

        public void Chime(bool violation)
        {
            if (violation)
            {
                // A soft downward phrase leaves room for the explanation.
                Tone(392, 0, 0.24, Wave.Triangle, 0.12);
                Tone(330, 0.12, 0.24, Wave.Sine, 0.11);
                Tone(262, 0.24, 0.32, Wave.Sine, 0.10);
            }
            else
            {
                Tone(262, 0, 0.55, Wave.Sine, 0.06);
                double[] notes = { 523, 659, 784, 1047 };
                for (int i = 0; i < notes.Length; i++) Tone(notes[i], i * 0.10, 0.32, Wave.Triangle, 0.11);
                Tone(784, 0.30, 0.40, Wave.Sine, 0.06);
            }
        }

        public void RetryCue()
        {
            Tone(440, 0, 0.14, Wave.Sine, 0.07);
            Tone(587, 0.09, 0.20, Wave.Sine, 0.07);
        }

        public void UpdateFrame(float dt, SoundFrame f)
        {
            if (f.Motorcycle.HasValue)
            {
                bool moto = f.Motorcycle.Value;
                float rpm = Mathf.Abs(f.Speed) / (moto ? 18f : 20f);
                float b = moto ? 62f : 34f;
                tEngineFreq = b * (1f + rpm * 2.6f);
                tEngineCut = (moto ? 460f : 240f) + f.Throttle * (moto ? 1100f : 700f) + rpm * 600f;
                tEngineGain = (moto ? 0.07f : 0.06f) + f.Throttle * 0.05f;
            }
            else tEngineGain = 0f;

            // Two-tone ringtone pattern while the phone rings.
            if (f.Ringing)
            {
                ringTimer -= dt;
                if (ringTimer <= 0f)
                {
                    for (int i = 0; i < 4; i++) Tone(i % 2 == 1 ? 1320 : 990, i * 0.12, 0.1, Wave.Square, 0.06);
                    ringTimer = 1.6f;
                }
            }
            else ringTimer = 0f;

            // Siren: a wail sweeping up and down every 2.4 s, louder as the ambulance closes in.
            float near = float.IsInfinity(f.SirenDist) ? 0f : Mathf.Clamp01(1f - f.SirenDist / 220f);
            sirenPhase += dt;
            float sweep = 0.5f - 0.5f * Mathf.Cos(sirenPhase * Mathf.PI * 2f / 2.4f);
            tSirenFreq = 650f + 650f * sweep;
            tSirenGain = 0.24f * near;
            tEngineGain *= 1f - .55f * near;

            // Horn: a car's twin tone is lower than a motorcycle's single small horn.
            bool m = f.Motorcycle == true;
            tHornA = m ? 520f : 410f;
            tHornB = m ? 535f : 500f;
            tHornGain = f.Horn && f.Motorcycle.HasValue ? 0.07f : 0f;

            // Sustained high/low crossing alarm. An inactive warning (including pause/menu)
            // sends infinity and fades this channel out without leaving scheduled tones behind.
            tCrossingGain = float.IsNaN(f.BellDist) || float.IsInfinity(f.BellDist)
                ? 0f : .18f * Mathf.Clamp01(1f - f.BellDist / 200f);

            tWind = !AmbienceEnabled ? 0f : f.Motorcycle.HasValue ? 0.018f + Mathf.Clamp01(Mathf.Abs(f.Speed) / 18f) * 0.025f : 0.006f;
            tRain = AmbienceEnabled && f.Rain ? 0.05f : 0f;

            // Decorative chirps/drones wait until the driver has spent eight seconds moving.
            // Menu, pause and onboarding restart this grace without muting safety cues.
            if (!f.Motorcycle.HasValue || f.DecorativeAmbienceSuppressed) ambienceStartupQuiet = 8f;
            else if (Mathf.Abs(f.Speed) > 0.5f) ambienceStartupQuiet = Mathf.Max(0f, ambienceStartupQuiet - dt);
            if (AmbienceEnabled && f.Motorcycle.HasValue && ambienceStartupQuiet <= 0f) ambienceTimer -= dt;
            else ambienceTimer = 2f;
            if (ambienceTimer <= 0f)
            {
                if (random.NextDouble() < 0.6) Tone(110 + random.NextDouble() * 40, 0, 2.4, Wave.Saw, 0.012, 70);
                else for (int i = 0; i < 3; i++) Tone(2800 + random.NextDouble() * 600, i * 0.14, 0.08, Wave.Sine, 0.025, 3400);
                ambienceTimer = 3f + (float)random.NextDouble() * 5f;
            }
            PushTargets();
        }

        // ------------------------------------------------------------------------------ audio thread

        static double Osc(Wave w, double phase)
        {
            double p = phase - Math.Floor(phase);
            switch (w)
            {
                case Wave.Square: return p < 0.5 ? 1 : -1;
                case Wave.Saw: return 2 * p - 1;
                case Wave.Triangle: return 1 - 4 * Math.Abs(p - 0.5);
                default: return Math.Sin(p * 2 * Math.PI);
            }
        }

        /// <summary>One-pole low-pass coefficient for a cutoff frequency.</summary>
        float Coef(float cutoff) => 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / rate);

        // Per-sample uniform noise without allocation (xorshift), safe on the audio thread.
        uint noiseState = 22222;
        float Noise()
        {
            noiseState ^= noiseState << 13;
            noiseState ^= noiseState >> 17;
            noiseState ^= noiseState << 5;
            return (noiseState & 0xFFFFFF) / (float)0x800000 - 1f;
        }

#if !(UNITY_WEBGL && !UNITY_EDITOR)
        void OnAudioFilterRead(float[] data, int channels)
        {
            float peak=0;
            double dt = 1.0 / rate;
            // Smoothing per sample: about 50–100 ms like setTargetAtTime in the web build.
            float slow = Coef(2f), mid = Coef(10f), fast = Coef(40f);
            Voice[] active;
            lock (gate)
            {
                active = voices.ToArray();
                voices.RemoveAll((v) => now > v.Start + v.Dur + 0.02);
            }

            for (int i = 0; i < data.Length; i += channels)
            {
                master += (tMaster - master) * fast;
                engineFreq += (tEngineFreq - engineFreq) * mid;
                engineCut += (tEngineCut - engineCut) * mid;
                engineGain += (tEngineGain - engineGain) * mid;
                sirenFreq += (tSirenFreq - sirenFreq) * fast;
                sirenGain += (tSirenGain - sirenGain) * mid;
                hornA += (tHornA - hornA) * fast;
                hornB += (tHornB - hornB) * fast;
                hornGain += (tHornGain - hornGain) * fast;
                crossingGain += (tCrossingGain - crossingGain) * fast;
                wind += (tWind - wind) * slow;
                rain += (tRain - rain) * slow;

                double s = 0;

                // Engine: saw plus a square an octave down, through a resonant-ish two-pole low-pass.
                pEngine += engineFreq * dt;
                pSub += engineFreq * 0.5 * dt;
                float raw = (float)(Osc(Wave.Saw, pEngine) + Osc(Wave.Square, pSub));
                float c = Coef(engineCut);
                lpEngine1 += (raw - lpEngine1) * c;
                lpEngine2 += (lpEngine1 - lpEngine2) * c;
                s += lpEngine2 * engineGain;

                // Wind: band-passed noise around 400 Hz (state-variable filter).
                float n = Noise();
                float fw = 2f * Mathf.Sin(Mathf.PI * 400f / rate);
                bpWindLow += fw * bpWindBand;
                float high = n - bpWindLow - 2.5f * bpWindBand;
                bpWindBand += fw * high;
                s += bpWindBand * wind;

                // Rain: bright hiss (noise high-passed around 1.5 kHz).
                lpRain += (n - lpRain) * Coef(1500f);
                hpRain = n - lpRain;
                s += hpRain * rain;

                // Siren and horn.
                pSiren += sirenFreq * dt;
                s += Osc(Wave.Triangle, pSiren) * sirenGain;
                pHornA += hornA * dt;
                pHornB += hornB * dt;
                float horn = (float)(Osc(Wave.Square, pHornA) + Osc(Wave.Square, pHornB));
                lpHorn += (horn - lpHorn) * Coef(2200f);
                s += lpHorn * hornGain;

                bool crossingActive = tCrossingGain > 0;
                if (crossingActive && !crossingWasActive) crossingCursor = 0;
                crossingWasActive = crossingActive;
                if (crossingGain > .000001f)
                {
                    s += crossingLoop[crossingCursor] * crossingGain;
                    crossingCursor = (crossingCursor + 1) % crossingLoop.Length;
                }

                // One-shot tones.
                foreach (var v in active)
                {
                    double t = now - v.Start;
                    if (t < 0 || t > v.Dur) continue;
                    double f = v.SlideTo > 0 ? v.Freq * Math.Pow(v.SlideTo / v.Freq, t / v.Dur) : v.Freq;
                    v.Phase += f * dt;
                    double env = t < 0.01 ? t / 0.01 : Math.Pow(0.0001, (t - 0.01) / Math.Max(0.001, v.Dur - 0.01));
                    s += Osc(v.Wave, v.Phase) * v.Volume * env;
                }

                float outSample = Mathf.Clamp((float)s * master, -1f, 1f);
                peak=Mathf.Max(peak,Mathf.Abs(outSample));
                for (int ch = 0; ch < channels; ch++) data[i + ch] = outSample;
                now += dt;
            }
            System.Threading.Interlocked.Add(ref renderedFrames,data.Length/channels);
            outputPeak=peak;
        }
#endif
    }
}
