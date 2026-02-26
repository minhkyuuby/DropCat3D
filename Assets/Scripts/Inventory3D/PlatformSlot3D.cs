using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryItem3D))]
    public sealed class PlatformSlot3D : MonoBehaviour
    {
        [SerializeField] private FrogType acceptedType = FrogType.Green;

        [Min(1)]
        [SerializeField] private int capacity = 3;

        [SerializeField] private InventoryGrid3D gridOverride;

        [SerializeField, Tooltip("If enabled, resolves frogs in the same cell when the platform is placed.")]
        private bool resolveFrogsOnPlace = true;

        [SerializeField, Tooltip("If enabled, resolves frogs whenever the platform moves to a new grid cell during play.")]
        private bool resolveFrogsOnCellChange = true;

        [SerializeField, Tooltip("Current number of frogs already accepted.")]
        private int currentCount;

        public FrogType AcceptedType => acceptedType;
        public int Capacity => capacity;
        public int CurrentCount => currentCount;
        public bool ResolveFrogsOnPlace => resolveFrogsOnPlace;
        public bool ResolveFrogsOnCellChange => resolveFrogsOnCellChange;

        private Vector2Int lastCell;
        private bool hasLastCell;

        public bool TryAcceptFrog(FrogItem3D frog)
        {
            if (frog == null)
            {
                return false;
            }

            if (frog.FrogType != acceptedType)
            {
                return false;
            }

            if (currentCount >= capacity)
            {
                return false;
            }

            if (!IsFrogAlignedWithPlatform(frog))
            {
                return false;
            }

            currentCount++;

            if (currentCount >= capacity)
            {
                RemovePlatform();
            }

            return true;
        }

        public void ResolveFrogsInCell()
        {
            var grid = ResolveGrid();
            if (grid == null)
            {
                return;
            }

            var item = GetComponent<InventoryItem3D>();
            if (item == null)
            {
                return;
            }

            if (!grid.TryFindOriginCell(item, out var platformCell))
            {
                platformCell = grid.WorldToCell(item.transform.position);
            }

            var offsets = item.OccupiedCells(platformCell);
            foreach (var cell in offsets)
            {
                var frogsInCell = grid.GetFrogsInCell(cell);
                if (frogsInCell == null || frogsInCell.Count == 0)
                {
                    continue;
                }

                for (int i = frogsInCell.Count - 1; i >= 0; i--)
                {
                    var frog = frogsInCell[i];
                    if (frog == null)
                    {
                        continue;
                    }

                    if (TryAcceptFrog(frog))
                    {
                        grid.UnregisterFrog(frog, cell);
                        Destroy(frog.gameObject);
                    }
                }
            }
        }

        private void Update()
        {
            if (!resolveFrogsOnCellChange)
            {
                return;
            }

            var grid = ResolveGrid();
            if (grid == null)
            {
                return;
            }

            var currentCell = grid.WorldToCell(transform.position);
            if (hasLastCell && currentCell == lastCell)
            {
                return;
            }

            hasLastCell = true;
            lastCell = currentCell;
            ResolveFrogsInCell();
        }

        private bool IsFrogAlignedWithPlatform(FrogItem3D frog)
        {
            var grid = ResolveGrid();
            if (grid == null)
            {
                return true;
            }

            var item = GetComponent<InventoryItem3D>();
            if (item == null)
            {
                return true;
            }

            if (!grid.TryFindOriginCell(item, out var platformCell))
            {
                platformCell = grid.WorldToCell(item.transform.position);
            }

            var frogCell = grid.WorldToCell(frog.transform.position);
            foreach (var cell in item.OccupiedCells(platformCell))
            {
                if (cell == frogCell)
                {
                    return true;
                }
            }

            return false;
        }

        private InventoryGrid3D ResolveGrid()
        {
            if (gridOverride != null)
            {
                return gridOverride;
            }

            return GetComponentInParent<InventoryGrid3D>();
        }

        private void RemovePlatform()
        {
            var grid = ResolveGrid();
            var item = GetComponent<InventoryItem3D>();
            if (grid != null && item != null)
            {
                grid.Remove(item);
            }

            Destroy(gameObject);
        }
    }
}
