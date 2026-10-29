using System.Collections.Generic;
using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.AudioSys
{
    /// <summary>
    /// All game sounds are synthesised in code (no audio files), cached as clips (WebGL-safe).
    /// Engines: each cylinder firing is an impulse that rings through exhaust/intake resonators, plus a
    /// body thump, combustion breath, diesel clatter and overrun crackle, soft-clipped like a real
    /// exhaust note. Recorded at 4 rpm points × on/off throttle so pitch-shifting stays natural.
    /// </summary>
    public static class SoundSynth
    {
        const int Rate = 44100;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public class EngineVoice
        {
            public int cylinders;
            public float idleRpm, redRpm;
            public float resonance, q, resonance2;   // exhaust tone Hz, sharpness, second resonance ratio
            public float breath, roughness, clatter, crackle, drive;
            public float[] pattern = { 1f };          // firing loudness pattern (cross-plane V8 burble)
            public bool turbo;
        }

        public const int Layers = 7;
        const int EngineRate = 32000;          // engine loops: plenty for exhaust sound, keeps the 12 loops quick to render

        public static EngineVoice Voice(EngineSound e)
        {
            switch (e)
            {
                case EngineSound.Diesel4:
                    return new EngineVoice { cylinders = 4, idleRpm = 750, redRpm = 4300, resonance = 120, q = 2.2f, resonance2 = 3.1f,
                        breath = 0.35f, roughness = 0.18f, clatter = 0.55f, drive = 1.4f, turbo = true };
                case EngineSound.V8:
                    return new EngineVoice { cylinders = 8, idleRpm = 650, redRpm = 6300, resonance = 95, q = 3.5f, resonance2 = 2.2f,
                        breath = 0.2f, roughness = 0.06f, crackle = 0.25f, drive = 2f,
                        pattern = new[] { 1f, 0.72f, 1.12f, 0.8f, 0.95f, 0.7f, 1.18f, 0.83f } };
                case EngineSound.V10:
                    return new EngineVoice { cylinders = 10, idleRpm = 1000, redRpm = 8700, resonance = 210, q = 4f, resonance2 = 2f,
                        breath = 0.15f, roughness = 0.04f, crackle = 0.35f, drive = 2.4f };
                case EngineSound.Single:
                    return new EngineVoice { cylinders = 1, idleRpm = 1300, redRpm = 8000, resonance = 170, q = 2.5f, resonance2 = 3.5f,
                        breath = 0.3f, roughness = 0.15f, crackle = 0.05f, drive = 1.8f };
                case EngineSound.SportBike:
                    return new EngineVoice { cylinders = 4, idleRpm = 1400, redRpm = 13500, resonance = 300, q = 3.5f, resonance2 = 2.4f,
                        breath = 0.2f, roughness = 0.05f, crackle = 0.3f, drive = 2.2f };
                default:
                    return new EngineVoice { cylinders = 4, idleRpm = 800, redRpm = 6500, resonance = 160, q = 3f, resonance2 = 2.6f,
                        breath = 0.25f, roughness = 0.08f, drive = 1.6f };
            }
        }

        /// <summary>Rpm each layer is recorded at: idle, then 8 … 92 % of the rev range. Close layers keep pitch-shifting small (no "chipmunk" sound).</summary>
        public static float LayerRpm(EngineVoice v, int layer)
        {
            if (layer <= 0) return v.idleRpm;
            float[] f = { 0.08f, 0.2f, 0.35f, 0.52f, 0.72f, 0.92f };
            return Mathf.Lerp(v.idleRpm, v.redRpm, f[Mathf.Min(layer, f.Length) - 1]);
        }

        /// <summary>RBJ band-pass biquad — gives each firing pulse its exhaust "ring".</summary>
        class BandPass
        {
            readonly float _b0, _b2, _a1, _a2;
            float _x1, _x2, _y1, _y2;

            public BandPass(float freq, float q)
            {
                float w0 = 2f * Mathf.PI * Mathf.Min(freq, Rate * 0.45f) / Rate;
                float alpha = Mathf.Sin(w0) / (2f * q);
                float a0 = 1f + alpha;
                _b0 = alpha / a0;
                _b2 = -alpha / a0;
                _a1 = -2f * Mathf.Cos(w0) / a0;
                _a2 = (1f - alpha) / a0;
            }

            public float Process(float x)
            {
                float y = _b0 * x + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }

        class LowPass
        {
            readonly float _a;
            float _y;
            public LowPass(float freq) { _a = 1f - Mathf.Exp(-2f * Mathf.PI * freq / Rate); }
            public float Process(float x) { _y += (x - _y) * _a; return _y; }
        }

        /// <summary>Seamless engine loop for one rpm layer (on-throttle or overrun), rendered from the physical EngineModel.</summary>
        public static AudioClip Engine(EngineSound e, int layer, bool onLoad)
        {
            string key = $"eng_{e}_{layer}_{onLoad}";
            if (Cache.TryGetValue(key, out var cached)) return cached;
            float rpm = LayerRpm(Voice(e), layer);
            int seed = ((int)e + 1) * 97 + layer * 13 + (onLoad ? 1 : 0);
            // 1.5 s loops so the repeat isn't noticeable
            return Store(key, EngineModel.RenderLoop(EngineModel.VoiceFor(e), rpm, onLoad ? 1f : 0.05f, EngineRate, 1.5f, seed), EngineRate);
        }

        // ---------- driving ambience ----------

        /// <summary>Turbo whistle (pitched by rpm, louder with boost).</summary>
        public static AudioClip Turbo() => Make("turbo", () =>
        {
            var bp = new BandPass(3200f, 8f);
            var rng = new System.Random(21);
            int n = Rate;
            var b = new float[n + 2048];
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                b[i] = Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 6400f * t) * 0.1f
                       + bp.Process((float)rng.NextDouble() * 2f - 1f) * 0.6f;
            }
            Normalize(b, 0.5f);
            return LoopCrossfade(b, n, 2048);
        });

        /// <summary>Turbo blow-off "pssh-chu-chu" when the throttle snaps shut.</summary>
        public static AudioClip BlowOff() => Make("blowoff", () =>
        {
            var bp = new BandPass(2600f, 1.5f);
            var rng = new System.Random(22);
            int n = (int)(Rate * 0.5f);
            var b = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t * 100f) * Mathf.Exp(-t * 7f) * (0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 24f * t));
                b[i] = bp.Process((float)rng.NextDouble() * 2f - 1f) * env;
            }
            Normalize(b, 0.7f);
            return b;
        });

        /// <summary>Tyre roar on asphalt (low rumble), volume and pitch follow speed.</summary>
        public static AudioClip RoadNoise() => Make("road", () => NoiseLoop(23, f => new LowPass(260f), 0.6f, 2f));

        /// <summary>Wind rushing past the body at speed.</summary>
        public static AudioClip Wind() => Make("wind", () => NoiseLoop(24, f => new LowPass(1400f), 0.5f, 2f, true));

        /// <summary>Tyre squeal for handbrake turns and hard cornering.</summary>
        public static AudioClip Skid() => Make("skid", () =>
        {
            var bp = new BandPass(1500f, 3f);
            var rng = new System.Random(25);
            int n = Rate;
            var b = new float[n + 2048];
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float squeal = Mathf.Sin(2f * Mathf.PI * (1100f + 60f * Mathf.Sin(2f * Mathf.PI * 7f * t)) * t);
                b[i] = bp.Process((float)rng.NextDouble() * 2f - 1f) * 0.7f + squeal * 0.25f;
            }
            Normalize(b, 0.6f);
            return LoopCrossfade(b, n, 2048);
        });

        /// <summary>Single exhaust backfire pop (sports engines lifting off at high rpm).</summary>
        public static AudioClip Backfire() => Make("backfire", () =>
        {
            var bp = new BandPass(180f, 4f);
            var rng = new System.Random(26);
            int n = (int)(Rate * 0.15f);
            var b = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                b[i] = Tanh((bp.Process(i == 0 ? 30f : white * Mathf.Exp(-t * 40f)) + white * Mathf.Exp(-t * 70f) * 0.5f) * 1.5f);
            }
            Normalize(b, 0.9f);
            return b;
        });

        /// <summary>Footstep on pavement: heel thud + shoe scuff. Four variants so steps don't repeat.</summary>
        public static AudioClip Footstep(int variant, bool run) => Make($"step_{variant}_{run}", () =>
        {
            var thudBp = new BandPass(run ? 130f : 110f, 2f);
            var scuffBp = new BandPass(2200f + variant * 250f, 1.2f);
            var rng = new System.Random(40 + variant);
            int n = (int)(Rate * 0.18f);
            var b = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                float thud = thudBp.Process(white * Mathf.Exp(-t * 60f)) * 3f;
                float scuff = scuffBp.Process(white) * Mathf.Exp(-Mathf.Max(0f, t - 0.015f) * 45f) * (t > 0.015f ? 0.6f : 0f);
                b[i] = thud + scuff * (run ? 1.2f : 0.8f);
            }
            Normalize(b, run ? 0.8f : 0.6f);
            return b;
        });

        public static AudioClip Horn(bool bike)
        {
            string key = bike ? "horn_bike" : "horn_car";
            if (Cache.TryGetValue(key, out var c)) return c;
            float f1 = bike ? 520f : 415f, f2 = bike ? 0f : 495f;
            int n = Rate / 2;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = Square(f1 * t) + (f2 > 0f ? Square(f2 * t) : 0f);
                data[i] = s * 0.18f;
            }
            return Store(key, data);
        }

        public static AudioClip Starter()
        {
            if (Cache.TryGetValue("starter", out var c)) return c;
            int n = (int)(Rate * 0.9f);
            var data = new float[n];
            var rng = new System.Random(5);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float crank = Mathf.Sin(2f * Mathf.PI * 11f * t) > 0.3f ? 1f : 0.4f;                // starter "whirr-whirr"
                float whine = Mathf.Sin(2f * Mathf.PI * (180f + 40f * t) * t) * 0.3f;
                float catchUp = t > 0.65f ? Mathf.Exp(-(t - 0.65f) * 8f) * ((float)rng.NextDouble() * 2f - 1f) : 0f;
                data[i] = (whine * crank + catchUp * 0.8f) * 0.35f;
            }
            return Store("starter", data);
        }

        public enum Gun { Pistol, Rifle, Shotgun, Click, Reload }

        public static AudioClip GunSound(Gun g)
        {
            string key = "gun_" + g;
            if (Cache.TryGetValue(key, out var c)) return c;
            var rng = new System.Random((int)g + 11);
            float len = g == Gun.Shotgun ? 0.7f : g == Gun.Rifle ? 0.45f : g == Gun.Reload ? 0.6f : g == Gun.Click ? 0.05f : 0.35f;
            int n = (int)(Rate * len);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                float s;
                switch (g)
                {
                    case Gun.Click:
                        s = white * Mathf.Exp(-t * 400f) * 0.5f;
                        break;
                    case Gun.Reload:
                        // two mechanical clacks (mag out / in) and a slide rack
                        s = white * (Mathf.Exp(-t * 300f) + Mathf.Exp(-Mathf.Max(0f, t - 0.28f) * 300f) * (t > 0.28f ? 1f : 0f)
                            + Mathf.Exp(-Mathf.Max(0f, t - 0.5f) * 200f) * (t > 0.5f ? 1.2f : 0f)) * 0.35f;
                        break;
                    default:
                    {
                        float crackDecay = g == Gun.Pistol ? 90f : g == Gun.Rifle ? 70f : 35f;
                        float boomHz = g == Gun.Pistol ? 140f : g == Gun.Rifle ? 110f : 75f;
                        lp += (white - lp) * (g == Gun.Shotgun ? 0.15f : 0.35f);
                        float crack = lp * Mathf.Exp(-t * crackDecay);
                        float boom = Mathf.Sin(2f * Mathf.PI * boomHz * t) * Mathf.Exp(-t * crackDecay * 0.4f);
                        float tail = white * Mathf.Exp(-t * 9f) * 0.08f;                    // echo off the buildings
                        s = (crack * 0.9f + boom * 0.7f + tail) * 0.9f;
                        break;
                    }
                }
                data[i] = Mathf.Clamp(s, -1f, 1f);
            }
            return Store(key, data);
        }

        // ---------- helpers ----------

        static float Tanh(float x)
        {
            if (x > 4f) return 1f;
            if (x < -4f) return -1f;
            float e = Mathf.Exp(2f * x);
            return (e - 1f) / (e + 1f);
        }

        static float Square(float phase) => (phase % 1f) < 0.5f ? 1f : -1f;

        static void Normalize(float[] b, float peak)
        {
            float max = 1e-6f;
            foreach (float s in b) max = Mathf.Max(max, Mathf.Abs(s));
            float g = peak / max;
            for (int i = 0; i < b.Length; i++) b[i] *= g;
        }

        /// <summary>Blend the extra tail into the head so the loop has no click.</summary>
        static float[] LoopCrossfade(float[] buf, int n, int fade)
        {
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = buf[i];
            for (int i = 0; i < fade && n + i < buf.Length; i++)
            {
                float w = i / (float)fade;
                o[i] = buf[i] * w + buf[n + i] * (1f - w);
            }
            return o;
        }

        static float[] NoiseLoop(int seed, System.Func<int, LowPass> filter, float peak, float seconds, bool gusts = false)
        {
            var lp = filter(0);
            var lp2 = filter(0);
            var rng = new System.Random(seed);
            int n = (int)(Rate * seconds);
            var b = new float[n + 2048];
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float s = lp2.Process(lp.Process((float)rng.NextDouble() * 2f - 1f));
                if (gusts) s *= 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);   // whole cycles per loop
                b[i] = s;
            }
            Normalize(b, peak);
            return LoopCrossfade(b, n, 2048);
        }

        static AudioClip Make(string key, System.Func<float[]> build)
        {
            if (Cache.TryGetValue(key, out var c)) return c;
            return Store(key, build());
        }

        static AudioClip Store(string key, float[] data, int rate = Rate)
        {
            var clip = AudioClip.Create(key, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            Cache[key] = clip;
            return clip;
        }
    }
}
