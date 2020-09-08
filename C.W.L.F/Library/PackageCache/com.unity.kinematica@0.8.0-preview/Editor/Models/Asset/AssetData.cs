using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Unity.Kinematica.Editor
{
    [Serializable]
    internal struct AssetData
    {
        [SerializeField]
        public bool syncedWithBinary;

        [SerializeField]
        public List<TaggedAnimationClip> animationLibrary;

        [SerializeField]
        public Avatar destinationAvatar;

        [FormerlySerializedAs("policies")]
        [SerializeField]
        public List<Asset.Metric> metrics;

        [Range(0.0f, 5.0f)]
        public float timeHorizon;

        [Range(0.0f, 120.0f)]
        public float sampleRate;

        public bool IsValid()
        {
            return animationLibrary != null
                && metrics != null;
        }
    }
    internal static class AssetDataFactory
    {
        public static AssetData Produce()
        {
            return new AssetData
            {
                animationLibrary = new List<TaggedAnimationClip>(),
                metrics = new List<Asset.Metric>(),
                timeHorizon = 1.0f,
                sampleRate = 30.0f
            };
        }

        public static AssetData ConvertToV1(List<TaggedAnimationClip> clips, Avatar avatar,
            List<Asset.Metric> metrics, float timeHorizon,
            float sampleRate)
        {
            return new AssetData
            {
                animationLibrary = clips,
                metrics = metrics,
                destinationAvatar = avatar,
                timeHorizon = timeHorizon,
                sampleRate = sampleRate
            };
        }
    }
}
