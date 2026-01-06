using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    [CreateAssetMenu(menuName = "CatDrop3D/Inventory3D/Block Shape Template", fileName = "BlockShapeTemplate")]
    public sealed class BlockShapeTemplate : ScriptableObject
    {
        [Tooltip("Cells occupied by this item, relative to its origin cell.")]
        [SerializeField] private List<Vector2Int> occupiedCells = new List<Vector2Int> { Vector2Int.zero };

        public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;

        public void Validate()
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
        }

        private void OnValidate() => Validate();

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

            foreach (var c in occupiedCells)
            {
                minX = Math.Min(minX, c.x);
                minY = Math.Min(minY, c.y);
                maxX = Math.Max(maxX, c.x);
                maxY = Math.Max(maxY, c.y);
            }

            // BoundsInt size is inclusive-exclusive.
            return new BoundsInt(minX, minY, 0, (maxX - minX) + 1, (maxY - minY) + 1, 1);
        }
    }
}
