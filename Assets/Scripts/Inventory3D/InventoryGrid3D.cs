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

        private InventoryItem3D[,] occupancy;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            if (origin == null)
            {
                origin = transform;
            }
            occupancy = new InventoryItem3D[width, height];
        }

        public bool IsInBounds(Vector2Int cell)
            => cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;

        public Vector3 CellToWorld(Vector2Int cell)
        {
            var basePos = origin != null ? origin.position : transform.position;
            return basePos + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            var basePos = origin != null ? origin.position : transform.position;
            var local = world - basePos;

            int x = Mathf.RoundToInt(local.x / cellSize);
            int y = Mathf.RoundToInt(local.z / cellSize);

            return new Vector2Int(x, y);
        }

        public bool CanPlace(InventoryItem3D item, Vector2Int originCell)
        {
            if (item == null)
            {
                return false;
            }

            foreach (var cell in item.OccupiedCells(originCell))
            {
                if (!IsInBounds(cell))
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

        public void Place(InventoryItem3D item, Vector2Int originCell)
        {
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
            var pos = CellToWorld(originCell);
            item.transform.position = new Vector3(pos.x, pos.y + item.YOffset, pos.z);
        }

        public void Remove(InventoryItem3D item)
        {
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
        }

        public bool TryFindOriginCell(InventoryItem3D item, out Vector2Int originCell)
        {
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
            if (origin == null)
            {
                return;
            }

            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            var basePos = origin.position;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var p = basePos + new Vector3(x * cellSize, 0f, y * cellSize);
                    Gizmos.DrawWireCube(p, new Vector3(cellSize, 0.01f, cellSize));
                }
            }
        }
    }
}
