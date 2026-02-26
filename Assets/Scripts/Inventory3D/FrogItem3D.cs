using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed class FrogItem3D : MonoBehaviour
    {
        [SerializeField] private FrogType frogType = FrogType.Green;

        [Tooltip("If enabled, tries to resolve a platform under the frog when it becomes active.")]
        [SerializeField] private bool autoResolveOnEnable = true;

        private bool consumed;
        private InventoryGrid3D registeredGrid;
        private Vector2Int registeredCell;
        private bool isRegistered;

        public FrogType FrogType => frogType;

        private void OnEnable()
        {
            TryRegisterWithGrid();
            if (autoResolveOnEnable)
            {
                TryResolveFromPosition();
            }
        }

        private void OnDisable()
        {
            UnregisterFromGrid();
        }

        private void TryRegisterWithGrid()
        {
            if (consumed)
            {
                return;
            }

            registeredGrid = FindFirstObjectByType<InventoryGrid3D>();
            if (registeredGrid == null)
            {
                return;
            }

            registeredCell = registeredGrid.WorldToCell(transform.position);
            if (!registeredGrid.IsCellValid(registeredCell))
            {
                Destroy(gameObject);
                return;
            }

            isRegistered = registeredGrid.RegisterFrog(this, registeredCell);
        }

        private void UnregisterFromGrid()
        {
            if (!isRegistered || registeredGrid == null)
            {
                return;
            }

            registeredGrid.UnregisterFrog(this, registeredCell);
            isRegistered = false;
        }

        public void TryResolveFromPosition()
        {
            if (consumed)
            {
                return;
            }

            var grid = registeredGrid != null ? registeredGrid : FindFirstObjectByType<InventoryGrid3D>();
            if (grid == null)
            {
                return;
            }

            var cell = isRegistered ? registeredCell : grid.WorldToCell(transform.position);
            if (!grid.IsCellValid(cell))
            {
                Destroy(gameObject);
                return;
            }

            var item = grid.GetCellItem(cell.x, cell.y);
            if (item == null)
            {
                return;
            }

            var slot = item.GetComponent<PlatformSlot3D>();
            if (slot != null && slot.TryAcceptFrog(this))
            {
                consumed = true;
                if (grid == registeredGrid)
                {
                    UnregisterFromGrid();
                }
                Destroy(gameObject);
            }
        }
    }
}
