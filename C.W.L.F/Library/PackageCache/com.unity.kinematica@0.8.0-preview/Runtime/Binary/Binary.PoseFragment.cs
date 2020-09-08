using System;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        /// <summary>
        /// A pose fragment contains information necessary to perform
        /// similarity calculations between animation poses.
        /// </summary>
        /// <remarks>
        /// A pose fragment can be constructed from any animation pose
        /// contained in the motion library. It requires a metric in order
        /// to extract the relevant information. The similarity
        /// calculation uses a weighted sum of character space joint positions
        /// and joint velocities over a user defined time horizon.
        /// <para>
        /// The main purpose of this class is to:
        /// * Create pose fragments from poses in the motion library.
        /// * Perform similarity calculation between pose fragments.
        /// </para>
        /// </remarks>
        public struct PoseFragment : IDisposable
        {
            /// <summary>
            /// Denotes the metric index that this pose fragment belongs to.
            /// </summary>
            public MetricIndex metricIndex;

            /// <summary>
            /// Denotes the sampling time that this pose fragment was generated from.
            /// </summary>
            public SamplingTime samplingTime;

            /// <summary>
            /// Denotes the feature array of this pose fragment.
            /// </summary>
            public NativeArray<float3> array;

            internal PoseFragment(ref Binary binary, MetricIndex metricIndex)
            {
                samplingTime = SamplingTime.Invalid;

                this.metricIndex = metricIndex;

                var numFeatures = GetNumFeatures(ref binary, metricIndex);

                array = new NativeArray<float3>(numFeatures, Allocator.Temp);
            }

            internal static int GetNumFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                ref Metric metric = ref binary.GetMetric(metricIndex);

                int numPositionFeatures = metric.numJoints;
                int numVelocityFeatures = metric.numJoints * metric.numPoseSamples;

                return numPositionFeatures + numVelocityFeatures;
            }

            internal static int GetNumQuantizedFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return 0;
            }

            internal static int GetNumNormalizedFeatures(ref Binary binary, MetricIndex metricIndex)
            {
                return 0;
            }

            /// <summary>
            /// Determines whether or not this pose fragment is valid.
            /// </summary>
            public bool IsValid => array.IsCreated;

            /// <summary>
            /// Disposes the underlying feature array.
            /// </summary>
            public void Dispose()
            {
                array.Dispose();
            }

            /// <summary>
            /// Represents an invalid pose fragment.
            /// </summary>
            public static PoseFragment Invalid
            {
                get => new PoseFragment();
            }

            /// <summary>
            /// Determines whether two pose fragments are equal.
            /// </summary>
            /// <param name="instance">The pose fragment to compare against the current pose fragment.</param>
            /// <param name="eps">Tolerance value to indicate by how much the result can diverge before being considered unequal.</param>
            /// <returns>True if the pose fragment passed as argument is equal to the current pose fragment; otherwise, false.</returns>
            public bool Equals(PoseFragment instance, float eps)
            {
                if (length != instance.length)
                {
                    return false;
                }

                for (int i = 0; i < length; ++i)
                {
                    if (!Missing.equalEps(this[i], instance[i], eps))
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Denotes the size of the feature array.
            /// </summary>
            public int length => array.Length;

            /// <summary>
            /// Gives access to the individual pose fragment features.
            /// </summary>
            public float3 this[int index] => array[index];

            internal static PoseFragment Create(ref Binary binary, MetricIndex metricIndex)
            {
                return new PoseFragment(ref binary, metricIndex);
            }

            internal struct Factory
            {
                public int writeIndex;

                public PoseFragment instance;

                public Factory(ref Binary binary, MetricIndex metricIndex)
                {
                    writeIndex = 0;

                    instance = PoseFragment.Create(ref binary, metricIndex);
                }

                public static Factory Create(ref Binary binary, MetricIndex metricIndex)
                {
                    return new Factory(ref binary, metricIndex);
                }

                public static PoseFragment Create(ref Binary binary, MetricIndex metricIndex, SamplingTime samplingTime)
                {
                    var factory = Create(ref binary, metricIndex);

                    factory.GenerateJointPositions(ref binary, samplingTime);

                    factory.GenerateJointVelocities(ref binary, samplingTime);

                    Assert.IsTrue(factory.writeIndex == factory.instance.array.Length);

                    factory.instance.samplingTime = samplingTime;

                    return factory.instance;
                }

                void GenerateJointPositions(ref Binary binary, SamplingTime samplingTime)
                {
                    ref var metric = ref binary.GetMetric(instance.metricIndex);

                    for (int i = 0; i < metric.numJoints; ++i)
                    {
                        int jointIndex = metric.joints[i].jointIndex;

                        var jointTransform =
                            binary.GetJointTransform(
                                jointIndex, samplingTime);

                        WriteFloat3(jointTransform.t);
                    }
                }

                void GenerateJointVelocities(ref Binary binary, SamplingTime samplingTime)
                {
                    var referenceTransform =
                        binary.GetTrajectoryTransform(samplingTime);

                    ref var metric = ref binary.GetMetric(instance.metricIndex);

                    var halfTimeSpan = metric.poseTimeSpan * 0.5f;
                    var numSamples = metric.numPoseSamples;
                    var deltaTime = metric.poseSampleRate;

                    for (int i = 0; i < metric.numJoints; ++i)
                    {
                        var advanceInSeconds = -halfTimeSpan;

                        var currentSamplingTime =
                            binary.Advance(samplingTime,
                                advanceInSeconds);

                        int jointIndex = metric.joints[i].jointIndex;

                        var deltaTransform = referenceTransform.inverseTimes(
                            binary.GetTrajectoryTransform(currentSamplingTime));

                        var previousJointTransform = deltaTransform *
                            binary.GetJointTransform(
                            jointIndex, currentSamplingTime.samplingTime);

                        for (int j = 0; j < numSamples; ++j)
                        {
                            advanceInSeconds += deltaTime;

                            currentSamplingTime =
                                binary.Advance(samplingTime,
                                    advanceInSeconds);

                            deltaTransform = referenceTransform.inverseTimes(
                                binary.GetTrajectoryTransform(currentSamplingTime));

                            var jointTransform = deltaTransform *
                                binary.GetJointTransform(
                                jointIndex, currentSamplingTime.samplingTime);

                            float3 linearDisplacement =
                                jointTransform.t - previousJointTransform.t;

                            WriteFloat3(linearDisplacement);

                            previousJointTransform = jointTransform;
                        }
                    }
                }

                void WriteFloat3(float3 value)
                {
                    instance.array[writeIndex++] = value;
                }
            }

            internal static PoseFragment Create(ref Binary binary, MetricIndex metricIndex, SamplingTime samplingTime)
            {
                return Factory.Create(ref binary, metricIndex, samplingTime);
            }
        }

        /// <summary>
        /// Returns a codebook index for a time index.
        /// </summary>
        /// <remarks>
        /// Tags define whether or not pose and trajectory fragments should be created for
        /// the animation frames they span. Given a time index this method performs a lookup
        /// to determine which codebook this time belongs to. This in turn gives access to the
        /// fragments that correspond to the time index.
        /// </remarks>
        /// <param name="timeIndex">The time index for which the codebook index should be retrieved.</param>
        /// <returns>Codebook index that corresponds to the time index passed as argument.</returns>
        public CodeBookIndex GetCodeBookAt(TimeIndex timeIndex)
        {
            int numCodeBooks = this.numCodeBooks;

            for (int i = 0; i < numCodeBooks; ++i)
            {
                ref var codeBook = ref GetCodeBook(i);

                int numIntervals = codeBook.intervals.Length;

                for (int j = 0; j < numIntervals; ++j)
                {
                    var intervalIndex = codeBook.intervals[j];

                    ref var interval = ref GetInterval(intervalIndex);

                    if (interval.Contains(timeIndex))
                    {
                        return i;
                    }
                }
            }

            return CodeBookIndex.Invalid;
        }

        /// <summary>
        /// Reconstructs a pose fragment from the runtime asset that corresponds to a sampling time.
        /// </summary>
        /// <remarks>
        /// The runtime asset building process generates pose fragments for any segment
        /// for which a metric has been defined. These pose fragments are stored inside
        /// the runtime asset in a compressed format.
        /// <para>
        /// Pose fragments can either be constructed from scratch or can be reconstructed
        /// from the information contained in the runtime asset. Reconstruction is faster
        /// than creating a pose fragment but is subject to loss of precision.
        /// </para>
        /// </remarks>
        /// <param name="samplingTime">The sampling time from which the pose fragment should be reconstructed.</param>
        /// <returns>The resulting pose fragment.</returns>
        public PoseFragment ReconstructPoseFragment(SamplingTime samplingTime)
        {
            int numCodeBooks = this.numCodeBooks;

            for (int i = 0; i < numCodeBooks; ++i)
            {
                ref var codeBook = ref GetCodeBook(i);

                int numIntervals = codeBook.intervals.Length;

                int fragmentIndex = 0;

                for (int j = 0; j < numIntervals; ++j)
                {
                    var intervalIndex = codeBook.intervals[j];

                    ref var interval = ref GetInterval(intervalIndex);

                    if (interval.Contains(samplingTime.timeIndex))
                    {
                        var relativeIndex =
                            samplingTime.frameIndex - interval.firstFrame;

                        var fragment =
                            PoseFragment.Create(
                                ref this, codeBook.metricIndex);

                        fragment.samplingTime = samplingTime;

                        var index = fragmentIndex + relativeIndex;

                        Assert.IsTrue(index < codeBook.numFragments);

                        fragment.metricIndex = codeBook.metricIndex;

                        codeBook.poses.DecodeFeatures(fragment.array, index);

                        codeBook.poses.InverseNormalize(fragment.array);

                        return fragment;
                    }

                    fragmentIndex += interval.numFrames;
                }
            }

            return PoseFragment.Invalid;
        }

        /// <summary>
        /// Create a pose fragment from a sampling time.
        /// </summary>
        /// <param name="metricIndex">The metric defining the pose fragment layout.</param>
        /// <param name="samplingTime">The sampling time that the pose fragment should be created for.</param>
        /// <returns>The resulting pose fragment.</returns>
        public PoseFragment CreatePoseFragment(MetricIndex metricIndex, SamplingTime samplingTime)
        {
            return PoseFragment.Create(ref this, metricIndex, samplingTime);
        }
    }
}
