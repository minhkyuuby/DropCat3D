using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed class BallItem3D : MonoBehaviour
    {
        [SerializeField] private BallType ballType = BallType.Green;

        [Tooltip("If enabled, tries to resolve a platform under the ball when it becomes active.")]
        [SerializeField] private bool autoResolveOnEnable = true;

        [Header("Resolve Animation")]
        [Min(0f)]
        [SerializeField] private float resolveLiftHeight = 0.35f;

        [Min(0f)]
        [SerializeField] private float resolveLiftDuration = 0.2f;

        private bool consumed;
        private InventoryGrid3D registeredGrid;
        private Vector2Int registeredCell;
        private bool isRegistered;
        private Coroutine resolveRoutine;

        public BallType BallType => ballType;

        void Awake()
        {
            consumed = false;
        }
        
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

            isRegistered = registeredGrid.RegisterBall(this, registeredCell);
        }

        private void UnregisterFromGrid()
        {
            if (!isRegistered || registeredGrid == null)
            {
                return;
            }

            registeredGrid.UnregisterBall(this, registeredCell);
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
            if (slot != null && slot.TryAcceptBall(this))
            {
                Consume();
            }
        }

        public void Consume()
        {
            if (consumed)
            {
                return;
            }

            consumed = true;
            UnregisterFromGrid();

            if (!isActiveAndEnabled || resolveLiftDuration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (resolveRoutine != null)
            {
                StopCoroutine(resolveRoutine);
            }

            resolveRoutine = StartCoroutine(ResolveLiftRoutine());
        }

        private System.Collections.IEnumerator ResolveLiftRoutine()
        {
            var start = transform.position;
            var up = registeredGrid != null ? registeredGrid.Frame.up : Vector3.up;
            var end = start + up * resolveLiftHeight;
            float duration = Mathf.Max(0.01f, resolveLiftDuration);
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
    }
}
