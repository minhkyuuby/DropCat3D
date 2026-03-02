#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatDrop3D.Inventory3D.Editor
{
    [CustomEditor(typeof(InventoryItem3D))]
    public sealed class InventoryItem3DEditor : UnityEditor.Editor
    {
        private const float CellSize = 20f;
        private const float GridPadding = 4f;
        private const int DefaultPreviewRadius = 4;

        private static readonly Color GridBackground = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color GridLine = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color CellFill = new Color(0.2f, 0.6f, 0.9f, 0.85f);
        private static readonly Color OriginFill = new Color(0.95f, 0.6f, 0.2f, 0.95f);

        private int previewRadius = DefaultPreviewRadius;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptReference();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("occupiedCells"), includeChildren: true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoVisualizeWithBlock"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockPrefab"));

            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("draggableAtRuntime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blocksGrid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("yOffset"));

            DrawInstanceButton();
            DrawClearButton();

            EditorGUILayout.Space();
            DrawShapeEditor();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInstanceButton()
        {
            var item = (InventoryItem3D)target;
            if (item == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!item.HasShape))
            {
                if (GUILayout.Button("Instance Block Prefab"))
                {
                    var grid = item.GetComponentInParent<InventoryGrid3D>();
                    float cellSize = grid != null ? grid.CellSize : 1f;

                    Undo.RegisterFullObjectHierarchyUndo(item.gameObject, "Clear Blocks");
                    ClearBlocks(item);

                    Undo.RecordObject(item.transform, "Instance Block Prefab");
                    item.EnsureVisuals(cellSize, force: true);
                    EditorUtility.SetDirty(item);
                }
            }
        }

        private void DrawClearButton()
        {
            var item = (InventoryItem3D)target;
            if (item == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(item.transform.childCount == 0))
            {
                if (GUILayout.Button("Clear Blocks"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(item.gameObject, "Clear Blocks");
                    ClearBlocks(item);
                    EditorUtility.SetDirty(item);
                }
            }
        }

        private static void ClearBlocks(InventoryItem3D item)
        {
            for (int i = item.transform.childCount - 1; i >= 0; i--)
            {
                var child = item.transform.GetChild(i);
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private void DrawScriptReference()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((InventoryItem3D)target), typeof(InventoryItem3D), false);
            }
        }

        private void DrawShapeEditor()
        {
            var item = (InventoryItem3D)target;
            if (item == null)
            {
                return;
            }

            var occupiedProp = serializedObject.FindProperty("occupiedCells");

            EditorGUILayout.HelpBox(
                "Occupied Cells are relative to the origin cell (0,0).\n" +
                "Click cells in the grid below to toggle them.\n" +
                "(0,0) is always included.",
                MessageType.Info);

            previewRadius = Mathf.Clamp(
                EditorGUILayout.IntField("Preview Radius", previewRadius),
                1,
                20);

            DrawCellToggleGrid(occupiedProp, previewRadius);

            EditorGUILayout.LabelField("Shape Preview", EditorStyles.boldLabel);

            var cells = item.OccupiedCellOffsets;
            if (cells == null || cells.Count == 0)
            {
                EditorGUILayout.HelpBox("Shape has no occupied cells.", MessageType.Warning);
                return;
            }

            var bounds = item.CalculateLocalBounds();
            int width = Mathf.Max(1, bounds.size.x);
            int height = Mathf.Max(1, bounds.size.y);

            float drawWidth = width * CellSize + GridPadding * 2f;
            float drawHeight = height * CellSize + GridPadding * 2f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect = GUILayoutUtility.GetRect(drawWidth, drawHeight, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();

            rect.width = drawWidth;
            rect.height = drawHeight;

            EditorGUI.DrawRect(rect, GridBackground);

            var inner = new Rect(rect.x + GridPadding, rect.y + GridPadding, width * CellSize, height * CellSize);

            DrawGridLines(inner, width, height);
            DrawCells(inner, cells, bounds);
        }

        private static void DrawCellToggleGrid(SerializedProperty occupiedCellsProp, int radius)
        {
            var current = ReadCells(occupiedCellsProp);
            current.Add(Vector2Int.zero);

            EditorGUILayout.LabelField("Click To Toggle", EditorStyles.boldLabel);

            for (int y = radius; y >= -radius; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int x = -radius; x <= radius; x++)
                {
                    var cell = new Vector2Int(x, y);
                    bool isOrigin = cell == Vector2Int.zero;
                    bool has = current.Contains(cell);

                    using (new EditorGUI.DisabledScope(isOrigin))
                    {
                        var label = isOrigin ? "O" : (has ? "X" : ".");
                        if (GUILayout.Button(label, GUILayout.Width(22), GUILayout.Height(22)))
                        {
                            if (has)
                            {
                                current.Remove(cell);
                            }
                            else
                            {
                                current.Add(cell);
                            }

                            current.Add(Vector2Int.zero);
                            WriteCells(occupiedCellsProp, current);
                            GUI.changed = true;
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private static HashSet<Vector2Int> ReadCells(SerializedProperty occupiedCellsProp)
        {
            var set = new HashSet<Vector2Int>();
            if (occupiedCellsProp == null || !occupiedCellsProp.isArray)
            {
                return set;
            }

            for (int i = 0; i < occupiedCellsProp.arraySize; i++)
            {
                var element = occupiedCellsProp.GetArrayElementAtIndex(i);
                int x = element.FindPropertyRelative("x").intValue;
                int y = element.FindPropertyRelative("y").intValue;
                set.Add(new Vector2Int(x, y));
            }

            return set;
        }

        private static void WriteCells(SerializedProperty occupiedCellsProp, HashSet<Vector2Int> cells)
        {
            if (occupiedCellsProp == null || !occupiedCellsProp.isArray)
            {
                return;
            }

            var list = new List<Vector2Int>(cells);
            list.Sort((a, b) =>
            {
                if (a == Vector2Int.zero && b != Vector2Int.zero) return -1;
                if (b == Vector2Int.zero && a != Vector2Int.zero) return 1;
                int cy = b.y.CompareTo(a.y);
                return cy != 0 ? cy : a.x.CompareTo(b.x);
            });

            occupiedCellsProp.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                var element = occupiedCellsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("x").intValue = list[i].x;
                element.FindPropertyRelative("y").intValue = list[i].y;
            }
        }

        private static void DrawGridLines(Rect area, int width, int height)
        {
            for (int x = 0; x <= width; x++)
            {
                float xPos = area.x + x * CellSize;
                var lineRect = new Rect(xPos, area.y, 1f, area.height);
                EditorGUI.DrawRect(lineRect, GridLine);
            }

            for (int y = 0; y <= height; y++)
            {
                float yPos = area.y + y * CellSize;
                var lineRect = new Rect(area.x, yPos, area.width, 1f);
                EditorGUI.DrawRect(lineRect, GridLine);
            }
        }

        private static void DrawCells(Rect area, IReadOnlyList<Vector2Int> cells, BoundsInt bounds)
        {
            if (cells == null)
            {
                return;
            }

            int yMax = bounds.yMax - 1;

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                int xIndex = cell.x - bounds.xMin;
                int yIndex = yMax - (cell.y - bounds.yMin);

                var cellRect = new Rect(
                    area.x + xIndex * CellSize + 1f,
                    area.y + yIndex * CellSize + 1f,
                    CellSize - 1f,
                    CellSize - 1f);

                var fill = cell == Vector2Int.zero ? OriginFill : CellFill;
                EditorGUI.DrawRect(cellRect, fill);
            }
        }
    }
}
#endif
