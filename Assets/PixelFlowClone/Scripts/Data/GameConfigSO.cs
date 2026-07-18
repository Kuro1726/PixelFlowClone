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

        [Header("Endgame Rush")]
        [Tooltip("When conveyor + waiting + queue collectors are at or below this count, rush mode applies.")]
        public int EndgameCollectorThreshold = 5;
        [Tooltip("Move speed multiplier in endgame rush (applied on top of CollectorMoveSpeed / path override).")]
        public float EndgameMoveSpeedMultiplier = 1.75f;
        [Tooltip("In endgame rush, lap-complete units stay on the conveyor instead of entering the queue.")]
        public bool EndgameSkipQueueOnLap = true;

        [Header("Raycast Consume")]
        public float RaycastDistance = 20f;
        public LayerMask PixelBlockLayer;
        public PerpendicularSide RaycastSide = PerpendicularSide.Inward;
        [Tooltip("Max seconds between consumes. At high CollectorMoveSpeed this is capped to ~0.4 of the time to travel one cell so consecutive blocks are not skipped.")]
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
