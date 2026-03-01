using UnityEditor;
using UnityEngine;

namespace CatDrop3D.Inventory3D.Editor
{
    [CustomEditor(typeof(PlatformCapacityVisualizer))]
    public sealed class PlatformCapacityVisualizerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var visualizer = (PlatformCapacityVisualizer)target;
            if (GUILayout.Button("Generate Visual"))
            {
                visualizer.GenerateVisual();
                EditorUtility.SetDirty(visualizer);
            }
        }
    }
}
