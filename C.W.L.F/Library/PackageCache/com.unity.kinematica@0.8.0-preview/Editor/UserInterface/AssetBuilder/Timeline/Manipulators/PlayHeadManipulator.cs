using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class PlayheadManipulator : MouseManipulator
    {
        Timeline m_Timeline;

        float ActiveTime
        {
            set
            {
                if (m_Timeline.TaggedClip != null)
                {
                    m_Timeline.SetActiveTime(value);
                }
            }
        }

        float DebugTime
        {
            set { m_Timeline.SetDebugTime(value); }
        }

        public PlayheadManipulator(Timeline timeline)
        {
            m_Timeline = timeline;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
        }

        void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
            {
                return;
            }

            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (m_Timeline.TaggedClip != null && !EditorApplication.isPlaying)
                {
                    ActiveTime = m_Timeline.WorldPositionToTime(evt.mousePosition.x);
                }

                DebugTime = m_Timeline.WorldPositionToTime(evt.mousePosition.x);
            }

            target.CaptureMouse();
            evt.StopPropagation();
        }

        void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            if (!target.HasMouseCapture())
            {
                return;
            }

            float time = m_Timeline.WorldPositionToTime(evt.mousePosition.x);
            if (m_Timeline.TaggedClip != null && !EditorApplication.isPlaying)
            {
                ActiveTime = time;
            }

            DebugTime = time;
        }

        void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (!target.HasMouseCapture() || !CanStopManipulation(evt))
            {
                return;
            }

            target.ReleaseMouse();
            evt.StopPropagation();
        }
    }
}
