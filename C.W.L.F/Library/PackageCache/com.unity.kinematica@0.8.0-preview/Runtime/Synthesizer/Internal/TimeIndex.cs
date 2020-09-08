namespace Unity.Kinematica
{
    /// <summary>
    /// A time index is used to uniquely identfy an animation frame.
    /// </summary>
    /// <remarks>
    /// Time indices are used to uniquely identify animation frames in the
    /// motion library (which is contained in the runtime asset).
    /// <para>
    /// Collections of continuous animation frames that have been tagged with
    /// at least a single tag will be stored in the runtime asset during the
    /// build process. These continuous sequences are denoted as segments.
    /// </para>
    /// <para>
    /// A time index in turn refers to a single animation frame that resides
    /// inside of a segment. Advancing playback time or performing any other kind
    /// of time-related manipulation uses the "time index" construct in order to ensure
    /// that reading from animation poses does not cross segment boundaries.
    /// </para>
    /// </remarks>
    /// <seealso cref="Binary.Segment"/>
    public struct TimeIndex
    {
        /// <summary>
        /// Denotes the segment this time index refers to.
        /// </summary>
        public short segmentIndex;

        /// <summary>
        /// Denotes the frame index relative to the beginning
        /// of the segment that this time index refers to.
        /// </summary>
        public short frameIndex;

        /// <summary>
        /// Determines if the given time index is valid or not.
        /// </summary>
        /// <returns>True if the time index is valid; false otherwise.</returns>
        public bool IsValid
        {
            get => segmentIndex >= 0;
        }

        /// <summary>
        /// Determines whether two time indices are equal.
        /// </summary>
        /// <param name="timeIndex">The time index to compare against the current time index.</param>
        /// <returns>True if the specified time index is equal to the current time index; otherwise, false.</returns>
        public bool Equals(TimeIndex timeIndex)
        {
            return
                (segmentIndex == timeIndex.segmentIndex) &&
                (frameIndex == timeIndex.frameIndex);
        }

        /// <summary>
        /// Creates a time index from a segment and frame index.
        /// </summary>
        /// <param name="segmentIndex">The segment index that the time index should refer to.</param>
        /// <param name="frameIndex">The segment-relative frame index that the time index should refer to.</param>
        public static TimeIndex Create(int segmentIndex, int frameIndex = 0)
        {
            return new TimeIndex
            {
                segmentIndex = (short)segmentIndex,
                frameIndex = (short)frameIndex
            };
        }

        /// <summary>
        /// Invalid time index.
        /// </summary>
        public static TimeIndex Invalid
        {
            get => new TimeIndex { segmentIndex = -1 };
        }
    }
}
