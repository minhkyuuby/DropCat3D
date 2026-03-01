using UnityEngine;

namespace CatDrop3D.Inventory3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlatformSlot3D))]
    public sealed class PlatformCapacityVisualizer : MonoBehaviour
    {
        [SerializeField] private TextMesh textMesh;

        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.6f, 0f);

        [SerializeField] private Color textColor = Color.white;

        [Min(0.01f)]
        [SerializeField] private float characterSize = 0.1f;

        [Min(1)]
        [SerializeField] private int fontSize = 64;

        [SerializeField] private PlatformSlot3D slot;

        private void Awake()
        {
            if (slot == null)
            {
                slot = GetComponent<PlatformSlot3D>();
            }
            GenerateVisual();
        }

        public void GenerateVisual()
        {
            if (slot == null)
            {
                slot = GetComponent<PlatformSlot3D>();
            }
            EnsureTextMesh();
            ApplyVisualSettings();
            UpdateTextMeshTransform();
            if (slot != null)
            {
                HandleCapacityLeftChanged(slot.CapacityLeft);
            }
        }

        private void OnEnable()
        {
            if (slot == null)
            {
                slot = GetComponent<PlatformSlot3D>();
            }
            if (slot != null)
            {
                slot.CapacityLeftChanged += HandleCapacityLeftChanged;
                HandleCapacityLeftChanged(slot.CapacityLeft);
            }
        }

        private void OnDisable()
        {
            if (slot != null)
            {
                slot.CapacityLeftChanged -= HandleCapacityLeftChanged;
            }
        }

        private void EnsureTextMesh()
        {
            if (textMesh != null)
            {
                return;
            }

            var go = new GameObject("CapacityLeftText");
            go.transform.SetParent(transform, worldPositionStays: false);
            textMesh = go.AddComponent<TextMesh>();
        }

        private void ApplyVisualSettings()
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = textColor;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = fontSize;
        }

        private void HandleCapacityLeftChanged(int capacityLeft)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.text = capacityLeft.ToString();
        }

        private void UpdateTextMeshTransform()
        {
            if (textMesh == null)
            {
                return;
            }

            var t = textMesh.transform;
            t.localPosition = localOffset;
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void OnValidate()
        {
            if(slot == null)
            {
                slot = GetComponent<PlatformSlot3D>();
            }
            if (textMesh != null)
            {
                textMesh.color = textColor;
                textMesh.characterSize = characterSize;
                textMesh.fontSize = fontSize;
                UpdateTextMeshTransform();
                if (slot != null)
                {
                    textMesh.text = slot.CapacityLeft.ToString();
                }
            }
        }
    }
}
