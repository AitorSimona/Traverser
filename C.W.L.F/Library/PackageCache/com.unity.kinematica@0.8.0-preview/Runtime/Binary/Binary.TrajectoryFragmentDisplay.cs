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
        internal struct TrajectoryFragmentDisplay
        {
            public MetricIndex metricIndex;

            public SamplingTime samplingTime;

            public TrajectoryFragment fragment;

            public struct Options
            {
                public bool showDetails;
                public bool showForward;
                public bool showVelocity;
                public float timeOffset;
                public Color baseColor;
                public Color velocityColor;
                public Color forwardColor;
                public Color detailsColor;

                public static Options Create()
                {
                    return new Options
                    {
                        showDetails = false,
                        showForward = true,
                        showVelocity = false,
                        baseColor = new Color(0.0f, 1.0f, 0.5f, 0.75f),
                        velocityColor = new Color(0.25f, 0.5f, 1.0f, 0.75f),
                        forwardColor = new Color(0.75f, 0.6f, 0.0f, 0.75f),
                        detailsColor = new Color(0.75f, 0.0f, 0.15f, 0.75f),
                    };
                }
            }

            public static Options CreateOptions()
            {
                return Options.Create();
            }

            public static TrajectoryFragmentDisplay Create(TrajectoryFragment fragment)
            {
                return new TrajectoryFragmentDisplay
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

            public float Display(ref Binary binary, AffineTransform anchorTransform, Options options)
            {
                binary.DebugDrawTrajectory(
                    anchorTransform, samplingTime,
                    binary.TimeHorizon, options.baseColor);

                AffineTransform referenceTransform =
                    binary.GetTrajectoryTransform(samplingTime);

                var timeHorizon = binary.TimeHorizon;

                AffineTransform rootTransform =
                    referenceTransform.inverseTimes(
                        binary.GetTrajectoryTransform(
                            binary.Advance(samplingTime,
                                timeHorizon * options.timeOffset)));

                AffineTransform transform =
                    anchorTransform * rootTransform;

                if (options.showForward)
                {
                    var forward =
                        anchorTransform.transformDirection(
                            Missing.zaxis(rootTransform.q));

                    DisplayAxis(transform.t, forward * 0.5f, options.forwardColor);
                }

                float linearSpeedInMetersPerSecond = 0.0f;

                if (options.showVelocity)
                {
                    float deltaTime = math.rcp(binary.SampleRate);

                    var trajectoryTimeSpan = deltaTime * 2.0f;

                    var velocity =
                        anchorTransform.transformDirection(
                            binary.GetTrajectoryVelocity(samplingTime,
                                timeHorizon * options.timeOffset, trajectoryTimeSpan));

                    linearSpeedInMetersPerSecond = math.length(velocity);

                    if (linearSpeedInMetersPerSecond >= 0.05f)
                    {
                        velocity = math.normalizesafe(velocity, float3.zero);

                        DisplayAxis(transform.t, velocity * 0.5f, options.velocityColor);
                    }
                }

                if (options.showDetails)
                {
                    DisplayDetails(ref binary, anchorTransform, options);
                }

                return linearSpeedInMetersPerSecond;
            }

            void DisplayDetails(ref Binary binary, AffineTransform anchorTransform, Options options)
            {
                void DisplayVelocity(AffineTransform rootTransform, float3 velocity)
                {
                    if (math.length(velocity) >= 0.05f)
                    {
                        velocity = math.normalizesafe(velocity, float3.zero);

                        DisplayAxis(rootTransform.t, velocity * 0.5f, options.velocityColor, 0.5f);
                    }
                }

                void DisplayForward(AffineTransform rootTransform, float3 forward)
                {
                    DisplayAxis(rootTransform.t, forward * 0.5f, options.forwardColor, 0.5f);
                }

                var timeHorizon = binary.TimeHorizon;

                ref var metric = ref binary.GetMetric(metricIndex);

                var numTrajectorySamples = metric.numTrajectorySamples;

                Assert.IsTrue(numTrajectorySamples > 0);

                var startTimeInSeconds =
                    math.min(timeHorizon, timeHorizon *
                        metric.trajectorySampleRange);

                var advanceInSeconds =
                    (timeHorizon - startTimeInSeconds) /
                    numTrajectorySamples;

                //
                // Display velocities and forward directions
                //

                var relativeTime = startTimeInSeconds;

                AffineTransform referenceTransform =
                    binary.GetTrajectoryTransform(samplingTime);

                var stride = numTrajectorySamples + 1;

                for (int i = 0; i <= numTrajectorySamples; ++i)
                {
                    AffineTransform relativeRootTransform =
                        referenceTransform.inverseTimes(
                            binary.GetTrajectoryTransform(
                                binary.Advance(samplingTime, relativeTime)));

                    AffineTransform rootTransform =
                        anchorTransform * relativeRootTransform;

                    var rootVelocity =
                        anchorTransform.transformDirection(fragment[stride * 0 + i]);

                    var rootForward =
                        anchorTransform.transformDirection(fragment[stride * 1 + i]);

                    DisplayVelocity(rootTransform, rootVelocity);
                    DisplayForward(rootTransform, rootForward);

                    relativeTime =
                        math.min(timeHorizon,
                            relativeTime + advanceInSeconds);
                }

                //
                // Display root displacement
                //

                if (metric.trajectoryDisplacements)
                {
                    relativeTime = startTimeInSeconds;

                    var previousRootPosition =
                        referenceTransform.inverseTimes(
                            binary.GetTrajectoryTransform(
                                binary.Advance(samplingTime, relativeTime))).t;

                    for (int i = 0; i < numTrajectorySamples; ++i)
                    {
                        var displacement = fragment[stride * 2 + i];

                        var rootPosition = previousRootPosition + displacement;

                        DebugDraw.DrawLine(
                            anchorTransform.transform(previousRootPosition),
                            anchorTransform.transform(rootPosition),
                            options.detailsColor);

                        previousRootPosition = rootPosition;
                    }
                }
            }

            static void DisplayAxis(float3 startPosition, float3 axis, Color color, float scale = 1.0f)
            {
                float height = 0.1f * scale;
                float width = height * 0.5f;

                var endPosition = startPosition + axis * scale;

                var length = math.length(axis) * scale;

                if (length >= height)
                {
                    var normalizedDirection = axis * math.rcp(length);

                    var rotation =
                        Missing.forRotation(Missing.up, normalizedDirection);

                    DebugDraw.DrawLine(startPosition, endPosition, color);

                    DebugDraw.DrawCone(
                        endPosition - normalizedDirection * scale * height,
                        rotation, width, height, color);
                }
            }
        }
    }
}
