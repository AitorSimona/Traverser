using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Kinematica.Editor
{
    [Serializable]
    internal partial class Asset : ScriptableObject, ISerializationCallbackReceiver
    {
        #region deprecatedV0

        [Obsolete("animationLibrary is now stored in AssetData. Use AnimationLibrary property instead", false)]
        [SerializeField]
        List<TaggedAnimationClip> animationLibrary = new List<TaggedAnimationClip>();

        [Obsolete("destinationAvatar is now stored in AssetData. Use DestinationAvatar property instead", false)]
        [FormerlySerializedAs("targetAvatar")]
        [SerializeField]
        Avatar destinationAvatar;

        [Obsolete("metrics is now stored in AssetData. Use Metrics property instead", false)]
        [FormerlySerializedAs("tagSettings")]
        internal List<Metric> metrics = new List<Metric>();

        [Obsolete("timeHorizon is now stored in AssetData. Use TimeHorizon property instead", false)]
        [Range(0.0f, 5.0f)]
        public float timeHorizon = 1.0f;

        [Obsolete("sampleRate is now stored in AssetData. Use SampleRate property instead", false)]
        [Range(0.0f, 120.0f)]
        public float sampleRate = 30.0f;

        #endregion //deprecatedV0

        const uint kAssetRewriteVersion = 1;
        const uint kAnimationClipGuidVersion = 2;
        const uint kCachedClipVersion = 3;
        const uint kCurrentSerializeVersion = 3;
        internal const string k_DefaultMetricName = "Default";
        internal const string k_MetricsPropertyPath = "m_Data.metrics";
        internal const string k_SampleRatePropertyPath = "m_Data.sampleRate";
        internal const string k_TimeHorizonPropertyPath = "m_Data.timeHorizon";
        internal const string k_MetricsCountPropertyPath = "m_Data.metricCount";

        [SerializeField]
        AssetData m_Data;

        [SerializeField]
        uint m_SerializeVersion;

        Asset()
        {
            m_Data = AssetDataFactory.Produce();
        }

        bool m_DelayUpdateVersion;
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (!m_DelayUpdateVersion)
            {
                m_SerializeVersion = kCurrentSerializeVersion;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_SerializeVersion < kAssetRewriteVersion)
            {
                ConvertToV1();
            }

            EnsureUniqueMetricNames();

            if (m_SerializeVersion < kCurrentSerializeVersion)
            {
                m_DelayUpdateVersion = true;
                //Notify Listeners that the asset was deserialized
                EditorApplication.delayCall += () =>
                {
                    // Conversion must occur outside of a serialization call
                    if (m_SerializeVersion < kAnimationClipGuidVersion)
                    {
                        ConvertToV2();
                    }

                    if (m_SerializeVersion < kCachedClipVersion)
                    {
                        ConvertToV3();
                    }

                    NotifyTaggedClipDeserialization();
                    AssetWasDeserialized?.Invoke(this);
                    m_DelayUpdateVersion = false;

                    EditorUtility.SetDirty(this);
                };
            }
            else
            {
                NotifyTaggedClipDeserialization();
                AssetWasDeserialized?.Invoke(this);
            }
        }

        internal void OnSave()
        {
            AssetWasDeserialized?.Invoke(this);
        }

        private void EnsureUniqueMetricNames()
        {
            var uniqueNames = new HashSet<string>();
            for (int i = 0; i < m_Data.metrics.Count; i++)
            {
                string name = m_Data.metrics[i].name;

                if (!uniqueNames.Add(name))
                {
                    string newName;
                    int count = 0;
                    do
                    {
                        count++;
                        newName = string.Format("{0}{1}", name, count);
                    }
                    while (!uniqueNames.Add(newName));

                    var modified = m_Data.metrics[i];
                    modified.name = newName;
                    m_Data.metrics[i] = modified;
                }
            }
        }

        private void ConvertToV1()
        {
#pragma warning disable 612, 618
            m_Data = AssetDataFactory.ConvertToV1(animationLibrary, destinationAvatar, metrics, timeHorizon, sampleRate);
            animationLibrary = null;
            metrics = null;
            destinationAvatar = null;
            timeHorizon = -1;
            sampleRate = -1;
#pragma warning restore 612, 618
        }

        const string k_V2ConversionText = "Converting Kinematica Asset to V2";
        void ConvertToV2()
        {
            int clipCount = AnimationLibrary.Count;
            EditorUtility.DisplayProgressBar(k_V2ConversionText, string.Empty, 0f);

            for (var index = 0; index < clipCount; index++)
            {
                var tac = AnimationLibrary[index];
                EditorUtility.DisplayProgressBar(k_V2ConversionText, $"Converting {tac.ClipName} references", (float)index / clipCount);
                tac.ConvertToV2();
            }

            EditorUtility.ClearProgressBar();
        }

        void ConvertToV3()
        {
            int clipCount = AnimationLibrary.Count;
            EditorUtility.DisplayProgressBar($"Converting Kinematica Asset {name}.asset to V3", string.Empty, 0f);

            for (var index = 0; index < clipCount; index++)
            {
                var tac = AnimationLibrary[index];
                EditorUtility.DisplayProgressBar($"Converting Kinematica Asset {name}.asset to V3", $"Converting {tac.ClipName} references", (float)index / clipCount);
                tac.ConvertToV3();
            }

            EditorUtility.ClearProgressBar();
        }

        void NotifyTaggedClipDeserialization()
        {
            foreach (TaggedAnimationClip taggedClip in AnimationLibrary)
            {
                taggedClip.OnDeserialize();
            }
        }
    }
}
