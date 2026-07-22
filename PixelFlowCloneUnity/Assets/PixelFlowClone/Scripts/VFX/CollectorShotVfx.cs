using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Managers;
using UnityEngine;

namespace PixelFlowClone.VFX
{
    /// <summary>
    /// Cosmetic white projectile shown after a collector successfully consumes a block.
    /// Gameplay remains raycast-driven; these pooled visuals never own collision logic.
    /// </summary>
    public class CollectorShotVfx : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField, Min(4)] private int _poolSize = 20;

        [Header("Projectile Fallbacks (GameConfig Missing)")]
        [SerializeField, Min(0.03f)] private float _travelDuration = 0.16f;
        [SerializeField, Min(0.03f)] private float _headSize = 0.12f;
        [SerializeField, Min(0.01f)] private float _trailWidth = 0.055f;
        [SerializeField, Min(0.02f)] private float _trailTime = 0.18f;
        [SerializeField] private int _sortingOrder = 45;

        private readonly Queue<Shot> _available = new();
        private readonly List<Shot> _all = new();
        private Material _trailMaterial;
        private Sprite _headSprite;

        private sealed class Shot
        {
            public GameObject Root;
            public SpriteRenderer Head;
            public TrailRenderer Trail;
            public Vector3 Origin;
            public Vector3 Target;
            public float Elapsed;
            public float Duration;
            public float TrailReleaseTime;
            public bool IsFlying;
            public bool IsActive;
        }

        private void Awake()
        {
            ApplyGameConfig();
            EnsureResources();
            EnsurePool();
        }

        private void OnEnable()
        {
            GameEvents.OnCollectorShot -= HandleCollectorShot;
            GameEvents.OnCollectorShot += HandleCollectorShot;
        }

        private void OnDisable()
        {
            GameEvents.OnCollectorShot -= HandleCollectorShot;
            StopAndReleaseAll();
        }

        private void OnDestroy()
        {
            GameEvents.OnCollectorShot -= HandleCollectorShot;

            if (_trailMaterial != null)
                Destroy(_trailMaterial);
            if (_headSprite != null)
            {
                Texture2D texture = _headSprite.texture;
                Destroy(_headSprite);
                if (texture != null)
                    Destroy(texture);
            }
        }

        public static CollectorShotVfx CreateRuntime(Transform parent = null)
        {
            var go = new GameObject("CollectorShotVfx");
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.AddComponent<CollectorShotVfx>();
        }

        private void HandleCollectorShot(Vector3 origin, Vector3 target)
        {
            ApplyGameConfig();
            Shot shot = Rent();
            if (shot == null)
                return;

            shot.Origin = ForceGameplayPlane(origin);
            shot.Target = ForceGameplayPlane(target);
            shot.Elapsed = 0f;
            shot.Duration = Mathf.Max(0.03f, _travelDuration);
            shot.TrailReleaseTime = 0f;
            shot.IsFlying = true;
            shot.IsActive = true;

            shot.Root.SetActive(true);
            shot.Root.transform.position = shot.Origin;
            shot.Head.enabled = true;
            shot.Trail.Clear();
            shot.Trail.emitting = true;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _all.Count; i++)
                TickShot(_all[i], deltaTime);
        }

        private void TickShot(Shot shot, float deltaTime)
        {
            if (shot == null || !shot.IsActive)
                return;

            if (shot.IsFlying)
            {
                shot.Elapsed += deltaTime;
                float t = Mathf.Clamp01(shot.Elapsed / shot.Duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                shot.Root.transform.position = Vector3.LerpUnclamped(shot.Origin, shot.Target, eased);

                if (t < 1f)
                    return;

                shot.Root.transform.position = shot.Target;
                shot.Head.enabled = false;
                shot.Trail.emitting = false;
                shot.IsFlying = false;
                shot.TrailReleaseTime = Time.time + Mathf.Max(0.02f, _trailTime);
                return;
            }

            if (Time.time >= shot.TrailReleaseTime)
                Release(shot);
        }

        private Shot Rent()
        {
            EnsurePool();
            if (_available.Count > 0)
                return _available.Dequeue();

            // Reuse the oldest active visual if an unusually large burst exhausts the pool.
            for (int i = 0; i < _all.Count; i++)
            {
                Shot candidate = _all[i];
                if (!candidate.IsActive)
                    return candidate;
            }

            Shot fallback = _all.Count > 0 ? _all[0] : null;
            if (fallback != null)
            {
                fallback.Trail.emitting = false;
                fallback.Trail.Clear();
                fallback.IsActive = false;
                fallback.IsFlying = false;
            }

            return fallback;
        }

        private void Release(Shot shot)
        {
            if (shot == null || !shot.IsActive)
                return;
            shot.IsActive = false;
            shot.Trail.emitting = false;
            shot.Trail.Clear();
            shot.Root.SetActive(false);
            _available.Enqueue(shot);
        }

        private void StopAndReleaseAll()
        {
            _available.Clear();

            for (int i = 0; i < _all.Count; i++)
            {
                Shot shot = _all[i];
                shot.IsActive = false;
                shot.Trail.emitting = false;
                shot.Trail.Clear();
                shot.Root.SetActive(false);
                _available.Enqueue(shot);
            }
        }

        private void EnsurePool()
        {
            EnsureResources();
            int targetSize = Mathf.Max(4, _poolSize);
            while (_all.Count < targetSize)
            {
                Shot shot = CreateShot(_all.Count);
                _all.Add(shot);
                _available.Enqueue(shot);
            }
        }

        private Shot CreateShot(int index)
        {
            var root = new GameObject($"WhiteShot_{index:00}");
            root.transform.SetParent(transform, false);

            SpriteRenderer head = root.AddComponent<SpriteRenderer>();
            head.sprite = _headSprite;
            head.color = Color.white;

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.material = _trailMaterial;
            trail.minVertexDistance = 0.015f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.72f, 0.38f),
                new Keyframe(1f, 0f));
            trail.colorGradient = CreateTrailGradient();
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCornerVertices = 4;
            trail.numCapVertices = 4;
            trail.emitting = false;

            root.SetActive(false);
            var shot = new Shot
            {
                Root = root,
                Head = head,
                Trail = trail
            };
            ConfigureShot(shot);
            return shot;
        }

        private void ApplyGameConfig()
        {
            GameConfigSO config = ConveyorPathManager.HasInstance
                ? ConveyorPathManager.Instance.Config
                : null;
            if (config == null)
                return;

            _travelDuration = Mathf.Max(0.03f, config.CollectorShotTravelDuration);
            _headSize = Mathf.Max(0.03f, config.CollectorShotHeadSize);
            _trailWidth = Mathf.Max(0.01f, config.CollectorShotTrailWidth);
            _trailTime = Mathf.Max(0.02f, config.CollectorShotTrailTime);

            for (int i = 0; i < _all.Count; i++)
                ConfigureShot(_all[i]);
        }

        private void ConfigureShot(Shot shot)
        {
            if (shot == null)
                return;

            float headSize = Mathf.Max(0.03f, _headSize);
            shot.Root.transform.localScale = Vector3.one * headSize;
            shot.Head.sortingOrder = _sortingOrder;
            shot.Trail.time = Mathf.Max(0.02f, _trailTime);
            shot.Trail.widthMultiplier = Mathf.Max(0.01f, _trailWidth) / headSize;
            shot.Trail.sortingOrder = _sortingOrder - 1;
        }

        private void EnsureResources()
        {
            if (_headSprite == null)
                _headSprite = CreateCircleSprite();

            if (_trailMaterial != null)
                return;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogError("[CollectorShotVfx] No compatible trail shader was found.", this);
                return;
            }

            _trailMaterial = new Material(shader)
            {
                name = "CollectorWhiteShotTrail (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Gradient CreateTrailGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 0.28f),
                    new GradientColorKey(new Color(0.82f, 0.86f, 0.95f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.28f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CollectorWhiteShotHead",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = ((x + 0.5f) / size) * 2f - 1f;
                    float py = ((y + 0.5f) / size) * 2f - 1f;
                    float radius = Mathf.Sqrt(px * px + py * py);
                    float alpha = 1f - Mathf.SmoothStep(0.86f, 1f, radius);
                    float edge = Mathf.SmoothStep(0.72f, 0.92f, radius);
                    Color color = Color.Lerp(Color.white, new Color(0.72f, 0.76f, 0.84f), edge * 0.42f);
                    color.a = radius < 1f ? alpha : 0f;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
            sprite.name = "CollectorWhiteShotHead";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Vector3 ForceGameplayPlane(Vector3 point)
        {
            point.z = -0.05f;
            return point;
        }
    }
}
