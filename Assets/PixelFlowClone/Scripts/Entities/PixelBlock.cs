using System.Collections;
using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.Entities
{
    /// <summary>
    /// A static colored cell in the central grid. Spawned and released via PoolManager,
    /// owned logically by GridManager. Consumed when a matching collector raycasts into it.
    /// Visual: soft fill + black outline child. Neighboring fills cover inner outlines so
    /// only the cluster perimeter reads as a thick stroke (Pixel Flow style).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PixelBlock : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _outlineRenderer;
        [SerializeField] private BoxCollider2D _collider;
        [SerializeField] private float _outlineScale = 1.12f;
        [SerializeField] private Color _outlineColor = new(0.05f, 0.05f, 0.08f, 1f);

        public ColorId Color { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public bool IsConsumed { get; private set; }

        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
        }

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            EnsureOutline();
        }

        /// <summary>
        /// Initializes block identity and visuals after being pulled from the pool.
        /// </summary>
        public void Initialize(ColorId color, Vector2Int gridPosition, Vector3 worldPosition)
        {
            Initialize(color, gridPosition, worldPosition, 1f);
        }

        public void Initialize(ColorId color, Vector2Int gridPosition, Vector3 worldPosition, float uniformScale)
        {
            Color = color;
            GridPosition = gridPosition;
            transform.position = worldPosition;
            float s = Mathf.Max(0.01f, uniformScale);
            transform.localScale = new Vector3(s, s, 1f);

            IsConsumed = false;
            if (_collider != null) _collider.enabled = true;

            EnsureOutline();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = ColorPalette.ToColor(color);
                _spriteRenderer.enabled = true;
            }

            if (_outlineRenderer != null)
            {
                _outlineRenderer.sprite = _spriteRenderer != null ? _spriteRenderer.sprite : _outlineRenderer.sprite;
                _outlineRenderer.color = _outlineColor;
                _outlineRenderer.enabled = true;
                _outlineRenderer.transform.localScale = Vector3.one * _outlineScale;
                _outlineRenderer.sortingOrder = (_spriteRenderer != null ? _spriteRenderer.sortingOrder : 0) - 1;
            }
        }

        /// <summary>
        /// Marks the block consumed and disables interaction. The actual pool release
        /// is performed by GridManager so it can update RemainingBlocks bookkeeping.
        /// </summary>
        public void Consume()
        {
            if (IsConsumed)
                return;

            IsConsumed = true;
            if (_collider != null) _collider.enabled = false;
        }

        /// <summary>
        /// Plays the hit feedback after the collector shot reaches this block.
        /// The collider is already disabled, so this animation is visual-only.
        /// </summary>
        public IEnumerator PlayHitDisappearAnimation(
            float punchMultiplier,
            float punchDuration,
            float shrinkDuration)
        {
            Vector3 restScale = transform.localScale;
            Vector3 punchScale = restScale * Mathf.Max(1f, punchMultiplier);

            float elapsed = 0f;
            punchDuration = Mathf.Max(0.01f, punchDuration);
            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / punchDuration);
                transform.localScale = Vector3.LerpUnclamped(
                    restScale,
                    punchScale,
                    Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            elapsed = 0f;
            shrinkDuration = Mathf.Max(0.02f, shrinkDuration);
            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / shrinkDuration);
                transform.localScale = Vector3.LerpUnclamped(
                    punchScale,
                    Vector3.zero,
                    t * t);
                yield return null;
            }

            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Resets transient state before the block returns to the pool.
        /// </summary>
        public void ResetFromPool()
        {
            Color = ColorId.None;
            GridPosition = Vector2Int.zero;
            IsConsumed = false;
            transform.localScale = Vector3.one;
            if (_collider != null) _collider.enabled = true;
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
            if (_outlineRenderer != null) _outlineRenderer.enabled = true;
        }

        private void EnsureOutline()
        {
            if (_spriteRenderer == null)
                return;

            if (_outlineRenderer != null)
            {
                if (_outlineRenderer.sprite == null)
                    _outlineRenderer.sprite = _spriteRenderer.sprite;
                return;
            }

            Transform existing = transform.Find("Outline");
            if (existing != null)
            {
                _outlineRenderer = existing.GetComponent<SpriteRenderer>();
                if (_outlineRenderer == null)
                    _outlineRenderer = existing.gameObject.AddComponent<SpriteRenderer>();
            }
            else
            {
                var go = new GameObject("Outline");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                _outlineRenderer = go.AddComponent<SpriteRenderer>();
            }

            _outlineRenderer.sprite = _spriteRenderer.sprite;
            _outlineRenderer.color = _outlineColor;
            _outlineRenderer.sortingOrder = _spriteRenderer.sortingOrder - 1;
            _outlineRenderer.transform.localScale = Vector3.one * _outlineScale;
        }
    }
}
