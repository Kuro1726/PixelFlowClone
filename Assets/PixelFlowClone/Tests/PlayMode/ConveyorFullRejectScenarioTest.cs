using System.Collections;
using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;

/// <summary>
/// P2-23: conveyor full + waiting dispatch → reject (stays in waiting, never enters queue).
/// Context Menu or enable auto-run on TestHarness.
/// </summary>
public class ConveyorFullRejectScenarioTest : MonoBehaviour
{
    private const string LogTag = "[P2-23]";

    [SerializeField] private LevelDataSO _level;
    [SerializeField] private GameConfigSO _config;
    [SerializeField] private bool _autoRunOnStart;

    private readonly List<CollectorUnit> _frontBuffer = new();
    private CollectorUnit _rejectedUnit;

    private void Start()
    {
        if (_autoRunOnStart)
            StartCoroutine(RunScenario());
    }

#if UNITY_EDITOR
    [ContextMenu("P2-23/Run Conveyor-Full Reject Scenario")]
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

        ConveyorPathManager conveyor = ConveyorPathManager.Instance;
        conveyor.ClearActiveUnits();
        QueueManager.Instance.SpawnWaitingFromLevel(_level);

        int max = conveyor.MaxCapacity;
        if (max <= 0)
        {
            Fail("MaxCapacity is 0.");
            yield break;
        }

        Debug.Log($"{LogTag} Step 1: fill conveyor to {max}/{max} (pool units, waiting untouched)");
        if (!FillConveyorToCapacity(conveyor, max))
            yield break;

        if (conveyor.ActiveCount != max || conveyor.HasCapacity)
        {
            Fail($"Expected full conveyor Active={max} HasCapacity=false, got Active={conveyor.ActiveCount}.");
            yield break;
        }

        PassStep($"Conveyor full {max}/{max}");

        CollectorUnit subject = PickWaitingFront();
        if (subject == null)
        {
            Fail("No waiting front left to reject.");
            yield break;
        }

        int waitingBefore = QueueManager.Instance.Waiting.Count;
        int queueBefore = QueueManager.Instance.OccupiedSlots;
        CollectorState stateBefore = subject.State;

        _rejectedUnit = null;
        GameEvents.OnConveyorDispatchRejected += HandleRejected;

        Debug.Log(
            $"{LogTag} Step 2: TryDispatchFromWaiting while full " +
            $"({subject.Color}, cap={subject.Capacity})");

        bool ok = QueueManager.Instance.TryDispatchFromWaiting(subject);

        GameEvents.OnConveyorDispatchRejected -= HandleRejected;

        if (ok)
        {
            Fail("TryDispatchFromWaiting returned true — should reject when conveyor is full.");
            yield break;
        }

        if (subject.State != CollectorState.InWaitingStack || stateBefore != CollectorState.InWaitingStack)
        {
            Fail($"Subject must stay InWaitingStack, got {subject.State}.");
            yield break;
        }

        if (!QueueManager.Instance.Waiting.Contains(subject))
        {
            Fail("Subject left the waiting stack after reject.");
            yield break;
        }

        if (QueueManager.Instance.Waiting.Count != waitingBefore)
        {
            Fail(
                $"Waiting count changed on reject: before={waitingBefore} " +
                $"after={QueueManager.Instance.Waiting.Count}.");
            yield break;
        }

        if (QueueManager.Instance.OccupiedSlots != queueBefore)
        {
            Fail(
                $"Queue occupied changed on reject: before={queueBefore} " +
                $"after={QueueManager.Instance.OccupiedSlots} — must not enter queue.");
            yield break;
        }

        if (conveyor.ActiveCount != max)
        {
            Fail($"Conveyor ActiveCount changed on reject: expected {max}, got {conveyor.ActiveCount}.");
            yield break;
        }

        if (_rejectedUnit != subject)
        {
            Fail("OnConveyorDispatchRejected did not fire for the waiting unit.");
            yield break;
        }

        PassStep("Waiting dispatch rejected — stayed in waiting, queue untouched");
        Debug.Log($"{LogTag} PASS — conveyor full + waiting tap → reject (no queue)");
        yield return null;
    }

    private bool FillConveyorToCapacity(ConveyorPathManager conveyor, int max)
    {
        for (int i = 0; i < max; i++)
        {
            CollectorUnit filler = PoolManager.Instance.GetCollector();
            filler.Initialize(ColorId.Red, 1);
            filler.ForceState(CollectorState.InWaitingStack);

            if (!conveyor.DispatchToConveyor(filler))
            {
                Fail($"Failed to fill conveyor at slot {i + 1}/{max}.");
                PoolManager.Instance.ReleaseCollector(filler);
                return false;
            }
        }

        return true;
    }

    private CollectorUnit PickWaitingFront()
    {
        _frontBuffer.Clear();
        QueueManager.Instance.Waiting?.GetFronts(_frontBuffer);
        return _frontBuffer.Count > 0 ? _frontBuffer[0] : null;
    }

    private void HandleRejected(CollectorUnit unit) => _rejectedUnit = unit;

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

    private static void PassStep(string step) =>
        Debug.Log($"{LogTag} OK — {step}");

    private static void Fail(string reason) =>
        Debug.LogError($"{LogTag} FAIL — {reason}");
}
