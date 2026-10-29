using System.Collections;
using UnityEngine;

namespace FortunerCity.AudioSys
{
    /// <summary>
    /// Background music made entirely in code (no files, no downloads): a relaxed lo-fi loop with electric piano
    /// chords, bass, drums and a soft flute melody. It is rendered a bar per frame so loading never stalls, then
    /// plays in 2D. M mutes / unmutes.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public float volume = 0.28f;
        AudioSource _src;
        bool _muted;

        public static MusicPlayer Create()
        {
            var go = new GameObject("Music");
            return go.AddComponent<MusicPlayer>();
        }

        IEnumerator Start()
        {
            var buf = new float[MusicSynth.Length];
            yield return MusicSynth.Render(buf);
            var clip = AudioClip.Create("MusicLoop", buf.Length, 1, MusicSynth.Rate, false);
            clip.SetData(buf, 0);
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = clip;
            _src.loop = true;
            _src.spatialBlend = 0f;
            _src.volume = volume;
            _src.priority = 0;
            _src.mute = _muted;
            _src.Play();
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.M)) return;
            _muted = !_muted;
            if (_src != null) _src.mute = _muted;
        }
    }

    /// <summary>Renders the music loop. Notes that ring past the end wrap to the start, so it loops seamlessly.</summary>
    public static class MusicSynth
    {
        public const int Rate = 22050;
        const float Bpm = 88f;
        const int Bars = 8;
        const float TwoPi = Mathf.PI * 2f;

        static float Beat => 60f / Bpm;
        public static int Length => Mathf.RoundToInt(Bars * 4 * Beat * Rate);

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        // A minor: Am9 - Fmaj9 - Cmaj7 - G6/9, two bars each (MIDI notes, voiced around middle C)
        static readonly int[][] Chords =
        {
            new[] { 57, 60, 64, 67, 71 },
            new[] { 53, 57, 60, 64, 67 },
            new[] { 48, 55, 59, 64, 67 },
            new[] { 55, 59, 62, 64, 69 },
        };
        static readonly int[] Roots = { 45, 41, 48, 43 };

        // melody for bars 5-8: (beat within the loop, MIDI note, length in beats), A minor pentatonic
        static readonly float[,] Melody =
        {
            { 16f, 76, 1.5f }, { 17.5f, 74, 0.5f }, { 18f, 72, 1f }, { 19f, 69, 1f },
            { 20f, 72, 2.5f }, { 22.5f, 74, 1.5f },
            { 24f, 76, 1f }, { 25f, 79, 1f }, { 26f, 76, 1.5f }, { 27.5f, 74, 0.5f },
            { 28f, 72, 1.5f }, { 29.5f, 69, 0.5f }, { 30f, 69, 2f },
        };

        // sine lookup over one cycle (phase given in cycles)
        const int TabSize = 4096;
        static float[] _sin;
        static float Sin(float cycles)
        {
            if (_sin == null)
            {
                _sin = new float[TabSize + 1];
                for (int i = 0; i <= TabSize; i++) _sin[i] = Mathf.Sin(i * TwoPi / TabSize);
            }
            float p = cycles - Mathf.Floor(cycles);
            return _sin[(int)(p * TabSize)];
        }

        /// <summary>Renders the whole loop at once (offline tools).</summary>
        public static float[] RenderLoop()
        {
            var buf = new float[Length];
            var it = Render(buf);
            while (it.MoveNext()) { }
            return buf;
        }

        /// <summary>Renders into buf, yielding after each bar so it can run across frames.</summary>
        public static IEnumerator Render(float[] buf)
        {
            var rng = new System.Random(11);
            int total = buf.Length;

            for (int bar = 0; bar < Bars; bar++)
            {
                int ci = (bar / 2) % Chords.Length;
                float barStart = bar * 4 * Beat;

                // electric piano: chord on beat 1 and a softer push on the "and" of 2
                foreach (int n in Chords[ci])
                {
                    EPiano(buf, barStart, Midi(n), 3.2f * Beat, 0.11f);
                    EPiano(buf, barStart + 2.5f * Beat, Midi(n), 1.4f * Beat, 0.06f);
                }

                // bass: root on 1, fifth on the "and" of 2, root again on 4
                Bass(buf, barStart, Midi(Roots[ci]), 1.6f * Beat, 0.32f);
                Bass(buf, barStart + 2.5f * Beat, Midi(Roots[ci] + 7), 0.8f * Beat, 0.24f);
                Bass(buf, barStart + 3f * Beat, Midi(Roots[ci]), 0.9f * Beat, 0.26f);

                // drums: kick on 1 and the "and" of 3, brushed snare on 2 and 4, swung hi-hats
                Kick(buf, barStart, 0.55f);
                Kick(buf, barStart + 2.5f * Beat, 0.45f);
                Snare(buf, barStart + Beat, 0.22f, rng);
                Snare(buf, barStart + 3f * Beat, 0.22f, rng);
                for (int h = 0; h < 8; h++)
                {
                    float swing = h % 2 == 1 ? 0.08f * Beat : 0f;
                    Hat(buf, barStart + h * 0.5f * Beat + swing, h % 2 == 0 ? 0.07f : 0.045f, rng);
                }
                yield return null;
            }

            for (int i = 0; i < Melody.GetLength(0); i++)
                Flute(buf, Melody[i, 0] * Beat, Midi((int)Melody[i, 1]), Melody[i, 2] * Beat, 0.1f);
            yield return null;

            // soft vinyl crackle, gentle tape saturation, normalise
            for (int i = 0; i < total; i++)
                if (rng.NextDouble() < 0.0004) buf[i] += (float)(rng.NextDouble() - 0.5) * 0.08f;
            float peak = 1e-4f;
            for (int i = 0; i < total; i++)
            {
                float x = buf[i] * 1.2f;
                buf[i] = x / (1f + Mathf.Abs(x));                 // soft clip
                peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            }
            float gain = 0.85f / peak;
            for (int i = 0; i < total; i++) buf[i] *= gain;
        }

        static void Add(float[] buf, int start, int i, float v) => buf[(start + i) % buf.Length] += v;
        static int Index(float time) => Mathf.RoundToInt(time * Rate);

        /// <summary>Two-operator FM electric piano: bell-like attack that mellows as it decays.</summary>
        static void EPiano(float[] buf, float start, float f, float len, float amp)
        {
            int s0 = Index(start), n = Mathf.RoundToInt((len + 0.6f) * Rate), off = Mathf.RoundToInt(len * Rate);
            float dt = 1f / Rate, kDecay = Mathf.Exp(-2.2f * dt), kIndex = Mathf.Exp(-6f * dt), kHarm = Mathf.Exp(-4f * dt), kRel = Mathf.Exp(-9f * dt);
            float env = 1f, idx = 1f, harm = 1f, rel = 1f;
            for (int i = 0; i < n; i++)
            {
                float c = f * i * dt;
                float mod = Sin(c) * (1.6f * idx + 0.25f) / TwoPi;
                float s = Sin(c + mod) + 0.15f * Sin(2f * c) * harm;
                float attack = Mathf.Min(1f, i * dt * 200f);
                Add(buf, s0, i, s * env * attack * rel * amp);
                env *= kDecay; idx *= kIndex; harm *= kHarm;
                if (i > off) rel *= kRel;
            }
        }

        static void Bass(float[] buf, float start, float f, float len, float amp)
        {
            int s0 = Index(start), n = Mathf.RoundToInt((len + 0.15f) * Rate), off = Mathf.RoundToInt(len * Rate);
            float dt = 1f / Rate, kDecay = Mathf.Exp(-1.5f * dt), kRel = Mathf.Exp(-25f * dt);
            float env = 1f, rel = 1f;
            for (int i = 0; i < n; i++)
            {
                float c = f * i * dt;
                float s = (Sin(c) + 0.25f * Sin(2f * c)) * 1.5f;
                s = s / (1f + Mathf.Abs(s));                      // warm saturation
                Add(buf, s0, i, s * env * rel * Mathf.Min(1f, i * dt * 120f) * amp);
                env *= kDecay;
                if (i > off) rel *= kRel;
            }
        }

        static void Kick(float[] buf, float start, float amp)
        {
            int s0 = Index(start), n = Mathf.RoundToInt(0.45f * Rate);
            float dt = 1f / Rate, kSweep = Mathf.Exp(-28f * dt), kDecay = Mathf.Exp(-7f * dt);
            float phase = 0f, sweep = 1f, env = 1f;
            for (int i = 0; i < n; i++)
            {
                phase += (48f + 90f * sweep) * dt;
                Add(buf, s0, i, Sin(phase) * env * amp);
                sweep *= kSweep; env *= kDecay;
            }
        }

        static void Snare(float[] buf, float start, float amp, System.Random rng)
        {
            int s0 = Index(start), n = Mathf.RoundToInt(0.3f * Rate);
            float dt = 1f / Rate, kNoise = Mathf.Exp(-14f * dt), kTone = Mathf.Exp(-30f * dt);
            float lp = 0f, envN = 1f, envT = 1f;
            for (int i = 0; i < n; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.45f;                       // soften: brushed lo-fi snare
                float tone = Sin(190f * i * dt) * envT;
                Add(buf, s0, i, (lp * envN + tone * 0.5f) * amp);
                envN *= kNoise; envT *= kTone;
            }
        }

        static void Hat(float[] buf, float start, float amp, System.Random rng)
        {
            int s0 = Index(start), n = Mathf.RoundToInt(0.06f * Rate);
            float k = Mathf.Exp(-70f / Rate), env = 1f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                Add(buf, s0, i, (noise - prev) * env * amp);      // crude high-pass for a crisp tick
                prev = noise;
                env *= k;
            }
        }

        /// <summary>Breathy flute: sine with delayed vibrato and a little filtered breath noise.</summary>
        static void Flute(float[] buf, float start, float f, float len, float amp)
        {
            int s0 = Index(start), n = Mathf.RoundToInt((len + 0.25f) * Rate), off = Mathf.RoundToInt(len * Rate);
            float dt = 1f / Rate, kRel = Mathf.Exp(-14f * dt);
            float phase = 0f, breath = 0f, rel = 1f;
            uint seed = (uint)(f * 1000f);
            for (int i = 0; i < n; i++)
            {
                float t = i * dt;
                float vib = 1f + 0.006f * Sin(5.2f * t) * Mathf.Clamp01((t - 0.25f) * 3f);
                phase += f * vib * dt;
                seed = seed * 1664525u + 1013904223u;
                breath += ((seed >> 8) / 8388608f - 1f - breath) * 0.12f;
                float s = Sin(phase) + 0.12f * Sin(2f * phase) + breath * 0.25f;
                Add(buf, s0, i, s * Mathf.Clamp01(t * 12f) * rel * amp);
                if (i > off) rel *= kRel;
            }
        }
    }
}
