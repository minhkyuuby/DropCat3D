#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CatDrop3D.Inventory3D
{
#if UNITY_EDITOR
    // Editor-only partial methods live here.
    internal static class SerializedObjectShim
    {
        public static SerializedObject Create(object target) => new SerializedObject(target as UnityEngine.Object);
    }
#endif
}
