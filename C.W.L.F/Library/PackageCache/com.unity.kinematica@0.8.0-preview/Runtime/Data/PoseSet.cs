using System;
using Unity.Collections;
using Unity.SnapshotDebugger;
using IntervalIndex = Unity.Kinematica.Binary.IntervalIndex;
using Buffer = Unity.SnapshotDebugger.Buffer;

namespace Unity.Kinematica
{
    /// <summary>
    /// Data type that refers to a sequence of animation poses.
    /// </summary>
    /// <remarks>
    /// A pose sequence is usually used as an array element where
    /// each element refers to a sub-set of poses that are contained
    /// in an interval.
    /// </remarks>
    /// <seealso cref="Binary.Interval"/>
    /// <seealso cref="IntervalIndex"/>
    [Data("Pose Set", DataFlags.SelfInputOutput)]
    public struct PoseSet : IDebugObject, Serializable, IDisposable
    {
        public NativeArray<PoseSequence> sequences;
        public Allocator allocator;

        public NativeString64 debugName;

        public DebugIdentifier debugIdentifier { get; set; }

        public static PoseSet Create(QueryResult result, Allocator allocator)
        {
            return new PoseSet()
            {
                sequences = result.sequences.ToArray(allocator),
                allocator = allocator,
                debugName = result.debugName,
                debugIdentifier = DebugIdentifier.Invalid
            };
        }

        public void Dispose()
        {
            if (allocator != Allocator.Invalid)
            {
                sequences.Dispose();
            }
        }

        public static implicit operator PoseSet(QueryResult result)
        {
            return Create(result, Allocator.Persistent);
        }

        public void WriteToStream(Buffer buffer)
        {
            buffer.WriteNativeArray(sequences, allocator);
            buffer.Write(debugName);
        }

        public void ReadFromStream(Buffer buffer)
        {
            sequences = buffer.ReadNativeArray<PoseSequence>(out allocator);
            debugName = buffer.ReadNativeString64();
        }
    }
}
