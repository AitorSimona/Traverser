using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
using System;
using Unity.SnapshotDebugger;
using System.Linq;

namespace Unity.Kinematica
{
    internal struct MovableText
    {
        public int identifier;
        public string title;
        public List<(string, Color)> lines;
        public Rect rect;
    }

    internal struct ParagraphLayout
    {
        public Rect rect;
        public Rect titleRect;
        public Vector2[] lineSizes;

        public static ParagraphLayout Create(MovableText movableText)
        {
            string[] lines = movableText.lines.Select(line => line.Item1).ToArray();

            Rect rect = movableText.rect;
            rect.size = Vector2.zero;
            Rect titleRect = Rect.zero;
            List<Vector2> lineSizes = new List<Vector2>();

            if (!string.IsNullOrEmpty(movableText.title))
            {
                titleRect.size = GUI.skin.label.CalcSize(new GUIContent(movableText.title));
                rect.width = titleRect.width;
            }

            foreach (string line in lines)
            {
                var textSize = GUI.skin.label.CalcSize(new GUIContent(line));
                lineSizes.Add(textSize);

                rect.width = math.max(rect.width, textSize.x);
                rect.height = rect.height + textSize.y;
            }

            rect.xMax = rect.xMax + 2.0f * TextDrawerBehavior.kWindowMargin;
            rect.yMax = rect.yMax + 2.0f * TextDrawerBehavior.kWindowMargin + TextDrawerBehavior.Skin.window.border.top;

            titleRect.x = (rect.width - titleRect.width) * 0.5f;
            titleRect.y = TextDrawerBehavior.kTitleVerticalMargin;

            return new ParagraphLayout()
            {
                rect = rect,
                titleRect = titleRect,
                lineSizes = lineSizes.ToArray()
            };
        }
    }

    internal class TextDrawerBehavior : MonoBehaviour
    {
        public static readonly float kWindowMargin = 10.0f;
        public static readonly float kTitleVerticalMargin = 3.0f;

        public static GUISkin Skin => instance.m_Skin;

        internal static void CreateInstance()
        {
            if (_instance == null)
            {
                TextDrawerBehavior[] behaviors = GameObject.FindObjectsOfType<TextDrawerBehavior>();
                if (behaviors.Length > 0)
                {
                    _instance = behaviors[0];
                }
                else
                {
                    GameObject gameObject = new GameObject("TextDrawer");
                    gameObject.hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.HideAndDontSave;
                    _instance = gameObject.AddComponent<TextDrawerBehavior>();
                    _instance.Init();
                }
            }
        }

        internal void Init()
        {
#if UNITY_EDITOR
            m_Skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Packages/com.unity.kinematica/Assets/Skins/Text_window_skin.guiskin");
#else
            m_Skin = null;
#endif
        }

        public static TextDrawerBehavior instance
        {
            get
            {
                CreateInstance();
                return _instance;
            }
        }

        static TextDrawerBehavior _instance = null;

        private void OnGUI()
        {
            UseSkin();

            for (int i = 0; i < movableTexts.Count; ++i)
            {
                DrawTextWindow(i);
            }
        }

        internal void UseSkin()
        {
            if (m_Skin != null)
            {
                GUI.skin = m_Skin;
            }
        }

        internal void DrawTextWindow(int windowIdentifier)
        {
            MovableText movableText = movableTexts[windowIdentifier];
            if (movableText.lines.Count == 0)
            {
                return;
            }

            ParagraphLayout layout = ParagraphLayout.Create(movableText);

            GUI.color = Color.white;

            movableText.rect = GUI.Window(movableText.identifier, layout.rect, DrawWindowContent, new GUIContent());
            movableTexts[windowIdentifier] = movableText;
        }

        internal void DrawWindowContent(int windowID)
        {
            MovableText movableTextInfo = movableTexts[FindMovableTextIndex(windowID)];
            ParagraphLayout layout = ParagraphLayout.Create(movableTextInfo);

            if (!string.IsNullOrEmpty(movableTextInfo.title))
            {
                GUIStyle titleStyle = new GUIStyle();
                titleStyle.normal.textColor = Color.white;
                GUI.Label(layout.titleRect, new GUIContent(movableTextInfo.title), titleStyle);
            }

            float verticalPosition = kWindowMargin + 18;
            for (int line = 0; line < movableTextInfo.lines.Count; ++line)
            {
                (string text, Color color) = movableTextInfo.lines[line];

                GUIStyle style = new GUIStyle();
                style.normal.textColor = color;
                style.border.left = 0;
                GUI.Label(new Rect(new Vector2(kWindowMargin, verticalPosition), layout.lineSizes[line]), text, style);

                verticalPosition += layout.lineSizes[line].y;
            }

            GUI.DragWindow(new Rect(0, 0, Screen.width, Screen.height));
        }

        internal int CreateMovableText(float2 position)
        {
            movableTexts.Add(new MovableText()
            {
                identifier = m_NextMovableTextIdentifier,
                title = null,
                rect = new Rect(position, Vector2.zero),
                lines = new List<(string, Color)>()
            });

            return m_NextMovableTextIdentifier++;
        }

        internal void DestroyMovableText(int identifier)
        {
            movableTexts.RemoveAll(text => text.identifier == identifier);
        }

        internal void ClearMovableText(int identifier)
        {
            int index = FindMovableTextIndex(identifier);
            if (index < 0)
            {
                return;
            }

            var text = movableTexts[index];
            text.title = null;
            text.lines.Clear();
            movableTexts[index] = text;
        }

        internal void SetMovableTextTitle(int identifier, string title)
        {
            int index = FindMovableTextIndex(identifier);
            if (index < 0)
            {
                return;
            }

            var text = movableTexts[index];
            text.title = title;
            movableTexts[index] = text;
        }

        internal void AddMovableTextLine(int identifier, string line, Color color)
        {
            int index = FindMovableTextIndex(identifier);
            if (index < 0)
            {
                return;
            }

            var text = movableTexts[index];
            text.lines.Add((line, color));
            movableTexts[index] = text;
        }

        internal Rect GetMovableTextRect(int identifier)
        {
            int index = FindMovableTextIndex(identifier);
            if (index < 0)
            {
                return Rect.zero;
            }

            return movableTexts[index].rect;
        }

        int FindMovableTextIndex(int identifier)
        {
            return movableTexts.FindIndex(text => text.identifier == identifier);
        }

        public List<MovableText> movableTexts = new List<MovableText>();

        int m_NextMovableTextIdentifier = 0;
        GUISkin m_Skin;
    }
}
