using System.Collections.Generic;
using System.Globalization;
using Unity.Kinematica.Editor.TimelineUtils;
using Unity.SnapshotDebugger.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    partial class TimeRuler
    {
        class Styles2
        {
            public GUIStyle timelineTick = "AnimationTimelineTick";
            public GUIStyle labelTickMarks = "CurveEditorLabelTickMarks";
            public GUIStyle playhead = "AnimationPlayHead";
        }

        internal const int kTickRulerDistMin = 3; // min distance between ruler tick marks before they disappear completely
        internal const int kTickRulerDistFull = 80; // distance between ruler tick marks where they gain full strength
        internal const int kTickRulerDistLabel = 60; // min distance between ruler tick mark labels
        internal const float kTickRulerHeightMax = 0.7f; // height of the ruler tick marks when they are highest
        internal const float kTickRulerFatThreshold = 0.5f; // size of ruler tick marks at which they begin getting fatter

        static Styles2 k_TimeAreaStyles;
        static Styles2 TimeAreaStyles
        {
            get { return k_TimeAreaStyles ?? (k_TimeAreaStyles = new Styles2()); }
        }

        const float k_RangeMin = Mathf.NegativeInfinity;
        const float k_RangeMax = Mathf.Infinity;

        TickHandler m_TickHandler;
        List<float> m_TickCache = new List<float>(1000);

        void DrawRuler(
            Rect position,
            float frameRate,
            TimelineViewMode timeFormat)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.xMax) || float.IsNaN(worldBound.x) || float.IsNaN(worldBound.xMax))
            {
                return;
            }

            Rect shownArea = new Rect(m_DrawInfo.layout.startTime, -90f, m_DrawInfo.layout.Duration, .05f);
            Color backupCol = GUI.color;
            GUI.BeginGroup(position);

            Color tempBackgroundColor = GUI.backgroundColor;

            m_TickHandler.SetRanges(m_DrawInfo.layout.startTime, m_DrawInfo.layout.endTime, worldBound.x, worldBound.xMax);
            m_TickHandler.SetTickStrengths(kTickRulerDistMin, kTickRulerDistFull, true);

            int labelLevel = m_TickHandler.GetLevelWithMinSeparation(kTickRulerDistLabel);

            if (Event.current.type == EventType.Repaint)
            {
                var originalColor = GUI.color;
                // Draw tick markers of various sizes
                for (int tickLevel = 0; tickLevel < m_TickHandler.tickLevels; tickLevel++)
                {
                    float strength = m_TickHandler.GetStrengthOfLevel(tickLevel) * .9f;
                    m_TickCache.Clear();
                    m_TickHandler.GetTicksAtLevel(tickLevel, true, m_TickCache);
                    for (int i = 0; i < m_TickCache.Count; i++)
                    {
                        if (m_TickCache[i] < k_RangeMin || m_TickCache[i] > k_RangeMax)
                        {
                            continue;
                        }

                        float frame = Mathf.Round(m_TickCache[i] * frameRate);

                        float height = position.height * Mathf.Min(1, strength) * kTickRulerHeightMax;
                        float x = FrameToPixel(frame, frameRate, position, shownArea);

                        // Draw line
                        float minY = position.height - height + 0.5f;
                        float maxY = position.height - 0.5f;

                        GUI.color = tickLevel >= labelLevel ? TimelineWidget.k_MajorTickColor : TimelineWidget.k_MinorTickColor;
                        Rect r = new Rect(new Vector2(x - .5f, minY),
                            new Vector2(1, maxY - minY)
                        );
                        GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
                    }
                }

                GUI.color = originalColor;
            }

            // Draw tick labels
            m_TickCache.Clear();
            m_TickHandler.GetTicksAtLevel(labelLevel, false, m_TickCache);
            for (int i = 0; i < m_TickCache.Count; i++)
            {
                if (m_TickCache[i] < k_RangeMin || m_TickCache[i] > k_RangeMax)
                    continue;

                float frame = Mathf.Round(m_TickCache[i] * frameRate);
                // Important to take floor of positions of GUI stuff to get pixel correct alignment of
                // stuff drawn with both GUI and Handles/GL. Otherwise things are off by one pixel half the time.
                float labelPos = FrameToPixel(frame, frameRate, position, shownArea);
                string label = TimelineUtility.GetTimeString(timeFormat, m_TickCache[i], (int)frameRate);
                var labelSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(label));
                float x = labelPos + 3;
                float y = -1;
                float w = labelSize.x + 2;
                float h = labelSize.y;

                GUI.Label(new Rect(x, y, w, h),
                    label,
                    TimeAreaStyles.timelineTick);
            }

            GUI.EndGroup();

            GUI.backgroundColor = tempBackgroundColor;
            GUI.color = backupCol;
        }

        static float FrameToPixel(float i, float frameRate, Rect rect, Rect theShownArea)
        {
            return (i - theShownArea.xMin * frameRate) * rect.width / (theShownArea.width * frameRate);
        }
    }
}
