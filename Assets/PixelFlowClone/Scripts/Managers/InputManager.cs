using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Queue;
using PixelFlowClone.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Converts Input System pointer presses into 2D world taps.
    /// Tappable objects are resolved through their collider and <see cref="ITappable"/>.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private LayerMask _tappableLayers = 1 << PhysicsLayers.Collector;
        [SerializeField] private GameConfigSO _config;
        [SerializeField] private float _tapCooldownSeconds = TapCooldownGate.DefaultCooldownSeconds;

        private readonly TapCooldownGate _tapCooldown = new(TapCooldownGate.DefaultCooldownSeconds);

        private InputAction _pointerPosition;
        private InputAction _pointerPress;

        protected override void OnSingletonAwake()
        {
            MakePersistent();

            if (_worldCamera == null)
                _worldCamera = Camera.main;

            RefreshTapCooldownFromConfig();
            CreateInputActions();
        }

        private void OnEnable()
        {
            CreateInputActions();
            RefreshTapCooldownFromConfig();
            _pointerPress.performed += HandlePointerPress;
            _pointerPosition.Enable();
            _pointerPress.Enable();
        }

        private void OnDisable()
        {
            if (_pointerPress != null)
            {
                _pointerPress.performed -= HandlePointerPress;
                _pointerPress.Disable();
            }

            _pointerPosition?.Disable();
        }

        private void CreateInputActions()
        {
            if (_pointerPosition == null)
            {
                _pointerPosition = new InputAction(
                    name: "PointerPosition",
                    type: InputActionType.PassThrough,
                    binding: "<Pointer>/position");
            }

            if (_pointerPress == null)
            {
                _pointerPress = new InputAction(
                    name: "PointerPress",
                    type: InputActionType.Button,
                    binding: "<Pointer>/press");
            }
        }

        private void HandlePointerPress(InputAction.CallbackContext context)
        {
            if (!context.ReadValueAsButton())
                return;

            ProcessTap(_pointerPosition.ReadValue<Vector2>());
        }

        /// <summary>
        /// Raycasts a screen-space tap against tappable 2D colliders.
        /// Returns true when an <see cref="ITappable"/> receives the tap.
        /// Guarded by <see cref="TapCooldownGate"/> (P3-16).
        /// </summary>
        public bool ProcessTap(Vector2 screenPosition)
        {
            if (GameManager.HasInstance && !GameManager.Instance.AcceptsGameplayInput)
                return false;

            RefreshTapCooldownFromConfig();
            if (!_tapCooldown.IsReady)
                return false;

            Camera worldCamera = ResolveWorldCamera();
            if (worldCamera == null)
            {
                Debug.LogWarning("[InputManager] Cannot process tap — no world camera.");
                return false;
            }

            if (!TryScreenToWorld(worldCamera, screenPosition, out Vector2 worldPosition))
                return false;

            Collider2D hit = Physics2D.OverlapPoint(worldPosition, _tappableLayers.value);
            if (hit == null)
                return false;

            ITappable tappable = hit.GetComponentInParent<ITappable>();
            if (tappable == null)
                return false;

            if (!PixelFlowClone.UI.Screens.GameplayInstruction.AllowsGameplayTap(tappable))
                return false;

            if (!_tapCooldown.TryAccept())
                return false;

            tappable.OnTap();

            if (AudioManager.HasInstance)
                AudioManager.Instance.PlayTap();

            return true;
        }

        private void RefreshTapCooldownFromConfig()
        {
            float seconds = _tapCooldownSeconds;
            GameConfigSO config = ResolveConfig();
            if (config != null)
                seconds = config.TapCooldownSeconds;

            _tapCooldown.SetCooldown(seconds);
        }

        private GameConfigSO ResolveConfig()
        {
            if (_config != null)
                return _config;

            if (ConveyorPathManager.HasInstance && ConveyorPathManager.Instance.Config != null)
                return ConveyorPathManager.Instance.Config;

            return null;
        }

        private Camera ResolveWorldCamera()
        {
            if (_worldCamera == null)
                _worldCamera = Camera.main;

            return _worldCamera;
        }

        private static bool TryScreenToWorld(
            Camera worldCamera,
            Vector2 screenPosition,
            out Vector2 worldPosition)
        {
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
            {
                worldPosition = default;
                return false;
            }

            float distance = -ray.origin.z / ray.direction.z;
            if (distance < 0f)
            {
                worldPosition = default;
                return false;
            }

            Vector3 point = ray.GetPoint(distance);
            worldPosition = new Vector2(point.x, point.y);
            return true;
        }

        protected override void OnDestroy()
        {
            OnDisable();
            _pointerPosition?.Dispose();
            _pointerPress?.Dispose();
            _pointerPosition = null;
            _pointerPress = null;
            base.OnDestroy();
        }
    }
}
