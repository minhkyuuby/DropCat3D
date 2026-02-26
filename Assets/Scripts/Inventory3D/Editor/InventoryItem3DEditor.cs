#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CatDrop3D.Inventory3D.Editor
{
    [CustomEditor(typeof(InventoryItem3D))]
    public sealed class InventoryItem3DEditor : UnityEditor.Editor
    {
        private const float CellSize = 20f;
        private const float GridPadding = 4f;

        private static readonly Color GridBackground = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color GridLine = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color CellFill = new Color(0.2f, 0.6f, 0.9f, 0.85f);
        private static readonly Color OriginFill = new Color(0.95f, 0.6f, 0.2f, 0.95f);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptReference();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("template"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoVisualizeWithBlock"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockPrefab"));

            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("draggableAtRuntime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blocksGrid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("yOffset"));

            DrawInstanceButton();
            DrawClearButton();

            EditorGUILayout.Space();
            DrawTemplatePreview();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInstanceButton()
        {
            var item = (InventoryItem3D)target;
            if (item == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(item.Template == null))
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

        private void DrawTemplatePreview()
        {
            var templateProp = serializedObject.FindProperty("template");
            var template = templateProp.objectReferenceValue as BlockShapeTemplate;

            EditorGUILayout.LabelField("Template Shape", EditorStyles.boldLabel);

            if (template == null)
            {
                EditorGUILayout.HelpBox("Assign a BlockShapeTemplate to preview its occupied cells.", MessageType.Info);
                return;
            }

            var cells = template.OccupiedCells;
            if (cells == null || cells.Count == 0)
            {
                EditorGUILayout.HelpBox("Template has no occupied cells.", MessageType.Warning);
                return;
            }

            var bounds = template.CalculateLocalBounds();
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
            DrawCells(inner, template, bounds);
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

        private static void DrawCells(Rect area, BlockShapeTemplate template, BoundsInt bounds)
        {
            var cells = template.OccupiedCells;
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
