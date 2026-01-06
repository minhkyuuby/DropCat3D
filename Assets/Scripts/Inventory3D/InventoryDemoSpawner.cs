using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    public sealed class InventoryDemoSpawner : MonoBehaviour
    {
        [SerializeField] private InventoryGrid3D grid;

        [Header("Spawn")]
        [SerializeField] private InventoryItem3D itemPrefab;
        [SerializeField] private BlockShapeTemplate[] templates;
        [SerializeField] private Vector2Int[] spawnCells;

        private void Awake()
        {
            if (grid == null)
            {
                grid = FindFirstObjectByType<InventoryGrid3D>();
            }
        }

        private void Start()
        {
            if (grid == null || itemPrefab == null || templates == null || templates.Length == 0)
            {
                return;
            }

            int count = Mathf.Min(templates.Length, spawnCells != null ? spawnCells.Length : 0);
            for (int i = 0; i < count; i++)
            {
                var item = Instantiate(itemPrefab);
                // Assign template at runtime.
                var itemComponent = item.GetComponent<InventoryItem3D>();
                if (itemComponent == null)
                {
                    Destroy(item.gameObject);
                    continue;
                }

                itemComponent.SetTemplate(templates[i]);

                var cell = spawnCells[i];
                if (grid.CanPlace(itemComponent, cell))
                {
                    grid.Place(itemComponent, cell);
                }
                else
                {
                    Destroy(item.gameObject);
                }
            }
        }
    }
}
