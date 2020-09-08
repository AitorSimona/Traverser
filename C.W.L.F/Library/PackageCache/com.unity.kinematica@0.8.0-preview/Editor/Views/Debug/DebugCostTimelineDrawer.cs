using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using Unity.SnapshotDebugger.Editor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Unity.Kinematica.Editor
{
    internal class DebugCostTimelineDrawer : ITimelineDebugDrawer
    {
        public static bool enabled = false;

        public Type AggregateType => typeof(DebugCostAggregate);

        public float GetDrawHeight(IFrameAggregate aggregate)
        {
            return enabled ? kPixelHeight : 0.0f;
        }

        public void Draw(FrameDebugProviderInfo providerInfo, IFrameAggregate aggregate, TimelineWidget.DrawInfo drawInfo)
        {
            if (!enabled)
            {
                return;
            }

            DrawCostTimeline(providerInfo, aggregate, drawInfo);
            DisplayOptions(drawInfo);
            DisplayYAxis(drawInfo);
            DisplayMouseCost(drawInfo);
        }

        void DisplayOptions(TimelineWidget.DrawInfo drawInfo)
        {
            Rect backgroundRect = new Rect(drawInfo.layout.drawRect.x, drawInfo.timeline.drawRect.y, kOptionsRectWidth, drawInfo.timeline.drawRect.height);
            TimelineWidget.DrawRectangle(backgroundRect, kOptionsBackgroundColor);

            Rect rect = drawInfo.timeline.drawRect;
            rect.x = drawInfo.layout.drawRect.x + kTextHorizontalMargin;
            rect.y += kTextVerticalSpacing;
            rect.width = kOptionsRectWidth;
            rect.height = kTextRectHeight;

            GUIStyle style = GUI.skin.toggle;

            style.normal.textColor = kTotalCostColor;
            m_bDisplayTotalCost = GUI.Toggle(rect, m_bDisplayTotalCost, new GUIContent("Total cost"), style);
            rect.y += kTextVerticalSpacing;

            style.normal.textColor = kPoseCostColor;
            m_bDisplayPoseCost = GUI.Toggle(rect, m_bDisplayPoseCost, new GUIContent("Pose cost"), style);
            rect.y += kTextVerticalSpacing;

            style.normal.textColor = kTrajectoryCostColor;
            m_bDisplayTrajectoryCost = GUI.Toggle(rect, m_bDisplayTrajectoryCost, new GUIContent("Trajectory cost"), style);
        }

        void DisplayYAxis(TimelineWidget.DrawInfo drawInfo)
        {
            float startX = math.max(drawInfo.layout.drawRect.x + kOptionsRectWidth, drawInfo.timeline.drawRect.x);

            Rect maxVal = new Rect(startX + kAxisHorizontalTextOffset, drawInfo.timeline.drawRect.y, drawInfo.timeline.drawRect.width, kTextRectHeight);
            GUIStyle style = new GUIStyle();
            style.normal.textColor = kAxisColor;

            GUI.Label(maxVal, new GUIContent($"{m_MaxCost:0.00}"), style);

            maxVal.y += drawInfo.timeline.drawRect.height - kTextRectHeight;
            GUI.Label(maxVal, new GUIContent("0"), style);


            TimelineWidget.DrawRectangle(new Rect(startX + kAxisHorizontalTextOffset * 0.5f, drawInfo.timeline.drawRect.y, 1.0f, drawInfo.timeline.drawRect.height), kAxisColor);
            TimelineWidget.DrawRectangle(new Rect(startX + kAxisHorizontalLineMargin, drawInfo.timeline.drawRect.y, kAxisHorizontalTextOffset - kAxisHorizontalLineMargin, 1.0f), kAxisColor);
            TimelineWidget.DrawRectangle(new Rect(startX + kAxisHorizontalLineMargin, drawInfo.timeline.drawRect.y + drawInfo.timeline.drawRect.height - 1, kAxisHorizontalTextOffset - kAxisHorizontalLineMargin, 1.0f), kAxisColor);
        }

        void DisplayMouseCost(TimelineWidget.DrawInfo drawInfo)
        {
            if (m_MouseCost >= 0.0f)
            {
                float normalizedY = 1.0f - math.min(m_MouseCost / m_MaxCost, 1.0f);
                float y = drawInfo.timeline.drawRect.height * normalizedY + kTextRectHeight;
                y = math.clamp(y, 0, drawInfo.timeline.drawRect.height - kTextRectHeight);

                Rect mouseRect = new Rect(drawInfo.GetPixelPosition(m_MouseTime), drawInfo.timeline.drawRect.y + y, kMouseCostTextWidth, kTextRectHeight);
                GUIStyle style = new GUIStyle();
                style.normal.textColor = kAxisColor;

                GUI.Label(mouseRect, new GUIContent($"{m_MouseCost:0.000}"), style);
            }
        }

        void DrawCostTimeline(FrameDebugProviderInfo providerInfo, IFrameAggregate aggregate, TimelineWidget.DrawInfo drawInfo)
        {
            Rect selectedRect = drawInfo.timeline.drawRect;
            int width = (int)selectedRect.width;
            int height = (int)selectedRect.height;

            if (width * height == 0)
            {
                return;
            }

            if (aggregate.IsEmpty)
            {
                return;
            }

            IMotionSynthesizerProvider synthesizerProvider = providerInfo.provider as IMotionSynthesizerProvider;
            if (synthesizerProvider == null || !synthesizerProvider.IsSynthesizerInitialized)
            {
                return;
            }

            DebugMemory debugMemory = synthesizerProvider.Synthesizer.Ref.ReadDebugMemory;

            foreach (SelectedFrameDebugProvider selected in Debugger.frameDebugger.Selection)
            {
                if (selected.providerInfo.uniqueIdentifier == providerInfo.uniqueIdentifier)
                {
                    CreateAndClearTexture(width, height);

                    if (selected.metadata != null)
                    {
                        DebugIdentifier selectedIdentifier = (DebugIdentifier)selected.metadata;
                        DebugReference reference = debugMemory.FindObjectReference(selectedIdentifier);

                        if (reference.IsValid)
                        {
                            object selectedObject = debugMemory.ReadObjectGeneric(reference);

                            if (selectedObject is IMotionMatchingQuery query)
                            {
                                DrawCostTimeline(query.DebugName.GetHashCode(), (DebugCostAggregate)aggregate, drawInfo);
                            }

                            if (selectedObject is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
                    }

                    m_CacheTexture.SetPixels(m_CachePixels);
                    m_CacheTexture.Apply();

                    GUI.DrawTexture(selectedRect, m_CacheTexture);

                    return;
                }
            }
        }

        void DrawCostTimeline(int queryIdentifier, DebugCostAggregate aggregate, TimelineWidget.DrawInfo drawInfo)
        {
            m_MouseTime = GetMouseTime(drawInfo);
            m_MouseCost = -1.0f;

            (DebugCostAggregate.Record, RecordCoordinates) ? prevRecord = null;

            m_MaxCost = kDefaultMaxCost;

            for (int index = 0; index < aggregate.NumRecords; ++index)
            {
                DebugCostAggregate.Record record = aggregate.GetRecord(index);

                if (record.queryIdentifier != queryIdentifier ||
                    record.endTime <= drawInfo.timeline.startTime)
                {
                    continue;
                }

                if (record.startTime >= drawInfo.timeline.endTime)
                {
                    break;
                }

                m_MaxCost = math.max(m_MaxCost, record.poseCost);
                if (record.trajectoryCost >= 0.0f)
                {
                    m_MaxCost = math.max(m_MaxCost, record.TotalCost);
                }

                if (m_MouseTime >= 0.0f && m_MouseTime >= record.startTime && m_MouseTime <= record.endTime)
                {
                    if (m_bDisplayTotalCost)
                    {
                        m_MouseCost = record.TotalCost;
                    }
                    else if (m_bDisplayPoseCost)
                    {
                        m_MouseCost = record.poseCost;
                    }
                    else if (m_bDisplayTrajectoryCost)
                    {
                        m_MouseCost = record.trajectoryCost;
                    }
                    else
                    {
                        m_MouseCost = -1.0f;
                    }
                }
            }

            for (int index = 0; index < aggregate.NumRecords; ++index)
            {
                DebugCostAggregate.Record record = aggregate.GetRecord(index);

                if (record.queryIdentifier != queryIdentifier ||
                    record.endTime <= drawInfo.timeline.startTime)
                {
                    continue;
                }

                if (record.startTime >= drawInfo.timeline.endTime)
                {
                    break;
                }

                RecordCoordinates recordCoordinates = GetRecordCoordinates(record, drawInfo);

                if (prevRecord.HasValue && prevRecord.Value.Item1.endTime == record.startTime)
                {
                    DrawRecord(recordCoordinates, prevRecord.Value.Item2, drawInfo);
                }
                else
                {
                    DrawRecord(recordCoordinates, drawInfo);
                }
                prevRecord = (record, recordCoordinates);
            }
        }

        public void OnPostDraw()
        {
        }

        void CreateAndClearTexture(int width, int height)
        {
            if (m_CacheTexture == null || m_CacheTexture.width != width || m_CacheTexture.height != height)
            {
                m_CacheTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                m_CachePixels = new Color[width * height];
            }

            for (int i = 0; i < m_CachePixels.Length; ++i)
            {
                m_CachePixels[i] = kBackgroundColor;
            }
        }

        RecordCoordinates GetRecordCoordinates(DebugCostAggregate.Record record, TimelineWidget.DrawInfo drawInfo)
        {
            return new RecordCoordinates()
            {
                startX = ClampPosX((int)math.round(drawInfo.GetPixelPosition(record.startTime) - drawInfo.timeline.drawRect.x)),
                endX = ClampPosX((int)math.round(drawInfo.GetPixelPosition(record.endTime) - drawInfo.timeline.drawRect.x)),
                poseY = ClampPosY((int)math.round(math.min(record.poseCost / m_MaxCost, 1.0f) * m_CacheTexture.height)),
                trajectoryY = record.trajectoryCost >= 0.0f ? ClampPosY((int)math.round(math.min(record.trajectoryCost / m_MaxCost, 1.0f) * m_CacheTexture.height)) : -1
            };
        }

        void DrawRecord(RecordCoordinates recordCoordinates, RecordCoordinates prevRecordCoordinates, TimelineWidget.DrawInfo drawInfo)
        {
            DrawRecord(recordCoordinates, drawInfo);

            int prevTotalY = prevRecordCoordinates.poseY;
            int totalY = recordCoordinates.poseY;

            if (recordCoordinates.trajectoryY >= 0)
            {
                if (m_bDisplayTrajectoryCost)
                {
                    DrawVerticalLine(new int2(prevRecordCoordinates.endX, prevRecordCoordinates.trajectoryY), new int2(recordCoordinates.startX, recordCoordinates.trajectoryY), kTrajectoryCostColor);
                }

                prevTotalY += prevRecordCoordinates.trajectoryY;
                totalY += recordCoordinates.trajectoryY;
            }

            if (m_bDisplayPoseCost)
            {
                DrawVerticalLine(new int2(prevRecordCoordinates.endX, prevRecordCoordinates.poseY), new int2(recordCoordinates.startX, recordCoordinates.poseY), kPoseCostColor);
            }

            if (m_bDisplayTotalCost)
            {
                DrawVerticalLine(new int2(prevRecordCoordinates.endX, prevTotalY), new int2(recordCoordinates.startX, totalY), kTotalCostColor);
            }
        }

        void DrawRecord(RecordCoordinates recordCoordinates, TimelineWidget.DrawInfo drawInfo)
        {
            int totalY = recordCoordinates.poseY;

            if (recordCoordinates.trajectoryY >= 0)
            {
                totalY += recordCoordinates.trajectoryY;

                if (m_bDisplayTrajectoryCost)
                {
                    for (int posX = recordCoordinates.startX; posX <= recordCoordinates.endX; ++posX)
                    {
                        GetPixel(posX, recordCoordinates.trajectoryY) = kTrajectoryCostColor;
                    }
                }
            }

            for (int posX = recordCoordinates.startX; posX <= recordCoordinates.endX; ++posX)
            {
                if (m_bDisplayPoseCost)
                {
                    GetPixel(posX, recordCoordinates.poseY) = kPoseCostColor;
                }

                if (m_bDisplayTotalCost)
                {
                    GetPixel(posX, totalY) = kTotalCostColor;
                }
            }
        }

        void DrawVerticalLine(int2 from, int2 to, Color color)
        {
            int numPixels = math.abs(to.y - from.y);
            if (numPixels == 0)
            {
                return;
            }

            int direction = (int)math.sign(to.y - from.y);

            float step = (to.x - from.x) / (float)numPixels;

            for (int i = 0; i < numPixels; ++i)
            {
                int posX = (int)math.round(from.x + step * i);
                int posY = from.y + i * direction;

                GetPixel(posX, posY) = color;
            }
        }

        float GetMouseTime(TimelineWidget.DrawInfo drawInfo)
        {
            Rect costTimelineRect = drawInfo.layout.drawRect;
            costTimelineRect.x += kOptionsRectWidth;

            if (costTimelineRect.Contains(Event.current.mousePosition))
            {
                return drawInfo.GetTime(Event.current.mousePosition.x);
            }

            return -1.0f;
        }

        struct RecordCoordinates
        {
            public int startX;
            public int endX;
            public int poseY;
            public int trajectoryY;
        }

        int ClampPosX(int posX) => math.clamp(posX, 0, m_CacheTexture.width - 1);

        int ClampPosY(int posY) => math.clamp(posY, 0, m_CacheTexture.height - 1);

        ref Color GetPixel(int x, int y) => ref m_CachePixels[y * m_CacheTexture.width + x];

        Texture2D m_CacheTexture;
        Color[] m_CachePixels;
        float m_MaxCost;
        float m_MouseTime;
        float m_MouseCost;

        bool m_bDisplayTotalCost = true;
        bool m_bDisplayPoseCost = false;
        bool m_bDisplayTrajectoryCost = false;

        static readonly float kTextRectHeight = 20.0f;
        static readonly float kTextHorizontalMargin = 5.0f;
        static readonly float kTextVerticalSpacing = 20.0f;
        static readonly float kOptionsRectWidth = 120.0f;
        static readonly Color kOptionsBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

        static readonly Color kAxisColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
        static readonly float kAxisHorizontalTextOffset = 15.0f;
        static readonly float kAxisHorizontalLineMargin = 2.0f;
        static readonly float kMouseCostTextWidth = 50.0f;

        static readonly int kPixelHeight = 100;
        static readonly float kDefaultMaxCost = 0.25f;

        static readonly Color kBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
        static readonly Color kPoseCostColor = new Color(0.4f, 1.0f, 0.4f, 1.0f);
        static readonly Color kTrajectoryCostColor = new Color(0.4f, 0.4f, 1.0f, 1.0f);
        static readonly Color kTotalCostColor = new Color(0.9f, 0.9f, 1.0f, 1.0f);
    }
}
