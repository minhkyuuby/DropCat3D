using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    [ExecuteAlways]
    public sealed class GridGroundCutout : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryGrid3D grid;
        [SerializeField] private Renderer targetRenderer;

        [Header("Settings")]
        [SerializeField] private bool cutoutEnabled = true;
        [SerializeField] private bool useBoundaryMask = true;

        private MaterialPropertyBlock propertyBlock;
        private Texture2D maskTexture;
        private Color32[] maskPixels;
        private int lastMaskWidth;
        private int lastMaskHeight;

        private static readonly int GridWorldToLocalId = Shader.PropertyToID("_GridWorldToLocal");
        private static readonly int GridMinId = Shader.PropertyToID("_GridMin");
        private static readonly int GridMaxId = Shader.PropertyToID("_GridMax");
        private static readonly int GridSizeId = Shader.PropertyToID("_GridSize");
        private static readonly int CellSizeId = Shader.PropertyToID("_CellSize");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int MaskEnabledId = Shader.PropertyToID("_MaskEnabled");
        private static readonly int CutoutEnabledId = Shader.PropertyToID("_CutoutEnabled");

        private void OnEnable()
        {
            EnsureRefs();
            ApplyProperties();
        }

        private void OnValidate()
        {
            EnsureRefs();
            ApplyProperties();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplyProperties();
            }
        }

        private void EnsureRefs()
        {
            if (grid == null)
            {
                grid = FindFirstObjectByType<InventoryGrid3D>();
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void ApplyProperties()
        {
            if (grid == null || targetRenderer == null)
            {
                return;
            }

            var frame = grid.Frame;
            float extentX = grid.Width * grid.CellSize * 0.5f;
            float extentZ = grid.Height * grid.CellSize * 0.5f;
            float minX = -extentX;
            float minZ = -extentZ;
            float maxX = extentX;
            float maxZ = extentZ;

            if (useBoundaryMask)
            {
                EnsureMaskTexture();
                UpdateMaskPixels();
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetMatrix(GridWorldToLocalId, frame.worldToLocalMatrix);
            propertyBlock.SetVector(GridMinId, new Vector4(minX, 0f, minZ, 0f));
            propertyBlock.SetVector(GridMaxId, new Vector4(maxX, 0f, maxZ, 0f));
            propertyBlock.SetVector(GridSizeId, new Vector4(grid.Width, grid.Height, 0f, 0f));
            propertyBlock.SetFloat(CellSizeId, grid.CellSize);
            propertyBlock.SetFloat(MaskEnabledId, useBoundaryMask ? 1f : 0f);
            if (maskTexture != null)
            {
                propertyBlock.SetTexture(MaskTexId, maskTexture);
            }
            propertyBlock.SetFloat(CutoutEnabledId, cutoutEnabled ? 1f : 0f);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void EnsureMaskTexture()
        {
            int width = Mathf.Max(1, grid.Width);
            int height = Mathf.Max(1, grid.Height);

            if (maskTexture != null && width == lastMaskWidth && height == lastMaskHeight)
            {
                return;
            }

            if (maskTexture != null)
            {
                DestroyImmediate(maskTexture);
            }

            maskTexture = new Texture2D(width, height, TextureFormat.R8, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "GridBoundaryMask"
            };

            maskPixels = new Color32[width * height];
            lastMaskWidth = width;
            lastMaskHeight = height;
        }

        private void UpdateMaskPixels()
        {
            if (maskTexture == null || maskPixels == null)
            {
                return;
            }

            int width = lastMaskWidth;
            int height = lastMaskHeight;
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool enabled = grid.GetBoundaryMaskCell(x, y);
                    byte v = enabled ? (byte)255 : (byte)0;
                    maskPixels[index++] = new Color32(v, v, v, 255);
                }
            }

            maskTexture.SetPixels32(maskPixels);
            maskTexture.Apply(false, false);
        }
    }
}
