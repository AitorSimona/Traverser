using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal partial class Asset
    {
        [Serializable]
        public class Metric
        {
            [Tooltip("Metric name.")]
            public string name;

            [Serializable]
            internal class TagTypeMask
            {
                [SerializeField]
                List<string> m_TagTypeNames;

                public List<string> tagTypeNames
                {
                    get { return m_TagTypeNames ?? (m_TagTypeNames = new List<string>()); }
                }

                public bool Contains(string name)
                {
                    return tagTypeNames.Contains(name);
                }

                public void Add(string name)
                {
                    tagTypeNames.Add(name);
                    MaskTypeNames();
                }

                public void Remove(string name)
                {
                    bool removed = tagTypeNames.Remove(name);
                    if (removed)
                    {
                        MaskTypeNames();
                    }
                }

                public void Clear()
                {
                    tagTypeNames.Clear();
                    MaskTypeNames();
                }

                static List<string> k_TagTypes;

                static List<string> TagTypeNamesCache
                {
                    get
                    {
                        if (k_TagTypes == null)
                        {
                            k_TagTypes = new List<string>();
                            foreach (Type t in TagAttribute.GetVisibleTypesInInspector())
                            {
                                k_TagTypes.Add(t.FullName);
                            }
                        }

                        return k_TagTypes;
                    }
                }

                internal int MaskTypeNames()
                {
                    Dictionary<string, bool> typeAssigned = TagTypeNamesCache.ToDictionary(name => name, name => false);
                    foreach (string s in tagTypeNames)
                    {
                        if (typeAssigned.ContainsKey(s))
                        {
                            typeAssigned[s] = true;
                        }
                    }

                    return BitMask.MaskFlags(typeAssigned.Values.ToList());
                }
            }

            [SerializeField]
            TagTypeMask tagTypes;

            internal TagTypeMask TagTypes
            {
                get { return tagTypes ?? (tagTypes = new TagTypeMask()); }
            }

            [Header("Pose Similarity")]
            [Tooltip("Number of poses taken into account when matching poses.")]
            [Range(0, 10)]
            public int numPoses;

            [Tooltip("Sample ratio controlling the number of samples for pose matching.")]
            [Range(0, 1)]
            public float poseSampleRatio;

            [Tooltip("Joints to consider for this policy.")]
            public List<string> joints;

            [Header("Trajectory Similarity")]
            [Tooltip("Sample ratio controlling the number of samples for trajectory matching.")]
            [Range(0, 1)]
            public float trajectorySampleRatio;

            [Tooltip("Denotes the sampling range for trajectory matching.")]
            [Range(-1, 1)]
            public float trajectorySampleRange;

            [Tooltip("If enabled, uses root displacements in addition to velocities for trajectory matching.")]
            public bool trajectoryDisplacements;

            [Header("Product Quantization Settings")]
            [Tooltip("Number of clustering attemnpts.")]
            public int numAttempts;

            [Tooltip("Number of iterations.")]
            public int numIterations;

            [Tooltip("Minimum number of samples for training.")]
            public int minimumNumberSamples;

            [Tooltip("Maximum number of samples for training.")]
            public int maximumNumberSamples;

            public bool IsValid()
            {
                return joints != null
                    && name != "";
            }

            public static Metric Copy(Metric src)
            {
                var metric = new Metric();

                metric.name = src.name;
                foreach (string tagType in src.TagTypes.tagTypeNames)
                {
                    metric.TagTypes.Add(tagType);
                }

                metric.numPoses = src.numPoses;
                metric.poseSampleRatio = src.poseSampleRatio;
                metric.joints = new List<string>(src.joints.Count);
                foreach (var joint in src.joints)
                {
                    metric.joints.Add(joint);
                }

                metric.trajectorySampleRatio = src.trajectorySampleRatio;
                metric.trajectorySampleRange = src.trajectorySampleRange;
                metric.trajectoryDisplacements = src.trajectoryDisplacements;
                metric.numAttempts = src.numAttempts;
                metric.numIterations = src.numIterations;
                metric.minimumNumberSamples = src.minimumNumberSamples;
                metric.maximumNumberSamples = src.maximumNumberSamples;

                return metric;
            }
        }

        const string defaultMetricName = "Default";

        internal static readonly Metric k_DefaultMetric = new Metric
        {
            name = defaultMetricName,
            numPoses = 2,
            trajectorySampleRatio = 0.3333f,
            trajectorySampleRange = -1.0f,
            trajectoryDisplacements = false,
            poseSampleRatio = 0.3333f,
            numAttempts = 1,
            numIterations = 25,
            minimumNumberSamples = 32,
            maximumNumberSamples = 256,
            joints = new List<string>()
        };
    }
}
