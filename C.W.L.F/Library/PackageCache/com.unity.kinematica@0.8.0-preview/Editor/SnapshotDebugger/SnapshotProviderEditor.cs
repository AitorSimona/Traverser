using UnityEditor;
using UnityEngine;

namespace Unity.SnapshotDebugger.Editor
{
    [CustomEditor(typeof(SnapshotProvider), true)]
    [CanEditMultipleObjects]
    internal class SnapshotProviderEditor : UnityEditor.Editor
    {
        private SnapshotProvider Target
        {
            get { return target as SnapshotProvider; }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            var label = $"Requires approximately {MemorySize.ToString(GetMemorySize())} per sample";

            EditorGUILayout.HelpBox(label, MessageType.Info);
        }

        int GetMemorySize()
        {
            var buffer = Buffer.Create(Collections.Allocator.Temp);

            Target.WriteToStream(buffer);

            return buffer.Size;
        }
    }
}
