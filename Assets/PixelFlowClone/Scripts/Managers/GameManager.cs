using System;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Global gameplay state machine. Win/loss condition evaluation is added in P2-15/P2-20.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private bool _startPlayingOnStart = true;

        public GameState CurrentState { get; private set; } = GameState.Loading;
        public bool AcceptsGameplayInput => CurrentState == GameState.Playing;

        public event Action<GameState, GameState> StateChanged;

        protected override void OnSingletonAwake()
        {
            MakePersistent();
            CurrentState = GameState.Loading;
        }

        private void OnEnable()
        {
            GameEvents.OnBlockConsumed -= HandleBlockConsumed;
            GameEvents.OnBlockConsumed += HandleBlockConsumed;
        }

        private void OnDisable()
        {
            GameEvents.OnBlockConsumed -= HandleBlockConsumed;
        }

        private void Start()
        {
            if (_startPlayingOnStart && CurrentState == GameState.Loading)
                StartPlaying();
        }

        public bool StartPlaying()
        {
            return TryChangeState(GameState.Playing);
        }

        public bool Pause()
        {
            return TryChangeState(GameState.Paused);
        }

        public bool Resume()
        {
            return TryChangeState(GameState.Playing);
        }

        public bool DeclareVictory()
        {
            if (!TryChangeState(GameState.Victory))
                return false;

            GameEvents.RaiseVictory();
            return true;
        }

        public bool DeclareDefeat()
        {
            if (!TryChangeState(GameState.Defeat))
                return false;

            GameEvents.RaiseDefeat();
            return true;
        }

        /// <summary>
        /// Victory occurs when the active grid has no blocks remaining.
        /// Called after every successful block consume.
        /// </summary>
        public bool CheckWinCondition()
        {
            if (CurrentState != GameState.Playing || !GridManager.HasInstance)
                return false;

            if (GridManager.Instance.RemainingBlocks > 0)
                return false;

            Debug.Log("[GameManager] Win condition met — no blocks remaining.");
            return DeclareVictory();
        }

        private void HandleBlockConsumed(Vector3 worldPosition, ColorId color)
        {
            CheckWinCondition();
        }

        /// <summary>
        /// Returns to Loading before a new level/session begins.
        /// </summary>
        public bool BeginLoading()
        {
            return TryChangeState(GameState.Loading);
        }

        public bool TryChangeState(GameState target)
        {
            GameState previous = CurrentState;
            if (previous == target || !IsValidTransition(previous, target))
                return false;

            CurrentState = target;
            ApplyTimeScale(target);
            StateChanged?.Invoke(previous, target);
            Debug.Log($"[GameManager] State: {previous} → {target}");
            return true;
        }

        private static void ApplyTimeScale(GameState state)
        {
            Time.timeScale = state switch
            {
                GameState.Paused => 0f,
                GameState.Victory => 0f,
                GameState.Defeat => 0f,
                _ => 1f
            };
        }

        public static bool IsValidTransition(GameState from, GameState to)
        {
            return (from, to) switch
            {
                (GameState.Loading, GameState.Playing) => true,
                (GameState.Playing, GameState.Paused) => true,
                (GameState.Paused, GameState.Playing) => true,
                (GameState.Playing, GameState.Victory) => true,
                (GameState.Playing, GameState.Defeat) => true,
                (GameState.Paused, GameState.Victory) => true,
                (GameState.Paused, GameState.Defeat) => true,
                (GameState.Playing, GameState.Loading) => true,
                (GameState.Paused, GameState.Loading) => true,
                (GameState.Victory, GameState.Loading) => true,
                (GameState.Defeat, GameState.Loading) => true,
                _ => false
            };
        }
    }
}
