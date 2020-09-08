using Unity.Mathematics;

namespace Unity.Kinematica
{
    /// <summary>
    /// TimeSampler allows to compute regularly spaced time samples inside a time interval
    /// </summary>
    public struct TimeSampler
    {
        public static TimeSampler CreateFromRange(float startTimeInSeconds, float endTimeInSeconds, int sampleCount)
        {
            return new TimeSampler()
            {
                startTimeInSeconds = startTimeInSeconds,
                advanceInSeconds = sampleCount > 1 ? (endTimeInSeconds - startTimeInSeconds) / (sampleCount - 1) : 0.0f,
                sampleCount = math.max(sampleCount, 1)
            };
        }

        /// <summary>
        /// Time of the first sample
        /// </summary>
        public float startTimeInSeconds;

        /// <summary>
        /// Time step between samples
        /// </summary>
        public float advanceInSeconds;

        /// <summary>
        /// Number of samples, should be at least one
        /// </summary>
        public int sampleCount;

        public float this[int index] => startTimeInSeconds + math.min(index, sampleCount - 1) * advanceInSeconds;
    }
}
