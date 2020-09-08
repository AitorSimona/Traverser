using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    interface ITimelineElement
    {
        void Unselect();
        void Reposition();
        System.Object Object { get; }
        float StartTime { get; }
    }


    abstract class TimeRangeElement : VisualElement, ITimelineElement
    {
        const float k_LabelCutoff = 36f; //TODO - ellipses

        protected readonly VisualElement m_LabelContainer;
        protected readonly Label m_Label;
        ITimelineElement m_TimelineElementImplementation;

        public Track Track { get; set; }

        public Timeline Timeline
        {
            get { return Track.m_Owner; }
        }

        protected TimeRangeElement(Track track)
        {
            Track = track;

            m_LabelContainer = new VisualElement();
            m_LabelContainer.AddToClassList("timelineTimeRangeLabelContainer");
            m_Label = new Label();
            m_Label.AddToClassList("timelineTimeRangeLabel");
            m_LabelContainer.Add(m_Label);

            Add(m_LabelContainer);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public virtual void Unselect()
        {
        }

        public virtual void Reposition()
        {
            Resize();
        }

        public virtual object Object
        {
            get { throw new System.NotImplementedException(); }
        }

        public virtual float StartTime
        {
            get { throw new System.NotImplementedException(); }
        }

        public virtual float EndTime
        {
            get { throw new System.NotImplementedException(); }
        }

        public abstract void Resize();

        protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (layout.width - m_Label.layout.width < k_LabelCutoff)
            {
                m_Label.style.visibility = Visibility.Hidden;
            }
            else
            {
                m_Label.style.visibility = Visibility.Visible;
            }
        }
    }

    abstract class SnappingElement : TimeRangeElement
    {
        protected SnappingElement m_SnappedTo;

        protected SnappingElement(Track track) : base(track)
        {
        }

        public abstract float GetSnapPosition(float targetPosition);
        public abstract void ShowManipulationLabel();
        public abstract void HideManipulationLabel();

        public void SnapTo(SnappingElement target)
        {
            m_SnappedTo = target;
        }

        public void UnSnap()
        {
            m_SnappedTo = null;
        }

        public bool SnapValid(float position)
        {
            if (m_SnappedTo == null)
            {
                return false;
            }

            return (TimelineSnappingMouseManipulator.k_SnapTimeComparer.Equals(StartTime, m_SnappedTo.StartTime) ||
                TimelineSnappingMouseManipulator.k_SnapTimeComparer.Equals(EndTime, m_SnappedTo.EndTime) ||
                TimelineSnappingMouseManipulator.k_SnapTimeComparer.Equals(StartTime, m_SnappedTo.EndTime) ||
                TimelineSnappingMouseManipulator.k_SnapTimeComparer.Equals(EndTime, m_SnappedTo.StartTime)) &&
                Mathf.Abs(position - m_SnappedTo.GetSnapPosition(position)) < TimelineSnappingMouseManipulator.k_SnapMoveDelta;
        }
    }
}
