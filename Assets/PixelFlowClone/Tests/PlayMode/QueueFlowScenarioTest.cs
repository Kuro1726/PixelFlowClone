using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;

/// <summary>
/// P2-22: automated scenario — waiting → conveyor → (lap) queue → conveyor.
/// Attach to TestHarness (or any GO in SCN_Gameplay). Use Context Menu or enable auto-run.
/// Skips grid spawn so capacity stays &gt; 0 and lap enqueues instead of exiting.
/// </summary>
public class QueueFlowScenarioTest : MonoBehaviour
{
    private const string LogTag = "[P2-22]";

    [SerializeField] private LevelDataSO _level;
    [SerializeField] private GameConfigSO _config;
    [SerializeField] private bool _autoRunOnStart;
    [SerializeField] private float _lapTimeoutSeconds = 45f;

    private readonly List<CollectorUnit> _frontBuffer = new();

    private void Start()
    {
        if (_autoRunOnStart)
            StartCoroutine(RunScenario());
    }

#if UNITY_EDITOR
    [ContextMenu("P2-22/Run Queue Flow Scenario")]
    private void ContextRunScenario()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"{LogTag} Enter Play Mode first, then run this Context Menu.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(RunScenario());
    }
#endif

    private IEnumerator RunScenario()
    {
        if (!ValidateSetup())
            yield break;

        EnsureManagers();

        if (!QueueManager.HasInstance)
        {
            Fail("QueueManager missing.");
            yield break;
        }

        PoolManager.Instance.Prewarm(_level, _config);
        if (GridManager.HasInstance)
            GridManager.Instance.ClearGrid();
        // No grid — collector must finish the lap with Capacity > 0 to enter the queue.
        QueueManager.Instance.SpawnWaitingFromLevel(_level);
        ConveyorPathManager.Instance.ClearActiveUnits();

        CollectorUnit subject = PickWaitingFrontWithCapacity();
        if (subject == null)
        {
            Fail("No waiting front with Capacity > 0.");
            yield break;
        }

        int capacity = subject.Capacity;
        ColorId color = subject.Color;
        Debug.Log($"{LogTag} Step 1: Waiting → Conveyor ({color}, cap={capacity})");

        if (!QueueManager.Instance.TryDispatchFromWaiting(subject))
        {
            Fail("TryDispatchFromWaiting returned false.");
            yield break;
        }

        if (subject.State != CollectorState.OnConveyor ||
            ConveyorPathManager.Instance.ActiveCount != 1)
        {
            Fail(
                $"After waiting dispatch expected OnConveyor + ActiveCount=1, " +
                $"got state={subject.State} active={ConveyorPathManager.Instance.ActiveCount}.");
            yield break;
        }

        PassStep("Waiting → Conveyor");

        Debug.Log($"{LogTag} Step 2: wait for lap → Queue (timeout={_lapTimeoutSeconds}s)");
        float deadline = Time.time + Mathf.Max(5f, _lapTimeoutSeconds);
        while (Time.time < deadline)
        {
            if (subject == null || subject.State == CollectorState.Pooled ||
                subject.State == CollectorState.Exiting)
            {
                Fail(
                    $"Collector left conveyor without entering queue " +
                    $"(state={subject?.State}, capacity may have hit 0).");
                yield break;
            }

            if (subject.State == CollectorState.InQueueSlot)
                break;

            yield return null;
        }

        if (subject.State != CollectorState.InQueueSlot)
        {
            Fail($"Timed out waiting for lap enqueue. state={subject.State}");
            yield break;
        }

        if (QueueManager.Instance.OccupiedSlots != 1 ||
            ConveyorPathManager.Instance.ActiveCount != 0)
        {
            Fail(
                $"After lap enqueue expected Occupied=1 Active=0, " +
                $"got Occupied={QueueManager.Instance.OccupiedSlots} " +
                $"Active={ConveyorPathManager.Instance.ActiveCount}.");
            yield break;
        }

        PassStep("Lap → Queue");

        Debug.Log($"{LogTag} Step 3: Queue → Conveyor");
        if (!QueueManager.Instance.TryDispatchFromQueue(subject))
        {
            Fail("TryDispatchFromQueue returned false.");
            yield break;
        }

        if (subject.State != CollectorState.OnConveyor ||
            ConveyorPathManager.Instance.ActiveCount != 1 ||
            QueueManager.Instance.OccupiedSlots != 0)
        {
            Fail(
                $"After queue dispatch expected OnConveyor Active=1 Occupied=0, " +
                $"got state={subject.State} active={ConveyorPathManager.Instance.ActiveCount} " +
                $"occupied={QueueManager.Instance.OccupiedSlots}.");
            yield break;
        }

        PassStep("Queue → Conveyor");
        Debug.Log($"{LogTag} PASS — waiting → conveyor → queue → conveyor");
    }

    private bool ValidateSetup()
    {
        if (_level == null)
        {
            Fail("Level is not assigned.");
            return false;
        }

        if (_config == null)
        {
            Fail("Config is not assigned.");
            return false;
        }

        if (!PoolManager.HasInstance)
        {
            Fail("PoolManager missing.");
            return false;
        }

        if (!ConveyorPathManager.HasInstance)
        {
            Fail("ConveyorPathManager missing.");
            return false;
        }

        return true;
    }

    private static void EnsureManagers()
    {
        if (!GridManager.HasInstance)
            new GameObject("GridManager").AddComponent<GridManager>();

        if (!InputManager.HasInstance)
            new GameObject("InputManager").AddComponent<InputManager>();

        PersistentManagers.EnsureGameManager();
    }

    private CollectorUnit PickWaitingFrontWithCapacity()
    {
        _frontBuffer.Clear();
        QueueManager.Instance.Waiting?.GetFronts(_frontBuffer);

        CollectorUnit best = null;
        for (int i = 0; i < _frontBuffer.Count; i++)
        {
            CollectorUnit unit = _frontBuffer[i];
            if (unit == null || unit.Capacity <= 0)
                continue;

            if (best == null || unit.Capacity > best.Capacity)
                best = unit;
        }

        return best;
    }

    private static void PassStep(string step) =>
        Debug.Log($"{LogTag} OK — {step}");

    private static void Fail(string reason) =>
        Debug.LogError($"{LogTag} FAIL — {reason}");
}
