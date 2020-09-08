using System.Collections.Generic;

namespace Unity.SnapshotDebugger
{
    public interface IFrameRecord {}

    public interface IFrameAggregate
    {
        bool IsEmpty { get; }

        void AddRecords(List<IFrameRecord> records, float frameStartTime, float frameEndTime);

        void PruneFramesBeforeTimestamp(float startTimeInSeconds);

        void PruneFramesStartingAfterTimestamp(float startTimeInSeconds);
    }
}
