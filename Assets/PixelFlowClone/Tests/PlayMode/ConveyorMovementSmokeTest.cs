using System.Collections;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;

/// <summary>
/// Play Mode harness: Level_001 grid + red collector, then blue after a delay.
/// </summary>
public class ConveyorMovementSmokeTest : MonoBehaviour
{
    [SerializeField] private LevelDataSO _level;
    [SerializeField] private GameConfigSO _config;
    [SerializeField] private bool _dispatchBlueCollector = true;
    [SerializeField] private float _blueDelaySeconds = 3f;

    private void OnEnable()
    {
        GameEvents.OnBlockConsumed += HandleBlockConsumed;
        GameEvents.OnCollectorLapComplete += HandleLapComplete;
        GameEvents.OnCollectorExited += HandleCollectorExited;
    }

    private void OnDisable()
    {
        GameEvents.OnBlockConsumed -= HandleBlockConsumed;
        GameEvents.OnCollectorLapComplete -= HandleLapComplete;
        GameEvents.OnCollectorExited -= HandleCollectorExited;
    }

    private void Start()
    {
        if (!ValidateSetup())
            return;

        StartCoroutine(RunSmokeTest());
    }

    private IEnumerator RunSmokeTest()
    {
        EnsureGridManager();

        PoolManager.Instance.Prewarm(_level, _config);
        GridManager.Instance.SpawnGrid(_level);
        Debug.Log($"[SmokeTest] Spawned grid RemainingBlocks={GridManager.Instance.RemainingBlocks}");

        DispatchCollector(ColorId.Red, capacity: 7);

        if (!_dispatchBlueCollector)
            yield break;

        Debug.Log($"[SmokeTest] Blue collector in {_blueDelaySeconds:0.##}s...");
        yield return new WaitForSeconds(_blueDelaySeconds);
        DispatchCollector(ColorId.Blue, capacity: 2);
    }

    private static void DispatchCollector(ColorId color, int capacity)
    {
        CollectorUnit unit = PoolManager.Instance.GetCollector();
        unit.Initialize(color, capacity);
        unit.ForceState(CollectorState.InWaitingStack);

        bool ok = ConveyorPathManager.Instance.DispatchToConveyor(unit);
        Debug.Log($"[SmokeTest] Dispatch {color} capacity={capacity} ok={ok}");
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
}
