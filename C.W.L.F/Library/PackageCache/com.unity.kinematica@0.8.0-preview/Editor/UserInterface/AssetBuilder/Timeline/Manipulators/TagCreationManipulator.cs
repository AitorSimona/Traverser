using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class TagCreationManipulator : TimelineSnappingMouseManipulator
    {
        class TagCreationElement : SnappingElement
        {
            Label m_ManipulateStartLabel;
            Label m_ManipulateEndLabel;

            float m_StartTime;
            float m_EndTime;

            public TagCreationElement(Track annotationsTrack) : base(annotationsTrack)
            {
                AddToClassList("creationManipulator");
                name = "tagCreator";
                style.visibility = Visibility.Hidden;
                m_ManipulateStartLabel = new Label();
                m_ManipulateStartLabel.AddToClassList("tagManipulateStartLabel");
                m_ManipulateStartLabel.AddToClassList("tagManipulateLabel");

                m_ManipulateEndLabel = new Label();
                m_ManipulateEndLabel.AddToClassList("tagManipulateEndLabel");
                m_ManipulateEndLabel.AddToClassList("tagManipulateLabel");

                Add(m_ManipulateStartLabel);
                Add(m_ManipulateEndLabel);
            }

            public override void Resize()
            {
                Timeline timeline = Timeline;
                float startPos = Timeline.TimeToLocalPos(m_StartTime, timeline.m_ScrollViewContainer);
                float endPos = Timeline.TimeToLocalPos(m_EndTime, timeline.m_ScrollViewContainer);
                float width = Math.Abs(endPos - startPos);

                style.width = width;

                Vector3 position = transform.position;
                position.x = startPos;
                transform.position = position;
            }

            public override float GetSnapPosition(float targetPosition)
            {
                return float.NaN;
            }

            public void SetTimes(float start, float end)
            {
                m_StartTime = start;
                m_EndTime = end;
                Resize();
                ShowManipulationLabel();
            }

            public override void ShowManipulationLabel()
            {
                m_ManipulateStartLabel.text = TimelineUtility.GetTimeString(Timeline.ViewMode, m_StartTime, (int)Timeline.TaggedClip.SampleRate);
                m_ManipulateStartLabel.style.visibility = Visibility.Visible;
                float estimatedTextSize = TimelineUtility.EstimateTextSize(m_ManipulateStartLabel);
                float controlWidth = Math.Max(float.IsNaN(estimatedTextSize) ? m_ManipulateStartLabel.layout.width : estimatedTextSize, 8) + 6;
                m_ManipulateStartLabel.style.left = -controlWidth;

                m_ManipulateEndLabel.text = TimelineUtility.GetTimeString(Timeline.ViewMode, m_EndTime, (int)Timeline.TaggedClip.SampleRate);
                m_ManipulateEndLabel.style.left = style.width.value.value + 8f;
                m_ManipulateEndLabel.style.visibility = Visibility.Visible;
            }

            public override void HideManipulationLabel()
            {
                m_ManipulateStartLabel.style.visibility = Visibility.Hidden;
                m_ManipulateEndLabel.style.visibility = Visibility.Hidden;
            }

            public override float StartTime
            {
                get { return m_StartTime; }
            }

            public override float EndTime
            {
                get { return m_EndTime; }
            }
        }
        const float k_MinDistance = 5f;

        Timeline m_Timeline;


        Vector3 m_MouseDownLocalPos;

        float m_MouseDownTime;

        public TagCreationManipulator(Timeline timeline, Track annotationsTrack) : base(null)
        {
            m_Timeline = timeline;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            m_Target = new TagCreationElement(annotationsTrack);
            m_Timeline.m_ScrollViewContainer.Add(m_Target);
        }

        protected override void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (m_Active)
            {
                return;
            }

            if (!CanStartManipulation(evt))
            {
                return;
            }

            if (!m_Timeline.CanAddTag())
            {
                return;
            }

            if (target == null)
            {
                m_Active = false;
                return;
            }

            base.OnMouseDownEvent(evt);

            m_MouseDownTime = m_Timeline.WorldPositionToTime(m_MouseDownPosition.x);
            m_MouseDownLocalPos = m_Timeline.m_ScrollViewContainer.WorldToLocal(evt.mousePosition);
            m_MouseDownLocalPos.y = m_Timeline.GetFreeVerticalHeight();
            m_Target.transform.position = m_MouseDownLocalPos;
            m_Target.HideManipulationLabel(); // TimelineSnappingMouseManipulator.OnMouseDownEvent will show the labels but we only want to do that once we've started moving
        }

        protected override void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            if (!m_Active || !target.HasMouseCapture() || EditorApplication.isPlaying)
            {
                return;
            }

            if (Math.Abs(evt.mousePosition.x - m_MouseDownPosition.x) < k_MinDistance)
            {
                m_Target.style.visibility = Visibility.Hidden;
                m_Target.HideManipulationLabel();
                m_Timeline.HideGuidelines();
                return;
            }

            float dragPosition = evt.mousePosition.x;
            if (m_Target.SnapValid(dragPosition))
            {
                return;
            }

            MouseDirection direction = GetMouseDirection(evt.mousePosition);
            m_MousePreviousPosition = evt.mousePosition;

            bool canPreview = m_Timeline.CanPreview();

            TryToSnap(dragPosition, direction, m_Timeline.TaggedClip.SampleRate, out float mouseMoveTime, snapToPlayhead: !canPreview);

            m_Target.style.visibility = Visibility.Visible;

            float startTime = Math.Min(m_MouseDownTime, mouseMoveTime);
            float endTime = Math.Max(m_MouseDownTime, mouseMoveTime);

            if (canPreview)
            {
                m_Timeline.SetActiveTime(mouseMoveTime);
            }

            m_Timeline.ShowGuidelines(startTime, endTime);

            (m_Target as TagCreationElement).SetTimes(startTime, endTime);
        }

        protected override void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(evt))
            {
                return;
            }

            base.OnMouseUpEvent(evt);

            float distanceFromMouseDown = Math.Abs(evt.mousePosition.x - m_MouseDownPosition.x);

            if (distanceFromMouseDown >= k_MinDistance && !EditorApplication.isPlaying)
            {
                float mouseUpTime = m_Timeline.WorldPositionToTime(evt.mousePosition.x);

                float startTime = Math.Min(m_MouseDownTime, mouseUpTime);
                float duration = Math.Max(m_MouseDownTime, mouseUpTime) - startTime;

                var menu = new GenericMenu();

                foreach (Type tagType in TagAttribute.GetVisibleTypesInInspector())
                {
                    menu.AddItem(new GUIContent($"{TagAttribute.GetDescription(tagType)}"),
                        false,
                        () => { m_Timeline.OnAddTagSelection(tagType, startTime, duration); });
                }

                menu.DropDown(new Rect(evt.mousePosition, Vector2.zero));
            }
            else
            {
                m_Timeline.ReSelectCurrentClip();
            }

            m_Target.style.visibility = Visibility.Hidden;
        }
    }
}
