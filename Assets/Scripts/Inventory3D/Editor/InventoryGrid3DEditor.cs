#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CatDrop3D.Inventory3D.Editor
{
    [CustomEditor(typeof(InventoryGrid3D))]
    public sealed class InventoryGrid3DEditor : UnityEditor.Editor
    {
        private static bool enableSceneDrag;

        private InventoryItem3D draggingItem;
        private Vector2Int dragStartCell;
        private Vector2Int lastValidCell;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            enableSceneDrag = EditorGUILayout.ToggleLeft("Enable Scene Drag (Edit Mode)", enableSceneDrag);
        }

        private void OnSceneGUI()
        {
            if (Application.isPlaying || !enableSceneDrag)
            {
                return;
            }

            var grid = (InventoryGrid3D)target;
            if (grid == null)
            {
                return;
            }

            var e = Event.current;
            if (e == null)
            {
                return;
            }

            if (e.alt)
            {
                return;
            }

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var frame = grid.Frame;
            var plane = new Plane(frame.up, frame.position);

            if (!plane.Raycast(ray, out float enter))
            {
                return;
            }

            var world = ray.GetPoint(enter);
            var cell = grid.WorldToCell(world);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var picked = HandleUtility.PickGameObject(e.mousePosition, false);
                var item = picked != null ? picked.GetComponentInParent<InventoryItem3D>() : null;
                if (item != null)
                {
                    draggingItem = item;
                    grid.RebuildOccupancyFromItems();
                    grid.Remove(draggingItem);

                    dragStartCell = grid.WorldToCell(item.transform.position);
                    lastValidCell = dragStartCell;

                    Undo.RecordObject(draggingItem.transform, "Move Inventory Item");
                    e.Use();
                }
            }
            else if (draggingItem != null && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
            {
                if (grid.CanPlace(draggingItem, cell))
                {
                    lastValidCell = cell;
                }

                MoveItemToCell(grid, draggingItem, lastValidCell);
                SceneView.RepaintAll();
                e.Use();
            }
            else if (draggingItem != null && e.type == EventType.MouseUp && e.button == 0)
            {
                if (grid.CanPlace(draggingItem, lastValidCell))
                {
                    grid.Place(draggingItem, lastValidCell);
                }
                else if (grid.CanPlace(draggingItem, dragStartCell))
                {
                    grid.Place(draggingItem, dragStartCell);
                }

                draggingItem = null;
                e.Use();
            }
        }

        private static void MoveItemToCell(InventoryGrid3D grid, InventoryItem3D item, Vector2Int cell)
        {
            var localPos = new Vector3(cell.x * grid.CellSize, item.YOffset, cell.y * grid.CellSize);
            var worldPos = grid.Frame.TransformPoint(localPos);
            item.transform.position = worldPos;
        }
    }
}
#endif
