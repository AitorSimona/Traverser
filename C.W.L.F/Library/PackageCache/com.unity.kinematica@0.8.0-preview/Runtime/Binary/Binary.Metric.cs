using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        //
        // Metric related methods
        //

        [StructLayout(LayoutKind.Sequential)]
        internal struct Metric
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Joint
            {
                public int nameIndex;
                public int jointIndex;
            }

            public float poseTimeSpan;
            public float poseSampleRatio;

            public float trajectorySampleRatio;
            public float trajectorySampleRange;
            public bool trajectoryDisplacements;

            public BlobArray<Joint> joints;

            public int numJoints => joints.Length;

            public int numPoseSamples => Missing.roundToInt(math.rcp(poseSampleRatio));

            public int numTrajectorySamples => Missing.roundToInt(math.rcp(trajectorySampleRatio));

            public float poseSampleRate => poseTimeSpan * poseSampleRatio;

            /// <summary>
            /// Return the different times at which the trajectory will be sampled in order to build a fragment.
            /// Those times are in seconds and relative to the sampling time of the fragment in the binary.
            /// </summary>
            public TimeSampler GetTrajectoryRelativeTimeSampler(ref Binary binary)
            {
                var startTimeInSeconds =
                    math.min(binary.timeHorizon, binary.timeHorizon *
                        trajectorySampleRange);

                return TimeSampler.CreateFromRange(startTimeInSeconds, binary.timeHorizon, numTrajectorySamples + 1);
            }
        }

        /// <summary>
        /// Denotes an index into the list of metrics contained in the runtime asset.
        /// </summary>
        public struct MetricIndex
        {
            internal int value;

            /// <summary>
            /// Determines whether two metric indices are equal.
            /// </summary>
            /// <param name="metricIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(MetricIndex metricIndex)
            {
                return value == metricIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a metric index to an integer value.
            /// </summary>
            public static implicit operator int(MetricIndex metricIndex)
            {
                return metricIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a metric index.
            /// </summary>
            public static implicit operator MetricIndex(int metricIndex)
            {
                return Create(metricIndex);
            }

            internal static MetricIndex Create(int metricIndex)
            {
                return new MetricIndex
                {
                    value = metricIndex
                };
            }

            /// <summary>
            /// Invalid metric index.
            /// </summary>
            public static MetricIndex Invalid => - 1;

            /// <summary>
            /// Determines if the given metric index is valid or not.
            /// </summary>
            /// <returns>True if the metric index is valid; false otherwise.</returns>
            public bool IsValid => value >= 0;
        }

        internal int numMetrics => metrics.Length;

        internal ref Metric GetMetric(MetricIndex index)
        {
            Assert.IsTrue(index < numMetrics);
            return ref metrics[index];
        }
    }
}
