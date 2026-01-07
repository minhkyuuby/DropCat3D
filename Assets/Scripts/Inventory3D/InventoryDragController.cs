using UnityEngine;
using UnityEngine.InputSystem;

namespace CatDrop3D.Inventory3D
{
    public sealed class InventoryDragController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private InventoryGrid3D grid;

        [Header("Input (New Input System)")]
        [Tooltip("Optional. If not set, the controller creates runtime actions for <Pointer>/position.")]
        [SerializeField] private InputActionReference pointerPosition;

        [Tooltip("Optional. If not set, the controller creates runtime actions for <Pointer>/press.")]
        [SerializeField] private InputActionReference pointerPress;

        [Header("Picking")]
        [SerializeField] private LayerMask itemLayerMask = ~0;

        [Header("Snap")]
        [Tooltip("Snaps the dragged origin cell to multiples of this value (in cells). 1 = normal grid snapping.")]
        [Min(1)]
        [SerializeField] private int snapCells = 1;

        [Header("Drag")]
        [Tooltip("How high the item lifts while dragging.")]
        [SerializeField] private float dragLift = 0.25f;

        [Header("Smoothing")]
        [Tooltip("Smoothly moves the item towards the snapped cell position.")]
        [SerializeField] private bool smoothSnapping = true;

        [Tooltip("Lower = snappier, higher = smoother. Typical range: 0.03 - 0.15")]
        [Min(0.001f)]
        [SerializeField] private float smoothTime = 0.06f;

        [Tooltip("Optional cap for SmoothDamp speed. 0 = unlimited.")]
        [Min(0f)]
        [SerializeField] private float maxSmoothSpeed = 0f;

        private InventoryItem3D draggingItem;
        private Vector2Int dragStartCell;
        private Vector2Int lastValidCell;
        // No longer store a fixed world-up plane; the grid can rotate.

        private Vector3 smoothVelocity;

        private InputAction runtimePointerPosition;
        private InputAction runtimePointerPress;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (grid == null)
            {
                grid = FindFirstObjectByType<InventoryGrid3D>();
            }

            // Plane is computed per frame from the grid's oriented transform.
        }

        private void OnEnable()
        {
            EnsureInputActions();

            var press = GetPointerPressAction();
            press.started += OnPointerPressStarted;
            press.canceled += OnPointerPressCanceled;

            GetPointerPositionAction().Enable();
            press.Enable();
        }

        private void OnDisable()
        {
            var press = GetPointerPressAction();
            press.started -= OnPointerPressStarted;
            press.canceled -= OnPointerPressCanceled;

            if (pointerPosition == null)
            {
                runtimePointerPosition?.Disable();
            }
            if (pointerPress == null)
            {
                runtimePointerPress?.Disable();
            }
        }

        private void Update()
        {
            if (targetCamera == null || grid == null)
            {
                return;
            }

            if (draggingItem != null)
            {
                UpdateDrag();
            }
        }

        private void OnPointerPressStarted(InputAction.CallbackContext _)
        {
            if (targetCamera == null || grid == null)
            {
                return;
            }

            if (draggingItem == null)
            {
                TryBeginDrag();
            }
        }

        private void OnPointerPressCanceled(InputAction.CallbackContext _)
        {
            if (draggingItem != null)
            {
                EndDrag();
            }
        }

        private void TryBeginDrag()
        {
            var screenPos = GetPointerScreenPosition();
            var ray = targetCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 500f, itemLayerMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            var item = hit.collider.GetComponentInParent<InventoryItem3D>();
            if (item == null)
            {
                return;
            }

            draggingItem = item;

            if (!grid.TryFindOriginCell(item, out dragStartCell))
            {
                // If the item wasn't placed yet, infer from current position.
                dragStartCell = grid.WorldToCell(item.transform.position);
            }

            lastValidCell = dragStartCell;

            smoothVelocity = Vector3.zero;

            // Free up occupancy while dragging.
            grid.Remove(draggingItem);
        }

        private void UpdateDrag()
        {
            var screenPos = GetPointerScreenPosition();
            var ray = targetCamera.ScreenPointToRay(screenPos);
            var frame = grid.Frame;
            var plane = new Plane(frame.up, frame.position);
            if (!plane.Raycast(ray, out float enter))
            {
                return;
            }

            var world = ray.GetPoint(enter);
            var rawCell = grid.WorldToCell(world);

            var snappedCell = new Vector2Int(
                GridDragMath.RoundToMultiple(rawCell.x, snapCells),
                GridDragMath.RoundToMultiple(rawCell.y, snapCells)
            );

            var resolvedCell = ResolveBlockingMovement(lastValidCell, snappedCell);
            lastValidCell = resolvedCell;

            // Lift along the grid's local up by transforming a local position.
            var localPos = new Vector3(
                resolvedCell.x * grid.CellSize,
                draggingItem.YOffset + dragLift,
                resolvedCell.y * grid.CellSize);
            var target = frame.TransformPoint(localPos);

            if (!smoothSnapping)
            {
                draggingItem.transform.position = target;
                return;
            }

            // SmoothDamp avoids visible popping while still keeping the snapped target exact.
            if (maxSmoothSpeed > 0f)
            {
                draggingItem.transform.position = Vector3.SmoothDamp(
                    draggingItem.transform.position,
                    target,
                    ref smoothVelocity,
                    smoothTime,
                    maxSmoothSpeed);
            }
            else
            {
                draggingItem.transform.position = Vector3.SmoothDamp(
                    draggingItem.transform.position,
                    target,
                    ref smoothVelocity,
                    smoothTime);
            }
        }

        private Vector2Int ResolveBlockingMovement(Vector2Int fromCell, Vector2Int toCell)
        {
            if (fromCell == toCell)
            {
                // Staying put.
                return fromCell;
            }

            Vector2Int lastPlaceable = fromCell;

            foreach (var step in GridDragMath.StepLine(fromCell, toCell))
            {
                if (!grid.CanPlace(draggingItem, step))
                {
                    // Blocked: do not allow passing through.
                    return lastPlaceable;
                }

                lastPlaceable = step;
            }

            return lastPlaceable;
        }

        private void EndDrag()
        {
            // Try place at lastValidCell; if blocked, revert.
            if (grid.CanPlace(draggingItem, lastValidCell))
            {
                grid.Place(draggingItem, lastValidCell);
            }
            else
            {
                // Restore to start.
                if (!grid.CanPlace(draggingItem, dragStartCell))
                {
                    // If start cell became blocked somehow, keep item where it is,
                    // but align height along grid's local up.
                    var frame2 = grid.Frame;
                    var localP = new Vector3(lastValidCell.x * grid.CellSize, draggingItem.YOffset, lastValidCell.y * grid.CellSize);
                    var worldP = frame2.TransformPoint(localP);
                    draggingItem.transform.position = worldP;
                    draggingItem = null;
                    return;
                }

                grid.Place(draggingItem, dragStartCell);
            }

            draggingItem = null;
        }

        private void EnsureInputActions()
        {
            if (pointerPosition == null && runtimePointerPosition == null)
            {
                runtimePointerPosition = new InputAction(
                    name: "Inventory_PointerPosition",
                    type: InputActionType.Value,
                    binding: "<Pointer>/position");
            }

            if (pointerPress == null && runtimePointerPress == null)
            {
                runtimePointerPress = new InputAction(
                    name: "Inventory_PointerPress",
                    type: InputActionType.Button,
                    binding: "<Pointer>/press");
            }
        }

        private InputAction GetPointerPositionAction()
        {
            return pointerPosition != null ? pointerPosition.action : runtimePointerPosition;
        }

        private InputAction GetPointerPressAction()
        {
            return pointerPress != null ? pointerPress.action : runtimePointerPress;
        }

        private Vector2 GetPointerScreenPosition()
        {
            var action = GetPointerPositionAction();
            if (action == null)
            {
                // As a last resort, fall back to the current pointer device.
                return Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            }

            return action.ReadValue<Vector2>();
        }
    }
}
