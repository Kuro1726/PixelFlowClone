using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.Conveyor
{
    /// <summary>
    /// Metadata for a conveyor loop. Waypoint world positions live in the scene
    /// (ConveyorWaypoint children under a path root); this asset stores entry index and speed override.
    /// </summary>
    [CreateAssetMenu(fileName = "ConveyorPath_", menuName = "PixelFlowClone/Conveyor Path")]
    public class ConveyorPathSO : ScriptableObject
    {
        [Header("Entry")]
        [Tooltip("Index of the waypoint where new units join the conveyor loop.")]
        [Min(0)]
        public int EntryWaypointIndex;

        [Header("Movement")]
        [Tooltip("Override move speed. 0 = use GameConfigSO.CollectorMoveSpeed.")]
        [Min(0f)]
        public float MoveSpeed;

        /// <summary>
        /// Returns configured override speed when MoveSpeed > 0, otherwise the global config speed.
        /// </summary>
        public float ResolveMoveSpeed(GameConfigSO config)
        {
            if (MoveSpeed > 0f)
                return MoveSpeed;

            return config != null ? config.CollectorMoveSpeed : 3f;
        }

        /// <summary>
        /// Clamps entry index into [0, waypointCount - 1]. Returns 0 when the list is empty.
        /// </summary>
        public int ClampEntryIndex(int waypointCount)
        {
            if (waypointCount <= 0)
                return 0;

            return Mathf.Clamp(EntryWaypointIndex, 0, waypointCount - 1);
        }

        private void OnValidate()
        {
            if (EntryWaypointIndex < 0)
                EntryWaypointIndex = 0;

            if (MoveSpeed < 0f)
                MoveSpeed = 0f;
        }
    }
}
