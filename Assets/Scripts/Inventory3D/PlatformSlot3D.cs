using System;
using System.Collections;
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

        [Header("Remove Animation")]
        [Min(0f)]
        [SerializeField] private float pressDownDistance = 0.25f;

        [Min(0f)]
        [SerializeField] private float pressDownDuration = 0.15f;

        public BallType AcceptedType => acceptedType;
        public int Capacity => capacity;
        public int CurrentCount => currentCount;
        public int CapacityLeft => Mathf.Max(0, capacity - currentCount);
        public bool ResolveBallsOnPlace => resolveBallsOnPlace;
        public bool ResolveBallsOnCellChange => resolveBallsOnCellChange;

        public event Action<int> CapacityLeftChanged;

        private Vector2Int lastCell;
        private bool hasLastCell;
        private bool isRemoving;
        private Coroutine removeRoutine;

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
            NotifyCapacityLeftChanged();

            if (currentCount >= capacity)
            {
                RemovePlatform();
            }

            return true;
        }

        private void OnEnable()
        {
            NotifyCapacityLeftChanged();
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
                        ball.Consume();
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
            if (isRemoving)
            {
                return;
            }

            isRemoving = true;

            var grid = ResolveGrid();
            var item = GetComponent<InventoryItem3D>();
            if (grid != null && item != null)
            {
                grid.Remove(item);
            }

            if (!isActiveAndEnabled || pressDownDuration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (removeRoutine != null)
            {
                StopCoroutine(removeRoutine);
            }

            removeRoutine = StartCoroutine(PressDownAndDestroy(grid, item));
        }

        private IEnumerator PressDownAndDestroy(InventoryGrid3D grid, InventoryItem3D item)
        {
            var start = transform.position;
            if (grid != null && item != null)
            {
                if (!grid.TryFindOriginCell(item, out var originCell))
                {
                    originCell = grid.WorldToCell(transform.position);
                }

                var localPos = grid.CellToLocal(originCell, item.YOffset);
                transform.position = grid.Frame.TransformPoint(localPos);
                start = transform.position;
            }

            var down = grid != null ? -grid.Frame.up : -transform.up;
            var end = start + down * pressDownDistance;
            float duration = Mathf.Max(0.01f, pressDownDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void NotifyCapacityLeftChanged()
        {
            CapacityLeftChanged?.Invoke(CapacityLeft);
        }
    }
}
