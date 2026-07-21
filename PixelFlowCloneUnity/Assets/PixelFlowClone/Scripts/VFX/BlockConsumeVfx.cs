using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.VFX
{
    /// <summary>
    /// Plays a short ParticleSystem burst at each consumed block (P3-17).
    /// Uses a small runtime pool so gameplay does not Instantiate every hit.
    /// </summary>
    public class BlockConsumeVfx : MonoBehaviour
    {
        [SerializeField] private int _poolSize = 12;
        [SerializeField] private float _burstLifetime = 0.45f;
        [SerializeField] private int _burstCount = 18;

        private readonly Queue<ParticleSystem> _available = new();
        private readonly List<ParticleSystem> _all = new();

        private void Awake()
        {
            EnsurePool();
        }

        private void OnEnable()
        {
            GameEvents.OnBlockConsumed -= HandleBlockConsumed;
            GameEvents.OnBlockConsumed += HandleBlockConsumed;
        }

        private void OnDisable()
        {
            GameEvents.OnBlockConsumed -= HandleBlockConsumed;
        }

        private void OnDestroy()
        {
            GameEvents.OnBlockConsumed -= HandleBlockConsumed;
        }

        public static BlockConsumeVfx CreateRuntime(Transform parent = null)
        {
            var go = new GameObject("BlockConsumeVfx");
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.AddComponent<BlockConsumeVfx>();
        }

        private void HandleBlockConsumed(Vector3 worldPosition, ColorId color)
        {
            if (color == ColorId.None)
                return;

            ParticleSystem ps = Rent();
            if (ps == null)
                return;

            ps.transform.position = worldPosition;
            ApplyColor(ps, ColorPalette.ToColor(color));
            ps.Clear(true);
            ps.Play(true);
            StartCoroutine(ReturnWhenFinished(ps));
        }

        private void EnsurePool()
        {
            _poolSize = Mathf.Max(1, _poolSize);
            while (_all.Count < _poolSize)
            {
                ParticleSystem ps = CreateBurstSystem(transform);
                _all.Add(ps);
                _available.Enqueue(ps);
            }
        }

        private ParticleSystem Rent()
        {
            EnsurePool();

            if (_available.Count > 0)
                return _available.Dequeue();

            // All busy — reuse the oldest pooled instance.
            ParticleSystem fallback = _all[0];
            fallback.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return fallback;
        }

        private System.Collections.IEnumerator ReturnWhenFinished(ParticleSystem ps)
        {
            float wait = Mathf.Max(0.05f, _burstLifetime + 0.1f);
            yield return new WaitForSeconds(wait);

            if (ps == null)
                yield break;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (!_available.Contains(ps))
                _available.Enqueue(ps);
        }

        private ParticleSystem CreateBurstSystem(Transform parent)
        {
            var go = new GameObject("ConsumeBurst");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = Mathf.Max(0.05f, _burstLifetime);
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.35f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(_burstCount, 4, 64)) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 40;

            go.SetActive(true);
            return ps;
        }

        private static void ApplyColor(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            Color bright = Color.Lerp(color, Color.white, 0.25f);
            Color dark = Color.Lerp(color, Color.black, 0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(bright, dark);
        }
    }
}
