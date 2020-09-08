using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.SnapshotDebugger
{
    internal class MemoryStorage : Storage, IDisposable
    {
        public static MemoryStorage Create(float capacityInSeconds)
        {
            return new MemoryStorage(capacityInSeconds);
        }

        public void Dispose()
        {
            foreach (Snapshot snapshot in snapshots)
            {
                snapshot.Dispose();
            }
            snapshots.Clear();
        }

        public override float capacityInSeconds
        {
            get; set;
        }

        public override float startTimeInSeconds
        {
            get
            {
                if (snapshots.Count > 0)
                {
                    return snapshots[0].startTimeInSeconds;
                }

                return 0.0f;
            }
        }

        public override float endTimeInSeconds
        {
            get
            {
                if (snapshots.Count > 0)
                {
                    return snapshots[snapshots.Count - 1].endTimeInSeconds;
                }

                return 0.0f;
            }
        }

        public override int memorySize
        {
            get
            {
                int memorySize = 0;

                foreach (Snapshot snapshot in snapshots)
                {
                    memorySize += snapshot.memorySize;
                }

                return memorySize;
            }
        }

        public override void Record(Snapshot snapshot)
        {
            snapshots.Add(snapshot);

            Assert.IsTrue(!identifier2Snapshot.ContainsKey(snapshot.identifier));

            identifier2Snapshot[snapshot.identifier] = snapshot;

            while (durationInSeconds > capacityInSeconds)
            {
                snapshots[0].Dispose();
                snapshots.RemoveAt(0);
            }
        }

        public override Snapshot Retrieve(float timeStamp)
        {
            return FindSnapshotForTime(timeStamp);
        }

        public override Snapshot Retrieve(Identifier<Snapshot> identifier)
        {
            Assert.IsTrue(identifier2Snapshot.ContainsKey(identifier));

            return identifier2Snapshot[identifier];
        }

        public override void Commit()
        {
        }

        public override void Discard()
        {
            foreach (Snapshot snapshot in snapshots)
            {
                snapshot.Dispose();
            }
            snapshots.Clear();
        }

        public override void DiscardAfterTimeStamp(float timestamp)
        {
            while (snapshots.Count > 0 && snapshots[snapshots.Count - 1].startTimeInSeconds > timestamp)
            {
                snapshots[snapshots.Count - 1].Dispose();
                snapshots.RemoveAt(snapshots.Count - 1);
            }
        }

        public override void PrepareWrite()
        {
        }

        public override void PrepareRead()
        {
        }

        MemoryStorage(float timeInSeconds = -1.0f)
        {
            capacityInSeconds = timeInSeconds;
        }

        float durationInSeconds
        {
            get { return endTimeInSeconds - startTimeInSeconds; }
        }

        Snapshot FindSnapshotForTime(float timeInSeconds)
        {
            if (snapshots.Count > 0)
            {
                int min = 0;
                int max = snapshots.Count - 1;

                if (timeInSeconds < snapshots[min].startTimeInSeconds)
                {
                    return snapshots[min];
                }
                else if (timeInSeconds >= snapshots[max].endTimeInSeconds)
                {
                    return snapshots[max];
                }

                while (min <= max)
                {
                    int mid = (min + max) / 2;
                    if (snapshots[mid].Contains(timeInSeconds))
                    {
                        return snapshots[mid];
                    }
                    else if (timeInSeconds < snapshots[mid].startTimeInSeconds)
                    {
                        max = mid - 1;
                    }
                    else
                    {
                        min = mid + 1;
                    }
                }

                // The loop above should always succeed...
                Assert.IsTrue(false);
            }

            return null;
        }

        Dictionary<Identifier<Snapshot>, Snapshot> identifier2Snapshot = new Dictionary<Identifier<Snapshot>, Snapshot>();

        List<Snapshot> snapshots = new List<Snapshot>();
    }
}
