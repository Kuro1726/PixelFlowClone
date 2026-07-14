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

            float distance = config.RaycastDistance;
            // Return all hits along the ray (sorted by distance). Buffer sized for dense grids.
            int hitCount = Physics2D.RaycastNonAlloc(origin, perpendicular, Hits, distance, mask);

            bool matched = false;
            bool blockedByOtherColor = false;

            // Only the nearest collider counts: wrong color = blocked (cannot dig through Blue to Red).
            if (hitCount > 0 && Hits[0].collider != null)
            {
                PixelBlock nearest = Hits[0].collider.GetComponent<PixelBlock>();
                if (nearest != null && !nearest.IsConsumed)
                {
                    if (nearest.Color == collectorColor)
                    {
                        hitBlock = nearest;
                        matched = true;
                    }
                    else
                    {
                        blockedByOtherColor = true;
                    }
                }
            }

            DrawDebugRay(origin, perpendicular, distance, matched, hitCount > 0, blockedByOtherColor);
            return matched;
        }

        /// <summary>
        /// Draws the current perpendicular ray in the Scene view (Play Mode).
        /// Yellow = miss, cyan = blocked by other color, green = matching consumable.
        /// </summary>
        public static void DrawDebugRay(
            Vector2 origin,
            Vector2 perpendicularDirection,
            float distance,
            bool matchedConsumable,
            bool anyPhysicsHit,
            bool blockedByOtherColor = false)
        {
#if UNITY_EDITOR
            if (perpendicularDirection.sqrMagnitude < 0.001f || distance <= 0f)
                return;

            Color color = matchedConsumable
                ? Color.green
                : blockedByOtherColor
                    ? Color.magenta
                    : anyPhysicsHit
                        ? Color.cyan
                        : Color.yellow;

            Debug.DrawRay(origin, perpendicularDirection.normalized * distance, color, 0f, false);
#endif
        }

        /// <summary>
        /// Debug-only draw without consuming. Call from OnConveyor FixedUpdate so the ray stays visible.
        /// </summary>
        public static void DrawDebugRayPreview(
            Vector2 origin,
            Vector2 moveDirection,
            GameConfigSO config,
            Vector2 gridCenter)
        {
#if UNITY_EDITOR
            if (config == null || moveDirection.sqrMagnitude < 0.001f)
                return;

            Vector2 perpendicular = ComputePerpendicular(moveDirection.normalized, config.RaycastSide, origin, gridCenter);
            if (perpendicular.sqrMagnitude < 0.001f)
                return;

            DrawDebugRay(origin, perpendicular.normalized, config.RaycastDistance, matchedConsumable: false, anyPhysicsHit: false);
#endif
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
