using UnityEngine;

namespace Unity.Kinematica.Editor
{
    class MetricElement : TimeRangeElement
    {
        const float k_BorderWidth = 1f;

        class MetricBorderElement : TimeRangeElement
        {
            float m_OffsetTime;
            float m_Duration;

            public MetricBorderElement(float offsetTime, float duration, Track track) : base(track)
            {
                m_OffsetTime = offsetTime;
                m_Duration = duration;

                style.backgroundColor = Color.black;

                AddToClassList("metricBorderElement");
                SetEnabled(true);

                Resize();
            }

            public override void Resize()
            {
                float widthMultiplier = Timeline.WidthMultiplier;
                float start = m_OffsetTime * widthMultiplier;
                style.left = start;
                float end = (m_OffsetTime + m_Duration) * widthMultiplier;
                style.width = end - start;

                style.backgroundColor = Color.black;
                style.color = Color.black;
            }
        }
        public string MetricName { get; }

        public float m_StartTime;
        public float m_EndTime;

        MetricBorderElement m_LeftBorder;
        MetricBorderElement m_RightBorder;

        public MetricElement(string name, float start, float end, float activeStart, float activeEnd, bool emptyInterval, Track track) : base(track)
        {
            MetricName = name;
            float maxTime = Timeline.TaggedClip == null ? 1f : Timeline.TaggedClip.DurationInSeconds;
            m_StartTime = Mathf.Clamp(start, 0f, maxTime);
            m_EndTime = Mathf.Clamp(end, 0f, maxTime);

            AddToClassList("metricElement");
            SetEnabled(false);

            if (emptyInterval)
            {
                m_Label.text = "Too short !";
                style.backgroundColor = Color.red;
                tooltip = "The metric segment is too short, all its poses are discarded";
            }
            else
            {
                m_Label.text = MetricName;
                tooltip = MetricName;

                if (activeStart > start)
                {
                    m_LeftBorder = new MetricBorderElement(0.0f, activeStart - start, track);
                    Add(m_LeftBorder);
                }

                if (activeEnd < end && activeEnd > activeStart)
                {
                    float rightBorderStart = Mathf.Clamp(end - start - (end - activeEnd), 0f, end);
                    m_RightBorder = new MetricBorderElement(rightBorderStart, Mathf.Clamp(end - activeEnd, 0f, end - start) , track);
                    Add(m_RightBorder);
                }
            }

            Resize();
        }

        public override void Resize()
        {
            float start = Timeline.TimeToLocalPos(m_StartTime, Track);
            style.left = start;
            float end = Timeline.TimeToLocalPos(m_EndTime, Track);
            style.width = end - start - k_BorderWidth;

            if (m_LeftBorder != null)
            {
                m_LeftBorder.Resize();
            }

            if (m_RightBorder != null)
            {
                m_RightBorder.Resize();
            }
        }
    }
}
