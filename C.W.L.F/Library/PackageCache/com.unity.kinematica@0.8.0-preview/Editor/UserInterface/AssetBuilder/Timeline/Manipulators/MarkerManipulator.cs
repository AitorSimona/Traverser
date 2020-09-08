using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class MarkerManipulator : TimelineSnappingMouseManipulator
    {
        MarkerElement m_MarkerElement;

        MarkerElement MarkerElement
        {
            get { return m_MarkerElement ?? (m_MarkerElement = m_Target as MarkerElement); }
        }

        public MarkerManipulator(MarkerElement markerElement) : base(markerElement) {}

        bool m_StartingDrag = false;
        bool m_MarkerDragged = false;

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

            m_StartingDrag = true;
            m_MarkerDragged = false;

            if (!MarkerElement.m_Selected)
            {
                MarkerElement.SelectMarkerElement(MarkerElement, Utility.CheckMultiSelectModifier(evt));
            }
        }

        protected override void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            if (!m_Active || !target.HasMouseCapture() || EditorApplication.isPlaying)
            {
                return;
            }

            Timeline timeline = MarkerElement.Timeline;
            Asset asset = timeline.TargetAsset;
            TaggedAnimationClip clip = timeline.TaggedClip;

            if (m_StartingDrag)
            {
                Undo.RecordObject(asset, "Moving marker");
                m_StartingDrag = false;
            }

            Vector2 position = evt.mousePosition;

            MouseDirection direction = GetMouseDirection(position);

            m_MousePreviousPosition = position;

            float framerate = clip.SampleRate;
            float dragPosition = position.x;

            if (m_Target.SnapValid(dragPosition))
            {
                return;
            }

            bool canPreview = m_Target.Timeline.CanPreview();
            TryToSnap(dragPosition, direction, framerate, out float newTime, snapToPlayhead: !canPreview);

            if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(MarkerElement.value, newTime))
            {
                m_MarkerDragged = true;
                MarkerElement.value = newTime;
                MarkerElement.ShowManipulationLabel();

                if (canPreview)
                {
                    m_Target.Timeline.SetActiveTime(newTime);
                }
            }

            evt.StopPropagation();
        }

        protected override void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(evt))
            {
                return;
            }

            base.OnMouseUpEvent(evt);

            if (!m_MarkerDragged)
            {
                MarkerElement.SelectMarkerElement(MarkerElement, Utility.CheckMultiSelectModifier(evt));
            }

            m_MarkerDragged = false;
            m_StartingDrag = false;
        }
    }
}
