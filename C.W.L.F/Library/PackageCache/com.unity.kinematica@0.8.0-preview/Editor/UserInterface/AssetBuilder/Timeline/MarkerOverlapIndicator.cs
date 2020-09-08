using System;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class MarkerOverlapIndicator : VisualElement, ITimelineElement
    {
        const string k_MultiMarkerStyleClass = "markerOverlap";

        public MarkerOverlapIndicator(MarkerTrack track, float pos)
        {
            AddToClassList(k_MultiMarkerStyleClass);
            style.position = Position.Absolute;

            Image img = new Image();
            img.AddToClassList("markerOverlapImage");
            img.pickingMode = PickingMode.Ignore;
            pickingMode = PickingMode.Ignore;
            Add(img);

            m_Track = track;
            Value = pos;
        }

        public void Unselect() {}

        MarkerTrack m_Track;
        float m_Value;

        public float Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_Value, value))
                {
                    m_Value = value;
                    Reposition();
                }
            }
        }

        public void Reposition()
        {
            style.left = Value;
        }

        public object Object => null;
        public float StartTime { get { throw new NotImplementedException();} }

        public Timeline Timeline
        {
            get
            {
                return m_Track.m_Owner;
            }
        }
    }
}
