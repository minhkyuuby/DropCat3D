using System.Collections.Generic;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlatformSlot3D))]
    public sealed class PlatformShapeRuntimeVisualizer : MonoBehaviour
    {
        [SerializeField] private bool showRuntimeShape = true;

        [SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0.2f, 0.9f);

        [Min(0f)]
        [SerializeField] private float outlineHeight = 0.02f;

        [Min(0f)]
        [SerializeField] private float lineThickness = 0.02f;

        [SerializeField] private bool onlyRenderInPlayMode = true;

        [SerializeField] private InventoryGrid3D gridOverride;

        private InventoryItem3D item;
        private static Material outlineMaterial;

        private void Awake()
        {
            item = GetComponent<InventoryItem3D>();
        }

        private void OnRenderObject()
        {
            if (!showRuntimeShape)
            {
                return;
            }

            if (onlyRenderInPlayMode && !Application.isPlaying)
            {
                return;
            }

            var grid = ResolveGrid();
            if (grid == null || item == null)
            {
                return;
            }

            EnsureOutlineMaterial();
            outlineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(item.transform.localToWorldMatrix);
            GL.Begin(lineThickness > 0f ? GL.QUADS : GL.LINES);
            GL.Color(outlineColor);

            DrawOutline(grid);

            GL.End();
            GL.PopMatrix();
        }

        private void DrawOutline(InventoryGrid3D grid)
        {
            var occupied = new HashSet<Vector2Int>();
            var offsets = item.OccupiedCellOffsets;
            if (offsets == null || offsets.Count == 0)
            {
                occupied.Add(Vector2Int.zero);
            }
            else
            {
                for (int i = 0; i < offsets.Count; i++)
                {
                    occupied.Add(offsets[i]);
                }
            }

            float half = grid.CellSize * 0.5f;
            float y = outlineHeight;

            foreach (var cell in occupied)
            {
                var center = new Vector3(cell.x * grid.CellSize, y, cell.y * grid.CellSize);
                var left = center.x - half;
                var right = center.x + half;
                var bottom = center.z - half;
                var top = center.z + half;

                if (!occupied.Contains(new Vector2Int(cell.x - 1, cell.y)))
                {
                    DrawEdge(new Vector3(left, y, bottom), new Vector3(left, y, top), new Vector3(1f, 0f, 0f));
                }

                if (!occupied.Contains(new Vector2Int(cell.x + 1, cell.y)))
                {
                    DrawEdge(new Vector3(right, y, bottom), new Vector3(right, y, top), new Vector3(1f, 0f, 0f));
                }

                if (!occupied.Contains(new Vector2Int(cell.x, cell.y - 1)))
                {
                    DrawEdge(new Vector3(left, y, bottom), new Vector3(right, y, bottom), new Vector3(0f, 0f, 1f));
                }

                if (!occupied.Contains(new Vector2Int(cell.x, cell.y + 1)))
                {
                    DrawEdge(new Vector3(left, y, top), new Vector3(right, y, top), new Vector3(0f, 0f, 1f));
                }
            }
        }

        private void DrawEdge(Vector3 start, Vector3 end, Vector3 axis)
        {
            if (lineThickness <= 0f)
            {
                GL.Vertex(start);
                GL.Vertex(end);
                return;
            }

            float half = lineThickness * 0.5f;
            var offset = axis * half;
            GL.Vertex(start - offset);
            GL.Vertex(start + offset);
            GL.Vertex(end + offset);
            GL.Vertex(end - offset);
        }

        private InventoryGrid3D ResolveGrid()
        {
            if (gridOverride != null)
            {
                return gridOverride;
            }

            return GetComponentInParent<InventoryGrid3D>();
        }

        private static void EnsureOutlineMaterial()
        {
            if (outlineMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Hidden/Internal-Colored");
            outlineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            outlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            outlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            outlineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            outlineMaterial.SetInt("_ZWrite", 0);
        }
    }
}
