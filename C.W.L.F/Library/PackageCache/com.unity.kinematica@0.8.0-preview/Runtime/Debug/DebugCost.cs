using System.Collections.Generic;
using Unity.Collections;
using Unity.SnapshotDebugger;

namespace Unity.Kinematica
{
    internal struct DebugCostRecord : IFrameRecord
    {
        public NativeString64 queryDebugName;

        public float poseCost;

        public float trajectoryCost;
    }

    internal class DebugCostAggregate : IFrameAggregate
    {
        internal struct Record
        {
            public float startTime;
            public float endTime;

            public int queryIdentifier;
            public float poseCost;
            public float trajectoryCost;

            public float TotalCost => trajectoryCost >= 0.0f ? poseCost + trajectoryCost : poseCost;
        }

        internal int NumRecords => m_Records.Count;

        internal Record GetRecord(int i) => m_Records[i];

        public bool IsEmpty => m_Records.Count == 0;

        public void AddRecords(List<IFrameRecord> records, float frameStartTime, float frameEndTime)
        {
            foreach (IFrameRecord record in records)
            {
                DebugCostRecord costRecord = (DebugCostRecord)record;
                m_Records.PushBack(new Record()
                {
                    startTime = frameStartTime,
                    endTime = frameEndTime,
                    queryIdentifier = costRecord.queryDebugName.GetHashCode(),
                    poseCost = costRecord.poseCost,
                    trajectoryCost = costRecord.trajectoryCost,
                });
            }
        }

        public void PruneFramesBeforeTimestamp(float startTimeInSeconds)
        {
            while (m_Records.Count > 0 && m_Records[0].endTime <= startTimeInSeconds)
            {
                m_Records.PopFront();
            }
        }

        public void PruneFramesStartingAfterTimestamp(float startTimeInSeconds)
        {
            while (m_Records.Count > 0 && m_Records.Last.startTime > startTimeInSeconds)
            {
                m_Records.PopBack();
            }
        }

        CircularList<Record> m_Records = new CircularList<Record>();
    }
}
