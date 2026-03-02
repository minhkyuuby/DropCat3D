using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed class InventoryItem3D : MonoBehaviour
    {
        [Tooltip("Cells occupied by this item, relative to its origin cell.")]
        [SerializeField] private List<Vector2Int> occupiedCells = new List<Vector2Int> { Vector2Int.zero };
        public bool autoVisualizeWithBlock = true;

        [Header("Behavior")]
        [Tooltip("If false, the item can sit on the grid without blocking other items.")]
        [SerializeField] private bool blocksGrid = true;

        [Tooltip("If false, the player cannot drag this item during play mode.")]
        [SerializeField] private bool draggableAtRuntime = true;

        [Header("Visuals")]
        [Tooltip("Optional prefab used for each block. If null, Unity cube primitives are used.")]
        [SerializeField] private GameObject blockPrefab;

        [Tooltip("Vertical lift so the item doesn't z-fight with the grid plane.")]
        [SerializeField] private float yOffset = 0.05f;

        public IReadOnlyList<Vector2Int> OccupiedCellOffsets => occupiedCells;
        public bool BlocksGrid => blocksGrid;
        public bool DraggableAtRuntime => draggableAtRuntime;
        public bool HasShape => occupiedCells != null && occupiedCells.Count > 0;

        public void SetOccupiedCells(List<Vector2Int> cells)
        {
            occupiedCells = cells;
            ValidateShape();
        }

        public float YOffset => yOffset;

        public IEnumerable<Vector2Int> OccupiedCells(Vector2Int originCell)
        {
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                yield return originCell;
                yield break;
            }

            for (int i = 0; i < occupiedCells.Count; i++)
            {
                yield return originCell + occupiedCells[i];
            }
        }

        public void EnsureVisuals(float cellSize, bool force = false)
        {
            if (!force && !autoVisualizeWithBlock) return;
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                return;
            }

            // If the item already has children, assume the visuals are authored.
            if (transform.childCount > 0)
            {
                return;
            }

            for (int i = 0; i < occupiedCells.Count; i++)
            {
                var offset = occupiedCells[i];
                var localPos = new Vector3(offset.x * cellSize, 0f, offset.y * cellSize);

                GameObject block;
                if (blockPrefab != null)
                {
                    block = Instantiate(blockPrefab, transform);
                    block.transform.localPosition = localPos;
                    block.transform.localRotation = Quaternion.identity;
                    block.transform.localScale = Vector3.one * (cellSize * 0.95f);
                }
                else
                {
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"Block_{offset.x}_{offset.y}";
                    block.transform.SetParent(transform, worldPositionStays: false);
                    block.transform.localPosition = localPos;
                    block.transform.localRotation = Quaternion.identity;
                    block.transform.localScale = Vector3.one * (cellSize * 0.95f);
                }

                // Make sure picking works.
                if (block.GetComponent<Collider>() == null)
                {
                    block.AddComponent<BoxCollider>();
                }
            }
        }

        private void OnValidate()
        {
            ValidateShape();
        }

        public void ValidateShape()
        {
            if (occupiedCells == null)
            {
                occupiedCells = new List<Vector2Int> { Vector2Int.zero };
                return;
            }

            if (occupiedCells.Count == 0)
            {
                occupiedCells.Add(Vector2Int.zero);
                return;
            }

            var seen = new HashSet<Vector2Int>();
            for (int i = occupiedCells.Count - 1; i >= 0; i--)
            {
                var cell = occupiedCells[i];
                if (seen.Contains(cell))
                {
                    occupiedCells.RemoveAt(i);
                    continue;
                }
                seen.Add(cell);
            }

            if (!seen.Contains(Vector2Int.zero))
            {
                occupiedCells.Add(Vector2Int.zero);
            }
        }

        public BoundsInt CalculateLocalBounds()
        {
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                return new BoundsInt(0, 0, 0, 1, 1, 1);
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int i = 0; i < occupiedCells.Count; i++)
            {
                var c = occupiedCells[i];
                minX = Math.Min(minX, c.x);
                minY = Math.Min(minY, c.y);
                maxX = Math.Max(maxX, c.x);
                maxY = Math.Max(maxY, c.y);
            }

            return new BoundsInt(minX, minY, 0, (maxX - minX) + 1, (maxY - minY) + 1, 1);
        }
    }
}
