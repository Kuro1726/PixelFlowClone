using System.Collections.Generic;
using PixelFlowClone.Data;
using UnityEngine;

namespace PixelFlowClone.Utils
{
    /// <summary>
    /// Shared layout math for grid bounds, conveyor loop, camera fit, and raycast reach.
    /// </summary>
    public static class LevelLayout
    {
        public const float DefaultPathMargin = 2.5f;

        /// <summary>Queue row sits below the conveyor bottom edge (outside the loop).</summary>
        public const float DefaultQueueGapBelowPath = 1.25f;

        /// <summary>Gap between queue row and waiting front (the spacing that feels “cramped”).</summary>
        public const float DefaultWaitingGapBelowQueue = 2.75f;

        /// <summary>Waiting front below path = queue gap + waiting-below-queue (legacy absolute helper).</summary>
        public static float DefaultWaitingGapBelowPath =>
            DefaultQueueGapBelowPath + DefaultWaitingGapBelowQueue;

        /// <summary>Extra world units below the path so FitCamera includes waiting collectors.</summary>
        public const float DefaultWaitingCameraExtra = 9f;

        public static float ResolveQueueGapBelowPath(LevelDataSO level)
        {
            if (level != null && level.QueueGapBelowPath > 0.01f)
                return level.QueueGapBelowPath;
            return DefaultQueueGapBelowPath;
        }

        public static float ResolveWaitingGapBelowQueue(LevelDataSO level)
        {
            if (level != null && level.WaitingGapBelowQueue > 0.01f)
                return level.WaitingGapBelowQueue;
            return DefaultWaitingGapBelowQueue;
        }

        public static float ResolveWaitingGapBelowPath(LevelDataSO level)
        {
            return ResolveQueueGapBelowPath(level) + ResolveWaitingGapBelowQueue(level);
        }

        /// <summary>
        /// Axis-aligned bounds of cell <em>centers</em> (GridOrigin … last cell).
        /// </summary>
        public static void GetGridCenterBounds(LevelDataSO level, out Vector2 min, out Vector2 max)
        {
            if (level == null)
            {
                min = Vector2.zero;
                max = Vector2.zero;
                return;
            }

            int cellsX = Mathf.Max(1, level.GridSize.x);
            int cellsY = Mathf.Max(1, level.GridSize.y);
            min = level.GridOrigin;
            max = level.GridOrigin + new Vector2(
                (cellsX - 1) * level.CellSpacing.x,
                (cellsY - 1) * level.CellSpacing.y);
        }

        /// <summary>
        /// Bounds that enclose block sprites (cell centers expanded by half cell size).
        /// Conveyor path should be built outside this box.
        /// </summary>
        public static void GetGridBounds(LevelDataSO level, out Vector2 min, out Vector2 max)
        {
            GetGridCenterBounds(level, out min, out max);
            if (level == null)
                return;

            float halfX = Mathf.Max(0.01f, level.CellSpacing.x) * 0.5f;
            float halfY = Mathf.Max(0.01f, level.CellSpacing.y) * 0.5f;
            min -= new Vector2(halfX, halfY);
            max += new Vector2(halfX, halfY);
        }

        public static Vector2 GetGridCenter(LevelDataSO level)
        {
            GetGridCenterBounds(level, out Vector2 min, out Vector2 max);
            return (min + max) * 0.5f;
        }

        /// <summary>
        /// 8-point rectangle loop clockwise around the level grid (entry at index 0, bottom-left).
        /// Path sits <paramref name="pathMargin"/> outside the block sprite bounds.
        /// </summary>
        public static IReadOnlyList<Vector2> ComputeConveyorLoopPositions(
            LevelDataSO level,
            float pathMargin = DefaultPathMargin)
        {
            GetGridBounds(level, out Vector2 gridMin, out Vector2 gridMax);
            float margin = Mathf.Max(0.5f, pathMargin);
            float minX = gridMin.x - margin;
            float minY = gridMin.y - margin;
            float maxX = gridMax.x + margin;
            float maxY = gridMax.y + margin;
            float midX = (minX + maxX) * 0.5f;
            float midY = (minY + maxY) * 0.5f;

            return new List<Vector2>
            {
                new(minX, minY),
                new(midX, minY),
                new(maxX, minY),
                new(maxX, midY),
                new(maxX, maxY),
                new(midX, maxY),
                new(minX, maxY),
                new(minX, midY)
            };
        }

        /// <summary>
        /// Distance from the conveyor rim to past the grid center (covers inward consume on large grids).
        /// </summary>
        public static float RecommendedRaycastDistance(LevelDataSO level, float pathMargin = DefaultPathMargin)
        {
            GetGridBounds(level, out Vector2 min, out Vector2 max);
            float halfW = (max.x - min.x) * 0.5f + pathMargin;
            float halfH = (max.y - min.y) * 0.5f + pathMargin;
            return Mathf.Max(halfW, halfH) + 1f;
        }

        /// <summary>World position of the conveyor bottom-edge center (waypoint 1).</summary>
        public static Vector2 GetConveyorBottomCenter(LevelDataSO level, float pathMargin = DefaultPathMargin)
        {
            GetGridBounds(level, out Vector2 gridMin, out Vector2 gridMax);
            float midX = (gridMin.x + gridMax.x) * 0.5f;
            float bottomY = gridMin.y - pathMargin;
            return new Vector2(midX, bottomY);
        }

        /// <summary>World anchor for the horizontal queue row (outside loop, below bottom path).</summary>
        public static Vector2 GetQueueSlotsWorldPosition(
            LevelDataSO level,
            float pathMargin = DefaultPathMargin,
            float gapBelowPath = -1f)
        {
            Vector2 bottom = GetConveyorBottomCenter(level, pathMargin);
            float gap = gapBelowPath > 0.01f ? gapBelowPath : ResolveQueueGapBelowPath(level);
            return new Vector2(bottom.x, bottom.y - gap);
        }

        /// <summary>World anchor for the waiting stack front (below queue, outside the loop).</summary>
        public static Vector2 GetWaitingStackWorldPosition(
            LevelDataSO level,
            float pathMargin = DefaultPathMargin,
            float gapBelowPath = -1f)
        {
            Vector2 bottom = GetConveyorBottomCenter(level, pathMargin);
            float gap = gapBelowPath > 0.01f ? gapBelowPath : ResolveWaitingGapBelowPath(level);
            return new Vector2(bottom.x, bottom.y - gap);
        }

        /// <summary>
        /// Orthographic size that frames grid + conveyor margin on the given camera.
        /// </summary>
        public static float RecommendedOrthographicSize(
            LevelDataSO level,
            Camera camera,
            float pathMargin = DefaultPathMargin,
            float padding = 1.5f,
            float bottomExtra = -1f)
        {
            GetGridBounds(level, out Vector2 min, out Vector2 max);
            float waitingDepth = ResolveWaitingGapBelowPath(level) + 4f;
            float extra = bottomExtra > 0.01f
                ? bottomExtra
                : Mathf.Max(DefaultWaitingCameraExtra, waitingDepth);
            float width = (max.x - min.x) + pathMargin * 2f + padding * 2f;
            float height = (max.y - min.y) + pathMargin * 2f + padding * 2f + Mathf.Max(0f, extra);

            float aspect = camera != null && camera.aspect > 0.01f ? camera.aspect : (9f / 16f);
            float sizeForHeight = height * 0.5f;
            float sizeForWidth = width * 0.5f / aspect;
            return Mathf.Max(sizeForHeight, sizeForWidth, 1f);
        }

        public static void FitCameraToLevel(Camera camera, LevelDataSO level, float pathMargin = DefaultPathMargin)
        {
            if (camera == null || level == null)
                return;

            Vector2 center = GetGridCenter(level);
            float waitingDepth = ResolveWaitingGapBelowPath(level) + 4f;
            float bottomExtra = Mathf.Max(DefaultWaitingCameraExtra, waitingDepth);
            // Shift frame down so waiting stack below the path stays on-screen.
            center.y -= bottomExtra * 0.5f;
            camera.orthographic = true;
            camera.orthographicSize = RecommendedOrthographicSize(level, camera, pathMargin, bottomExtra: bottomExtra);
            camera.transform.position = new Vector3(center.x, center.y, camera.transform.position.z);
        }
    }
}
