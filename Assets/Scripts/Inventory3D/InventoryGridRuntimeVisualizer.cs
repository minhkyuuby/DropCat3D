using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryGrid3D))]
    public sealed class InventoryGridRuntimeVisualizer : MonoBehaviour
    {
        [SerializeField] private bool showRuntimeGrid;

        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.35f);

        [Min(0f)]
        [SerializeField] private float gridHeight = 0.01f;

        [Min(0f)]
        [SerializeField] private float lineThickness = 0.02f;

        [SerializeField] private bool onlyRenderInPlayMode = true;

        private InventoryGrid3D grid;
        private static Material runtimeGridMaterial;

        public bool IsRuntimeGridVisible => showRuntimeGrid;

        private void Awake()
        {
            grid = GetComponent<InventoryGrid3D>();
        }

        public void SetRuntimeGridVisible(bool visible)
        {
            showRuntimeGrid = visible;
        }

        private void OnRenderObject()
        {
            if (grid == null)
            {
                return;
            }

            if (onlyRenderInPlayMode && !Application.isPlaying)
            {
                return;
            }

            if (!showRuntimeGrid)
            {
                return;
            }

            EnsureRuntimeGridMaterial();
            runtimeGridMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(grid.Frame.localToWorldMatrix);
            GL.Begin(lineThickness > 0f ? GL.QUADS : GL.LINES);
            GL.Color(gridColor);

            var offset = grid.GridCenterOffsetLocal;
            float minX = -0.5f * grid.CellSize - offset.x;
            float maxX = (grid.Width - 0.5f) * grid.CellSize - offset.x;
            float minZ = -0.5f * grid.CellSize - offset.z;
            float maxZ = (grid.Height - 0.5f) * grid.CellSize - offset.z;
            float y = gridHeight;

            for (int x = 0; x <= grid.Width; x++)
            {
                float px = (x - 0.5f) * grid.CellSize - offset.x;
                if (lineThickness > 0f)
                {
                    DrawLineQuad(new Vector3(px, y, minZ), new Vector3(px, y, maxZ), new Vector3(1f, 0f, 0f), lineThickness);
                }
                else
                {
                    GL.Vertex(new Vector3(px, y, minZ));
                    GL.Vertex(new Vector3(px, y, maxZ));
                }
            }

            for (int z = 0; z <= grid.Height; z++)
            {
                float pz = (z - 0.5f) * grid.CellSize - offset.z;
                if (lineThickness > 0f)
                {
                    DrawLineQuad(new Vector3(minX, y, pz), new Vector3(maxX, y, pz), new Vector3(0f, 0f, 1f), lineThickness);
                }
                else
                {
                    GL.Vertex(new Vector3(minX, y, pz));
                    GL.Vertex(new Vector3(maxX, y, pz));
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        private static void EnsureRuntimeGridMaterial()
        {
            if (runtimeGridMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Hidden/Internal-Colored");
            runtimeGridMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            runtimeGridMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            runtimeGridMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            runtimeGridMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            runtimeGridMaterial.SetInt("_ZWrite", 0);
        }

        private static void DrawLineQuad(Vector3 start, Vector3 end, Vector3 axis, float thickness)
        {
            float half = thickness * 0.5f;
            var offset = axis * half;
            GL.Vertex(start - offset);
            GL.Vertex(start + offset);
            GL.Vertex(end + offset);
            GL.Vertex(end - offset);
        }
    }
}
