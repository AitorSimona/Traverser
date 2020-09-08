using System;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    partial class Timeline
    {
        public float WidthMultiplier
        {
            get
            {
                if (TaggedClip == null || !TaggedClip.Valid)
                {
                    return 1f;
                }

                float duration = TaggedClip.DurationInSeconds * k_TimelineLengthMultiplier + SecondsBeforeZero;
                if (FloatComparer.AreEqual(duration, 0f, FloatComparer.kEpsilon) || float.IsNaN(m_TimelineScrollableArea.layout.width))
                {
                    return 1f;
                }

                return m_TimelineScrollableArea.layout.width / duration;
            }
        }

        void ResizeContents()
        {
            if (TaggedClip == null)
            {
                return;
            }

            foreach (Track t in m_TrackElements)
            {
                t.ResizeContents();
            }

            UpdatePlayheadPositions();
            AdjustTicks();
        }

        void UpdatePlayheadPositions(bool propagateToLabel = true)
        {
            //TODO - let the playhead itself handle this ...
            ActiveTick.style.left = TimeToLocalPos(ActiveTime);
            ActiveDebugTick.style.left = TimeToLocalPos(DebugTime);
            if (propagateToLabel)
            {
                SetFPSLabelText();
            }
        }

        void AdjustTicks()
        {
            if (TaggedClip == null)
            {
                return;
            }

            float startOfClipPos = TimeToLocalPos(0f);
            float endOfClipPos = TimeToLocalPos(TaggedClip.Clip.DurationInSeconds);

            m_ClipLengthBar.style.left = startOfClipPos;
            m_ClipLengthBar.style.width = endOfClipPos - startOfClipPos;

            m_ClipArea.style.left = startOfClipPos;
            m_ClipArea.style.width = endOfClipPos - startOfClipPos;
            m_ClipArea.style.height = m_TimelineScrollableArea.layout.height;
            AdjustTagBackgrounds();

            AnnotationsTrack annotationsTrack = null;
            float takenHeight = 0f;

            foreach (var track in m_TrackElements)
            {
                if (track is AnnotationsTrack at)
                {
                    annotationsTrack = at;
                }
                else
                {
                    takenHeight += track.layout.height;
                }
            }

            annotationsTrack.style.minHeight = Math.Max(m_ScrollViewContainer.layout.height - takenHeight, 0f);
        }

        void AdjustTagBackgrounds()
        {
            m_ClipArea.Clear();
            if (TaggedClip == null)
            {
                return;
            }

            for (var i = 0; i < TaggedClip.Tags.Count; ++i)
            {
                var background = new VisualElement();
                background.AddToClassList("tagElementBackground");
                m_ClipArea.Add(background);
            }
        }
    }
}
