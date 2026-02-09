using System;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed class InventoryGrid3D : MonoBehaviour
    {
        [Header("Grid")]
        [Min(1)]
        [SerializeField] private int width = 10;

        [Min(1)]
        [SerializeField] private int height = 6;

        [Min(0.01f)]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("World-space origin at cell (0,0) center.")]
        [SerializeField] private Transform origin;

        [Header("Boundary")]
        [SerializeField, HideInInspector] private bool useBoundaryMask;

        [SerializeField, HideInInspector] private bool[] boundaryMask = Array.Empty<bool>();

        [SerializeField, HideInInspector] private int maskWidth;

        [SerializeField, HideInInspector] private int maskHeight;

        private InventoryItem3D[,] occupancy;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Transform Frame => origin != null ? origin : transform;
        public bool UseBoundaryMask => useBoundaryMask;

        private void Awake()
        {
            if (origin == null)
            {
                origin = transform;
            }
            EnsureBoundaryMask();
            occupancy = new InventoryItem3D[width, height];
        }

        private void OnValidate()
        {
            if (origin == null)
            {
                origin = transform;
            }
            EnsureBoundaryMask();
        }

        private void EnsureInitialized()
        {
            if (occupancy == null || occupancy.GetLength(0) != width || occupancy.GetLength(1) != height)
            {
                occupancy = new InventoryItem3D[width, height];
            }
            EnsureBoundaryMask();
        }

        public bool IsInBounds(Vector2Int cell)
            => cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;

        public bool IsCellValid(Vector2Int cell)
        {
            if (!IsInBounds(cell))
            {
                return false;
            }

            if (useBoundaryMask && !GetMaskCell(cell.x, cell.y))
            {
                return false;
            }

            return true;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            var frame = Frame;
            var localPos = new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
            return frame.TransformPoint(localPos);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            var frame = Frame;
            var local = frame.InverseTransformPoint(world);
            int x = Mathf.RoundToInt(local.x / cellSize);
            int y = Mathf.RoundToInt(local.z / cellSize);
            return new Vector2Int(x, y);
        }

        public bool CanPlace(InventoryItem3D item, Vector2Int originCell)
        {
            EnsureInitialized();
            if (item == null)
            {
                return false;
            }

            foreach (var cell in item.OccupiedCells(originCell))
            {
                if (!IsCellValid(cell))
                {
                    return false;
                }

                if (occupancy[cell.x, cell.y] != null)
                {
                    return false;
                }
            }

            return true;
        }

        public InventoryItem3D GetCellItem(int x, int y)
        {
            EnsureInitialized();
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return null;
            }

            return occupancy[x, y];
        }

        public void Place(InventoryItem3D item, Vector2Int originCell)
        {
            EnsureInitialized();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!CanPlace(item, originCell))
            {
                throw new InvalidOperationException($"Cannot place item at {originCell}.");
            }

            foreach (var cell in item.OccupiedCells(originCell))
            {
                occupancy[cell.x, cell.y] = item;
            }

            item.EnsureVisuals(cellSize);
            var frame = Frame;
            // Parent the item under the grid while preserving world rotation/scale.
            item.transform.SetParent(frame, worldPositionStays: true);
            var localPos = new Vector3(originCell.x * cellSize, item.YOffset, originCell.y * cellSize);
            item.transform.localPosition = localPos;
        }

        public void Remove(InventoryItem3D item)
        {
            EnsureInitialized();
            if (item == null || occupancy == null)
            {
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (occupancy[x, y] == item)
                    {
                        occupancy[x, y] = null;
                    }
                }
            }

            // Detach from grid parent when removed.
            if (item.transform != null && item.transform.parent == Frame)
            {
                item.transform.SetParent(null, worldPositionStays: true);
            }
        }

        public bool TryFindOriginCell(InventoryItem3D item, out Vector2Int originCell)
        {
            EnsureInitialized();
            originCell = default;
            if (item == null || occupancy == null)
            {
                return false;
            }

            // Find any occupied cell and infer origin by subtracting one of the template offsets.
            // Default to assuming (0,0) offset exists.
            Vector2Int? anyCell = null;
            for (int x = 0; x < width && anyCell == null; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (occupancy[x, y] == item)
                    {
                        anyCell = new Vector2Int(x, y);
                        break;
                    }
                }
            }

            if (anyCell == null)
            {
                return false;
            }

            var template = item.Template;
            if (template == null || template.OccupiedCells.Count == 0)
            {
                originCell = anyCell.Value;
                return true;
            }

            // Prefer offset (0,0) if present.
            var offsets = template.OccupiedCells;
            for (int i = 0; i < offsets.Count; i++)
            {
                if (offsets[i] == Vector2Int.zero)
                {
                    originCell = anyCell.Value;
                    return true;
                }
            }

            // Otherwise use first offset.
            originCell = anyCell.Value - offsets[0];
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            var frame = Frame;
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = frame.localToWorldMatrix;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var p = new Vector3(x * cellSize, 0f, y * cellSize);
                    Gizmos.DrawWireCube(p, new Vector3(cellSize, 0.01f, cellSize));
                    if (!IsCellValid(new Vector2Int(x, y)))
                    {
                        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
                        Gizmos.DrawCube(p, new Vector3(cellSize, 0.005f, cellSize));
                        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
                    }
                }
            }
            Gizmos.matrix = prevMatrix;
        }

        public void RebuildOccupancyFromItems()
        {
            EnsureInitialized();
            Array.Clear(occupancy, 0, occupancy.Length);

            var items = GetComponentsInChildren<InventoryItem3D>(includeInactive: true);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    continue;
                }

                var originCell = WorldToCell(item.transform.position);
                foreach (var cell in item.OccupiedCells(originCell))
                {
                    if (IsCellValid(cell))
                    {
                        occupancy[cell.x, cell.y] = item;
                    }
                }
            }
        }

        public void SetBoundaryMaskEnabled(bool enabled)
        {
            useBoundaryMask = enabled;
        }

        public bool GetBoundaryMaskCell(int x, int y)
        {
            EnsureBoundaryMask();
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            return GetMaskCell(x, y);
        }

        public void SetBoundaryMaskCell(int x, int y, bool enabled)
        {
            EnsureBoundaryMask();
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            boundaryMask[y * width + x] = enabled;
        }


        private void EnsureBoundaryMask()
        {
            int size = Mathf.Max(0, width * height);
            if (boundaryMask == null || boundaryMask.Length != size || maskWidth != width || maskHeight != height)
            {
                var oldMask = boundaryMask;
                int oldWidth = maskWidth;
                int oldHeight = maskHeight;
                boundaryMask = new bool[size];

                for (int i = 0; i < boundaryMask.Length; i++)
                {
                    boundaryMask[i] = true;
                }

                if (oldMask != null && oldMask.Length > 0 && oldWidth > 0 && oldHeight > 0)
                {
                    int copyWidth = Mathf.Min(oldWidth, width);
                    int copyHeight = Mathf.Min(oldHeight, height);
                    for (int y = 0; y < copyHeight; y++)
                    {
                        for (int x = 0; x < copyWidth; x++)
                        {
                            int oldIndex = y * oldWidth + x;
                            int newIndex = y * width + x;
                            boundaryMask[newIndex] = oldMask[oldIndex];
                        }
                    }
                }

                maskWidth = width;
                maskHeight = height;
            }
        }

        private bool GetMaskCell(int x, int y)
        {
            int index = y * width + x;
            if (boundaryMask == null || index < 0 || index >= boundaryMask.Length)
            {
                return false;
            }

            return boundaryMask[index];
        }

    }
}
