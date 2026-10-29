using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.AudioSys
{
    /// <summary>
    /// Player vehicle sound, mixed like a racing game:
    /// - engine = live physical model (EngineSynth) fed with rpm + load every frame; in WebGL, where live
    ///   audio isn't available, 7 rpm layers × on/off throttle pre-rendered from the same model
    /// - simulated gearbox (rpm climbs, drops ~1/3 on each up-shift with a throttle cut)
    /// - turbo whistle + blow-off (diesel), exhaust pops on lift-off (V8 / V10 / sports bike)
    /// - tyre roar and wind that grow with speed, tyre squeal on handbrake / hard cornering
    /// - cabin muffling in the driver's-eye camera, horn (H), starter on entry
    /// </summary>
    public class VehicleAudio : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        const bool LiveEngine = false;
#else
        const bool LiveEngine = true;
#endif
        public PlayerDriveController drive;
        EngineSynth _synth;
        FortunerCity.CameraSys.ChaseCamera _chase;
        readonly AudioSource[] _on = new AudioSource[SoundSynth.Layers];
        readonly AudioSource[] _off = new AudioSource[SoundSynth.Layers];
        readonly float[] _layerRpm = new float[SoundSynth.Layers];
        AudioSource _turbo, _road, _wind, _skid, _horn, _fx;
        readonly System.Collections.Generic.List<AudioSource> _loops = new System.Collections.Generic.List<AudioSource>();
        SoundSynth.EngineVoice _voice;
        int _gears = 6, _gear = 1;
        float _rpm, _shiftTimer, _startTimer, _load, _prevThrottle, _blowCooldown, _popTimer, _skidVol;
        float _wobble, _wobbleTarget, _wobbleTimer, _boost;
        bool _bike, _pops;

        public static VehicleAudio Attach(GameObject car, PlayerDriveController drive)
        {
            var info = car.GetComponent<VehicleInfo>();
            var engine = info != null ? info.engine : EngineSound.Petrol4;

            var va = car.AddComponent<VehicleAudio>();
            va.drive = drive;
            va._voice = SoundSynth.Voice(engine);
            va._gears = info != null ? info.gears : 6;
            va._bike = info != null && info.isBike;
            va._pops = va._voice.crackle >= 0.2f;
            va._rpm = va._voice.idleRpm;
            for (int l = 0; l < SoundSynth.Layers; l++)
                va._layerRpm[l] = SoundSynth.LayerRpm(va._voice, l);
            if (LiveEngine)
                va._synth = EngineSynth.Create(car.transform, engine);
            else
                for (int l = 0; l < SoundSynth.Layers; l++)
                {
                    va._on[l] = va.Source("EngineOn" + l, SoundSynth.Engine(engine, l, true), true, 0.25f);
                    va._off[l] = va.Source("EngineOff" + l, SoundSynth.Engine(engine, l, false), true, 0.25f);
                }
            va._turbo = va._voice.turbo ? va.Source("Turbo", SoundSynth.Turbo(), true, 0.3f) : null;
            va._road = va.Source("Road", SoundSynth.RoadNoise(), true, 0.2f);
            va._wind = va.Source("Wind", SoundSynth.Wind(), true, 0.1f);
            va._skid = va.Source("Skid", SoundSynth.Skid(), true, 0.5f);
            va._horn = va.Source("Horn", SoundSynth.Horn(va._bike), true, 0.4f);
            va._fx = va.Source("Fx", null, false, 0.3f);
            va.StartEngine();

            if (car.GetComponent<CrashAudio>() == null) car.AddComponent<CrashAudio>();
            return va;
        }

        AudioSource Source(string name, AudioClip clip, bool loop, float spatial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = spatial;
            s.dopplerLevel = 0f;
            s.volume = 0f;
            if (loop && name != "Horn") _loops.Add(s);
            return s;
        }

        /// <summary>Engine off when the driver gets out, starter + engine when someone gets in.</summary>
        public void SetRunning(bool on)
        {
            enabled = on;
            if (on) { StartEngine(); return; }
            foreach (var s in _loops) s.Stop();
            if (_synth != null) _synth.Stop();
            _horn.Stop();
            if (drive != null) drive.displayGear = 0;
        }

        void StartEngine()
        {
            _fx.PlayOneShot(SoundSynth.Starter(), 0.8f);
            _startTimer = 0.6f;
            _rpm = _voice.idleRpm;
            _gear = 1;
            foreach (var s in _loops)
            {
                s.volume = 0f;
                s.time = Random.value * 0.5f;    // de-phase the loops
                s.Play();
            }
            if (_synth != null)
            {
                _synth.model.gain = 0f;
                _synth.model.targetRpm = _voice.idleRpm;
                _synth.Play();
            }
        }

        void Update()
        {
            if (drive == null) return;
            float dt = Time.deltaTime;
            float spd = drive.SpeedKmh;
            float top = Mathf.Max(30f, drive.maxSpeedKmh);
            float throttle = Mathf.Clamp01(drive.IsReverse ? -drive.Throttle : drive.Throttle);

            // --- gearbox: gear g spans 0 … top·(g/gears); rpm drops to ~g/(g+1) after an up-shift
            float edge = top * _gear / _gears;
            if (spd / edge > 0.96f && _gear < _gears && _shiftTimer <= 0f)
            {
                _gear++;
                _shiftTimer = 0.28f;
                if (_pops && Random.value < 0.6f) Pop(0.7f);       // shift crackle on sporty engines
            }
            else if (_gear > 1 && spd < top * (_gear - 1) / _gears * 0.72f) _gear--;
            edge = top * _gear / _gears;
            float frac = Mathf.Clamp01(spd / edge);

            float targetRpm;
            if (spd < 3f && !drive.IsReverse)
                targetRpm = _voice.idleRpm + throttle * (_voice.redRpm - _voice.idleRpm) * 0.55f;   // revving at a standstill
            else
                targetRpm = Mathf.Lerp(_voice.idleRpm * 1.3f, _voice.redRpm, frac);
            if (_shiftTimer > 0f) { _shiftTimer -= dt; targetRpm *= 0.85f; }
            _rpm = Mathf.MoveTowards(_rpm, targetRpm, (_voice.redRpm - _voice.idleRpm) * 2.2f * dt);
            float rpmFrac = Mathf.InverseLerp(_voice.idleRpm, _voice.redRpm, _rpm);
            drive.displayGear = spd < 1f ? 0 : _gear;

            // --- a real engine never holds a perfectly steady rpm: small wander, strongest at idle
            _wobbleTimer -= dt;
            if (_wobbleTimer <= 0f)
            {
                _wobbleTimer = Random.Range(0.08f, 0.25f);
                _wobbleTarget = (Random.value - 0.5f) * 2f * Mathf.Lerp(0.014f, 0.003f, rpmFrac);
            }
            _wobble = Mathf.Lerp(_wobble, _wobbleTarget, 1f - Mathf.Exp(-6f * dt));
            float heardRpm = _rpm * (1f + _wobble);

            // --- load: on-throttle vs overrun (idling counts as steady load)
            float idleLoad = 1f - Mathf.InverseLerp(_layerRpm[0], _layerRpm[1], _rpm);
            float wantLoad = _shiftTimer > 0f ? 0f : Mathf.Max(throttle, idleLoad);
            _load = Mathf.MoveTowards(_load, wantLoad, 6f * dt);

            float master = Mathf.Lerp(0.45f, 0.85f, rpmFrac);
            if (_startTimer > 0f) { _startTimer -= dt; master *= 1f - Mathf.Clamp01(_startTimer / 0.6f); }
            if (_chase == null && Camera.main != null) _chase = Camera.main.GetComponent<FortunerCity.CameraSys.ChaseCamera>();
            bool cabin = _chase != null && _chase.HoodView;

            if (_synth != null)
            {
                // live model: it follows rpm and load itself (fixed exhaust resonances, no pitch-shifting)
                var m = _synth.model;
                m.targetRpm = heardRpm;
                m.targetLoad = _shiftTimer > 0f ? 0f : Mathf.Max(throttle, idleLoad * 0.5f);
                m.interior = cabin;
                m.gain = master * 0.75f;
            }
            else
            {
                // WebGL fallback — 14 loops: equal-power rpm cross-fades (no volume dip between layers) × load
                for (int l = 0; l < SoundSynth.Layers; l++)
                {
                    float w = Mathf.Sqrt(LayerWeight(l));
                    float pitch = Mathf.Clamp(heardRpm / _layerRpm[l], 0.5f, 2.2f);
                    _on[l].pitch = _off[l].pitch = pitch;
                    _on[l].volume = master * w * _load * (cabin ? 0.7f : 1f);
                    _off[l].volume = master * w * (1f - _load) * 0.6f * (cabin ? 0.7f : 1f);
                }
            }

            // --- turbo whistle, blow-off, lift-off pops
            if (_turbo != null)
            {
                // boost builds up with a little lag and drops quickly when the throttle closes
                float wantBoost = Mathf.Clamp01(throttle * Mathf.InverseLerp(0.15f, 0.7f, rpmFrac) * 1.2f);
                _boost = Mathf.MoveTowards(_boost, wantBoost, (wantBoost > _boost ? 1.4f : 4f) * dt);
                _turbo.pitch = 0.55f + _boost * 0.6f + rpmFrac * 0.35f;
                _turbo.volume = _boost * 0.17f;
            }
            _blowCooldown -= dt;
            bool liftOff = _prevThrottle > 0.6f && throttle < 0.1f && rpmFrac > 0.35f;
            if (liftOff && _voice.turbo && _blowCooldown <= 0f)
            {
                _fx.PlayOneShot(SoundSynth.BlowOff(), 0.55f);
                _blowCooldown = 1.5f;
            }
            if (liftOff && _pops && _synth == null) _popTimer = 0.7f;     // the live model makes its own crackle
            if (_popTimer > 0f)
            {
                _popTimer -= dt;
                if (Random.value < dt * 6f) Pop(Random.Range(0.4f, 0.9f));
            }
            _prevThrottle = throttle;

            // --- road roar, wind, tyre squeal
            _road.volume = Mathf.Clamp01(spd / 110f) * (_bike ? 0.18f : 0.32f);
            _road.pitch = 0.7f + Mathf.Clamp01(spd / 150f) * 0.6f;
            float w2 = Mathf.Clamp01(spd / 160f);
            _wind.volume = w2 * w2 * (_bike ? 0.5f : 0.3f) * (cabin ? 0.45f : 1f);
            _wind.pitch = 0.8f + w2 * 0.4f;

            var rb = drive.GetComponent<Rigidbody>();
            float slip = rb != null ? Mathf.Abs(rb.angularVelocity.y) * spd * 0.02f : 0f;
            bool squeal = !_bike && ((drive.Handbrake && spd > 15f) || slip > 0.45f);
            _skidVol = Mathf.MoveTowards(_skidVol, squeal ? 0.4f : 0f, 3f * dt);
            _skid.volume = _skidVol;
            _skid.pitch = 0.9f + Mathf.Clamp01(spd / 100f) * 0.3f;

            // --- horn
            bool horn = Input.GetKey(KeyCode.H) && Time.timeScale > 0f;
            if (horn && !_horn.isPlaying) { _horn.volume = 1f; _horn.Play(); }
            else if (!horn && _horn.isPlaying) _horn.Stop();
        }

        /// <summary>Triangular cross-fade weight of rpm layer l at the current rpm.</summary>
        float LayerWeight(int l)
        {
            float r = _rpm;
            float c = _layerRpm[l];
            float lo = l > 0 ? _layerRpm[l - 1] : float.NegativeInfinity;
            float hi = l < SoundSynth.Layers - 1 ? _layerRpm[l + 1] : float.PositiveInfinity;
            if (r <= c) return float.IsInfinity(lo) ? 1f : Mathf.Clamp01((r - lo) / (c - lo));
            return float.IsInfinity(hi) ? 1f : Mathf.Clamp01((hi - r) / (hi - c));
        }

        void Pop(float volume)
        {
            _fx.pitch = Random.Range(0.8f, 1.2f);
            _fx.PlayOneShot(SoundSynth.Backfire(), volume);
        }
    }

    /// <summary>Quiet 3D engine hum for AI traffic. Not attached by default: only the vehicle the player drives is heard.</summary>
    public class TrafficAudio : MonoBehaviour
    {
        AudioSource _src;
        Vector3 _last;

        public static void Attach(GameObject car, EngineSound engine)
        {
            var ta = car.AddComponent<TrafficAudio>();
            var s = car.AddComponent<AudioSource>();
            s.clip = SoundSynth.Engine(engine, 1, true);
            s.loop = true;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 4f;
            s.maxDistance = 35f;
            s.volume = 0.3f;
            s.dopplerLevel = 0.4f;
            s.time = Random.value * 0.8f;
            s.Play();
            ta._src = s;
            ta._last = car.transform.position;
        }

        void Update()
        {
            float spd = (transform.position - _last).magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
            _last = transform.position;
            _src.pitch = Mathf.Lerp(0.7f, 1.6f, Mathf.InverseLerp(0f, 25f, spd));
        }

        /// <summary>Removed when the player takes the car over (VehicleAudio takes its place).</summary>
        public static void Remove(GameObject car)
        {
            var ta = car.GetComponent<TrafficAudio>();
            if (ta == null) return;
            if (ta._src != null) Object.Destroy(ta._src);
            Object.Destroy(ta);
        }
    }

    public class CrashAudio : MonoBehaviour
    {
        AudioSource _src;
        float _cooldown;

        void Awake()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0.85f;
            _src.clip = MakeThump();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (_cooldown > 0f) return;
            if (collision.relativeVelocity.magnitude < 4f) return;
            _src.pitch = Random.Range(0.85f, 1.15f);
            _src.volume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 20f);
            _src.Play();
            _cooldown = 0.35f;
        }

        void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
        }

        static AudioClip MakeThump()
        {
            const int sampleRate = 44100;
            int count = sampleRate / 6;
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 70f * t) * env * 0.7f
                          + (Random.value * 2f - 1f) * env * 0.3f;
            }
            var clip = AudioClip.Create("Crash", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
