using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Conveyor
{
    /// <summary>
    /// Casts a ray perpendicular to the collector's move direction to find matching PixelBlocks.
    /// Ray direction is never aimed diagonally at map center — it is always Left/Right of velocity,
    /// or the side of that perpendicular that points toward the grid (Inward).
    /// </summary>
    public static class PerpendicularRaycastSensor
    {
        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[4];

        public static bool TryDetectConsumable(
            Vector2 origin,
            Vector2 moveDirection,
            ColorId collectorColor,
            GameConfigSO config,
            out PixelBlock hitBlock)
        {
            return TryDetectConsumable(origin, moveDirection, collectorColor, config, Vector2.zero, out hitBlock);
        }

        /// <param name="gridCenter">World center of the pixel grid. Used when RaycastSide is Inward/Outward.</param>
        public static bool TryDetectConsumable(
            Vector2 origin,
            Vector2 moveDirection,
            ColorId collectorColor,
            GameConfigSO config,
            Vector2 gridCenter,
            out PixelBlock hitBlock)
        {
            hitBlock = null;

            if (config == null)
                return false;

            if (moveDirection.sqrMagnitude < 0.001f)
                return false;

            Vector2 perpendicular = ComputePerpendicular(moveDirection.normalized, config.RaycastSide, origin, gridCenter);
            if (perpendicular.sqrMagnitude < 0.001f)
                return false;

            perpendicular.Normalize();

            int mask = config.PixelBlockLayer.value != 0
                ? config.PixelBlockLayer.value
                : PhysicsLayers.GetLayerMask(PhysicsLayers.PixelBlock);

            int hitCount = Physics2D.RaycastNonAlloc(
                origin,
                perpendicular,
                Hits,
                config.RaycastDistance,
                mask);

#if UNITY_EDITOR
            Debug.DrawRay(origin, perpendicular * config.RaycastDistance, Color.yellow, 0.1f);
#endif

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = Hits[i].collider;
                if (collider == null)
                    continue;

                PixelBlock block = collider.GetComponent<PixelBlock>();
                if (block == null || block.IsConsumed)
                    continue;

                if (block.Color != collectorColor)
                    continue;

                hitBlock = block;
                return true;
            }

            return false;
        }

        public static Vector2 ComputePerpendicular(
            Vector2 moveDirection,
            PerpendicularSide side,
            Vector2 origin,
            Vector2 gridCenter)
        {
            Vector2 left = new Vector2(-moveDirection.y, moveDirection.x);
            Vector2 right = new Vector2(moveDirection.y, -moveDirection.x);

            return side switch
            {
                PerpendicularSide.Left => left,
                PerpendicularSide.Right => right,
                PerpendicularSide.Inward => PickTowardTarget(left, right, origin, gridCenter),
                PerpendicularSide.Outward => PickTowardTarget(left, right, origin, gridCenter) * -1f,
                _ => left
            };
        }

        /// <summary>
        /// Chooses the left/right perpendicular whose side faces the grid center (same side of moveDirection).
        /// </summary>
        private static Vector2 PickTowardTarget(Vector2 left, Vector2 right, Vector2 origin, Vector2 gridCenter)
        {
            Vector2 toCenter = gridCenter - origin;
            if (toCenter.sqrMagnitude < 0.0001f)
                return left;

            // Dot against left: positive means left points more toward center.
            return Vector2.Dot(left, toCenter) >= 0f ? left : right;
        }
    }
}
