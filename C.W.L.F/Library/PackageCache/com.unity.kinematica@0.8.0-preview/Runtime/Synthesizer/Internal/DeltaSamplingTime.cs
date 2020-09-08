using Unity.Mathematics;

namespace Unity.Kinematica
{
    /// <summary>
    /// A delta sampling time is used to uniquely identfy a
    /// sub-sampled animation frame and optionally a root transform
    /// offset that occured while reading across segment boundaries.
    /// </summary>
    /// <remarks>
    /// The behavior when a time index is advanced (forward or backward) across
    /// a segment boundary is determined by whether or not boundary clips have
    /// been specified in the Kinematica asset builder tool. If we advance a time
    /// index across a segment boundary an additional root delta transform might
    /// be required to bridge the segment gap.
    /// </remarks>
    /// <seealso cref="Binary.Advance"/>
    /// <seealso cref="SamplingTime"/>
    public struct DeltaSamplingTime
    {
        /// <summary>
        /// Denotes the sampling time this delta sampling time refers to.
        /// </summary>
        public SamplingTime samplingTime;

        /// <summary>
        /// Denotes the optional delta root transform required to bridge the segment boundary.
        /// </summary>
        public AffineTransform deltaTransform;

        /// <summary>
        /// Determines whether or not a boundary has been crossed as a result of advancing a time index.
        /// </summary>
        public BlittableBool crossedBoundary;

        public static DeltaSamplingTime Invalid => Create(SamplingTime.Invalid, AffineTransform.identity, false);

        internal static DeltaSamplingTime Create(SamplingTime samplingTime, AffineTransform deltaTransform, BlittableBool crossedBoundary)
        {
            return new DeltaSamplingTime
            {
                crossedBoundary = crossedBoundary,
                samplingTime = samplingTime,
                deltaTransform = deltaTransform
            };
        }
    }
}
