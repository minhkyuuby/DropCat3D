using System;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed partial class InventoryGrid3D
    {
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
    }
}
