using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixelFlowClone.Conveyor
{
    /// <summary>
    /// A single point on the closed conveyor loop. Place as children under a path root;
    /// assign sequential Index values (0, 1, 2, …). Gizmos draw the loop in the Editor.
    /// </summary>
    [ExecuteAlways]
    public class ConveyorWaypoint : MonoBehaviour
    {
        [SerializeField] private int _index;

        [Header("Gizmo")]
        [SerializeField] private Color _waypointColor = new(0.2f, 0.85f, 1f, 0.9f);
        [SerializeField] private Color _pathColor = new(1f, 0.85f, 0.1f, 0.85f);
        [SerializeField] private float _gizmoRadius = 0.15f;

        public int Index => _index;
        public Vector2 Position => transform.position;

        public void SetIndex(int index)
        {
            _index = Mathf.Max(0, index);
        }

        /// <summary>
        /// Returns all sibling waypoints under the same parent, sorted by Index.
        /// </summary>
        public IReadOnlyList<ConveyorWaypoint> GetSiblingsOrdered()
        {
            if (transform.parent == null)
                return new[] { this };

            return transform.parent
                .GetComponentsInChildren<ConveyorWaypoint>()
                .OrderBy(w => w.Index)
                .ToList();
        }

        public ConveyorWaypoint GetNext(IReadOnlyList<ConveyorWaypoint> ordered)
        {
            if (ordered == null || ordered.Count == 0)
                return null;

            int i = IndexOfSelf(ordered);
            if (i < 0)
                return ordered[0];

            return ordered[(i + 1) % ordered.Count];
        }

        private int IndexOfSelf(IReadOnlyList<ConveyorWaypoint> ordered)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] == this)
                    return i;
            }

            return -1;
        }

        private void OnDrawGizmos()
        {
            IReadOnlyList<ConveyorWaypoint> siblings = GetSiblingsOrdered();
            Vector3 pos = transform.position;

            Gizmos.color = _waypointColor;
            Gizmos.DrawSphere(pos, _gizmoRadius);

#if UNITY_EDITOR
            UnityEditor.Handles.color = _waypointColor;
            UnityEditor.Handles.Label(pos + Vector3.up * 0.25f, _index.ToString());
#endif

            ConveyorWaypoint next = GetNext(siblings);
            if (next == null || next == this)
                return;

            Gizmos.color = _pathColor;
            Gizmos.DrawLine(pos, next.transform.position);
        }

        private void OnValidate()
        {
            if (_index < 0)
                _index = 0;
        }
    }
}
