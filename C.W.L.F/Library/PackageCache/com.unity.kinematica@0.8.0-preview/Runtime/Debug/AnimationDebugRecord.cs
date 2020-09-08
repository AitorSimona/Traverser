using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine;

namespace Unity.Kinematica
{
    internal class AnimationDebugRecord : IFrameAggregate
    {
        public AnimationDebugRecord()
        {
            m_AnimationRecords = new CircularList<AnimationRecord>();
            m_NumActiveAnims = 0;
            m_LinesEndTimes = new List<float>();
        }

        public CircularList<AnimationRecord> AnimationRecords => m_AnimationRecords;

        public int NumLines => m_LinesEndTimes.Count;

        public bool IsEmpty => m_AnimationRecords.Count == 0;

        public void AddRecords(List<IFrameRecord> records, float frameStartTime, float frameEndTime)
        {
            List<AnimationFrameDebugInfo> frameSnapshots = records.Select(r => (AnimationFrameDebugInfo)r).ToList();

            List<int> newSnapshots = new List<int>(); // snaphots from sequences that weren't already present in the records

            // Update blending-out sequences
            float deltaTime = frameEndTime - frameStartTime;
            for (int i = m_AnimationRecords.Count - m_NumActiveAnims; i < m_AnimationRecords.Count; ++i)
            {
                AnimationRecord animRecord = m_AnimationRecords[i];
                if (frameSnapshots.Any(s => s.sequenceIdentifier == animRecord.sequenceIdentifier))
                {
                    continue; // sequence will updated anyway with a new frame
                }

                if (animRecord.blendOutTime > 0.0f)
                {
                    // sequence is obsolete (no update this frame) but still require blending out
                    animRecord.blendOutTime -= deltaTime;

                    AnimationFrameInfo lastFrame = animRecord.animFrames[animRecord.animFrames.Count - 1];
                    float deltaWeight = deltaTime / animRecord.blendOutDuration;

                    frameSnapshots.Add(new AnimationFrameDebugInfo()
                    {
                        sequenceIdentifier = animRecord.sequenceIdentifier,
                        animName = animRecord.animName,
                        animFrame = lastFrame.animFrame,
                        weight = math.max(lastFrame.weight - deltaWeight, 0.0f),
                        blendOutDuration = 0.0f
                    });
                }
            }

            int updatedSnapshots = 0;

            for (int snapshotIndex = 0; snapshotIndex < frameSnapshots.Count; ++snapshotIndex)
            {
                AnimationFrameDebugInfo frameDebugInfo = frameSnapshots[snapshotIndex];

                bool bAddNewRecord = true;

                for (int i = m_AnimationRecords.Count - m_NumActiveAnims; i < m_AnimationRecords.Count - updatedSnapshots; ++i)
                {
                    AnimationRecord animRecord = m_AnimationRecords[i];
                    if (animRecord.sequenceIdentifier == frameDebugInfo.sequenceIdentifier)
                    {
                        // snapshot from already existing sequence, just add a new animation frame
                        animRecord.endTime = frameEndTime;
                        animRecord.AddAnimationFrame(frameEndTime, frameDebugInfo.weight, frameDebugInfo.animFrame, frameDebugInfo.blendOutDuration);

                        m_LinesEndTimes[animRecord.rank] = frameEndTime;
                        m_AnimationRecords.SwapElements(i, m_AnimationRecords.Count - updatedSnapshots - 1);
                        ++updatedSnapshots;

                        bAddNewRecord = false;
                        break;
                    }
                }

                if (bAddNewRecord)
                {
                    newSnapshots.Add(snapshotIndex);
                }
            }

            foreach (int snapshotIndex in newSnapshots)
            {
                // snapshot from new sequence, add a new record
                AnimationFrameDebugInfo frameDebugInfo = frameSnapshots[snapshotIndex];

                CircularList<AnimationFrameInfo> animFrames = new CircularList<AnimationFrameInfo>();
                animFrames.PushBack(new AnimationFrameInfo { endTime = frameEndTime, weight = frameDebugInfo.weight, animFrame = frameDebugInfo.animFrame });

                m_AnimationRecords.PushBack(new AnimationRecord
                {
                    sequenceIdentifier = frameDebugInfo.sequenceIdentifier,
                    animName = frameDebugInfo.animName,
                    startTime = frameStartTime,
                    endTime = frameEndTime,
                    blendOutDuration = frameDebugInfo.blendOutDuration,
                    blendOutTime = frameDebugInfo.blendOutDuration,
                    rank = GetNewAnimRank(frameStartTime, frameEndTime),
                    animFrames = animFrames
                });
            }

            m_NumActiveAnims = frameSnapshots.Count;
        }

        public void PruneFramesBeforeTimestamp(float timestamp)
        {
            int i = 0;
            while (i < m_AnimationRecords.Count)
            {
                AnimationRecord animRecord = m_AnimationRecords[i];
                animRecord.PruneAnimationFramesBeforeTimestamp(timestamp);
                if (animRecord.animFrames.Count == 0)
                {
                    // all frames from animation record have been removed, we can remove the record
                    m_AnimationRecords.SwapElements(i, 0);
                    m_AnimationRecords.PopFront();
                }
                else
                {
                    ++i;
                }
            }

            m_NumActiveAnims = Mathf.Min(m_NumActiveAnims, m_AnimationRecords.Count);
        }

        public void PruneFramesStartingAfterTimestamp(float startTimeSeconds)
        {
            int i = m_AnimationRecords.Count - 1;
            while (i >= 0)
            {
                AnimationRecord animRecord = m_AnimationRecords[i];
                animRecord.PruneAnimationFramesStartingAfterTimestamp(startTimeSeconds);
                if (animRecord.animFrames.Count == 0)
                {
                    // all frames from animation record have been removed, we can remove the record
                    m_AnimationRecords.SwapElements(i, m_AnimationRecords.Count - 1);
                    m_AnimationRecords.PopBack();
                }

                --i;
            }

            m_NumActiveAnims = 0;
            for (i = 0; i < m_AnimationRecords.Count; ++i)
            {
                if (m_AnimationRecords[i].endTime >= startTimeSeconds)
                {
                    ++m_NumActiveAnims;
                }
            }

            RecomputeRanks();
        }

        int GetNewAnimRank(float startTime, float endTime)
        {
            // find the highest available rank
            for (int rank = 0; rank < m_LinesEndTimes.Count; ++rank)
            {
                if (m_LinesEndTimes[rank] <= startTime)
                {
                    m_LinesEndTimes[rank] = endTime;
                    return rank;
                }
            }

            m_LinesEndTimes.Add(endTime);
            return m_LinesEndTimes.Count - 1;
        }

        void RecomputeRanks()
        {
            m_LinesEndTimes.Clear();
            for (int i = 0; i < m_AnimationRecords.Count; ++i)
            {
                AnimationRecord animRecord = m_AnimationRecords[i];
                for (int rank = m_LinesEndTimes.Count; rank <= animRecord.rank; ++rank)
                {
                    m_LinesEndTimes.Add(0.0f);
                }

                m_LinesEndTimes[animRecord.rank] = math.max(m_LinesEndTimes[animRecord.rank], animRecord.endTime);
            }
        }

        CircularList<AnimationRecord>       m_AnimationRecords;
        int                                 m_NumActiveAnims;
        List<float>                         m_LinesEndTimes;
    }
}
