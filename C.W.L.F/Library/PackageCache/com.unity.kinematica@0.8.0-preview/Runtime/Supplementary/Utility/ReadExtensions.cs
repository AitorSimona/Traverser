using Unity.Mathematics;
using Unity.SnapshotDebugger;

namespace Unity.Kinematica
{
    /// <summary>
    /// Static class that contains various extension methods
    /// that allow reading data types from a buffer.
    /// </summary>
    /// <seealso cref="Buffer"/>
    public static class ReadExtensions
    {
        /// <summary>
        /// Reads a transform from the buffer.
        /// </summary>
        /// <returns>The transform that has been read from the buffer.</returns>
        public static AffineTransform ReadAffineTransform(this Buffer buffer)
        {
            float3 t = buffer.ReadVector3();
            quaternion q = buffer.ReadQuaternion();

            return new AffineTransform(t, q);
        }

        /// <summary>
        /// Reads a quantized transform from the buffer.
        /// </summary>
        /// <returns>The original transform that has been read from the buffer.</returns>
        public static AffineTransform ReadAffineTransformQuantized(this Buffer buffer)
        {
            float3 t = buffer.ReadVector3Quantized();
            quaternion q = buffer.ReadQuaternionQuantized();

            return new AffineTransform(t, q);
        }

        /// <summary>
        /// Reads a time index from the buffer.
        /// </summary>
        /// <returns>The time index that has been read from the buffer.</returns>
        public static TimeIndex ReadTimeIndex(this Buffer buffer)
        {
            var segmentIndex = buffer.Read16();
            var frameIndex = buffer.Read16();

            return TimeIndex.Create(segmentIndex, frameIndex);
        }

        /// <summary>
        /// Reads a sampling time from the buffer.
        /// </summary>
        /// <returns>The sampling time that has been read from the buffer.</returns>
        public static SamplingTime ReadSamplingTime(this Buffer buffer)
        {
            var theta = buffer.ReadSingle();
            var timeIndex = buffer.ReadTimeIndex();

            return SamplingTime.Create(timeIndex, theta);
        }
    }
}
