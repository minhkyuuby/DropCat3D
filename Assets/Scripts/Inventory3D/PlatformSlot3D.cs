using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryItem3D))]
    public sealed class PlatformSlot3D : MonoBehaviour
    {
        [SerializeField] private BallType acceptedType = BallType.Green;

        [Min(1)]
        [SerializeField] private int capacity = 3;

        [SerializeField] private InventoryGrid3D gridOverride;

        [SerializeField, Tooltip("If enabled, resolves balls in the same cell when the platform is placed.")]
        private bool resolveBallsOnPlace = true;

        [SerializeField, Tooltip("If enabled, resolves balls whenever the platform moves to a new grid cell during play.")]
        private bool resolveBallsOnCellChange = true;

        [SerializeField, Tooltip("Current number of balls already accepted.")]
        private int currentCount;

        public BallType AcceptedType => acceptedType;
        public int Capacity => capacity;
        public int CurrentCount => currentCount;
        public bool ResolveBallsOnPlace => resolveBallsOnPlace;
        public bool ResolveBallsOnCellChange => resolveBallsOnCellChange;

        private Vector2Int lastCell;
        private bool hasLastCell;

        public bool TryAcceptBall(BallItem3D ball)
        {
            if (ball == null)
            {
                return false;
            }

            if (ball.BallType != acceptedType)
            {
                return false;
            }

            if (currentCount >= capacity)
            {
                return false;
            }

            if (!IsBallAlignedWithPlatform(ball))
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

        public void ResolveBallsInCell()
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
                var ballsInCell = grid.GetBallsInCell(cell);
                if (ballsInCell == null || ballsInCell.Count == 0)
                {
                    continue;
                }

                for (int i = ballsInCell.Count - 1; i >= 0; i--)
                {
                    var ball = ballsInCell[i];
                    if (ball == null)
                    {
                        continue;
                    }

                    if (TryAcceptBall(ball))
                    {
                        grid.UnregisterBall(ball, cell);
                        Destroy(ball.gameObject);
                    }
                }
            }
        }

        private void Update()
        {
            if (!resolveBallsOnCellChange)
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
            ResolveBallsInCell();
        }

        private bool IsBallAlignedWithPlatform(BallItem3D ball)
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

            var ballCell = grid.WorldToCell(ball.transform.position);
            foreach (var cell in item.OccupiedCells(platformCell))
            {
                if (cell == ballCell)
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
