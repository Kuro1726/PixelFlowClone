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

        /// <summary>Radius of the rounded conveyor corners in world units.</summary>
        public const float DefaultConveyorCornerRadius = 0.9f;

        /// <summary>Number of straight samples used for each 90-degree conveyor corner.</summary>
        public const int DefaultConveyorCornerSegments = 5;

        /// <summary>Queue row sits below the conveyor bottom edge (outside the loop).</summary>
        public const float DefaultQueueGapBelowPath = 1.25f;

        /// <summary>Gap between queue row and waiting front (the spacing that feels “cramped”).</summary>
        public const float DefaultWaitingGapBelowQueue = 2.75f;

        /// <summary>Waiting front below path = queue gap + waiting-below-queue (legacy absolute helper).</summary>
        public static float DefaultWaitingGapBelowPath =>
            DefaultQueueGapBelowPath + DefaultWaitingGapBelowQueue;

        /// <summary>Extra world units below the path so FitCamera includes waiting collectors.</summary>
        public const float DefaultWaitingCameraExtra = 9f;

        /// <summary>
        /// World pad above path for capacity TMP (local y ≈ 0.6) plus HUD clearance.
        /// </summary>
        public const float DefaultHudTopCameraExtra = 1.15f;

        /// <summary>
        /// Padding below waiting front. Covers ~3 stacked units at 1.5 spacing + half sprite.
        /// </summary>
        public const float DefaultWaitingStackDepth = 4.5f;

        /// <summary>
        /// Scene conveyor sits ~1 unit outside the 5×5 playfield (waypoints at ±3.5).
        /// Do not use <see cref="DefaultPathMargin"/> (2.5) for camera fit — it over-zooms.
        /// </summary>
        public const float DefaultPlayfieldPathMargin = 1f;

        /// <summary>Slight safety margin; keep near 1 so playfield fills the Game view.</summary>
        public const float DefaultCameraFitSafety = 1.02f;

        public static float ResolveQueueGapBelowPath(LevelDataSO level, GameConfigSO config = null)
        {
            if (level != null && level.QueueGapBelowPath > 0.01f)
                return level.QueueGapBelowPath;
            if (config != null && config.QueueGapBelowPath > 0.01f)
                return config.QueueGapBelowPath;
            return DefaultQueueGapBelowPath;
        }

        public static float ResolveWaitingGapBelowQueue(LevelDataSO level, GameConfigSO config = null)
        {
            if (level != null && level.WaitingGapBelowQueue > 0.01f)
                return level.WaitingGapBelowQueue;
            if (config != null && config.WaitingGapBelowQueue > 0.01f)
                return config.WaitingGapBelowQueue;
            return DefaultWaitingGapBelowQueue;
        }

        public static float ResolveWaitingGapBelowPath(LevelDataSO level, GameConfigSO config = null)
        {
            return ResolveQueueGapBelowPath(level, config) + ResolveWaitingGapBelowQueue(level, config);
        }

        public static float ResolveQueueUnitSpacing(LevelDataSO level, GameConfigSO config, float sceneDefault)
        {
            if (level != null && level.QueueUnitSpacing > 0.01f)
                return level.QueueUnitSpacing;
            if (config != null && config.QueueUnitSpacing > 0.01f)
                return config.QueueUnitSpacing;
            return sceneDefault;
        }

        public static float ResolveWaitingUnitSpacing(LevelDataSO level, GameConfigSO config, float sceneDefault)
        {
            if (level != null && level.WaitingUnitSpacing > 0.01f)
                return level.WaitingUnitSpacing;
            if (config != null && config.WaitingUnitSpacing > 0.01f)
                return config.WaitingUnitSpacing;
            return sceneDefault;
        }

        public static float ResolveWaitingColumnSpacing(LevelDataSO level, GameConfigSO config, float sceneDefault)
        {
            if (level != null && level.WaitingColumnSpacing > 0.01f)
                return level.WaitingColumnSpacing;
            if (config != null && config.WaitingColumnSpacing > 0.01f)
                return config.WaitingColumnSpacing;
            return sceneDefault;
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
        /// Rounded rectangle loop around a fixed playfield frame.
        /// Path sits <paramref name="pathMargin"/> outside the block frame.
        /// </summary>
        public static IReadOnlyList<Vector2> ComputeConveyorLoopPositionsForPlayfield(
            Vector2 playfieldCenter,
            Vector2 playfieldSize,
            float pathMargin = DefaultPlayfieldPathMargin,
            float cornerRadius = DefaultConveyorCornerRadius,
            int cornerSegments = DefaultConveyorCornerSegments)
        {
            float halfW = Mathf.Max(0.01f, playfieldSize.x) * 0.5f;
            float halfH = Mathf.Max(0.01f, playfieldSize.y) * 0.5f;
            float margin = Mathf.Max(0.25f, pathMargin);

            float minX = playfieldCenter.x - halfW - margin;
            float minY = playfieldCenter.y - halfH - margin;
            float maxX = playfieldCenter.x + halfW + margin;
            float maxY = playfieldCenter.y + halfH + margin;
            return BuildRoundedRectangleLoop(
                minX, minY, maxX, maxY, cornerRadius, cornerSegments);
        }

        /// <summary>
        /// Rounded rectangle loop counter-clockwise around the level grid.
        /// Entry index 0 is the bottom-left corner's bottom tangent, moving right.
        /// Path sits <paramref name="pathMargin"/> outside the block sprite bounds.
        /// </summary>
        public static IReadOnlyList<Vector2> ComputeConveyorLoopPositions(
            LevelDataSO level,
            float pathMargin = DefaultPathMargin,
            float cornerRadius = DefaultConveyorCornerRadius,
            int cornerSegments = DefaultConveyorCornerSegments)
        {
            GetGridBounds(level, out Vector2 gridMin, out Vector2 gridMax);
            float margin = Mathf.Max(0.5f, pathMargin);
            float minX = gridMin.x - margin;
            float minY = gridMin.y - margin;
            float maxX = gridMax.x + margin;
            float maxY = gridMax.y + margin;
            return BuildRoundedRectangleLoop(
                minX, minY, maxX, maxY, cornerRadius, cornerSegments);
        }

        private static IReadOnlyList<Vector2> BuildRoundedRectangleLoop(
            float minX,
            float minY,
            float maxX,
            float maxY,
            float cornerRadius,
            int cornerSegments)
        {
            float width = Mathf.Max(0.01f, maxX - minX);
            float height = Mathf.Max(0.01f, maxY - minY);
            float radius = Mathf.Clamp(cornerRadius, 0.01f, Mathf.Min(width, height) * 0.5f);
            int segments = Mathf.Clamp(cornerSegments, 2, 12);
            float midX = (minX + maxX) * 0.5f;
            float midY = (minY + maxY) * 0.5f;

            var result = new List<Vector2>(segments * 4 + 8);

            // Start on the bottom edge so the configured entry continues moving right.
            AddDistinct(result, new Vector2(minX + radius, minY));
            AddDistinct(result, new Vector2(midX, minY));
            AddDistinct(result, new Vector2(maxX - radius, minY));
            AddArc(result, new Vector2(maxX - radius, minY + radius), radius, -90f, 0f, segments, true);

            AddDistinct(result, new Vector2(maxX, midY));
            AddDistinct(result, new Vector2(maxX, maxY - radius));
            AddArc(result, new Vector2(maxX - radius, maxY - radius), radius, 0f, 90f, segments, true);

            AddDistinct(result, new Vector2(midX, maxY));
            AddDistinct(result, new Vector2(minX + radius, maxY));
            AddArc(result, new Vector2(minX + radius, maxY - radius), radius, 90f, 180f, segments, true);

            AddDistinct(result, new Vector2(minX, midY));
            AddDistinct(result, new Vector2(minX, minY + radius));
            // Skip the last point because it is identical to the loop's first point.
            AddArc(result, new Vector2(minX + radius, minY + radius), radius, 180f, 270f, segments, false);

            return result;
        }

        private static void AddArc(
            List<Vector2> points,
            Vector2 center,
            float radius,
            float startDegrees,
            float endDegrees,
            int segments,
            bool includeEnd)
        {
            int last = includeEnd ? segments : segments - 1;
            for (int i = 1; i <= last; i++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, i / (float)segments) * Mathf.Deg2Rad;
                AddDistinct(points, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        private static void AddDistinct(List<Vector2> points, Vector2 point)
        {
            if (points.Count == 0 || (points[points.Count - 1] - point).sqrMagnitude > 0.000001f)
                points.Add(point);
        }

        public static float ResolveConveyorPathMargin(GameConfigSO config)
        {
            if (config != null && config.ConveyorPathMargin > 0.01f)
                return config.ConveyorPathMargin;
            return DefaultPlayfieldPathMargin;
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

        /// <summary>World position of conveyor bottom-edge center for a fixed playfield frame.</summary>
        public static Vector2 GetPlayfieldConveyorBottomCenter(
            Vector2 playfieldCenter,
            Vector2 playfieldSize,
            float pathMargin = -1f)
        {
            float margin = pathMargin > 0.01f ? pathMargin : DefaultPlayfieldPathMargin;
            float halfH = Mathf.Max(0.01f, playfieldSize.y) * 0.5f;
            return new Vector2(playfieldCenter.x, playfieldCenter.y - halfH - margin);
        }

        /// <summary>Queue row world position under a fixed playfield conveyor.</summary>
        public static Vector2 GetPlayfieldQueueWorldPosition(
            Vector2 playfieldCenter,
            Vector2 playfieldSize,
            LevelDataSO level,
            float pathMargin = -1f,
            GameConfigSO config = null)
        {
            Vector2 bottom = GetPlayfieldConveyorBottomCenter(playfieldCenter, playfieldSize, pathMargin);
            float gap = ResolveQueueGapBelowPath(level, config);
            return new Vector2(bottom.x, bottom.y - gap);
        }

        /// <summary>Waiting front world position under a fixed playfield conveyor.</summary>
        public static Vector2 GetPlayfieldWaitingWorldPosition(
            Vector2 playfieldCenter,
            Vector2 playfieldSize,
            LevelDataSO level,
            float pathMargin = -1f,
            GameConfigSO config = null)
        {
            Vector2 bottom = GetPlayfieldConveyorBottomCenter(playfieldCenter, playfieldSize, pathMargin);
            float gap = ResolveWaitingGapBelowPath(level, config);
            return new Vector2(bottom.x, bottom.y - gap);
        }

        /// <summary>World position of the conveyor bottom-edge center (waypoint 1) — SO GridOrigin space.</summary>
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
        /// Orthographic size that frames grid + conveyor + waiting on the given camera.
        /// Prefer <see cref="FitCameraToPlayfield"/> at runtime — LevelDataSO GridOrigin is NOT world layout.
        /// </summary>
        public static float RecommendedOrthographicSize(
            LevelDataSO level,
            Camera camera,
            float pathMargin = DefaultPathMargin,
            float padding = 1.25f,
            float bottomExtra = -1f,
            float topExtra = -1f)
        {
            // Legacy SO-space estimate (editor-only fallback). Runtime must use playfield.
            GetContentVerticalBounds(level, pathMargin, bottomExtra, topExtra, out float contentMinY, out float contentMaxY);
            GetGridBounds(level, out Vector2 min, out Vector2 max);

            float width = (max.x - min.x) + pathMargin * 2f + padding * 2f;
            float height = (contentMaxY - contentMinY) + padding * 2f;

            float aspect = camera != null && camera.aspect > 0.01f ? camera.aspect : (9f / 16f);
            float sizeForHeight = height * 0.5f;
            float sizeForWidth = width * 0.5f / aspect;
            return Mathf.Max(sizeForHeight, sizeForWidth, 1f);
        }

        /// <summary>
        /// Frames the fixed scene playfield (grid frame + conveyor margin + waiting stack).
        /// Pass <paramref name="config"/> so values can be tuned in GameConfig asset.
        /// </summary>
        public static void FitCameraToPlayfield(
            Camera camera,
            Vector2 playfieldCenter,
            Vector2 playfieldSize,
            float waitingFrontY,
            GameConfigSO config = null,
            float pathMargin = -1f)
        {
            if (camera == null)
                return;

            float margin = pathMargin > 0.01f
                ? pathMargin
                : LevelLayout.ResolveConveyorPathMargin(config);
            float topPad = config != null ? Mathf.Max(0f, config.CameraTopPad) : DefaultHudTopCameraExtra;
            float stackDepth = config != null
                ? Mathf.Max(0.5f, config.CameraWaitingStackDepth)
                : DefaultWaitingStackDepth;
            float padding = config != null ? Mathf.Max(0f, config.CameraPadding) : 0.35f;
            float screenScale = config != null ? Mathf.Max(0.5f, config.GameplayScreenScale) : 1f;
            float hudFraction = config != null
                ? Mathf.Clamp(config.CameraHudScreenFraction, 0.04f, 0.18f)
                : 0.1f;
            float verticalBias = config != null ? config.CameraVerticalBias : 0.35f;

            float halfW = Mathf.Max(0.01f, playfieldSize.x) * 0.5f;
            float halfH = Mathf.Max(0.01f, playfieldSize.y) * 0.5f;

            float pathTopY = playfieldCenter.y + halfH + margin;
            float pathBottomY = playfieldCenter.y - halfH - margin;

            float contentMaxY = pathTopY + topPad;
            float contentMinY = Mathf.Min(pathBottomY, waitingFrontY - stackDepth);
            float contentMinX = playfieldCenter.x - halfW - margin;
            float contentMaxX = playfieldCenter.x + halfW + margin;

            float width = (contentMaxX - contentMinX) + padding * 2f;
            float height = (contentMaxY - contentMinY) + padding * 2f;
            float centerY = (contentMinY + contentMaxY) * 0.5f;

            float aspect = camera.aspect > 0.01f ? camera.aspect : (9f / 16f);
            float sizeForHeight = height * 0.5f;
            float sizeForWidth = width * 0.5f / aspect;
            float fittedSize = Mathf.Max(sizeForHeight, sizeForWidth, 1f) * DefaultCameraFitSafety;
            float zoomedSize = fittedSize / screenScale;
            float minimumWidthSize = sizeForWidth * DefaultCameraFitSafety;
            float size = Mathf.Max(zoomedSize, minimumWidthSize, 1f);

            float topOfView = centerY + size;
            float desiredContentTop = topOfView - size * 2f * hudFraction;
            float labelTop = pathTopY + 0.7f;
            if (labelTop > desiredContentTop)
                centerY -= labelTop - desiredContentTop;

            // Positive bias lowers playfield on screen (away from HUD).
            centerY += verticalBias;

            float bottomOfView = centerY - size;
            if (contentMinY < bottomOfView)
                size = centerY - contentMinY;

            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.transform.position = new Vector3(
                playfieldCenter.x,
                centerY,
                camera.transform.position.z);
        }

        /// <summary>
        /// Editor fallback using LevelDataSO bounds. Prefer <see cref="FitCameraToPlayfield"/> in play mode.
        /// </summary>
        public static void FitCameraToLevel(Camera camera, LevelDataSO level, float pathMargin = DefaultPathMargin)
        {
            if (camera == null || level == null)
                return;

            FitCameraToPlayfield(
                camera,
                playfieldCenter: Vector2.zero,
                playfieldSize: new Vector2(5f, 5f),
                waitingFrontY: GetWaitingStackWorldPosition(level, pathMargin).y);
        }

        /// <summary>
        /// World Y range covering conveyor top (plus HUD pad) down through the waiting stack (SO-space).
        /// </summary>
        private static void GetContentVerticalBounds(
            LevelDataSO level,
            float pathMargin,
            float bottomExtra,
            float topExtra,
            out float contentMinY,
            out float contentMaxY)
        {
            GetGridBounds(level, out _, out Vector2 gridMax);
            float margin = Mathf.Max(0.5f, pathMargin);
            float hudPad = topExtra > 0.01f ? topExtra : DefaultHudTopCameraExtra;
            float stackDepth = bottomExtra > 0.01f ? bottomExtra : DefaultWaitingStackDepth;

            contentMaxY = gridMax.y + margin + hudPad;
            contentMinY = GetWaitingStackWorldPosition(level, pathMargin).y - stackDepth;
        }
    }
}
