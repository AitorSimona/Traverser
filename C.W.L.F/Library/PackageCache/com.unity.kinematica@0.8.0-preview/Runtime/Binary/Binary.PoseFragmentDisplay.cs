using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine;

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
        /// * Visually display the pose fragment for debug purposes.
        /// </para>
        /// </remarks>
        internal struct PoseFragmentDisplay
        {
            public MetricIndex metricIndex;

            public SamplingTime samplingTime;

            public PoseFragment fragment;

            public struct Options
            {
                public bool showTimeSpan;
                public bool showDetails;
                public float timeOffset;
                public Color poseColor;
                public Color detailsColor;

                public static Options Create()
                {
                    return new Options
                    {
                        showTimeSpan = false,
                        showDetails = false,
                        timeOffset = 0.0f,
                        poseColor = Color.yellow,
                        detailsColor = new Color(0.75f, 0.0f, 0.15f, 0.75f)
                    };
                }
            }

            public static Options CreateOptions()
            {
                return Options.Create();
            }

            public static PoseFragmentDisplay Create(PoseFragment fragment)
            {
                return new PoseFragmentDisplay
                {
                    fragment = fragment,
                    metricIndex = fragment.metricIndex,
                    samplingTime = fragment.samplingTime
                };
            }

            public void Display(ref Binary binary, Options options, bool worldSpace)
            {
                if (worldSpace)
                {
                    var anchorTransform =
                        binary.GetTrajectoryTransform(
                            samplingTime);

                    Display(ref binary, anchorTransform, options);
                }
                else
                {
                    Display(ref binary, AffineTransform.identity, options);
                }
            }

            public void Display(ref Binary binary, AffineTransform anchorTransform, Options options)
            {
                DeltaSamplingTime currentSamplingTime;
                AffineTransform deltaTransform;
                AffineTransform referenceTransform = binary.GetTrajectoryTransform(samplingTime);
                if (options.showTimeSpan)
                {
                    ref var metric = ref binary.GetMetric(metricIndex);

                    var halfTimeSpan = metric.poseTimeSpan * 0.5f;
                    var numSamples = metric.numPoseSamples;
                    var deltaTime = metric.poseSampleRate;

                    var currentTime = -halfTimeSpan;

                    Color color = options.poseColor;

                    for (int j = 0; j <= numSamples; ++j)
                    {
                        currentSamplingTime =
                            binary.Advance(samplingTime,
                                currentTime);

                        deltaTransform = referenceTransform.inverseTimes(
                            binary.GetTrajectoryTransform(currentSamplingTime));

                        var rootTransform = anchorTransform * deltaTransform;

                        float x = currentTime / halfTimeSpan;

                        float sigma = Missing.inverseAbsHat(
                            x, 0.0f);

                        Assert.IsTrue(sigma >= -1.0f && sigma <= 1.0f);

                        color.a = 0.1f + (sigma * 0.5f);

                        binary.DebugDrawPoseWorldSpace(
                            rootTransform, currentSamplingTime.samplingTime, color);

                        currentTime += deltaTime;
                    }
                }

                currentSamplingTime = binary.Advance(samplingTime, options.timeOffset * binary.TimeHorizon);
                deltaTransform = referenceTransform.inverseTimes(binary.GetTrajectoryTransform(currentSamplingTime));
                binary.DebugDrawPoseWorldSpace(anchorTransform * deltaTransform, currentSamplingTime.samplingTime, options.poseColor);


                if (options.showDetails)
                {
                    DisplayJointPositions(ref binary, anchorTransform, options);

                    DisplayJointVelocities(ref binary, anchorTransform, options);
                }
            }

            void DisplayPoseAtOffset(ref Binary binary, AffineTransform anchorTransform, Options options)
            {
                var referenceTransform =
                    binary.GetTrajectoryTransform(samplingTime);

                ref var metric = ref binary.GetMetric(metricIndex);

                var halfTimeSpan = metric.poseTimeSpan * 0.5f;

                var theta = (options.timeOffset + 1.0f) * 0.5f;

                var currentSamplingTime = binary.Advance(samplingTime,
                    metric.poseTimeSpan * theta - halfTimeSpan);

                var deltaTransform = referenceTransform.inverseTimes(
                    binary.GetTrajectoryTransform(currentSamplingTime));

                var rootTransform = anchorTransform * deltaTransform;

                binary.DebugDrawPoseWorldSpace(
                    rootTransform, currentSamplingTime.samplingTime, options.poseColor);
            }

            void DisplayJointPositions(ref Binary binary, AffineTransform anchorTransform, Options options)
            {
                ref var metric = ref binary.GetMetric(metricIndex);

                for (int i = 0; i < metric.numJoints; ++i)
                {
                    var jointPosition = anchorTransform.transform(fragment[i]);

                    DebugDraw.DrawCube(jointPosition, quaternion.identity, 0.01f, options.detailsColor);
                }
            }

            void DisplayJointVelocities(ref Binary binary, AffineTransform anchorTransform, Options options)
            {
                var referenceTransform =
                    binary.GetTrajectoryTransform(samplingTime);

                ref var metric = ref binary.GetMetric(metricIndex);

                var halfTimeSpan = metric.poseTimeSpan * 0.5f;
                var numSamples = metric.numPoseSamples;
                var numJoints = metric.numJoints;

                var currentSamplingTime =
                    binary.Advance(samplingTime, -halfTimeSpan);

                var deltaTransform = referenceTransform.inverseTimes(
                    binary.GetTrajectoryTransform(currentSamplingTime));

                var readIndex = numJoints;

                for (int i = 0; i < numJoints; ++i)
                {
                    int jointIndex = metric.joints[i].jointIndex;

                    var jointTransform =
                        deltaTransform * binary.GetJointTransform(
                            jointIndex, currentSamplingTime.samplingTime);

                    var worldSpaceJointTransform =
                        anchorTransform * jointTransform;

                    for (int j = 0; j < numSamples; ++j)
                    {
                        var jointVelocity = fragment[readIndex++];

                        var worldSpaceJointVelocity =
                            anchorTransform.transformDirection(
                                jointVelocity);

                        var previousJointPosition =
                            worldSpaceJointTransform.t;

                        worldSpaceJointTransform.t +=
                            worldSpaceJointVelocity;

                        Debug.DrawLine(
                            previousJointPosition,
                            worldSpaceJointTransform.t,
                            options.detailsColor);
                    }
                }

                Assert.IsTrue(readIndex == fragment.length);
            }
        }
    }
}
