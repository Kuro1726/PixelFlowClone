using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;

/// <summary>
/// Play Mode harness: Level_001 grid + waiting spawn.
/// Taps go through <see cref="InputManager"/> (P2-11).
/// </summary>
public class ConveyorMovementSmokeTest : MonoBehaviour
{
    [SerializeField] private LevelDataSO _level;
    [SerializeField] private GameConfigSO _config;

    private void OnEnable()
    {
        GameEvents.OnBlockConsumed += HandleBlockConsumed;
        GameEvents.OnCollectorLapComplete += HandleLapComplete;
        GameEvents.OnCollectorExited += HandleCollectorExited;
        GameEvents.OnConveyorCountChanged += HandleConveyorCountChanged;
        GameEvents.OnQueueCountChanged += HandleQueueCountChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnBlockConsumed -= HandleBlockConsumed;
        GameEvents.OnCollectorLapComplete -= HandleLapComplete;
        GameEvents.OnCollectorExited -= HandleCollectorExited;
        GameEvents.OnConveyorCountChanged -= HandleConveyorCountChanged;
        GameEvents.OnQueueCountChanged -= HandleQueueCountChanged;
    }

    private void Start()
    {
        // Menu → Play already selected a level; skip harness spawn so LevelManager.Apply owns the scene.
        if (LevelManager.HasInstance && LevelManager.Instance.CurrentLevel != null)
        {
            Debug.Log("[SmokeTest] Skipped auto-spawn — LevelManager CurrentLevel is set.");
            return;
        }

        if (!ValidateSetup())
            return;

        RunSmokeTest();
    }

    private void RunSmokeTest()
    {
        EnsureGridManager();
        EnsureInputManager();
        PersistentManagers.EnsureGameManager();
        PersistentManagers.EnsureLevelManager(_level);

        if (!QueueManager.HasInstance)
        {
            Debug.LogError("[SmokeTest] QueueManager missing — assign Waiting on QueueManager and add component.");
            return;
        }

        // Same path as Bootstrap → Menu → Play (ConfigureFromLevel + camera + queue layout).
        ConveyorPathManager.Instance.ConfigureFromLevel(
            _level,
            ConveyorPathManager.Instance.PathRoot,
            _config);
        PoolManager.Instance.Prewarm(_level, _config);
        QueueManager.Instance.LoadLevel(_level);
        GridManager.Instance.SpawnGrid(_level);
        Debug.Log(
            $"[SmokeTest] Spawned grid RemainingBlocks={GridManager.Instance.RemainingBlocks}, " +
            $"waiting={QueueManager.Instance.Waiting?.Count ?? 0}. " +
            "Tap front waiting / queue units via InputManager.");
    }

    private bool ValidateSetup()
    {
        if (_level == null)
        {
            Debug.LogError(
                "[SmokeTest] Level is not assigned. Drag Level_001.asset onto TestHarness → Level.");
            return false;
        }

        if (_config == null)
        {
            Debug.LogError(
                "[SmokeTest] Config is not assigned. Drag GameConfig.asset onto TestHarness → Config.");
            return false;
        }

        if (!PoolManager.HasInstance)
        {
            Debug.LogError("[SmokeTest] PoolManager missing. Add PoolManager with PF_CollectorUnit + PF_PixelBlock.");
            return false;
        }

        if (!ConveyorPathManager.HasInstance)
        {
            Debug.LogError("[SmokeTest] ConveyorPathManager missing.");
            return false;
        }

        return true;
    }

    private static void EnsureGridManager()
    {
        if (GridManager.HasInstance)
            return;

        var go = new GameObject("GridManager");
        go.AddComponent<GridManager>();
        Debug.Log("[SmokeTest] Created GridManager at runtime.");
    }

    private static void EnsureInputManager()
    {
        if (InputManager.HasInstance)
            return;

        var go = new GameObject("InputManager");
        go.AddComponent<InputManager>();
        Debug.Log("[SmokeTest] Created InputManager at runtime.");
    }

    private void HandleBlockConsumed()
    {
        int remaining = GridManager.HasInstance ? GridManager.Instance.RemainingBlocks : -1;
        Debug.Log($"[SmokeTest] Block consumed. RemainingBlocks={remaining}");
    }

    private static void HandleLapComplete(CollectorUnit unit)
    {
        Debug.Log($"[SmokeTest] Lap complete: color={unit.Color}, capacity={unit.Capacity}");
    }

    private static void HandleCollectorExited(CollectorUnit unit)
    {
        Debug.Log($"[SmokeTest] Collector exited: color={unit.Color}");
    }

    private static void HandleConveyorCountChanged(int active, int max)
    {
        Debug.Log($"[SmokeTest] Conveyor count: {active}/{max}");
    }

    private static void HandleQueueCountChanged(int occupied, int max)
    {
        Debug.Log($"[SmokeTest] Queue count: {occupied}/{max}");
    }
}
