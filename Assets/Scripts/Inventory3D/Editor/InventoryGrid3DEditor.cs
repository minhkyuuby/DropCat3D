#if UNITY_EDITOR
using CatDrop3D.Inventory3D;
using UnityEditor;
using UnityEngine;

namespace CatDrop3D.Inventory3D.Editor
{
    [CustomEditor(typeof(InventoryGrid3D))]
    public sealed class InventoryGrid3DEditor : UnityEditor.Editor
    {
        private const float CellWidth = 32f;

        private static bool enableSceneDrag;

        private InventoryItem3D draggingItem;
        private Vector2Int dragStartCell;
        private Vector2Int lastValidCell;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space();
            enableSceneDrag = EditorGUILayout.ToggleLeft("Enable Scene Drag (Edit Mode)", enableSceneDrag);

            var grid = (InventoryGrid3D)target;
            if (grid == null)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Boundary", EditorStyles.boldLabel);

            bool useMask = EditorGUILayout.ToggleLeft("Use Boundary Mask", grid.UseBoundaryMask);
            if (useMask != grid.UseBoundaryMask)
            {
                Undo.RecordObject(grid, "Toggle Boundary Mask");
                grid.SetBoundaryMaskEnabled(useMask);
                EditorUtility.SetDirty(grid);
            }

            if (grid.UseBoundaryMask)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Mask Editor", EditorStyles.miniBoldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Fill"))
                {
                    SetAllMaskCells(grid, true);
                }
                if (GUILayout.Button("Clear"))
                {
                    SetAllMaskCells(grid, false);
                }
                if (GUILayout.Button("Invert"))
                {
                    InvertMaskCells(grid);
                }
                EditorGUILayout.EndHorizontal();

                DrawMaskGrid(grid);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Grid Status", EditorStyles.boldLabel);

            DrawGrid(grid);
            serializedObject.ApplyModifiedProperties();
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

        private static void DrawGrid(InventoryGrid3D grid)
        {
            int width = grid.Width;
            int height = grid.Height;

            if (width <= 0 || height <= 0)
            {
                EditorGUILayout.HelpBox("Grid size is invalid.", MessageType.Info);
                return;
            }

            for (int y = height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!grid.IsCellValid(cell))
                    {
                        var content = new GUIContent("#", "Blocked by boundary");
                        GUILayout.Label(content, GUILayout.Width(CellWidth));
                        continue;
                    }

                    var item = grid.GetCellItem(x, y);
                    var ct = new GUIContent(item == null ? "." : "X", item == null ? "Empty" : item.name);
                    GUILayout.Label(ct, GUILayout.Width(CellWidth));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawMaskGrid(InventoryGrid3D grid)
        {
            int width = grid.Width;
            int height = grid.Height;

            if (width <= 0 || height <= 0)
            {
                EditorGUILayout.HelpBox("Grid size is invalid.", MessageType.Info);
                return;
            }

            var enabledColor = new Color(0.4f, 0.8f, 0.4f, 1f);
            var disabledColor = new Color(0.8f, 0.3f, 0.3f, 1f);

            for (int y = height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < width; x++)
                {
                    bool enabled = grid.GetBoundaryMaskCell(x, y);
                    var prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = enabled ? enabledColor : disabledColor;
                    if (GUILayout.Button(enabled ? " " : " ", GUILayout.Width(CellWidth), GUILayout.Height(CellWidth)))
                    {
                        Undo.RecordObject(grid, "Toggle Boundary Cell");
                        grid.SetBoundaryMaskCell(x, y, !enabled);
                        EditorUtility.SetDirty(grid);
                    }
                    GUI.backgroundColor = prevColor;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void SetAllMaskCells(InventoryGrid3D grid, bool enabled)
        {
            Undo.RecordObject(grid, "Edit Boundary Mask");
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    grid.SetBoundaryMaskCell(x, y, enabled);
                }
            }
            EditorUtility.SetDirty(grid);
        }

        private static void InvertMaskCells(InventoryGrid3D grid)
        {
            Undo.RecordObject(grid, "Invert Boundary Mask");
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    bool enabled = grid.GetBoundaryMaskCell(x, y);
                    grid.SetBoundaryMaskCell(x, y, !enabled);
                }
            }
            EditorUtility.SetDirty(grid);
        }

        private static void MoveItemToCell(InventoryGrid3D grid, InventoryItem3D item, Vector2Int cell)
        {
            var localPos = grid.CellToLocal(cell, item.YOffset);
            var worldPos = grid.Frame.TransformPoint(localPos);
            item.transform.position = worldPos;
        }
    }
}
#endif
