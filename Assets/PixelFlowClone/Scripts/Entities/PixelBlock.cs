using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.Entities
{
    /// <summary>
    /// A static colored cell in the central grid. Spawned and released via PoolManager,
    /// owned logically by GridManager. Consumed when a matching collector raycasts into it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PixelBlock : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _collider;

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
            if (_spriteRenderer != null) _spriteRenderer.color = ColorPalette.ToColor(color);
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
        /// Resets transient state before the block returns to the pool.
        /// </summary>
        public void ResetFromPool()
        {
            Color = ColorId.None;
            GridPosition = Vector2Int.zero;
            IsConsumed = false;
            transform.localScale = Vector3.one;
            if (_collider != null) _collider.enabled = true;
        }
    }
}
