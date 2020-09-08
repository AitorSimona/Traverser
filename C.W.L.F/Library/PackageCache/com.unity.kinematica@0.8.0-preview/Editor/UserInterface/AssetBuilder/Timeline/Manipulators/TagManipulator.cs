using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class TagManipulator : TimelineSnappingMouseManipulator
    {
        // TODO - read this value from the style
        internal const float k_TagHandleMinWidth = 12f;
        const float k_MinDistanceForTagCreation = 1.5f;

        public enum Mode { StartTime, Duration, Body, None };

        Mode m_Mode;
        Mode m_OverrideMode;

        TagElement m_TagElement;

        TagElement TagElement
        {
            get { return m_TagElement ?? (m_TagElement = m_Target as TagElement); }
        }

        float m_TimeOffsetAtMouseDown;

        public TagManipulator(TagElement tagElement, Mode mode) : base(tagElement)
        {
            m_Mode = mode;
            m_OverrideMode = Mode.None;
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

            base.OnMouseDownEvent(evt);

            TagElement.Select(evt);

            m_TimeOffsetAtMouseDown = TagElement.PositionToTime(m_MouseDownPosition.x) - TagElement.m_Tag.startTime;
        }

        protected override void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            Timeline timeline = TagElement.Timeline;
            TaggedAnimationClip taggedAnimationClip = timeline.TaggedClip;
            if (!m_Active || !target.HasMouseCapture() || !taggedAnimationClip.Valid || EditorApplication.isPlaying)
            {
                return;
            }

            if (Math.Abs(m_MousePreviousPosition.x - evt.mousePosition.x) < k_MinDistanceForTagCreation)
            {
                if (Math.Abs(m_MouseDownPosition.y - evt.mousePosition.y) >= k_MinDistanceForTagCreation)
                {
                    OnMouseMoveReorderEvent(evt);
                }

                return;
            }

            Vector2 position = evt.mousePosition;
            MouseDirection direction = GetMouseDirection(position);
            m_MousePreviousPosition = position;
            float framerate = taggedAnimationClip.SampleRate;

            TagAnnotation tag = TagElement.m_Tag;

            Asset asset = taggedAnimationClip.Asset;

            Mode mode = m_Mode;
            if (m_OverrideMode == Mode.None)
            {
                float mousePos = evt.mousePosition.x;
                float fromStart = Math.Abs(mousePos - TagElement.worldBound.x);
                float fromEnd = Math.Abs(mousePos - TagElement.worldBound.xMax);
                // If the tag element is this small it will be too difficult to accurately grab the center and even accurately pick either end
                // we'll figure out which side of the tag is closest and assume the user meant to click that side.
                if (TagElement.layout.width <= 14f)
                {
                    if (fromStart <= fromEnd)
                    {
                        mode = Mode.StartTime;
                    }
                    else
                    {
                        mode = Mode.Duration;
                    }

                    m_OverrideMode = mode;
                }
            }
            else
            {
                mode = m_OverrideMode;
            }

            float newTime = float.NaN;

            float dragPosition = position.x;
            bool canPreview = timeline.CanPreview();

            // when we're dragging the entire tag we need to snap the edges of the tag, not the mouse position so we'll handle that inside its switch case
            if (mode != Mode.Body)
            {
                if (m_Target.SnapValid(dragPosition))
                {
                    return;
                }

                TryToSnap(dragPosition, direction, taggedAnimationClip.SampleRate, out newTime, snapToPlayhead: !canPreview);
            }

            float previewTime = -1f;

            switch (mode)
            {
                case Mode.StartTime:
                    float endTime = tag.startTime + tag.duration;
                    Undo.RecordObject(asset, "Drag Tag start");

                    float delta = newTime - tag.startTime;
                    if (tag.duration - delta < TagElement.MinTagDuration)
                    {
                        tag.startTime = endTime - TagElement.MinTagDuration;
                        tag.duration = TagElement.MinTagDuration;
                    }
                    else
                    {
                        tag.startTime = newTime;
                        tag.duration = endTime - tag.startTime;
                    }

                    TagElement.Timeline.ShowStartGuideline(tag.startTime);
                    previewTime = newTime;

                    break;

                case Mode.Duration:
                    Undo.RecordObject(asset, "Drag Tag Duration");
                    float newDuration = newTime - tag.startTime;
                    if (newDuration <= TagElement.MinTagDuration)
                    {
                        tag.duration = TagElement.MinTagDuration;
                    }
                    else
                    {
                        tag.duration = newDuration;
                    }

                    TagElement.Timeline.ShowEndGuideline(tag.EndTime);
                    previewTime = tag.EndTime;
                    break;

                case Mode.Body:
                    Undo.RecordObject(asset, "Drag Tag");
                    newTime = m_Target.Timeline.WorldPositionToTime(position.x);
                    if (TagElement.Timeline.TimelineUnits == TimelineViewMode.frames)
                    {
                        newTime = (float)TimelineUtility.RoundToFrame(newTime - m_TimeOffsetAtMouseDown, framerate);
                    }
                    else
                    {
                        newTime -= m_TimeOffsetAtMouseDown;
                    }

                    if (!m_Target.SnapValid(newTime) && !m_Target.SnapValid(newTime + tag.duration))
                    {
                        float mouseOffset = TagElement.Timeline.TimeToWorldPos(newTime);
                        if (!TryToSnap(mouseOffset, direction, taggedAnimationClip.SampleRate, out newTime, snapToPlayhead: !canPreview))
                        {
                            float durationPos = TagElement.Timeline.TimeToWorldPos(newTime + tag.duration);
                            //Try to snap via the end of the current tag
                            if (TryToSnap(durationPos, direction, taggedAnimationClip.SampleRate, out float candidateEndTime, snapToPlayhead: !canPreview))
                            {
                                newTime = candidateEndTime - tag.duration;
                            }
                        }

                        tag.startTime = newTime;

                        TagElement.Timeline.ShowGuidelines(tag.startTime, tag.EndTime);
                    }

                    previewTime = tag.EndTime;

                    break;
            }

            if (canPreview && previewTime >= 0f)
            {
                TagElement.Timeline.SetActiveTime(previewTime);
            }

            tag.NotifyChanged();
            TagElement.Timeline.SendTagModified();

            evt.StopPropagation();

            TagElement.ShowManipulationLabel();
        }

        void OnMouseMoveReorderEvent(MouseMoveEvent evt)
        {
            if (m_Mode == Mode.Body)
            {
                if (!TagElement.ContainsPoint(TagElement.WorldToLocal(evt.mousePosition)) &&
                    Math.Abs(m_MousePreviousPosition.y - evt.mousePosition.y) >= (TagElement.layout.height / 2f) + 3f)
                {
                    bool draggingUp = evt.mousePosition.y < m_MouseDownPosition.y;
                    if (TagElement.Timeline.ReorderTimelineElements(TagElement, draggingUp ? -1 : 1))
                    {
                        m_MousePreviousPosition = evt.mousePosition;
                        m_MouseDownPosition = evt.mousePosition;
                    }
                }
            }
        }

        protected override void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(evt))
            {
                m_Active = false;
                return;
            }

            base.OnMouseUpEvent(evt);

            TagElement.Select(evt);
            m_OverrideMode = Mode.None;
        }
    }
}
