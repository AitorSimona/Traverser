using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    abstract class TimelineSnappingMouseManipulator : MouseManipulator
    {
        public const float k_SnapMoveDelta = 15f;
        public static readonly FloatComparer k_SnapTimeComparer = new FloatComparer(0.01f);

        protected enum MouseDirection { None, Decreasing, Increasing }

        protected SnappingElement m_Target;

        protected bool m_Active;
        protected Vector2 m_MouseDownPosition;
        protected Vector2 m_MousePreviousPosition;

        protected TimelineSnappingMouseManipulator(SnappingElement target)
        {
            m_Target = target;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = Utility.GetMultiSelectModifier() });
        }

        protected bool TryToSnap(float dragPosition, MouseDirection direction, float framerate, out float newTime,
            bool snapToClipStart = true, bool snapToClipDuration = true, bool snapToPlayhead = true)
        {
            Timeline timeline = m_Target.Timeline;

            if (SnappingEnabled)
            {
                m_Target.UnSnap();
                SnappingElement snapTarget = FindSnapTarget(dragPosition, direction);
                if (snapTarget != null)
                {
                    newTime = timeline.WorldPositionToTime(snapTarget.GetSnapPosition(dragPosition));
                    m_Target.SnapTo(snapTarget);
                    if (timeline.TimelineUnits == TimelineViewMode.frames)
                    {
                        newTime = (float)TimelineUtility.RoundToFrame(newTime, framerate);
                    }

                    timeline.ShowSnap(newTime);

                    return true;
                }

                float zeroPos = timeline.TimeToWorldPos(0f);
                float duration = m_Target.Track.Clip.DurationInSeconds;
                float endPos = timeline.TimeToWorldPos(duration);

                if (snapToClipStart && Mathf.Abs(dragPosition - zeroPos) < k_SnapMoveDelta)
                {
                    timeline.ShowSnap(0f);
                    newTime = 0f;
                    return true;
                }

                if (snapToClipDuration && Mathf.Abs(dragPosition - endPos) < k_SnapMoveDelta)
                {
                    newTime = duration;
                    timeline.ShowSnap(newTime);
                    return true;
                }

                float activeTimePos = timeline.TimeToWorldPos(timeline.ActiveTime);
                if (snapToPlayhead && Mathf.Abs(dragPosition - activeTimePos) < k_SnapMoveDelta)
                {
                    newTime = timeline.ActiveTime;
                    timeline.ShowSnap(timeline.ActiveTime);
                    return true;
                }
            }

            newTime = timeline.WorldPositionToTime(dragPosition);

            if (timeline.TimelineUnits == TimelineViewMode.frames)
            {
                newTime = (float)TimelineUtility.RoundToFrame(newTime, framerate);
            }

            m_Target.UnSnap();
            timeline.HideSnap();

            return false;
        }

        SnappingElement SnapTo(VisualElement manipulated, float desiredPosition)
        {
            List<KeyValuePair<SnappingElement, float>> candidates = new List<KeyValuePair<SnappingElement, float>>();

            foreach (var ite in m_Target.Timeline.TimelineElements())
            {
                if (manipulated == ite)
                {
                    continue;
                }

                float snapTime = ite.GetSnapPosition(desiredPosition);
                if (float.IsNaN(snapTime))
                {
                    continue;
                }

                float distance = Mathf.Abs(desiredPosition - snapTime);
                if (distance <= k_SnapMoveDelta)
                {
                    candidates.Add(new KeyValuePair<SnappingElement, float>(ite, snapTime));
                }
            }

            if (candidates.Any())
            {
                return candidates.OrderBy(kvp => kvp.Value).First().Key;
            }

            return null;
        }

        SnappingElement FindSnapTarget(float dragPosition, MouseDirection direction)
        {
            SnappingElement snapTarget = SnapTo(m_Target, dragPosition);
            if (snapTarget != null)
            {
                float snapPosition = snapTarget.GetSnapPosition(dragPosition);
                if (snapPosition < dragPosition && direction == MouseDirection.Decreasing)
                {
                    return snapTarget;
                }

                if (snapPosition > dragPosition && direction == MouseDirection.Increasing)
                {
                    return snapTarget;
                }
            }

            return null;
        }

        //TODO - toggle option, keyboard modifier?
        protected bool SnappingEnabled
        {
            get { return true; }
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

        protected virtual void OnMouseDownEvent(MouseDownEvent evt)
        {
            m_Active = true;
            m_MouseDownPosition = evt.mousePosition;
            m_MousePreviousPosition = m_MouseDownPosition;

            m_Target.ShowManipulationLabel();
            target.CaptureMouse();
            evt.StopPropagation();
        }

        protected abstract void OnMouseMoveEvent(MouseMoveEvent evt);

        protected virtual void OnMouseUpEvent(MouseUpEvent evt)
        {
            m_Target.HideManipulationLabel();

            m_Active = false;
            target.ReleaseMouse();
            evt.StopPropagation();

            m_Target.Timeline.HideSnap();
            m_Target.Timeline.HideGuidelines();
        }

        protected MouseDirection GetMouseDirection(Vector2 position)
        {
            MouseDirection direction;
            if (FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_MousePreviousPosition.x, position.x))
            {
                direction = MouseDirection.None;
            }
            else if (m_MousePreviousPosition.x < position.x)
            {
                direction = MouseDirection.Increasing;
            }
            else
            {
                direction = MouseDirection.Decreasing;
            }

            return direction;
        }
    }
}
