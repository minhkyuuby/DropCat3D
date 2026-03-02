using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed partial class InventoryGrid3D : MonoBehaviour
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

        private void Start()
        {
            RunGridSetup();
        }

        private void OnValidate()
        {
            if (origin == null)
            {
                origin = transform;
            }
            EnsureBoundaryMask();
        }

        public void ValidateItemsOnStart()
        {
            var items = GetComponentsInChildren<InventoryItem3D>(includeInactive: true);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    continue;
                }

                var originCell = WorldToCell(item.transform.position);
                bool isValid = true;
                foreach (var cell in item.OccupiedCells(originCell))
                {
                    if (!IsCellValid(cell))
                    {
                        isValid = false;
                        break;
                    }
                }

                if (!isValid)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(item.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(item.gameObject);
                    }
                }
            }
        }

        public void RunGridSetup()
        {
            EnsureInitialized();
            ValidateItemsOnStart();
            RebuildOccupancyFromItems();
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
            if (Application.isPlaying && slot != null && slot.ResolveBallsOnPlace)
            {
                slot.ResolveBallsInCell();
            }
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

            // Find any occupied cell and infer origin by subtracting one of the shape offsets.
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

            var offsets = item.OccupiedCellOffsets;
            if (offsets == null || offsets.Count == 0)
            {
                originCell = anyCell.Value;
                return true;
            }

            // Prefer offset (0,0) if present.
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

    }
}
