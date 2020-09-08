using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    public partial struct Binary
    {
        /// <summary>
        /// An interval contains information about a sequence of animation frames
        /// that collectively share the same set of tags.
        /// </summary>
        /// <remarks>
        /// The runtime asset building process only considers animation frames
        /// that are tagged with at least a single trait. Continuous sequences
        /// of such frames are arranged into segments. Segments in turn can contain
        /// multiple intervals with different traits applied. Intervals are therefore
        /// defined to be sequences of animation frames that collectively share
        /// the same set of tags.
        /// </remarks>
        /// <seealso cref="Segment"/>
        public struct Interval
        {
            /// <summary>
            /// Denotes the segment this interval belongs to.
            /// </summary>
            public SegmentIndex segmentIndex;

            /// <summary>
            /// Association with list of tags that are defined over this interval.
            /// </summary>
            public TagListIndex tagListIndex;

            /// <summary>
            /// Denotes the associated code book.
            /// </summary>
            public CodeBookIndex codeBookIndex;

            /// <summary>
            /// Denotes the first frame of the sequence of frames relative to the segment.
            /// </summary>
            public int firstFrame;

            /// <summary>
            /// Denotes the number of sequence frames relative to the segment.
            /// </summary>
            public int numFrames;

            /// <summary>
            /// Denotes the end frame of the sequence of frames relative to the segment.
            /// </summary>
            public int onePastLastFrame => firstFrame + numFrames;

            /// <summary>
            /// Denotes a time index corresponding to the first frame of the interval.
            /// </summary>
            public TimeIndex timeIndex
            {
                get
                {
                    return new TimeIndex
                    {
                        segmentIndex = (short)segmentIndex,
                        frameIndex = (short)firstFrame
                    };
                }
            }

            /// <summary>
            /// Creates a time index relative to the beginning of the interval.
            /// </summary>
            /// <param name="frameIndex">An index that is relative to the beginning of the interval.</param>
            /// <returns>Time index relative to the beginning of the interval.</returns>
            public TimeIndex GetTimeIndex(int frameIndex)
            {
                Assert.IsTrue(Contains(frameIndex));

                return TimeIndex.Create(
                    segmentIndex, frameIndex);
            }

            /// <summary>
            /// Determines whether or not the given frame index is contained in the interval.
            /// </summary>
            /// <returns>True if the given frame index is contained in this interval; false otherwise.</returns>
            public bool Contains(int frameIndex)
            {
                return
                    (frameIndex >= firstFrame) &&
                    (frameIndex < onePastLastFrame);
            }

            /// <summary>
            /// Determines whether or not the given time index is contained in the interval.
            /// </summary>
            /// <returns>True if the given time index is contained in this interval; false otherwise.</returns>
            public bool Contains(TimeIndex timeIndex)
            {
                if (timeIndex.segmentIndex == segmentIndex)
                {
                    return Contains(timeIndex.frameIndex);
                }

                return false;
            }
        }

        /// <summary>
        /// Denotes an index into the list of intervals contained in the runtime asset.
        /// </summary>
        public struct IntervalIndex
        {
            internal int value;

            /// <summary>
            /// Determines whether two interval indices are equal.
            /// </summary>
            /// <param name="intervalIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(IntervalIndex intervalIndex)
            {
                return value == intervalIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a interval index to an integer value.
            /// </summary>
            public static implicit operator int(IntervalIndex intervalIndex)
            {
                return intervalIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a interval index.
            /// </summary>
            public static implicit operator IntervalIndex(int intervalIndex)
            {
                return Create(intervalIndex);
            }

            internal static IntervalIndex Create(int intervalIndex)
            {
                return new IntervalIndex
                {
                    value = intervalIndex
                };
            }

            /// <summary>
            /// Empty interval index.
            /// </summary>
            public static IntervalIndex Empty => - 1;
        }

        /// <summary>
        /// Returns the number of intervals stored in the runtime asset.
        /// </summary>
        /// <seealso cref="Interval"/>
        public int numIntervals => intervals.Length;

        /// <summary>
        /// Retrieves a reference to an interval stored in the runtime asset.
        /// </summary>
        /// <param name="intervalIndex">The interval index to retrieve the reference for.</param>
        /// <returns>Interval reference that corresponds to the index passed as argument.</returns>
        public ref Interval GetInterval(IntervalIndex intervalIndex)
        {
            Assert.IsTrue(intervalIndex < numIntervals);
            return ref intervals[intervalIndex];
        }

        internal unsafe MemoryArray<Interval> GetIntervals()
        {
            return new MemoryArray<Interval>
            {
                ptr = intervals.GetUnsafePtr(),
                length = intervals.Length
            };
        }
    }
}
