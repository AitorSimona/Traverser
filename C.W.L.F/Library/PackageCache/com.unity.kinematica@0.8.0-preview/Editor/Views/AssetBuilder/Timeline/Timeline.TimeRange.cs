using UnityEngine;
using UnityEngine.Assertions.Comparers;

namespace Unity.Kinematica.Editor
{
    delegate void RangeChangeHandler(Vector2 newRange);
    interface ITimeRange
    {
        /// <summary>
        /// Current Range Start
        /// </summary>
        float StartTime { get;}

        /// <summary>
        /// Current Range End
        /// </summary>
        float EndTime { get;}

        /// <summary>
        /// Range Minimum Limit
        /// </summary>
        float MinimumTime { get;}

        /// <summary>
        /// Range Minimum Limit
        /// </summary>
        float MaximumTime { get;}

        /// <summary>
        /// Minimum Difference between StartTime and EndTime
        /// </summary>
        float MinimumTimeResolution { get; }

        /// <summary>
        /// Set the time range
        /// </summary>
        /// <param name="timeRange">Time range, with the format [StartTime, EndTime]</param>
        /// <returns>Resulting time range as constrained by MinimumTime, MaximumTime and MinimumResolution</returns>
        Vector2 SetTimeRange(Vector2 timeRange);

        /// <summary>
        /// Returns the current Time Range
        /// </summary>
        /// <returns>Current Time Range, with the format [StartTime, EndTime]</returns>
        Vector2 GetTimeRange();

        /// <summary>
        /// Event invoked when
        /// </summary>
        /// <returns>Current Time Range, with the format [StartTime, EndTime]</returns>
        event RangeChangeHandler OnRangeChanged;
    }
    partial class Timeline
    {
        class TimeRange : ITimeRange
        {
            float m_StartTime;
            float m_EndTime;
            float m_ActiveTime;
            float m_DebugTime;
            readonly float m_MinimumRange;
            readonly float m_MinimumTime;
            readonly float m_MaximumTime;
            readonly float m_FrameRate;

            public event RangeChangeHandler OnRangeChanged;

            static float Constrain(float value, float low, float high)
            {
                return Mathf.Max(Mathf.Min(value, high), low);
            }

            public float Limit(float value)
            {
                return Constrain(value, m_MinimumTime, m_MaximumTime);
            }

            public TimeRange(float minimumTime, float maximumTime, float minimumRange, float frameRate)
            {
                m_FrameRate = frameRate;
                minimumTime = (float)TimelineUtility.RoundToFrame(minimumTime, frameRate);
                maximumTime = (float)TimelineUtility.RoundToFrame(maximumTime, frameRate);
                minimumRange = (float)TimelineUtility.RoundToFrame(minimumRange, frameRate);
                m_StartTime = minimumTime;
                m_EndTime = maximumTime;
                m_ActiveTime = m_StartTime;
                m_DebugTime = m_StartTime;
                m_MinimumRange = minimumRange;
                m_MinimumTime = minimumTime;
                m_MaximumTime = maximumTime;
            }

            float RoundValue(float value)
            {
                return (float)TimelineUtility.RoundToFrame(value, m_FrameRate);
            }

            public float StartTime
            {
                get { return m_StartTime; }
                set
                {
                    if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_StartTime, value))
                    {
                        m_StartTime = Constrain(value, m_MinimumTime, m_EndTime - m_MinimumRange);
                    }
                }
            }

            public float EndTime
            {
                get { return m_EndTime; }
                set
                {
                    if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_EndTime, value))
                    {
                        m_EndTime = Constrain(value, m_StartTime + m_MinimumRange, m_MaximumTime);
                    }
                }
            }

            public float ActiveTime
            {
                get { return m_ActiveTime; }
            }

            public void SetActiveTime(float value, bool round = false)
            {
                if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_ActiveTime, value))
                {
                    if (round)
                    {
                        value = RoundValue(value);
                    }

                    m_ActiveTime = Constrain(value, m_MinimumTime, m_MaximumTime);
                }
            }

            public float DebugTime
            {
                get { return m_DebugTime; }
            }

            public void SetDebugTime(float value, bool round = false)
            {
                if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_DebugTime, value))
                {
                    if (round)
                    {
                        value = RoundValue(value);
                    }

                    m_DebugTime = Constrain(value, m_MinimumTime, m_MaximumTime);
                }
            }

            public float MinimumTime
            {
                get { return m_MinimumTime; }
            }

            public float MaximumTime
            {
                get { return m_MaximumTime; }
            }

            public float MinimumTimeResolution
            {
                get { return m_MinimumRange; }
            }

            public Vector2 GetTimeRange()
            {
                return new Vector2(m_StartTime, m_EndTime);
            }

            public Vector2 SetTimeRange(Vector2 timeRange)
            {
                Vector2 previousRange = GetTimeRange();
                StartTime = timeRange.x;
                EndTime = timeRange.y;
                Vector2 newRange = GetTimeRange();

                if (previousRange != newRange)
                {
                    OnRangeChanged?.Invoke(newRange);
                }

                return GetTimeRange();
            }

            public bool PanTimeRange(float diff)
            {
                if (diff < 0 && m_StartTime <= m_MinimumTime)
                {
                    return false;
                }

                if (diff > 0 && m_EndTime >= m_MaximumTime)
                {
                    return false;
                }

                Vector2 previousRange = GetTimeRange();
                float originalSpan = m_EndTime - m_StartTime;
                float expectedStart = m_StartTime + diff;
                float expectedEnd = m_EndTime + diff;

                StartTime = expectedStart;
                EndTime = expectedEnd;

                float newSpan = m_EndTime - m_StartTime;

                if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(originalSpan, newSpan))
                {
                    if (FloatComparer.s_ComparerWithDefaultTolerance.Equals(m_StartTime, m_MinimumTime))
                    {
                        EndTime = m_StartTime + originalSpan;
                    }
                    else
                    {
                        StartTime = m_EndTime - originalSpan;
                    }
                }

                Vector2 newRange = GetTimeRange();

                if (previousRange != newRange)
                {
                    OnRangeChanged?.Invoke(newRange);
                    return true;
                }

                return false;
            }
        }

        TimeRange m_TimeRange;

        void UpdateTimeRange()
        {
            float minTime = -1.0f;
            float maxTime = 2.0f;
            float frameRate = 60.0f;
            float clipLength = 0f;

            if (TaggedClip != null && TaggedClip.Valid)
            {
                frameRate = TaggedClip.SampleRate;
                clipLength = TaggedClip.DurationInSeconds;
                minTime = -clipLength;
                maxTime = 2 * clipLength;
            }

            float resolution = 2 / frameRate;
            float previousActiveTime = -1f;
            if (m_TimeRange != null)
            {
                m_TimeRange.OnRangeChanged -= OnTimeRangeChanged;
                previousActiveTime = m_TimeRange.ActiveTime;
            }

            m_TimeRange = new TimeRange(minTime, maxTime, resolution, frameRate);
            m_TimeRange.OnRangeChanged += OnTimeRangeChanged;

            Vector2 centerRange = new Vector2(-1, clipLength + 1);
            m_TimeRange.SetTimeRange(centerRange);
            // We need to force a rescaling here as SetTimeRange early outs if the previous/new ranges are the same
            RescaleToTimeRange(centerRange);

            if (previousActiveTime > 0 && TaggedClip != null)
            {
                SetActiveTime(Mathf.Clamp(previousActiveTime, 0f, TaggedClip.DurationInSeconds));
            }
            else
            {
                SetActiveTime(0.0f);
            }

            SetDebugTime(0.0f);
        }

        void OnTimeRangeChanged(Vector2 newRange)
        {
            RescaleToTimeRange(newRange);
        }

        void RescaleToTimeRange(Vector2 newRange)
        {
            if (float.IsNaN(m_TimelineScrollableArea.parent.worldBound.width))
            {
                return;
            }

            float currentMaximumRange = m_TimeRange.MaximumTime - m_TimeRange.MinimumTime;
            float newW = newRange.y - newRange.x;
            float newRatio = currentMaximumRange / newW;
            float startRatio = (newRange.x - m_TimeRange.MinimumTime) / currentMaximumRange;

            float containerWidth = m_TimelineScrollableArea.parent.worldBound.width;
            float newWidth = containerWidth * newRatio;
            m_TimelineScrollableArea.style.width = newWidth;
            m_GutterTracks.style.width = newWidth;
            m_TimelineScrollableArea.transform.position = new Vector3(-newWidth * startRatio, m_TimelineScrollableArea.transform.position.y);
            m_GutterTracks.transform.position = new Vector3(-newWidth * startRatio, m_GutterTracks.transform.position.y);
        }
    }
}
