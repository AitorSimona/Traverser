using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        //
        // Segment related methods
        //

        /// <summary>
        /// An segment contains information about animation frames that have
        /// been extracted from the supplied animation clips during the build process.
        /// </summary>
        /// <remarks>
        /// Collections of continuous animation frames that have been tagged with
        /// at least a single tag will be stored in the runtime asset during the
        /// build process. These continuous sequences are denoted as segments and
        /// the respective metadata is accessible in the runtime asset.
        /// </remarks>
        /// <seealso cref="Interval"/>
        public struct Segment
        {
            /// <summary>
            /// Denotes a sequence of animation frames
            /// </summary>
            public struct Interval
            {
                /// <summary>
                /// Denotes the first frame of the interval.
                /// </summary>
                public int firstFrame;

                /// <summary>
                /// Denotes the number of frames for the interval.
                /// </summary>
                public int numFrames;

                /// <summary>
                /// Denotes the first frame of the interval.
                /// </summary>
                public int FirstFrame => firstFrame;

                /// <summary>
                /// Denotes the number of frames for the interval.
                /// </summary>
                public int NumFrames => numFrames;

                /// <summary>
                /// Denotes the one-past-last frame of the interval.
                /// </summary>
                public int OnePastLastFrame => firstFrame + numFrames;

                /// <summary>
                /// Determines whether or not the given frame is contained in the interval.
                /// </summary>
                /// <param name="frame">An index to check the interval for.</param>
                /// <returns>True if the given index is contained in the interval; false otherwise.</returns>
                public bool Contains(int frame)
                {
                    return (frame >= firstFrame && frame <= OnePastLastFrame);
                }
            }

            /// <summary>
            /// Denotes the name of the animation clip this segment has been created from.
            /// </summary>
            /// <seealso cref="GetString"/>
            public int nameIndex;

            internal SerializableGuid guid;

            /// <summary>
            /// Denotes the sequence of frames relative to the original animation clip.
            /// </summary>
            public Interval source;

            /// <summary>
            /// Denotes the sequence of frames relative to the motion library stored in the runtime asset.
            /// </summary>
            public Interval destination;

            /// <summary>
            /// Denotes the first tag this segment has been associated with.
            /// </summary>
            public TagIndex tagIndex;

            /// <summary>
            /// Denotes the number of tags that have been associated with this segment.
            /// </summary>
            public int numTags;

            /// <summary>
            /// Denotes the first marker that this segment contains.
            /// </summary>
            public MarkerIndex markerIndex;

            /// <summary>
            /// Denotes the number of markers that this segment contains.
            /// </summary>
            public int numMarkers;

            /// <summary>
            /// Denotes the first interval that this segment contains.
            /// </summary>
            public IntervalIndex intervalIndex;

            /// <summary>
            /// Denotes the number of intervals that this segment contains.
            /// </summary>
            public int numIntervals;

            /// <summary>
            /// Denotes the "left boundary" segment defined for this segment.
            /// </summary>
            public SegmentIndex previousSegment;

            /// <summary>
            /// Denotes the "right boundary" segment defined for this segment.
            /// </summary>
            public SegmentIndex nextSegment;

            internal int MapRelativeFrameIndex(int frameIndex)
            {
                var ratio =
                    (float)source.numFrames /
                    (float)destination.numFrames;

                return Missing.truncToInt(frameIndex * ratio);
            }

            internal int InverseMapRelativeFrameIndex(int frameIndex)
            {
                var ratio =
                    (float)destination.numFrames /
                    (float)source.numFrames;

                return Missing.truncToInt(frameIndex * ratio);
            }
        }

        /// <summary>
        /// Denotes an index into the list of segments contained in the runtime asset.
        /// </summary>
        public struct SegmentIndex
        {
            internal int value;

            /// <summary>
            /// Determines if the given segment index is valid or not.
            /// </summary>
            /// <returns>True if the segment index is valid; false otherwise.</returns>
            public bool IsValid => value != Invalid;

            /// <summary>
            /// Determines whether two segment indices are equal.
            /// </summary>
            /// <param name="segmentIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(SegmentIndex segmentIndex)
            {
                return value == segmentIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a segment index to an integer value.
            /// </summary>
            public static implicit operator int(SegmentIndex segmentIndex)
            {
                return segmentIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a segment index.
            /// </summary>
            public static implicit operator SegmentIndex(int segmentIndex)
            {
                return Create(segmentIndex);
            }

            internal static SegmentIndex Create(int segmentIndex)
            {
                return new SegmentIndex
                {
                    value = segmentIndex
                };
            }

            /// <summary>
            /// Invalid segment index.
            /// </summary>
            public static SegmentIndex Invalid => - 1;
        }

        /// <summary>
        /// Returns the number of segments stored in the runtime asset.
        /// </summary>
        /// <seealso cref="Segment"/>
        public int numSegments => segments.Length;

        /// <summary>
        /// Retrieves a reference to a segment stored in the runtime asset.
        /// </summary>
        /// <param name="index">The segment index to retrieve the reference for.</param>
        /// <returns>Segment reference that corresponds to the index passed as argument.</returns>
        public ref Segment GetSegment(SegmentIndex index)
        {
            Assert.IsTrue(index < numSegments);
            return ref segments[index];
        }
    }
}
