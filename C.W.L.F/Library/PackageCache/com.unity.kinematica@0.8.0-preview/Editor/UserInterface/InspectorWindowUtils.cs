using System;
using UnityEditor;

namespace Unity.Kinematica.Editor
{
    static class InspectorWindowUtils
    {
        public static void RepaintInspectors()
        {
            foreach (var ew in UnityEngine.Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (ew.titleContent.text.Equals("Inspector", StringComparison.OrdinalIgnoreCase))
                {
                    ew.Repaint();
                }
            }
        }
    }
}
