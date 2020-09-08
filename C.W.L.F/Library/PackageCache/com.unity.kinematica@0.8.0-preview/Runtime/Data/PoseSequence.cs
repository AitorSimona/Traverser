using IntervalIndex = Unity.Kinematica.Binary.IntervalIndex;

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
    public struct PoseSequence
    {
        /// <summary>
        /// Refers to an interval. A pose sequence can refer to the
        /// entire interval or a sub-set of the interval.
        /// </summary>
        public IntervalIndex intervalIndex;

        /// <summary>
        /// Denotes the first animation frame (relative to the segment start).
        /// </summary>
        public int firstFrame;

        /// <summary>
        /// Denotes the number of animation frames (relative to the segment start).
        /// </summary>
        public int numFrames;

        /// <summary>
        /// Denotes the one-last-last animation frame (relative to the segment start).
        /// </summary>
        public int onePastLastFrame => firstFrame + numFrames;

        internal static PoseSequence Create(IntervalIndex intervalIndex, int firstFrame, int numFrames)
        {
            return new PoseSequence
            {
                intervalIndex = intervalIndex,
                firstFrame = firstFrame,
                numFrames = numFrames
            };
        }
    }
}
