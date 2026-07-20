using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.UI;
using PixelFlowClone.UI.Popups;
using PixelFlowClone.UI.Screens;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Global UI orchestrator (P3-15): screen stack + popup show/hide queue.
    /// Scene views register themselves; this manager drives visibility from game events.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        private readonly Dictionary<ScreenId, GameObject> _screens = new();
        private readonly Stack<ScreenId> _screenStack = new();
        private readonly Dictionary<PopupId, MonoBehaviour> _popups = new();
        private readonly Stack<PopupId> _popupStack = new();

        private GameplayHUD _hud;

        public ScreenId CurrentScreen => _screenStack.Count > 0 ? _screenStack.Peek() : ScreenId.None;
        public PopupId TopPopup => _popupStack.Count > 0 ? _popupStack.Peek() : PopupId.None;
        public int PopupCount => _popupStack.Count;
        public GameplayHUD Hud => _hud;

        protected override void OnSingletonAwake()
        {
            MakePersistent();
        }

        private void OnEnable()
        {
            GameEvents.OnConveyorCountChanged -= HandleConveyorCountChanged;
            GameEvents.OnConveyorCountChanged += HandleConveyorCountChanged;

            if (GameManager.HasInstance)
            {
                GameManager.Instance.StateChanged -= HandleGameStateChanged;
                GameManager.Instance.StateChanged += HandleGameStateChanged;
            }
        }

        private void Start()
        {
            // GameManager may spawn after UIManager OnEnable during bootstrap.
            if (GameManager.HasInstance)
            {
                GameManager.Instance.StateChanged -= HandleGameStateChanged;
                GameManager.Instance.StateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnConveyorCountChanged -= HandleConveyorCountChanged;

            if (GameManager.HasInstance)
                GameManager.Instance.StateChanged -= HandleGameStateChanged;
        }

        protected override void OnDestroy()
        {
            GameEvents.OnConveyorCountChanged -= HandleConveyorCountChanged;

            if (GameManager.HasInstance)
                GameManager.Instance.StateChanged -= HandleGameStateChanged;

            base.OnDestroy();
        }

        public void RegisterScreen(ScreenId id, GameObject root)
        {
            if (id == ScreenId.None || root == null)
                return;

            _screens[id] = root;
        }

        public void UnregisterScreen(ScreenId id)
        {
            if (_screens.ContainsKey(id))
                _screens.Remove(id);

            // Drop stack entries for a screen that left the scene.
            if (_screenStack.Count == 0)
                return;

            var kept = new Stack<ScreenId>();
            foreach (ScreenId entry in _screenStack)
            {
                if (entry != id)
                    kept.Push(entry);
            }

            _screenStack.Clear();
            while (kept.Count > 0)
                _screenStack.Push(kept.Pop());
        }

        public void RegisterHud(GameplayHUD hud)
        {
            _hud = hud;
            SyncHudVisibilityFromGameState();
        }

        public void UnregisterHud(GameplayHUD hud)
        {
            if (_hud == hud)
                _hud = null;
        }

        public void RegisterPopup(PopupId id, MonoBehaviour popup)
        {
            if (id == PopupId.None || popup == null)
                return;

            _popups[id] = popup;
        }

        public void UnregisterPopup(PopupId id, MonoBehaviour popup = null)
        {
            if (!_popups.TryGetValue(id, out MonoBehaviour existing))
                return;

            if (popup != null && existing != popup)
                return;

            _popups.Remove(id);
            RemovePopupFromStack(id);
        }

        /// <summary>Pushes <paramref name="id"/> and shows only the top registered screen.</summary>
        public void ShowScreen(ScreenId id)
        {
            if (id == ScreenId.None)
                return;

            if (_screenStack.Count > 0 && _screenStack.Peek() == id)
            {
                ApplyScreenVisibility();
                return;
            }

            _screenStack.Push(id);
            ApplyScreenVisibility();
            Debug.Log($"[UIManager] ShowScreen {id} (stack={_screenStack.Count})");
        }

        /// <summary>Pops the current screen (if any) and restores the previous one.</summary>
        public void HideScreen()
        {
            if (_screenStack.Count == 0)
                return;

            ScreenId popped = _screenStack.Pop();
            ApplyScreenVisibility();
            Debug.Log($"[UIManager] HideScreen {popped} → {CurrentScreen}");
        }

        public void ClearScreens()
        {
            _screenStack.Clear();
            ApplyScreenVisibility();
        }

        public void ShowPopup(PopupId id)
        {
            if (id == PopupId.None)
                return;

            if (!_popups.TryGetValue(id, out MonoBehaviour popup) || popup == null)
            {
                Debug.LogWarning($"[UIManager] ShowPopup({id}) — not registered.");
                return;
            }

            if (_popupStack.Contains(id))
                return;

            _popupStack.Push(id);
            InvokeShow(popup);
            Debug.Log($"[UIManager] ShowPopup {id} (stack={_popupStack.Count})");
        }

        public void HidePopup(PopupId id)
        {
            if (id == PopupId.None)
                return;

            if (_popups.TryGetValue(id, out MonoBehaviour popup) && popup != null)
                InvokeHide(popup);

            RemovePopupFromStack(id);

            if (id == PopupId.Victory)
                SyncHudVisibilityFromGameState();
        }

        public void HideTopPopup()
        {
            if (_popupStack.Count == 0)
                return;

            HidePopup(_popupStack.Pop());
        }

        public void HideAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                PopupId id = _popupStack.Pop();
                if (_popups.TryGetValue(id, out MonoBehaviour popup) && popup != null)
                    InvokeHide(popup);
            }

            SyncHudVisibilityFromGameState();
        }

        public void ShowVictory()
        {
            HideHud();
            ShowPopup(PopupId.Victory);
        }

        public void ShowDefeat() => ShowPopup(PopupId.Defeat);

        public void ShowPause() => ShowPopup(PopupId.Pause);

        public void UpdateConveyorHUD(int active, int max)
        {
            if (_hud != null)
                _hud.SetConveyorCount(active, max);
        }

        public void UpdateQueueHUD(int occupied, int max)
        {
            // Queue count HUD cancelled (P3-11); kept for API compatibility with planning.
        }

        private void HandleConveyorCountChanged(int active, int max) => UpdateConveyorHUD(active, max);

        private void HandleGameStateChanged(GameState previous, GameState next)
        {
            switch (next)
            {
                case GameState.Paused:
                    ShowPause();
                    break;
                case GameState.Playing:
                    ShowHud();
                    HidePopup(PopupId.Pause);
                    HidePopup(PopupId.Victory);
                    HidePopup(PopupId.Defeat);
                    break;
                case GameState.Victory:
                    HidePopup(PopupId.Pause);
                    ShowVictory();
                    break;
                case GameState.Defeat:
                    HidePopup(PopupId.Pause);
                    ShowDefeat();
                    break;
                case GameState.Loading:
                    HideHud();
                    HideAllPopups();
                    break;
            }
        }

        private void ShowHud()
        {
            if (_hud != null)
                _hud.ShowGameplayElements();
        }

        private void HideHud()
        {
            if (_hud != null)
                _hud.HideGameplayElements();
        }

        private void SyncHudVisibilityFromGameState()
        {
            if (_hud == null || !GameManager.HasInstance)
                return;

            GameState state = GameManager.Instance.CurrentState;
            if (state == GameState.Victory || state == GameState.Loading)
                HideHud();
            else
                ShowHud();
        }

        private void ApplyScreenVisibility()
        {
            ScreenId top = CurrentScreen;
            foreach (KeyValuePair<ScreenId, GameObject> pair in _screens)
            {
                if (pair.Value == null)
                    continue;

                // Gameplay lives in its own scene; unload owns lifecycle. Do not SetActive the context root.
                if (pair.Key == ScreenId.Gameplay)
                    continue;

                bool visible = pair.Key == top;
                if (pair.Value.activeSelf != visible)
                    pair.Value.SetActive(visible);
            }
        }

        private void RemovePopupFromStack(PopupId id)
        {
            if (_popupStack.Count == 0 || !_popupStack.Contains(id))
                return;

            var kept = new Stack<PopupId>();
            while (_popupStack.Count > 0)
            {
                PopupId entry = _popupStack.Pop();
                if (entry != id)
                    kept.Push(entry);
            }

            while (kept.Count > 0)
                _popupStack.Push(kept.Pop());
        }

        private static void InvokeShow(MonoBehaviour popup)
        {
            switch (popup)
            {
                case VictoryPopup victory:
                    victory.Show();
                    break;
                case DefeatPopup defeat:
                    defeat.Show();
                    break;
                case PausePopup pause:
                    pause.Show();
                    break;
                case SettingsPopup settings:
                    settings.Show();
                    break;
                default:
                    popup.gameObject.SetActive(true);
                    break;
            }
        }

        private static void InvokeHide(MonoBehaviour popup)
        {
            switch (popup)
            {
                case VictoryPopup victory:
                    victory.Hide();
                    break;
                case DefeatPopup defeat:
                    defeat.Hide();
                    break;
                case PausePopup pause:
                    pause.Hide();
                    break;
                case SettingsPopup settings:
                    settings.Hide();
                    break;
                default:
                    popup.gameObject.SetActive(false);
                    break;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("P3-15/Log UI Status")]
        private void DebugLogStatus()
        {
            Debug.Log(
                $"[P3-15] CurrentScreen={CurrentScreen}, TopPopup={TopPopup}, PopupCount={PopupCount}, " +
                $"screens={_screens.Count}, popups={_popups.Count}, hud={(_hud != null)}");
            foreach (KeyValuePair<ScreenId, GameObject> pair in _screens)
                Debug.Log($"[P3-15]   screen {pair.Key} → {(pair.Value != null ? pair.Value.name : "null")}");
            foreach (KeyValuePair<PopupId, MonoBehaviour> pair in _popups)
                Debug.Log($"[P3-15]   popup {pair.Key} → {(pair.Value != null ? pair.Value.name : "null")}");
        }

        [ContextMenu("P3-15/Show Victory Popup")]
        private void DebugShowVictory()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[P3-15] Enter Play Mode first.");
                return;
            }

            ShowVictory();
            DebugLogStatus();
        }

        [ContextMenu("P3-15/Show Defeat Popup")]
        private void DebugShowDefeat()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[P3-15] Enter Play Mode first.");
                return;
            }

            ShowDefeat();
            DebugLogStatus();
        }

        [ContextMenu("P3-15/Show Pause Popup")]
        private void DebugShowPause()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[P3-15] Enter Play Mode first.");
                return;
            }

            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Playing)
                GameManager.Instance.Pause();
            else
                ShowPause();
            DebugLogStatus();
        }

        [ContextMenu("P3-15/Hide All Popups")]
        private void DebugHideAllPopups()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[P3-15] Enter Play Mode first.");
                return;
            }

            HideAllPopups();
            if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameState.Paused)
                GameManager.Instance.Resume();
            DebugLogStatus();
        }
#endif
    }
}
