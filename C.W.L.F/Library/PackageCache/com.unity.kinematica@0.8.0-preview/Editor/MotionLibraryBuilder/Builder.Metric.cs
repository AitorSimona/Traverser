using System;
using System.Collections.Generic;

using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public class Metric
        {
            public int index;

            public float poseTimeSpan;
            public float trajectorySampleRatio;
            public float trajectorySampleRange;
            public float poseSampleRatio;
            public bool trajectoryDisplacements;

            //
            // Product Quantization Settings
            //

            public int numAttempts;
            public int numIterations;
            public int minimumNumberSamples;
            public int maximumNumberSamples;

            public List<string> joints = new List<string>();

            public List<Type> traitTypes = new List<Type>();

            public static Metric Create()
            {
                return new Metric();
            }
        }

        List<Metric> metrics = new List<Metric>();

        public void BuildMetrics()
        {
            GenerateMetrics();

            ref Binary binary = ref Binary;

            allocator.Allocate(metrics.Count, ref binary.metrics);

            int metricIndex = 0;

            foreach (var metric in metrics)
            {
                binary.metrics[metricIndex].poseTimeSpan = metric.poseTimeSpan;
                binary.metrics[metricIndex].poseSampleRatio = metric.poseSampleRatio;
                binary.metrics[metricIndex].trajectorySampleRatio = metric.trajectorySampleRatio;
                binary.metrics[metricIndex].trajectorySampleRange = metric.trajectorySampleRange;
                binary.metrics[metricIndex].trajectoryDisplacements = metric.trajectoryDisplacements;

                var numJoints = metric.joints.Count;

                allocator.Allocate(numJoints,
                    ref binary.metrics[metricIndex].joints);

                for (int i = 0; i < numJoints; ++i)
                {
                    string jointName = metric.joints[i];

                    int jointNameIndex =
                        stringTable.GetStringIndex(jointName);

                    int jointIndex =
                        binary.animationRig.GetJointIndexForNameIndex(
                            jointNameIndex);

                    binary.metrics[metricIndex].joints[i].nameIndex = jointNameIndex;
                    binary.metrics[metricIndex].joints[i].jointIndex = jointIndex;
                }

                metricIndex++;
            }
        }

        Metric FindMetricForTraitType(Type type)
        {
            foreach (var metric in metrics)
            {
                if (metric.traitTypes.Contains(type))
                {
                    return metric;
                }
            }

            return null;
        }

        void GenerateMetrics()
        {
            foreach (var source in asset.Metrics)
            {
                var metric = Metric.Create();

                metric.index = metrics.Count;
                metric.poseTimeSpan = source.numPoses / asset.SampleRate;
                metric.trajectorySampleRatio = source.trajectorySampleRatio;
                metric.trajectorySampleRange = source.trajectorySampleRange;
                metric.trajectoryDisplacements = source.trajectoryDisplacements;
                metric.poseSampleRatio = source.poseSampleRatio;

                metric.numAttempts = source.numAttempts;
                metric.numIterations = source.numIterations;
                metric.minimumNumberSamples = source.minimumNumberSamples;
                metric.maximumNumberSamples = source.maximumNumberSamples;

                foreach (string jointName in source.joints)
                {
                    if (rig.GetJointIndexFromName(jointName) < 0)
                    {
                        var avatarName = asset.DestinationAvatar.name;
                        var metricName = source.name;
                        Debug.LogError($"Joint {jointName} refered to in metric {metricName} not found in avatar {avatarName}");
                        return;
                    }

                    metric.joints.Add(jointName);
                }

                if (metric.joints.Count == 0)
                {
                    throw new Exception($"No valid joint in metric {source.name}");
                }

                foreach (var type in TagAttribute.GetTypes())
                {
                    if (source.TagTypes.Contains(type.FullName))
                    {
                        var runtimeType =
                            GetGenericPayloadArgument(type);

                        metric.traitTypes.Add(runtimeType);
                    }
                }

                metrics.Add(metric);
            }
        }

        public static Type GetGenericPayloadArgument(Type type)
        {
            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericType)
                {
                    if (i.GetGenericTypeDefinition() == typeof(Payload<>))
                    {
                        Debug.Assert(i.GetGenericArguments().Length > 0);

                        return i.GetGenericArguments()[0];
                    }
                }
            }

            return null;
        }
    }
}
