using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.AudioSys
{
    /// <summary>
    /// Physically-inspired engine sound generator (pure C#, safe to run on the audio thread).
    ///
    /// Each cylinder's exhaust valve opening releases a pressure pulse (smooth blow-down whose length
    /// follows the crank angle, so pulses shorten as rpm rises). Pulses get per-cylinder imbalance and
    /// per-firing variation, then travel through an exhaust pipe (a damped feedback delay line → a fixed
    /// harmonic series like a real pipe) and a muffler (fixed resonances + a load-dependent low-pass).
    /// Intake breath, valve-train tick, diesel clatter, low rumble and overrun crackle are added on top.
    /// Because the pipe / muffler resonances stay fixed while only the firing rate changes, it doesn't
    /// get the "chipmunk" sound of pitch-shifted loops.
    /// </summary>
    public class EngineModel
    {
        public class Voice
        {
            public int cylinders = 4;
            public float[] firing = { 0f, 0.25f, 0.5f, 0.75f };    // firing positions in the 2-rev cycle (0..1)
            public float[] cylinderGain = { 1f, 0.95f, 1.05f, 0.97f };
            public float idleRpm = 800f, redRpm = 6500f;
            public float pipeDelay = 0.007f, pipeFeedback = 0.6f;   // seconds, 0..0.9
            public float res1 = 140f, q1 = 2f, gain1 = 0.6f;         // muffler / body resonances (fixed)
            public float res2 = 420f, q2 = 3f, gain2 = 0.35f;
            public float brightOn = 2800f, brightOff = 1300f;        // muffler low-pass with / without throttle
            public float rumble = 0.35f;                              // sub-bass weight
            public float intake = 0.2f, intakeHz = 900f;
            public float mech = 0.08f;                                // valve-train tick
            public float clatter = 0f;                                // diesel combustion knock
            public float crackle = 0f;                                // overrun pops
            public float roughness = 0.08f;                           // firing-to-firing variation
            public float noise = 0.5f;                                // combustion noise burst riding on each firing
            public float rasp = 0.12f, raspHz = 1800f;                // turbulent exhaust flow noise (the "rasp")
            public float drive = 1.3f;
            public float level = 1f;                                  // loudness trim so every engine sits at a similar volume
            public bool turbo;
        }

        public static Voice VoiceFor(EngineSound e)
        {
            switch (e)
            {
                case EngineSound.Diesel4:   // 2.8 L turbo-diesel (Fortuner)
                    return new Voice { idleRpm = 750, redRpm = 4300, pipeDelay = 0.009f, pipeFeedback = 0.55f,
                        res1 = 95, q1 = 2f, gain1 = 0.7f, res2 = 280, q2 = 3f, gain2 = 0.3f, brightOn = 1700, brightOff = 900,
                        rumble = 0.65f, intake = 0.15f, intakeHz = 700, mech = 0.1f, clatter = 0.55f, roughness = 0.14f, drive = 1.2f, level = 1.1f, turbo = true, noise = 0.6f, rasp = 0.1f, raspHz = 1200 };
                case EngineSound.V8:        // cross-plane V8: uneven bank firing → burble
                    return new Voice { cylinders = 8, idleRpm = 650, redRpm = 6300,
                        firing = new[] { 0f, 0.115f, 0.25f, 0.385f, 0.5f, 0.62f, 0.75f, 0.88f },
                        cylinderGain = new[] { 1f, 0.82f, 1.08f, 0.86f, 0.97f, 0.8f, 1.12f, 0.88f },
                        pipeDelay = 0.011f, pipeFeedback = 0.55f, res1 = 75, q1 = 1.8f, gain1 = 0.7f, res2 = 380, q2 = 2.2f, gain2 = 0.5f,
                        brightOn = 3200, brightOff = 1300, rumble = 0.8f, intake = 0.2f, intakeHz = 650, mech = 0.05f,
                        crackle = 0.3f, roughness = 0.07f, drive = 1.7f, level = 3f, noise = 0.85f, rasp = 0.28f, raspHz = 1100 };
                case EngineSound.V10:       // high-revving V10 supercar
                    return new Voice { cylinders = 10, idleRpm = 1000, redRpm = 8700,
                        firing = new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f },
                        cylinderGain = new[] { 1f, 0.96f, 1.03f, 0.98f, 1.01f, 0.97f, 1.04f, 0.99f, 1.02f, 0.95f },
                        pipeDelay = 0.005f, pipeFeedback = 0.55f, res1 = 180, q1 = 2.5f, gain1 = 0.55f, res2 = 520, q2 = 3.5f, gain2 = 0.45f,
                        brightOn = 5200, brightOff = 2500, rumble = 0.3f, intake = 0.35f, intakeHz = 1200, mech = 0.04f,
                        crackle = 0.4f, roughness = 0.03f, drive = 1.6f, level = 1.4f, noise = 0.25f, rasp = 0.07f, raspHz = 1800 };
                case EngineSound.Single:    // 70 cc single (CD70): putt-putt with a long gap between firings
                    return new Voice { cylinders = 1, firing = new[] { 0f }, cylinderGain = new[] { 1f }, idleRpm = 1300, redRpm = 8000,
                        pipeDelay = 0.006f, pipeFeedback = 0.5f, res1 = 160, q1 = 2f, gain1 = 0.6f, res2 = 600, q2 = 3f, gain2 = 0.35f,
                        brightOn = 3000, brightOff = 1500, rumble = 0.2f, intake = 0.15f, intakeHz = 1000, mech = 0.2f,
                        crackle = 0.05f, roughness = 0.12f, drive = 1.4f, noise = 0.6f, rasp = 0.12f, raspHz = 1600 };
                case EngineSound.SportBike: // 600 cc inline-four screamer
                    return new Voice { idleRpm = 1400, redRpm = 13500, pipeDelay = 0.004f, pipeFeedback = 0.5f,
                        res1 = 240, q1 = 2.5f, gain1 = 0.5f, res2 = 700, q2 = 3.5f, gain2 = 0.45f, brightOn = 6000, brightOff = 3000,
                        rumble = 0.2f, intake = 0.4f, intakeHz = 1500, mech = 0.05f, crackle = 0.35f, roughness = 0.04f, drive = 1.5f, level = 0.5f, noise = 0.3f, rasp = 0.1f, raspHz = 2400 };
                default:                    // 4-cylinder petrol crossover
                    return new Voice();
            }
        }

        // ---------- runtime parameters (set from the main thread) ----------
        public volatile float targetRpm;
        public volatile float targetLoad;     // 0 = closed throttle (overrun), 1 = full load
        public volatile bool interior;        // heard from inside the cabin: muffled, bassier
        public float gain = 0.5f;

        readonly Voice _v;
        readonly int _sr;
        readonly System.Random _rng;
        float _rpm, _load;
        double _phase;
        readonly float[] _pulseT, _pulseAmp, _noiseEnv;
        float _tickT = 1f, _clatterAmp;
        readonly float[] _delay = new float[4096];
        int _dw;
        float _dampState, _lpState, _lp2State, _rumbleState, _intakeLp, _dcX, _dcY, _dcX2, _dcY2, _interiorState, _clatterT = 1f, _popT = 1f;
        readonly Biquad _bp1, _bp2, _intakeBp, _raspBp, _clatterBp;

        public EngineModel(Voice voice, int sampleRate, int seed)
        {
            _v = voice;
            _sr = sampleRate;
            _rng = new System.Random(seed);
            _pulseT = new float[voice.cylinders];
            _pulseAmp = new float[voice.cylinders];
            _noiseEnv = new float[voice.cylinders];
            for (int i = 0; i < _pulseT.Length; i++) _pulseT[i] = 1f;
            _bp1 = new Biquad(voice.res1, voice.q1, sampleRate);
            _bp2 = new Biquad(voice.res2, voice.q2, sampleRate);
            _intakeBp = new Biquad(voice.intakeHz, 1f, sampleRate);
            _raspBp = new Biquad(voice.raspHz, 0.8f, sampleRate);
            _clatterBp = new Biquad(2600f, 1.2f, sampleRate);
            _rpm = targetRpm = voice.idleRpm;
            _dt = 1f / sampleRate;
            _rpmSmooth = 1f - Mathf.Exp(-_dt * 18f);
            _loadSmooth = 1f - Mathf.Exp(-_dt * 12f);
            _rumbleA = 1f - Mathf.Exp(-2f * Mathf.PI * 110f * _dt);
            _interiorA = 1f - Mathf.Exp(-2f * Mathf.PI * 650f * _dt);
            _hpR = Mathf.Exp(-2f * Mathf.PI * 45f * _dt);
        }

        readonly float _dt, _rpmSmooth, _loadSmooth, _rumbleA, _interiorA, _hpR;

        float Rand() => (float)_rng.NextDouble();

        /// <summary>Fill an interleaved buffer (adds the same mono signal to every channel).</summary>
        public void Render(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                float s = NextSample();
                for (int c = 0; c < channels; c++) data[i + c] = s;
            }
        }

        public float NextSample()
        {
            float dt = _dt;
            // smooth the controls so gear shifts / throttle blips don't click
            _rpm += (targetRpm - _rpm) * _rpmSmooth;
            _load += (targetLoad - _load) * _loadSmooth;
            float rpm = Mathf.Max(200f, _rpm);
            float rpmFrac = Mathf.Clamp01((rpm - _v.idleRpm) / (_v.redRpm - _v.idleRpm));

            // --- crank: one 4-stroke cycle = 2 revolutions
            double prev = _phase;
            _phase += rpm / 120.0 * dt;
            bool wrapped = _phase >= 1.0;
            if (wrapped) _phase -= 1.0;

            // exhaust valve open ≈ 220° of crank → seconds
            float openTime = 0.611f * 60f / rpm;
            float idleWobble = 1f - rpmFrac;
            float interval = 120f / rpm / _v.cylinders;                                    // seconds between firings
            for (int k = 0; k < _v.cylinders; k++)
            {
                double f = _v.firing[k];
                // did the crank pass this cylinder's firing point during this sample (incl. the cycle wrap)?
                bool crossed = wrapped ? (f > prev || f <= _phase) : (prev < f && _phase >= f);
                if (!crossed) continue;
                float vary = 1f + (Rand() - 0.5f) * 2f * (_v.roughness + idleWobble * 0.05f);
                float amp = _v.cylinderGain[k % _v.cylinderGain.Length] * vary * (0.3f + 0.7f * _load);
                if (_load < 0.15f) amp *= 0.5f + 0.4f * Rand();                         // lumpy overrun
                _pulseAmp[k] = amp;
                // real combustion never lands exactly on time: a small random delay breaks the "buzzer" periodicity
                _pulseT[k] = -Rand() * interval * (0.03f + _v.roughness * 0.25f + idleWobble * 0.03f);
                _noiseEnv[k] = amp * (0.6f + 0.8f * Rand());
                if (_v.clatter > 0f) { _clatterT = 0f; _clatterAmp = amp; }
                if (_v.crackle > 0f && _load < 0.1f && rpmFrac > 0.35f && Rand() < _v.crackle * 0.05f) _popT = 0f;
            }

            // --- pressure pulses: sharp blow-down peak as the valve cracks open, then a fast decay
            //     (sharp enough that 8-10 cylinders don't smear into a flat, silent pressure)
            float white = Rand() * 2f - 1f;
            float pulse = 0f, burst = 0f;
            float noiseDecay = Mathf.Exp(-dt / Mathf.Max(0.0015f, openTime * 0.45f));
            for (int k = 0; k < _v.cylinders; k++)
            {
                float t = _pulseT[k];
                if (t < openTime)
                {
                    if (t >= 0f)
                    {
                        float x = t / openTime;
                        float shape = (1f - Mathf.Exp(-x * 40f)) * Mathf.Exp(-x * 7f);
                        pulse += _pulseAmp[k] * shape;
                        // combustion roar: noise burst that starts with the blow-down and dies away a little slower
                        burst += _noiseEnv[k];
                        _noiseEnv[k] *= noiseDecay;
                    }
                    _pulseT[k] = t + dt;
                }
            }
            float exc = pulse + white * (Mathf.Abs(pulse) * 0.3f + burst * _v.noise * 0.55f);

            // --- exhaust pipe: damped feedback delay → fixed harmonic series
            int delaySamples = Mathf.Clamp((int)(_v.pipeDelay * _sr), 8, _delay.Length - 1);
            int dr = _dw - delaySamples;
            if (dr < 0) dr += _delay.Length;
            float back = _delay[dr];
            _dampState += (back - _dampState) * 0.45f;                                  // losses in the pipe
            float pipe = exc + _v.pipeFeedback * _dampState;
            _delay[_dw] = pipe;
            _dw = (_dw + 1) % _delay.Length;

            // --- muffler: fixed resonances + low-pass that opens up under load
            float cutoff = Mathf.Lerp(_v.brightOff, _v.brightOn, _load) * (0.8f + 0.4f * rpmFrac);
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff * dt);
            _lpState += (pipe - _lpState) * a;
            _lp2State += (_lpState - _lp2State) * a;
            float body = _lp2State + _bp1.Process(pipe) * _v.gain1 + _bp2.Process(pipe) * _v.gain2;

            // --- low rumble (kept moderate: small speakers can't play deep bass, and too much of it buries the mids)
            _rumbleState += (pipe - _rumbleState) * _rumbleA;
            body += _rumbleState * _v.rumble * 0.9f;

            // --- turbulent flow rasp: band-passed noise that grows with load and rpm, gated by the firing pulses
            float flow = (0.25f + 0.75f * _load) * (0.3f + rpmFrac) * (0.35f + Mathf.Min(1.5f, Mathf.Abs(pulse) + burst));
            body += _raspBp.Process(white) * _v.rasp * flow;

            // --- intake breath (gets louder with throttle and rpm)
            _intakeLp += (white - _intakeLp) * 0.5f;
            float breathEnv = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin((float)(_phase * Mathf.PI * Mathf.Max(1, _v.cylinders))));
            body += _intakeBp.Process(_intakeLp) * _v.intake * (0.2f + 0.8f * _load) * (0.4f + rpmFrac) * breathEnv;

            // --- valve-train tick once per cycle and diesel knock: short filtered bursts (a "tak", not a hiss)
            float knock = _clatterBp.Process(white);
            if (wrapped) _tickT = 0f;
            if (_tickT < 0.002f)
            {
                body += knock * _v.mech * 0.5f * Mathf.Exp(-_tickT * 2500f);
                _tickT += dt;
            }
            if (_clatterT < 0.006f)
            {
                body += knock * _v.clatter * 1.4f * _clatterAmp * Mathf.Exp(-_clatterT * 700f) * (0.3f + 0.7f * _load);
                _clatterT += dt;
            }

            // --- overrun crackle (added after the level trim so every engine pops at a similar loudness)
            float pop = 0f;
            if (_popT < 0.07f)
            {
                pop = (white * 0.7f + Mathf.Sin(2f * Mathf.PI * 95f * _popT)) * Mathf.Exp(-_popT * 55f) * 0.35f;
                _popT += dt;
            }

            // --- soft saturation, high-pass, optional cabin muffling
            float y = body * _v.drive;
            y = y / (1f + Mathf.Abs(y));
            // two-pole high-pass at 45 Hz: drops sub-bass that speakers can't play and that only eats headroom
            float hp1 = y - _dcX + _hpR * _dcY;
            _dcX = y;
            _dcY = hp1;
            float hp = hp1 - _dcX2 + _hpR * _dcY2;
            _dcX2 = hp1;
            _dcY2 = hp;
            if (interior)
            {
                _interiorState += (hp - _interiorState) * _interiorA;
                hp = _interiorState * 1.6f;
            }
            return (hp * _v.level + pop) * gain;
        }

        /// <summary>Render a seamless loop at fixed rpm/load (WebGL builds and AI traffic use these clips).</summary>
        public static float[] RenderLoop(Voice v, float rpm, float load, int sampleRate, float seconds, int seed)
        {
            var m = new EngineModel(v, sampleRate, seed) { targetRpm = rpm, targetLoad = load, gain = 1f };
            m._rpm = rpm;
            m._load = load;
            for (int i = 0; i < sampleRate / 5; i++) m.NextSample();   // settle the filters
            int n = (int)(sampleRate * seconds);
            const int Fade = 2048;
            var buf = new float[n + Fade];
            for (int i = 0; i < buf.Length; i++) buf[i] = m.NextSample();
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = buf[i];
            for (int i = 0; i < Fade; i++)
            {
                float w = i / (float)Fade;
                o[i] = buf[i] * w + buf[n + i] * (1f - w);
            }
            // match loudness (RMS), not peaks, so neighbouring rpm layers cross-fade without pumping and the
            // overrun layers don't come out louder than the on-throttle ones; a soft limiter catches the peaks
            double sq = 0;
            foreach (float s in o) sq += s * s;
            float rms = Mathf.Max(1e-6f, (float)System.Math.Sqrt(sq / n));
            float g = 0.2f / rms;
            for (int i = 0; i < n; i++)
            {
                float x = o[i] * g;
                float ax = Mathf.Abs(x);
                o[i] = ax <= 0.7f ? x : Mathf.Sign(x) * (0.7f + 0.28f * (1f - Mathf.Exp(-(ax - 0.7f) / 0.28f)));
            }
            return o;
        }

        class Biquad
        {
            readonly float _b0, _b2, _a1, _a2;
            float _x1, _x2, _y1, _y2;

            public Biquad(float freq, float q, int sr)
            {
                float w0 = 2f * Mathf.PI * Mathf.Min(freq, sr * 0.45f) / sr;
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
    }
}
