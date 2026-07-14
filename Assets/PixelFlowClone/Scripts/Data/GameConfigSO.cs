using UnityEngine;

namespace PixelFlowClone.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PixelFlowClone/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Capacity Limits")]
        public int MaxConveyorUnits = 5;
        public int MaxQueueSlots = 5;

        [Header("Collector Movement")]
        public float CollectorMoveSpeed = 3f;
        public float LapCompleteEpsilon = 0.05f;

        [Header("Raycast Consume")]
        public float RaycastDistance = 20f;
        public LayerMask PixelBlockLayer;
        public PerpendicularSide RaycastSide = PerpendicularSide.Inward;
        [Tooltip("Minimum seconds between two successful block consumes on the same collector.")]
        public float ConsumeCooldownSeconds = 0.35f;

        [Header("Input")]
        public float TapCooldownSeconds = 0.15f;

        [Header("Pooling")]
        public int CollectorPoolPrewarm = 20;
        public int BlockPoolPrewarm = 100;

        private void OnEnable()
        {
            if (PixelBlockLayer.value == 0)
                PixelBlockLayer = PhysicsLayers.GetLayerMask(PhysicsLayers.PixelBlock);
        }
    }
}
