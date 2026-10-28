using UnityEngine;
using FortunerCity.Vehicle;

namespace FortunerCity.AudioSys
{
    /// <summary>
    /// Live engine sound: runs EngineModel on the audio thread (OnAudioFilterRead on a clip-less
    /// AudioSource), so rpm and throttle change the sound continuously instead of pitch-shifting loops.
    /// Not available in WebGL (VehicleAudio falls back to pre-rendered layers there).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class EngineSynth : MonoBehaviour
    {
        public EngineModel model;
        AudioSource _src;

        public static EngineSynth Create(Transform parent, EngineSound engine)
        {
            var go = new GameObject("EngineSynth");
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;        // the player's own engine: centred, not panned
            src.dopplerLevel = 0f;
            var synth = go.AddComponent<EngineSynth>();
            synth._src = src;
            synth.model = new EngineModel(EngineModel.VoiceFor(engine), AudioSettings.outputSampleRate, Random.Range(1, 100000));
            return synth;
        }

        public void Play() { if (!_src.isPlaying) _src.Play(); }
        public void Stop() { _src.Stop(); }

        void OnAudioFilterRead(float[] data, int channels)
        {
            var m = model;
            if (m == null) { System.Array.Clear(data, 0, data.Length); return; }
            m.Render(data, channels);
        }
    }
}
