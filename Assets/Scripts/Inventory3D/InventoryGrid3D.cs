using System;
using System.Collections.Generic;
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

        [Tooltip("World-space origin at grid center.")]
        [SerializeField] private Transform origin;

        [Header("Boundary")]
        [SerializeField, HideInInspector] private bool useBoundaryMask;

        [SerializeField, HideInInspector] private bool[] boundaryMask = Array.Empty<bool>();

        [SerializeField, HideInInspector] private int maskWidth;

        [SerializeField, HideInInspector] private int maskHeight;

        private InventoryItem3D[,] occupancy;
        private List<BallItem3D>[,] ballOccupancy;
        private Dictionary<InventoryItem3D, Vector2Int> itemOrigins;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Transform Frame => origin != null ? origin : transform;
        public bool UseBoundaryMask => useBoundaryMask;
        public Vector3 GridCenterOffsetLocal
            => new Vector3((width - 1) * cellSize * 0.5f, 0f, (height - 1) * cellSize * 0.5f);

        public Vector3 CellToLocal(Vector2Int cell, float y = 0f)
        {
            return new Vector3(cell.x * cellSize, y, cell.y * cellSize) - GridCenterOffsetLocal;
        }

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
            if (ballOccupancy == null || ballOccupancy.GetLength(0) != width || ballOccupancy.GetLength(1) != height)
            {
                ballOccupancy = new List<BallItem3D>[width, height];
            }
            if (itemOrigins == null)
            {
                itemOrigins = new Dictionary<InventoryItem3D, Vector2Int>();
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
            var localPos = CellToLocal(cell, 0f);
            return frame.TransformPoint(localPos);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            var frame = Frame;
            var local = frame.InverseTransformPoint(world) + GridCenterOffsetLocal;
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

                if (item.BlocksGrid && occupancy[cell.x, cell.y] != null)
                {
                    return false;
                }

                if (IsBlockedByDifferentBallType(item, cell))
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

            if (item.BlocksGrid)
            {
                foreach (var cell in item.OccupiedCells(originCell))
                {
                    occupancy[cell.x, cell.y] = item;
                }
            }

            itemOrigins[item] = originCell;

            item.EnsureVisuals(cellSize);
            var frame = Frame;
            // Parent the item under the grid while preserving world rotation/scale.
            item.transform.SetParent(frame, worldPositionStays: true);
            var localPos = CellToLocal(originCell, item.YOffset);
            item.transform.localPosition = localPos;

            var slot = item.GetComponent<PlatformSlot3D>();
            if (slot != null && slot.ResolveBallsOnPlace)
            {
                slot.ResolveBallsInCell();
            }
        }

        public bool RegisterBall(BallItem3D ball, Vector2Int cell)
        {
            EnsureInitialized();
            if (ball == null)
            {
                return false;
            }

            if (!IsCellValid(cell))
            {
                return false;
            }

            var list = ballOccupancy[cell.x, cell.y];
            if (list == null)
            {
                list = new List<BallItem3D>();
                ballOccupancy[cell.x, cell.y] = list;
            }

            if (!list.Contains(ball))
            {
                list.Add(ball);
            }

            return true;
        }

        public void UnregisterBall(BallItem3D ball, Vector2Int cell)
        {
            EnsureInitialized();
            if (ball == null)
            {
                return;
            }

            if (!IsInBounds(cell))
            {
                return;
            }

            var list = ballOccupancy[cell.x, cell.y];
            if (list == null)
            {
                return;
            }

            list.Remove(ball);
        }

        public IReadOnlyList<BallItem3D> GetBallsInCell(Vector2Int cell)
        {
            EnsureInitialized();
            if (!IsInBounds(cell))
            {
                return null;
            }

            return ballOccupancy[cell.x, cell.y];
        }

        public void Remove(InventoryItem3D item)
        {
            EnsureInitialized();
            if (item == null || occupancy == null)
            {
                return;
            }

            itemOrigins?.Remove(item);

            if (!item.BlocksGrid)
            {
                if (item.transform != null && item.transform.parent == Frame)
                {
                    item.transform.SetParent(null, worldPositionStays: true);
                }
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

            if (itemOrigins != null && itemOrigins.TryGetValue(item, out originCell))
            {
                return true;
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
            var offset = GridCenterOffsetLocal;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var p = new Vector3(x * cellSize, 0f, y * cellSize) - offset;
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
            itemOrigins?.Clear();

            var items = GetComponentsInChildren<InventoryItem3D>(includeInactive: true);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    continue;
                }

                if (!item.BlocksGrid)
                {
                    continue;
                }

                var originCell = WorldToCell(item.transform.position);
                itemOrigins[item] = originCell;
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


        private bool IsBlockedByDifferentBallType(InventoryItem3D item, Vector2Int cell)
        {
            var slot = item != null ? item.GetComponent<PlatformSlot3D>() : null;
            if (slot == null)
            {
                return false;
            }

            var balls = GetBallsInCell(cell);
            if (balls == null || balls.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < balls.Count; i++)
            {
                var ball = balls[i];
                if (ball == null)
                {
                    continue;
                }

                if (ball.BallType != slot.AcceptedType)
                {
                    return true;
                }
            }

            return false;
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
