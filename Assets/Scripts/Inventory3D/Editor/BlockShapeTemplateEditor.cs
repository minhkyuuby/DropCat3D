#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatDrop3D.Inventory3D.Editor
{
    [CustomEditor(typeof(BlockShapeTemplate))]
    public sealed class BlockShapeTemplateEditor : UnityEditor.Editor
    {
        private const int DefaultPreviewRadius = 4;

        private int previewRadius = DefaultPreviewRadius;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(occupiedProp, includeChildren: true);

            if (GUILayout.Button("Validate / De-duplicate"))
            {
                foreach (var t in targets)
                {
                    if (t is BlockShapeTemplate template)
                    {
                        Undo.RecordObject(template, "Validate BlockShapeTemplate");
                        template.Validate();
                        EditorUtility.SetDirty(template);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
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
                        var label = isOrigin ? "O" : (has ? "■" : "·");
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
                // Vector2Int is serialized as a struct with x/y.
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

            // Keep it stable-ish: origin first, then the rest.
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
    }
}
#endif
