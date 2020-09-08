using Unity.Mathematics;
using Unity.SnapshotDebugger;

namespace Unity.Kinematica
{
    /// <summary>
    /// Static class that contains various extension methods
    /// that allow writing data types to a buffer.
    /// </summary>
    /// <seealso cref="Buffer"/>
    public static class WriteExtensions
    {
        /// <summary>
        /// Writes an affine transform to the buffer.
        /// </summary>
        /// <param name="transform">The transform that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, AffineTransform transform)
        {
            buffer.Write(transform.t);
            buffer.Write(transform.q);
        }

        /// <summary>
        /// Writes an affine transform to the buffer.
        /// </summary>
        /// <remarks>
        /// The transform value will be stored in a quantized format.
        /// </remarks>
        /// <param name="transform">The transform that should be written to the buffer.</param>
        public static void WriteQuantized(this Buffer buffer, AffineTransform transform)
        {
            buffer.WriteQuantized(transform.t);
            buffer.WriteQuantized(transform.q);
        }

        /// <summary>
        /// Writes a time index to the buffer.
        /// </summary>
        /// <param name="timeIndex">The time index that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, TimeIndex timeIndex)
        {
            buffer.Write(timeIndex.segmentIndex);
            buffer.Write(timeIndex.frameIndex);
        }

        /// <summary>
        /// Writes a sampling time to the buffer.
        /// </summary>
        /// <param name="samplingTime">The sampling time that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, SamplingTime samplingTime)
        {
            buffer.Write(samplingTime.theta);
            buffer.Write(samplingTime.timeIndex);
        }
    }
}
