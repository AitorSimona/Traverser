using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using Unity.SnapshotDebugger.Editor;
using UnityEngine;
using System;

namespace Unity.Kinematica.Editor
{
    internal class AnimationTimelineDrawer : ITimelineDebugDrawer
    {
        internal struct AnimationFrameIdentifier
        {
            public int providerIdentifier;
            public int animIndex;
            public int animFrameIndex;
            public int mouseX;

            public bool IsValid
            {
                get
                {
                    return animIndex >= 0 && animFrameIndex >= 0;
                }
            }

            public static AnimationFrameIdentifier CreateInvalid()
            {
                return new AnimationFrameIdentifier()
                {
                    providerIdentifier = 0,
                    animIndex = -1,
                    animFrameIndex = -1,
                };
            }

            public bool Equals(AnimationFrameIdentifier rhs)
            {
                return (!IsValid && !rhs.IsValid) ||
                    (providerIdentifier == rhs.providerIdentifier && animIndex == rhs.animIndex && animFrameIndex == rhs.animFrameIndex);
            }
        }

        public AnimationTimelineDrawer()
        {
            m_SelectedAnimFrame = AnimationFrameIdentifier.CreateInvalid();
        }

        public Type AggregateType => typeof(AnimationDebugRecord);

        public float GetDrawHeight(IFrameAggregate aggregate)
        {
            return GetAnimStateTimelineHeight((AnimationDebugRecord)aggregate);
        }

        public void Draw(FrameDebugProviderInfo providerInfo, IFrameAggregate aggregate, TimelineWidget.DrawInfo drawInfo)
        {
            AnimationDebugRecord animDebugRecord = (AnimationDebugRecord)aggregate;
            DrawAnimationTimeline(providerInfo, animDebugRecord, drawInfo);
        }

        public void OnPostDraw()
        {
            if (m_SelectedAnimFrame.IsValid)
            {
                AnimationDebugRecord animStateRecord = Debugger.frameDebugger.FindAggregate<AnimationDebugRecord>(m_SelectedAnimFrame.providerIdentifier);
                if (animStateRecord == null)
                {
                    m_SelectedAnimFrame = AnimationFrameIdentifier.CreateInvalid();
                    return;
                }

                Assert.IsTrue(m_SelectedAnimFrame.animIndex < animStateRecord.AnimationRecords.Count);
                if (m_SelectedAnimFrame.animIndex >= animStateRecord.AnimationRecords.Count)
                {
                    m_SelectedAnimFrame = AnimationFrameIdentifier.CreateInvalid();
                    return;
                }

                AnimationRecord animRecord = animStateRecord.AnimationRecords[m_SelectedAnimFrame.animIndex];

                Assert.IsTrue(m_SelectedAnimFrame.animFrameIndex < animRecord.animFrames.Count);
                if (m_SelectedAnimFrame.animFrameIndex >= animRecord.animFrames.Count)
                {
                    m_SelectedAnimFrame = AnimationFrameIdentifier.CreateInvalid();
                    return;
                }

                AnimationFrameInfo animFrame = animRecord.animFrames[m_SelectedAnimFrame.animFrameIndex];

                string label = $"{animRecord.animName}\nFrame:{animFrame.animFrame:0.00}\nWeight:{animFrame.weight:0.00}";
                Vector2 labelSize = TimelineWidget.GetLabelSize(label);

                Rect toolTipRect = new Rect(Event.current.mousePosition.x + 20, Event.current.mousePosition.y, labelSize.x + 5.0f, labelSize.y + 5.0f);

                TimelineWidget.DrawRectangleWithDetour(toolTipRect, kTooltipBackgroundColor, kTooltipDetourColor);
                TimelineWidget.DrawLabel(toolTipRect, label, kTooltipTextColor);

                m_SelectedAnimFrame = AnimationFrameIdentifier.CreateInvalid();
            }
        }

        private void DrawAnimationTimeline(FrameDebugProviderInfo providerInfo, AnimationDebugRecord animStateRecord, TimelineWidget.DrawInfo drawInfo)
        {
            if (animStateRecord.AnimationRecords.Count == 0)
            {
                return;
            }

            // Animations
            for (int i = 0; i < animStateRecord.AnimationRecords.Count; ++i)
            {
                DrawAnimationWidget(providerInfo, animStateRecord, i, drawInfo);
            }
        }

        private void DrawAnimationWidget(FrameDebugProviderInfo providerInfo, AnimationDebugRecord animStateRecord, int animIndex, TimelineWidget.DrawInfo drawInfo)
        {
            AnimationRecord animation = animStateRecord.AnimationRecords[animIndex];

            if (animation.endTime < drawInfo.timeline.startTime)
            {
                return;
            }

            if (animation.startTime > drawInfo.timeline.endTime)
            {
                return;
            }

            float startPosition = drawInfo.GetPixelPosition(animation.startTime);
            float endPosition = drawInfo.GetPixelPosition(animation.endTime);

            Rect drawRect = drawInfo.timeline.drawRect;

            drawRect.y += animation.rank * kAnimWidgetOffset;
            Rect animRect = new Rect(startPosition, drawRect.y, endPosition - startPosition, kAnimWidgetHeight);

            TimelineWidget.DrawRectangleWithDetour(animRect, kAnimWidgetBackgroundColor, kAnimWidgetDetourColor);

            int barStartPosition = Missing.truncToInt(startPosition) + 1;
            int maxBarPosition = Missing.truncToInt(endPosition);
            for (int i = 0; i < animation.animFrames.Count; ++i)
            {
                int barEndPosition = Missing.truncToInt(drawInfo.GetPixelPosition(animation.animFrames[i].endTime));
                if (barEndPosition > barStartPosition)
                {
                    float weight = animation.animFrames[i].weight;
                    if (weight < 1.0f)
                    {
                        Rect barRect = new Rect(barStartPosition, drawRect.y, barEndPosition - barStartPosition, (1.0f - weight) * kAnimWidgetHeight);
                        TimelineWidget.DrawRectangle(barRect, kAnimWidgetWeightColor);
                    }
                }
                barStartPosition = barEndPosition;
            }

            TimelineWidget.DrawLabelInsideRectangle(animRect, animation.animName, kAnimWidgetTextColor);

            // check if mouse is hovering the anim widget
            if (endPosition > startPosition && animRect.Contains(Event.current.mousePosition))
            {
                float mouseNormalizedTime = (Event.current.mousePosition.x - startPosition) / (endPosition - startPosition);
                float mouseTime = animation.startTime + mouseNormalizedTime * (animation.endTime - animation.startTime);

                float curStartTime = animation.startTime;
                for (int i = 0; i < animation.animFrames.Count; ++i)
                {
                    float curEndTime = animation.animFrames[i].endTime;
                    if (curStartTime <= mouseTime && mouseTime <= curEndTime)
                    {
                        m_SelectedAnimFrame.providerIdentifier = providerInfo.uniqueIdentifier;
                        m_SelectedAnimFrame.animIndex = animIndex;
                        m_SelectedAnimFrame.animFrameIndex = i;
                        m_SelectedAnimFrame.mouseX = Missing.truncToInt(Event.current.mousePosition.x);
                        return;
                    }
                    curStartTime = curEndTime;
                }
            }
        }

        private float GetAnimStateTimelineHeight(AnimationDebugRecord animStateRecord)
        {
            return animStateRecord.AnimationRecords.Count > 0 ? animStateRecord.NumLines * kAnimWidgetOffset : 0.0f;
        }

        AnimationFrameIdentifier m_SelectedAnimFrame; // anim frame hovered by mouse

        static readonly float kAnimWidgetHeight = 25.0f;
        static readonly float kAnimWidgetOffset = 30.0f;

        static readonly Color kTooltipBackgroundColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);
        static readonly Color kTooltipDetourColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
        static readonly Color kTooltipTextColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

        static readonly Color kAnimWidgetBackgroundColor = new Color(0.2f, 0.2f, 0.23f, 1.0f);
        static readonly Color kAnimWidgetDetourColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
        static readonly Color kAnimWidgetTextColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);
        static readonly Color kAnimWidgetWeightColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
    }
}
