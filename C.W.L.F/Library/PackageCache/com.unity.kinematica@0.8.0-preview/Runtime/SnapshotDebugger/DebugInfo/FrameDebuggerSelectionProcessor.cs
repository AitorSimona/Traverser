using UnityEngine;

namespace Unity.SnapshotDebugger
{
    public interface IFrameDebuggerSelectionProcessor
    {
        void UpdateSelection();

        void DrawSelection(Camera camera);

        void DrawInspector();
    }
}
