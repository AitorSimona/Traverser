namespace Unity.SnapshotDebugger
{
    internal abstract class Storage
    {
        public abstract float capacityInSeconds { get; set; }

        public abstract float startTimeInSeconds { get; }

        public abstract float endTimeInSeconds { get; }

        public abstract int memorySize { get; }

        public abstract void Record(Snapshot snapshot);

        public abstract Snapshot Retrieve(float offset);

        public abstract Snapshot Retrieve(Identifier<Snapshot> identifier);

        public abstract void Commit();

        public abstract void Discard();

        public abstract void DiscardAfterTimeStamp(float timestamp);

        public abstract void PrepareWrite();

        public abstract void PrepareRead();
    }
}
