using Unity.Kinematica.Editor.TimelineUtils;
using Unity.Kinematica.Temporary;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    partial class TimeRuler : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TimeRuler, UxmlTraits>
        {
        }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
        }

        public TimelineWidget m_TimelineWidget;
        TimelineWidget.DrawInfo m_DrawInfo;

        PanManipulator m_PanManipulator;
        ZoomManipulator m_ZoomManipulator;

        VisualElement m_TimeArea;
        IMGUIContainer m_IMGUIContainer;

        int m_SampleRate = 60;

        public int SampleRate
        {
            get { return m_SampleRate; }
            set
            {
                if (m_SampleRate != value)
                {
                    m_SampleRate = value;
                    m_TickHandler.SetTickModulosForFrameRate(m_SampleRate);
                }
            }
        }

        public void Init(Timeline timeline, PanManipulator panManipulator, ZoomManipulator zoomManipulator)
        {
            m_TimelineWidget = new TimelineWidget();

            m_PanManipulator = panManipulator;
            m_ZoomManipulator = zoomManipulator;
            m_TimeArea = timeline.m_TimelineScrollableArea;

            m_IMGUIContainer = new IMGUIContainer(() =>
            {
                GUILayout.Space(0);
                var lastRect = GUILayoutUtility.GetLastRect();
                Rect rt = GUILayoutUtility.GetRect(lastRect.width, 22);

                GUI.BeginGroup(rt);

                Rect rect = new Rect(0, 0, rt.width, rt.height);

                if (rect.width > 0 && rect.height > 0)
                {
                    if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_DrawInfo.layout.startTime, m_TimelineWidget.RangeStart) ||
                        !FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_DrawInfo.layout.Duration, m_TimelineWidget.RangeWidth))
                    {
                        m_DrawInfo.layout.startTime = m_TimelineWidget.RangeStart;
                        m_DrawInfo.layout.endTime = m_TimelineWidget.RangeEnd;
                    }
                    else
                    {
                        DrawRuler(m_IMGUIContainer.layout, m_SampleRate, timeline.TimelineUnits);
                    }

                    ForwardEvents(rect);
                }

                GUI.EndGroup();
            });

            Add(m_IMGUIContainer);

            m_TickHandler = new TickHandler();
            float[] modulos =
            {
                0.0000001f, 0.0000005f, 0.000001f, 0.000005f, 0.00001f, 0.00005f, 0.0001f, 0.0005f,
                0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500,
                1000, 5000, 10000, 50000, 100000, 500000, 1000000, 5000000, 10000000
            };
            m_TickHandler.SetTickModulos(modulos);
        }

        public void SetIMGUIVisible(bool show)
        {
            m_IMGUIContainer.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
        }

        Vector2 m_Start;
        Vector2 m_Last;

        bool m_Active = false;

        void ForwardEvents(Rect rect)
        {
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.ScrollWheel)
                {
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Zoom);

                    float wheelDelta = e.alt ? -e.delta.x * 0.1f : e.delta.y;
                    float scale = Mathf.Clamp(1f - wheelDelta * ZoomManipulator.k_ZoomStep, 0.05f, 100000.0f);
                    Vector2 zoomCenter = this.ChangeCoordinatesTo(m_TimeArea, e.mousePosition);
                    m_ZoomManipulator.Zoom(zoomCenter, scale);

                    e.Use();
                }

                if (e.type == EventType.MouseDown && e.button == (int)MouseButton.MiddleMouse)
                {
                    m_Start = m_Last = e.mousePosition;
                    m_Active = true;
                    e.Use();
                }
                else if (m_Active)
                {
                    if (e.type == EventType.MouseUp)
                    {
                        m_Active = false;
                        e.Use();
                        return;
                    }

                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
                    if (e.type == EventType.MouseDrag) //|| e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                    {
                        m_PanManipulator.Pan(m_Last, e.mousePosition);
                        m_Last = e.mousePosition;

                        e.Use();
                    }
                }
            }
        }
    }
}
