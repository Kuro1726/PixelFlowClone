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

        [Header("Layout (Queue / Waiting)")]
        [Tooltip("Khoảng từ mép khung block (playfield) ra ConveyorPath. Tăng = path rộng hơn / bớt bó block.")]
        [Min(0.25f)]
        public float ConveyorPathMargin = 1.5f;

        [Tooltip("Khoảng từ đáy conveyor xuống hàng queue (world units).")]
        [Min(0.01f)]
        public float QueueGapBelowPath = 0.9f;

        [Tooltip("Khoảng từ hàng queue xuống waiting front (world units).")]
        [Min(0.01f)]
        public float WaitingGapBelowQueue = 1.25f;

        [Tooltip("Khoảng cách giữa các CollectorUnit trên hàng queue.")]
        [Min(0.01f)]
        public float QueueUnitSpacing = 1.5f;

        [Tooltip("Khoảng cách giữa các CollectorUnit trong 1 cột waiting (dọc).")]
        [Min(0.01f)]
        public float WaitingUnitSpacing = 1.5f;

        [Tooltip("Khoảng cách giữa các cột waiting (ngang).")]
        [Min(0.01f)]
        public float WaitingColumnSpacing = 1.5f;

        [Header("Camera Framing (Game View)")]
        [Tooltip("1 = mặc định. >1 phóng to gameplay. <1 thu nhỏ (nhìn xa hơn).")]
        [Min(0.5f)]
        public float GameplayScreenScale = 1f;

        [Tooltip("World units phía trên conveyor để Capacity + HUD không che nhau. Tăng nếu Capacity bị khuất.")]
        [Min(0f)]
        public float CameraTopPad = 1.45f;

        [Tooltip("World units phía dưới waiting front (độ sâu stack). Tăng nếu unit dưới bị cắt.")]
        [Min(0.5f)]
        public float CameraWaitingStackDepth = 4.75f;

        [Tooltip("Padding quanh khung camera (world units).")]
        [Min(0f)]
        public float CameraPadding = 0.35f;

        [Tooltip("Phần màn hình dành cho HUD (0.08–0.12). Tăng nếu Capacity dính vào thanh Pause.")]
        [Range(0.04f, 0.18f)]
        public float CameraHudScreenFraction = 0.1f;

        [Tooltip("Dương = hạ playfield xuống trên màn hình. Âm = kéo lên. Đơn vị world.")]
        public float CameraVerticalBias = 0.35f;

        [Header("Gameplay HUD")]
        [Tooltip("Chiều cao thanh HUD (tỷ lệ chiều cao màn hình).")]
        [Range(0.04f, 0.14f)]
        public float HudBarHeight = 0.08f;

        [Tooltip("Cỡ chữ Pause / 5/5 / Level.")]
        [Range(18f, 48f)]
        public float HudFontSize = 32f;

        [Header("Juice")]
        [Tooltip("Thời gian collector scale-down + bay ra trước khi trả pool (P3-18).")]
        [Min(0.05f)]
        public float CollectorExitDuration = 0.35f;

        [Tooltip("Khoảng bay ra (world units) khi capacity về 0.")]
        [Min(0f)]
        public float CollectorExitFlyDistance = 1.75f;

        private void OnEnable()
        {
            if (PixelBlockLayer.value == 0)
                PixelBlockLayer = PhysicsLayers.GetLayerMask(PhysicsLayers.PixelBlock);
        }
    }
}
