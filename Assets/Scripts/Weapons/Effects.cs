using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.Weapons
{
    /// <summary>
    /// Runtime visual effects built in code: smoke / fire particles, bullet impact puffs, tracers,
    /// muzzle flash light and bullet holes (pooled so firing never allocates much).
    /// </summary>
    public static class Effects
    {
        static Material _particle, _additive, _hole;
        static ParticleSystem _impacts;
        static readonly Queue<GameObject> Holes = new Queue<GameObject>();
        static readonly List<LineRenderer> Tracers = new List<LineRenderer>();
        static Mesh _holeMesh;

        static Texture2D SoftDot()
        {
            const int S = 32;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(S / 2f - 0.5f, S / 2f - 0.5f)) / (S / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            t.Apply();
            return t;
        }

        static Material ParticleMat(bool additive)
        {
            if (additive && _additive != null) return _additive;
            if (!additive && _particle != null) return _particle;
            var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { mainTexture = SoftDot() };
            if (m.HasProperty("_Mode"))
            {
                // Particles/Standard Unlit: 2 = fade (alpha blended), 4 = additive
                m.SetFloat("_Mode", additive ? 4f : 2f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
            }
            if (additive) _additive = m; else _particle = m;
            return m;
        }

        static ParticleSystem MakeSystem(string name, Transform parent, Vector3 localPos, bool additive)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // emit upwards
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat(additive);
            return ps;
        }

        public static ParticleSystem Smoke(Transform parent, Vector3 localPos, Color color, float rate)
        {
            var ps = MakeSystem("Smoke", parent, localPos, false);
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = 1.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startColor = color;
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.25f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.8f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            ps.Play();
            return ps;
        }

        public static ParticleSystem Fire(Transform parent, Vector3 localPos)
        {
            var ps = MakeSystem("Fire", parent, localPos, true);
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 1.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f, 0.9f), new Color(1f, 0.85f, 0.3f, 0.9f));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 40f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.3f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            ps.Play();

            var light = new GameObject("FireLight").AddComponent<Light>();
            light.transform.SetParent(ps.transform, false);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.range = 6f;
            light.intensity = 2f;
            return ps;
        }

        /// <summary>Small puff of dust / sparks where a bullet lands.</summary>
        public static void Impact(Vector3 point, Vector3 normal, bool metal)
        {
            if (_impacts == null)
            {
                var host = new GameObject("BulletImpacts");
                _impacts = host.AddComponent<ParticleSystem>();
                _impacts.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _impacts.main;
                main.startLifetime = 0.5f;
                main.startSize = 0.12f;
                main.maxParticles = 400;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = 0.6f;
                var emission = _impacts.emission;
                emission.enabled = false;
                host.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat(false);
                _impacts.Play();
            }
            var p = new ParticleSystem.EmitParams { position = point + normal * 0.02f };
            for (int i = 0; i < 6; i++)
            {
                p.velocity = (normal + Random.insideUnitSphere * 0.7f).normalized * Random.Range(1.5f, 4f);
                p.startColor = metal ? new Color(1f, 0.8f, 0.4f, 1f) : new Color(0.55f, 0.52f, 0.48f, 0.9f);
                p.startSize = metal ? 0.05f : Random.Range(0.08f, 0.18f);
                _impacts.Emit(p, 1);
            }
        }

        /// <summary>Dark bullet mark on a surface (parented to moving things like cars). Keeps the newest 40.</summary>
        public static void BulletHole(Vector3 point, Vector3 normal, Transform parent)
        {
            if (_hole == null)
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                _hole = new Material(shader) { color = new Color(0.05f, 0.05f, 0.05f) };
                var mb = new FortunerCity.Vehicle.MeshBuilder(1);
                mb.Quad(0, new Vector3(-0.04f, -0.04f, 0f), new Vector3(-0.04f, 0.04f, 0f), new Vector3(0.04f, 0.04f, 0f), new Vector3(0.04f, -0.04f, 0f), Vector3.back);
                _holeMesh = mb.ToMesh("BulletHole");
            }
            GameObject go = Holes.Count >= 40 ? Holes.Dequeue() : null;
            if (go == null)
            {
                go = new GameObject("BulletHole");
                go.AddComponent<MeshFilter>().sharedMesh = _holeMesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = _hole;
            }
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(point + normal * 0.01f, Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            Holes.Enqueue(go);
        }

        /// <summary>Brief bright streak from the muzzle to the hit point.</summary>
        public static void Tracer(Vector3 from, Vector3 to)
        {
            LineRenderer lr = null;
            foreach (var t in Tracers) if (!t.enabled) { lr = t; break; }
            if (lr == null)
            {
                var go = new GameObject("Tracer");
                lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = ParticleMat(true);
                lr.positionCount = 2;
                lr.startWidth = 0.025f;
                lr.endWidth = 0.01f;
                lr.startColor = new Color(1f, 0.9f, 0.6f, 0.9f);
                lr.endColor = new Color(1f, 0.8f, 0.4f, 0.2f);
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.AddComponent<AutoDisable>();
                Tracers.Add(lr);
            }
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.enabled = true;
            lr.GetComponent<AutoDisable>().Arm(0.05f);
        }

        /// <summary>Turns a renderer / light off again after a short time (tracers, muzzle flash).</summary>
        public class AutoDisable : MonoBehaviour
        {
            float _t;
            Behaviour _target;
            Renderer _renderer;

            public void Arm(float seconds)
            {
                _t = seconds;
                if (_renderer == null) _renderer = GetComponent<Renderer>();
                if (_target == null) _target = GetComponent<Light>();
                if (_renderer != null) _renderer.enabled = true;
                if (_target != null) _target.enabled = true;
                enabled = true;
            }

            void Update()
            {
                _t -= Time.deltaTime;
                if (_t > 0f) return;
                if (_renderer != null) _renderer.enabled = false;
                if (_target != null) _target.enabled = false;
                enabled = false;
            }
        }
    }
}
