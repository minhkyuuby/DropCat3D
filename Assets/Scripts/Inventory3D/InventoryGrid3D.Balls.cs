using System.Collections.Generic;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed partial class InventoryGrid3D
    {
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

        public bool HasAnyBalls()
        {
            EnsureInitialized();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var list = ballOccupancy[x, y];
                    if (list != null && list.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void RebuildBallOccupancyFromScene(bool includeInactive = true)
        {
            EnsureInitialized();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var list = ballOccupancy[x, y];
                    if (list != null)
                    {
                        list.Clear();
                    }
                }
            }

            var balls = FindObjectsOfType<BallItem3D>(includeInactive);
            for (int i = 0; i < balls.Length; i++)
            {
                var ball = balls[i];
                if (ball == null)
                {
                    continue;
                }

                var cell = WorldToCell(ball.transform.position);
                if (!IsCellValid(cell))
                {
                    continue;
                }

                RegisterBall(ball, cell);
            }
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
    }
}
