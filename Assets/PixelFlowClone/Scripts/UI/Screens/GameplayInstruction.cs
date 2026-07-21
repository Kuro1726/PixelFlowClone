using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using PixelFlowClone.Queue;
using PixelFlowClone.Utils;
using TMPro;
using UnityEngine;

namespace PixelFlowClone.UI.Screens
{
    /// <summary>
    /// Level-one tutorial: tap the highlighted waiting collector, wait for its lap,
    /// then tap that same collector from the queue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayInstruction : MonoBehaviour
    {
        private enum Step
        {
            WaitingForLevel,
            TapWaiting,
            WaitForLap,
            TapQueue,
            Complete
        }

        [SerializeField] private RectTransform _targetAnchor;
        [SerializeField] private RectTransform _fingerRect;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private int _tutorialLevelId = 1;
        [SerializeField] private Vector2 _fallbackFingerOffset = new(60f, -140f);
        [SerializeField, Min(0f)] private float _pressDistance = 18f;
        [SerializeField, Min(0.1f)] private float _pressSpeed = 3.5f;

        [Header("Step 1 - Waiting Collector")]
        [SerializeField, TextArea(2, 3)] private string _waitingText =
            "Tap shooter to place\ninto the conveyor.";
        [SerializeField] private Vector2 _waitingFingerAdjustment;

        [Header("Step 2 - Wait For Lap")]
        [SerializeField, TextArea(2, 3)] private string _lapText =
            "Wait for the shooter\nto finish one lap.";

        [Header("Step 3 - Queue Collector")]
        [SerializeField, TextArea(2, 3)] private string _queueText =
            "Tap shooter to place it\nback into the conveyor.";
        [SerializeField] private Vector2 _queueFingerAdjustment;

        private CanvasGroup _canvasGroup;
        private Canvas _outerCanvas;
        private RectTransform _trackingRect;
        private readonly List<CollectorUnit> _waitingFronts = new();
        private CollectorUnit _target;
        private Coroutine _beginRoutine;
        private Coroutine _queueRoutine;
        private Step _step = Step.WaitingForLevel;
        private Vector2 _fingerBaseOffset;
        private bool _interactionLocked;

        private static GameplayInstruction Active { get; set; }

        public static bool AllowsGameplayTap(ITappable tappable)
        {
            GameplayInstruction instruction = Active;
            if (instruction == null || !instruction._interactionLocked)
                return true;

            if (tappable is not CollectorUnit unit)
                return false;

            return (instruction._step == Step.TapWaiting ||
                    instruction._step == Step.TapQueue) &&
                   unit == instruction._target;
        }

        private void Awake()
        {
            ResolveUi();
            SetVisible(false);
        }

        private void Start()
        {
            SubscribeEvents();
            RestartWhenReady();
        }

        private void OnEnable()
        {
            Active = this;
            SubscribeEvents();
            RestartWhenReady();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            StopRunningCoroutines();
            SetInteractionLocked(false);
            if (Active == this)
                Active = null;
        }

        private void ResolveUi()
        {
            if (_targetAnchor == null)
                _targetAnchor = transform.Find("TargetAnchor") as RectTransform;
            if (_fingerRect == null && _targetAnchor != null)
                _fingerRect = _targetAnchor.Find("FingerImage") as RectTransform;
            if (_instructionText == null)
            {
                Transform child = transform.Find("InstructionText");
                if (child != null)
                    _instructionText = child.GetComponent<TMP_Text>();
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _outerCanvas = FindOuterCanvas();
            _trackingRect = _outerCanvas != null
                ? _outerCanvas.transform as RectTransform
                : transform as RectTransform;

            if (_fingerRect != null)
            {
                Vector2 savedOffset = _fingerRect.anchoredPosition;
                _fingerBaseOffset = savedOffset;
                _fingerRect.anchoredPosition = _fingerBaseOffset;
            }

            if (_instructionText == null)
                return;

            GameplayFontUtility.Apply(_instructionText);
            _instructionText.raycastTarget = false;
            _instructionText.fontStyle = FontStyles.Bold;
            _instructionText.alignment = TextAlignmentOptions.Center;
            _instructionText.enableWordWrapping = true;
            _instructionText.enableAutoSizing = true;
            _instructionText.fontSizeMin = 38f;
            _instructionText.fontSizeMax = 68f;

            RectTransform textRect = _instructionText.rectTransform;
            Vector2 size = textRect.sizeDelta;
            size.x = Mathf.Max(size.x, 900f);
            size.y = Mathf.Max(size.y, 180f);
            textRect.sizeDelta = size;
        }

        private Canvas FindOuterCanvas()
        {
            Transform current = transform.parent;
            while (current != null)
            {
                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null)
                    return canvas;
                current = current.parent;
            }

            return null;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            GameEvents.OnCollectorDispatchedFromWaiting += HandleWaitingDispatch;
            GameEvents.OnCollectorLapComplete += HandleLapComplete;
            GameEvents.OnCollectorDispatchedFromQueue += HandleQueueDispatch;
            if (LevelManager.HasInstance)
                LevelManager.Instance.LevelLoaded += HandleLevelLoaded;
        }

        private void UnsubscribeEvents()
        {
            GameEvents.OnCollectorDispatchedFromWaiting -= HandleWaitingDispatch;
            GameEvents.OnCollectorLapComplete -= HandleLapComplete;
            GameEvents.OnCollectorDispatchedFromQueue -= HandleQueueDispatch;
            if (LevelManager.HasInstance)
                LevelManager.Instance.LevelLoaded -= HandleLevelLoaded;
        }

        private void RestartWhenReady()
        {
            if (!isActiveAndEnabled)
                return;

            StopRunningCoroutines();
            SetInteractionLocked(false);
            _step = Step.WaitingForLevel;
            _target = null;
            SetVisible(false);
            _beginRoutine = StartCoroutine(BeginWhenReady());
        }

        private IEnumerator BeginWhenReady()
        {
            while (isActiveAndEnabled)
            {
                if (!LevelManager.HasInstance ||
                    LevelManager.Instance.CurrentLevel == null ||
                    !QueueManager.HasInstance ||
                    QueueManager.Instance.Waiting == null)
                {
                    yield return null;
                    continue;
                }

                if (LevelManager.Instance.CurrentLevel.LevelId != _tutorialLevelId)
                {
                    CompleteInstruction();
                    yield break;
                }

                CollectorUnit front = FindTopLeftWaitingCollector();
                if (front == null)
                {
                    yield return null;
                    continue;
                }

                _target = front;
                EnterStep(Step.TapWaiting);
                _beginRoutine = null;
                yield break;
            }
        }

        private void HandleLevelLoaded(LevelDataSO level, int index)
        {
            RestartWhenReady();
        }

        private void HandleWaitingDispatch(CollectorUnit unit)
        {
            if (_step == Step.TapWaiting && unit == _target)
                EnterStep(Step.WaitForLap);
        }

        private void HandleLapComplete(CollectorUnit unit)
        {
            if (_step != Step.WaitForLap || unit != _target)
                return;

            if (_queueRoutine != null)
                StopCoroutine(_queueRoutine);
            _queueRoutine = StartCoroutine(EnterQueueStepWhenReady(unit));
        }

        private IEnumerator EnterQueueStepWhenReady(CollectorUnit unit)
        {
            for (int i = 0; i < 3; i++)
            {
                if (unit != null && unit.State == CollectorState.InQueueSlot)
                {
                    EnterStep(Step.TapQueue);
                    _queueRoutine = null;
                    yield break;
                }
                yield return null;
            }

            Debug.LogWarning(
                "[GameplayInstruction] Tutorial collector completed a lap but did not enter queue.");
            CompleteInstruction();
            _queueRoutine = null;
        }

        private void HandleQueueDispatch(CollectorUnit unit)
        {
            if (_step == Step.TapQueue && unit == _target)
                CompleteInstruction();
        }

        private void EnterStep(Step next)
        {
            _step = next;
            SetInteractionLocked(true);
            SetVisible(true);

            if (next == Step.TapWaiting)
            {
                SetText(_waitingText);
                SetFingerVisible(true);
            }
            else if (next == Step.WaitForLap)
            {
                SetText(_lapText);
                SetFingerVisible(false);
            }
            else if (next == Step.TapQueue)
            {
                SetText(_queueText);
                SetFingerVisible(true);
            }
        }

        private void LateUpdate()
        {
            bool shouldTrack =
                _step == Step.TapWaiting ||
                _step == Step.TapQueue;
            if (!shouldTrack ||
                _target == null ||
                _targetAnchor == null ||
                _fingerRect == null ||
                _trackingRect == null)
                return;

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
                return;

            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(worldCamera, _target.transform.position);
            Camera uiCamera = _outerCanvas != null &&
                              _outerCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _outerCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _trackingRect, screenPoint, uiCamera, out Vector2 canvasPoint))
            {
                Vector3 worldPoint = _trackingRect.TransformPoint(canvasPoint);
                Transform targetParent = _targetAnchor.parent;
                Vector3 localPoint = targetParent.InverseTransformPoint(worldPoint);
                _targetAnchor.localPosition = new Vector3(
                    localPoint.x,
                    localPoint.y,
                    _targetAnchor.localPosition.z);
            }

            float pulse =
                (Mathf.Sin(Time.unscaledTime * _pressSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            _fingerRect.anchoredPosition =
                GetCurrentFingerOffset() + Vector2.down * (_pressDistance * pulse);
            float scale = Mathf.Lerp(1f, 0.92f, pulse);
            _fingerRect.localScale = new Vector3(scale, scale, 1f);
        }

        private void CompleteInstruction()
        {
            _step = Step.Complete;
            _target = null;
            SetFingerVisible(false);
            SetVisible(false);
            SetInteractionLocked(false);
        }

        private CollectorUnit FindTopLeftWaitingCollector()
        {
            _waitingFronts.Clear();
            QueueManager.Instance.Waiting.GetFronts(_waitingFronts);

            CollectorUnit leftmost = null;
            for (int i = 0; i < _waitingFronts.Count; i++)
            {
                CollectorUnit unit = _waitingFronts[i];
                if (unit == null)
                    continue;

                if (leftmost == null ||
                    unit.transform.position.x < leftmost.transform.position.x)
                    leftmost = unit;
            }

            return leftmost;
        }

        private void SetInteractionLocked(bool locked)
        {
            // UI buttons remain available. This flag only filters CollectorUnit taps
            // through InputManager/AllowsGameplayTap.
            _interactionLocked = locked;
        }

        private void SetText(string value)
        {
            if (_instructionText != null)
                _instructionText.text = value;
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = visible ? 1f : 0f;
        }

        private void SetFingerVisible(bool visible)
        {
            if (_fingerRect == null)
                return;

            _fingerRect.gameObject.SetActive(visible);
            if (!visible)
            {
                _fingerRect.anchoredPosition = GetCurrentFingerOffset();
                _fingerRect.localScale = Vector3.one;
            }
        }

        private Vector2 GetCurrentFingerOffset()
        {
            if (_step == Step.TapQueue)
                return _fingerBaseOffset + _queueFingerAdjustment;
            if (_step == Step.TapWaiting)
                return _fingerBaseOffset + _waitingFingerAdjustment;
            return _fingerBaseOffset;
        }

        private void StopRunningCoroutines()
        {
            if (_beginRoutine != null)
            {
                StopCoroutine(_beginRoutine);
                _beginRoutine = null;
            }
            if (_queueRoutine != null)
            {
                StopCoroutine(_queueRoutine);
                _queueRoutine = null;
            }
        }
    }
}
