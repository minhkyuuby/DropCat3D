using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    internal static class GridDragMath
    {
        public static int RoundToMultiple(int value, int multiple)
        {
            if (multiple <= 1)
            {
                return value;
            }

            // Round to nearest multiple.
            float scaled = (float)value / multiple;
            return Mathf.RoundToInt(scaled) * multiple;
        }

        // Simple integer line stepping (Bresenham-like) for 2D grid.
        // Yields a sequence of intermediate cells from start (exclusive) to end (inclusive).
        public static System.Collections.Generic.IEnumerable<Vector2Int> StepLine(Vector2Int start, Vector2Int end)
        {
            int x0 = start.x;
            int y0 = start.y;
            int x1 = end.x;
            int y1 = end.y;

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);

            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;

            int err = dx - dy;

            int x = x0;
            int y = y0;

            while (!(x == x1 && y == y1))
            {
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
                yield return new Vector2Int(x, y);
            }
        }
    }
}
