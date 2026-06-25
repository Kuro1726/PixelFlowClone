using UnityEngine;

namespace PixelFlowClone.Conveyor
{
    [CreateAssetMenu(fileName = "ConveyorPath_", menuName = "PixelFlowClone/Conveyor Path")]
    public class ConveyorPathSO : ScriptableObject
    {
        [Tooltip("Index of the waypoint where new units join the conveyor loop.")]
        public int EntryWaypointIndex;

        [Tooltip("Override move speed. 0 = use GameConfigSO.CollectorMoveSpeed.")]
        public float MoveSpeed;
    }
}
