using System;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

// Code copied from Unity\Editor\Graphs\UnityEditor.Graphs\Animation\AnimatorControllerTool.cs
// TODO - investigate making this publicly available in editor code
namespace Unity.Kinematica.Temporary
{
    class ZoomManipulator : MouseManipulator
    {
        const float k_MinZoomLevel = 0.1f;
        const float k_MaxZoomLevel = 3.0f;
        public const float k_ZoomStep = 0.05f;

        VisualElement m_ZoomedUI;

        bool m_Active;
        Vector2 m_Start;
        Vector2 m_Last;
        Vector2 m_ZoomCenter;

        public ZoomManipulator(VisualElement zoomedUi)
        {
            m_ZoomedUI = zoomedUi;

            activators.Add(new ManipulatorActivationFilter
                { button = MouseButton.RightMouse, modifiers = EventModifiers.Alt });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<WheelEvent>(OnScroll, TrickleDown.TrickleDown);

            target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<WheelEvent>(OnScroll, TrickleDown.TrickleDown);

            target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        }

        void OnScroll(WheelEvent e)
        {
            float zoomScale = 1f - e.delta.y * k_ZoomStep;
            Zoom(m_ZoomedUI.WorldToLocal(e.mousePosition), zoomScale);
            e.StopPropagation();
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (CanStartManipulation(e))
            {
                m_Start = m_Last = e.localMousePosition;
                m_ZoomCenter = target.ChangeCoordinatesTo(m_ZoomedUI, m_Start);

                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                Vector2 diff = e.localMousePosition - m_Last;
                float zoomScale = 1f + (diff.x + diff.y) * k_ZoomStep;
                Zoom(m_ZoomCenter, zoomScale);
                e.StopPropagation();

                m_Last = e.localMousePosition;
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (m_Active && CanStopManipulation(e))
            {
                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        public event Action<float, Vector2> ScaleChangeEvent;

        public void Zoom(Vector2 zoomCenter, float zoomScale)
        {
            Vector3 scale = m_ZoomedUI.transform.scale;

            scale = Vector3.Scale(scale, new Vector3(zoomScale, zoomScale, 1f));

            // Limit scale
            scale.x = Mathf.Clamp(scale.x, k_MinZoomLevel, k_MaxZoomLevel);
            scale.y = Mathf.Clamp(scale.y, k_MinZoomLevel, k_MaxZoomLevel);

            //TODO - implement the zooming/scaling here vs in the Timeline?
            ScaleChangeEvent?.Invoke(scale.x, zoomCenter);
        }
    }

    class PanManipulator : MouseManipulator
    {
        Vector2 m_LastPosition;
        Vector2 m_Start;
        public Vector2 panSpeed { get; set; }

        bool m_HorizontalOnly;
        bool m_VerticalOnly;
        public bool HorizontalOnly
        {
            get { return m_HorizontalOnly; }
            set
            {
                m_HorizontalOnly = value;
                if (value)
                {
                    m_VerticalOnly = false;
                }
            }
        }

        public bool VerticalOnly
        {
            get { return m_VerticalOnly; }
            set
            {
                m_VerticalOnly = value;
                if (value)
                {
                    m_HorizontalOnly = false;
                }
            }
        }

        bool m_Active;

        VisualElement m_PannedUI;

        public PanManipulator(VisualElement pannedUi)
        {
            m_PannedUI = pannedUi;

            m_Active = false;
            activators.Add(new ManipulatorActivationFilter
                { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            activators.Add(new ManipulatorActivationFilter
                { button = MouseButton.MiddleMouse, modifiers = EventModifiers.Alt });
            panSpeed = new Vector2(1, 1);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (CanStartManipulation(e))
            {
                m_Start = target.ChangeCoordinatesTo(m_PannedUI, e.localMousePosition);
                m_LastPosition = e.mousePosition;
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        public event Action<Vector2, Vector2> Panned;

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active)
            {
                Pan(m_LastPosition, e.mousePosition);
                m_LastPosition = e.mousePosition;
                e.StopPropagation();
            }
        }

        public void Pan(Vector2 from, Vector2 to)
        {
            var diff = from.x - to.x;
            if (FloatComparer.s_ComparerWithDefaultTolerance.Equals(0f, diff))
            {
                return;
            }

            Panned?.Invoke(from, to);
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (m_Active && CanStopManipulation(e))
            {
                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }
    }
}
