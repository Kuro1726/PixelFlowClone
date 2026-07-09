using PixelFlowClone.Data;
using TMPro;
using UnityEngine;

namespace PixelFlowClone.Entities
{
    /// <summary>
    /// Collector entity that travels the conveyor and consumes matching blocks.
    /// Movement / raycast wiring arrives in P1-25+.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CollectorUnit : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text _capacityLabel;

        private readonly CollectorStateMachine _stateMachine = new();

        public ColorId Color { get; private set; }
        public int Capacity { get; private set; }
        public CollectorState State => _stateMachine.CurrentState;

        public Rigidbody2D Body => _rigidbody;

        public bool TrySetState(CollectorState target) => _stateMachine.TryTransition(target);

        public void ForceState(CollectorState state) => _stateMachine.ResetTo(state);

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Configures color and capacity after being pulled from the pool.
        /// </summary>
        public void Initialize(ColorId color, int capacity)
        {
            Color = color;
            Capacity = capacity;

            if (_spriteRenderer != null) _spriteRenderer.color = ColorPalette.ToColor(color);
            RefreshCapacityLabel();
        }

        public void SetCapacity(int capacity)
        {
            Capacity = capacity;
            RefreshCapacityLabel();
        }

        public void OnSpawnFromPool()
        {
            // Reserved for re-enabling colliders / VFX when pulled from the pool.
        }

        public void ResetFromPool()
        {
            Color = ColorId.None;
            Capacity = 0;
            _stateMachine.ResetTo(CollectorState.Pooled);
            if (_capacityLabel != null) _capacityLabel.text = string.Empty;
        }

        private void RefreshCapacityLabel()
        {
            if (_capacityLabel != null)
                _capacityLabel.text = Capacity.ToString();
        }
    }
}
