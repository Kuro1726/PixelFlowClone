using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using PixelFlowClone.Managers;
using UnityEngine;

public class ConveyorMovementSmokeTest : MonoBehaviour
{
    void Start()
    {
        // 1. Lấy collector từ pool (PoolManager đã có prefab ở Bước 2)
        CollectorUnit unit = PoolManager.Instance.GetCollector();
        unit.Initialize(ColorId.Red, 10);

        // 2. Phải ở WaitingStack thì mới chuyển sang OnConveyor được
        unit.ForceState(CollectorState.InWaitingStack);

        // 3. Đẩy lên băng chuyền
        bool ok = ConveyorPathManager.Instance.DispatchToConveyor(unit);
        Debug.Log($"DispatchToConveyor: {ok}");

        GameEvents.OnCollectorLapComplete += u => Debug.Log($"Lap complete: {u.Color}, cap={u.Capacity}");
    }
}