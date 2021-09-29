using UnityEngine;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.Animations.Rigging
{
    static class SceneViewOverlay
    {
        public delegate void WindowFunction(Object target, SceneView sceneView);

        static SceneView s_SceneView;
        static List<OverlayWindow> s_Windows;

        static SceneViewOverlay()
        {
            s_Windows = new List<OverlayWindow>();
        }

        public static void Begin(SceneView sceneView)
        {
            s_SceneView = sceneView;

            if (Event.current.type == EventType.Layout)
                s_Windows.Clear();
        }

        static class Styles
        {
            public static readonly GUIStyle sceneViewOverlayTransparentBackground = "SceneViewOverlayTransparentBackground";
            public static readonly GUIStyle title = new GUIStyle(GUI.skin.window);

            public static readonly float windowPadding = 9f;
            public static readonly float minWidth = 210f;

            static Styles()
            {
                title.padding.top = title.padding.bottom;
            }
        }

        public static void End()
        {
            s_Windows.Sort();

            if (s_Windows.Count > 0)
            {
                var windowOverlayRect = new Rect(0, 0f, s_SceneView.position.width, s_SceneView.position.height);
                GUILayout.Window("SceneViewOverlay".GetHashCode(), windowOverlayRect, WindowTrampoline, "", Styles.sceneViewOverlayTransparentBackground);
            }

            s_SceneView = null;
        }

        static void WindowTrampoline(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(Styles.minWidth));

            var paddingOffset = -Styles.windowPadding;

            bool showSceneViewWindows = SceneView.lastActiveSceneView == s_SceneView;

            foreach (OverlayWindow win in s_Windows)
            {
                if (!showSceneViewWindows && win.editorWindow != s_SceneView)
                    continue;

                GUILayout.Space(Styles.windowPadding + paddingOffset);
                paddingOffset = 0f;
                ResetGUIState();
                if (win.canCollapse)
                {
                    GUILayout.BeginVertical(Styles.title);

                    win.expanded = EditorGUILayout.Foldout(win.expanded, win.title, true);

                    if (win.expanded)
                        win.sceneViewFunc(win.target, s_SceneView);
                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.BeginVertical(win.title, GUI.skin.window);
                    win.sceneViewFunc(win.target, s_SceneView);
                    GUILayout.EndVertical();
                }
            }

            GUILayout.EndVertical();

            var inputEaterRect = GUILayoutUtility.GetLastRect();
            EatMouseInput(inputEaterRect);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        static void EatMouseInput(Rect position)
        {
            var id = GUIUtility.GetControlID("SceneViewOverlay".GetHashCode(), FocusType.Passive, position);
            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                        Event.current.Use();
                    break;
                case EventType.ScrollWheel:
                    if (position.Contains(Event.current.mousePosition))
                        Event.current.Use();
                    break;
            }
        }

        static void ResetGUIState()
        {
            GUI.skin = null;
            GUI.backgroundColor = GUI.contentColor = Color.white;
            GUI.color = Color.white;
            GUI.enabled = true;
            GUI.changed = false;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.fieldWidth = 0;
            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.hierarchyMode = false;
            EditorGUIUtility.wideMode = false;
        }

        // pass window parameter to render in sceneviews that are not the active view.
        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order)
        {
            Window(title, sceneViewFunc, order, null);
        }

        // pass window parameter to render in sceneviews that are not the active view.
        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, Object target, EditorWindow window = null)
        {
            if (Event.current.type != EventType.Layout)
                return;

            var newWindow = new OverlayWindow(title, sceneViewFunc, order, target)
            {
                secondaryOrder = s_Windows.Count,
                canCollapse = false
            };


            s_Windows.Add(newWindow);
        }
    }

    internal class OverlayWindow : IComparable<OverlayWindow>
    {
        public OverlayWindow(GUIContent title, SceneViewOverlay.WindowFunction guiFunction, int primaryOrder, Object target)
        {
            this.title = title;
            this.sceneViewFunc = guiFunction;
            this.primaryOrder = primaryOrder;
            this.target = target;
            this.canCollapse = true;
            this.expanded = true;
        }

        public SceneViewOverlay.WindowFunction sceneViewFunc { get;  }
        public int primaryOrder { get; }
        public int secondaryOrder { get; set; }
        public Object target { get; }
        public EditorWindow editorWindow { get; set; }

        public bool canCollapse { get; set; }
        public bool expanded { get; set; }

        public GUIContent title { get; }

        public int CompareTo(OverlayWindow other)
        {
            var result =  other.primaryOrder.CompareTo(primaryOrder);
            if (result == 0)
                result = other.secondaryOrder.CompareTo(secondaryOrder);
            return result;
        }
    }
}
