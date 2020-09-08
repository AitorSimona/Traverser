using System;
using Unity.Collections;

using IntervalIndex = Unity.Kinematica.Binary.IntervalIndex;

namespace Unity.Kinematica
{
    /// <summary>
    /// A query result is wrapper around a pose sequence and
    /// is the result of a semantic filtering query.
    /// </summary>
    /// <seealso cref="MotionSynthesizer.Query"/>
    /// <seealso cref="PoseSequence"/>
    public struct QueryResult : IDisposable
    {
        /// <summary>
        /// Denotes the pose sequence that matches the query result.
        /// </summary>
        public NativeList<PoseSequence> sequences;

        public NativeString64 debugName;

        /// <summary>
        /// Disposes the underlying pose sequence.
        /// </summary>
        public void Dispose()
        {
            sequences.Dispose();
        }

        /// <summary>
        /// Denotes the number of elements of the pose sequence.
        /// </summary>
        public int length => sequences.Length;

        /// <summary>
        /// Allows access to a single element of the pose sequence.
        /// </summary>
        public PoseSequence this[int index] => sequences[index];

        /// <summary>
        /// Creates an empty query result.
        /// </summary>
        public static QueryResult Create()
        {
            return new QueryResult
            {
                sequences =
                    new NativeList<PoseSequence>(Allocator.Temp)
            };
        }

        /// <summary>
        /// Appends an element to the pose sequence.
        /// </summary>
        /// <remarks>
        /// Pose sequences are based on the notion of partial intervals,
        /// i.e. an element of a sequence can refer to an interval
        /// and optionally further reduce by selecting a sub-section
        /// of the interval.
        /// </remarks>
        /// <param name="intervalIndex">The interval index that the selected poses should refer to.</param>
        /// <param name="firstFrame">The first frame (relative to the segment) that the pose sequence should start at.</param>
        /// <param name="numFrames">The number of frames that the pose sequence should contain.</param>
        /// <seealso cref="Binary.Interval"/>
        /// <seealso cref="IntervalIndex"/>
        public void Add(IntervalIndex intervalIndex, int firstFrame, int numFrames)
        {
            sequences.Add(PoseSequence.Create(intervalIndex, firstFrame, numFrames));
        }

        /// <summary>
        /// An empty query result.
        /// </summary>
        public static QueryResult Empty => Create();
    }
}
