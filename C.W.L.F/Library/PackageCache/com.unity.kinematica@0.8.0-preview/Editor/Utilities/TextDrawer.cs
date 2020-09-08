using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Unity.Kinematica.Editor
{
    public class TextDrawer
    {
        static TextDrawer instance = new TextDrawer();

        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            SceneView.duringSceneGui += instance.OnSceneGUI;
        }

        void OnSceneGUI(SceneView obj)
        {
            if (!EditorApplication.isPlaying)
            {
                TextDrawerBehavior.instance.movableTexts.Clear();
            }

            Handles.BeginGUI();

            TextDrawerBehavior.instance.UseSkin();

            for (int i = 0; i < TextDrawerBehavior.instance.movableTexts.Count; ++i)
            {
                TextDrawerBehavior.instance.DrawTextWindow(i);
            }

            Handles.EndGUI();
        }
    }
}
