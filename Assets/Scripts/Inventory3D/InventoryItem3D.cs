using System.Collections.Generic;
using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed class InventoryItem3D : MonoBehaviour
    {
        [SerializeField] private BlockShapeTemplate template;

        [Header("Visuals")]
        [Tooltip("Optional prefab used for each block. If null, Unity cube primitives are used.")]
        [SerializeField] private GameObject blockPrefab;

        [Tooltip("Vertical lift so the item doesn't z-fight with the grid plane.")]
        [SerializeField] private float yOffset = 0.05f;

        public BlockShapeTemplate Template => template;

        public void SetTemplate(BlockShapeTemplate newTemplate)
        {
            template = newTemplate;
            if (template != null)
            {
                template.Validate();
            }
        }

        public float YOffset => yOffset;

        public IEnumerable<Vector2Int> OccupiedCells(Vector2Int originCell)
        {
            if (template == null)
            {
                yield return originCell;
                yield break;
            }

            var cells = template.OccupiedCells;
            for (int i = 0; i < cells.Count; i++)
            {
                yield return originCell + cells[i];
            }
        }

        public void EnsureVisuals(float cellSize)
        {
            if (template == null)
            {
                return;
            }

            // If the item already has children, assume the visuals are authored.
            if (transform.childCount > 0)
            {
                return;
            }

            var cells = template.OccupiedCells;
            for (int i = 0; i < cells.Count; i++)
            {
                var offset = cells[i];
                var localPos = new Vector3(offset.x * cellSize, 0f, offset.y * cellSize);

                GameObject block;
                if (blockPrefab != null)
                {
                    block = Instantiate(blockPrefab, transform);
                    block.transform.localPosition = localPos;
                    block.transform.localRotation = Quaternion.identity;
                    block.transform.localScale = Vector3.one;
                }
                else
                {
                    block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"Block_{offset.x}_{offset.y}";
                    block.transform.SetParent(transform, worldPositionStays: false);
                    block.transform.localPosition = localPos;
                    block.transform.localRotation = Quaternion.identity;
                    block.transform.localScale = Vector3.one * (cellSize * 0.95f);
                }

                // Make sure picking works.
                if (block.GetComponent<Collider>() == null)
                {
                    block.AddComponent<BoxCollider>();
                }
            }
        }

        private void OnValidate()
        {
            if (template != null)
            {
                template.Validate();
            }
        }
    }
}
