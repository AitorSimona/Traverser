using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using Unity.Mathematics;
using UnityEditor;
using NUnit.Framework;
using System.IO;

namespace Unity.Kinematica.Editor
{
    internal enum ETagImportOption
    {
        Import = 0,
        AddDefaultTag,
    }

    [Serializable]
    internal partial class TaggedAnimationClip : IDisposable
    {
        [SerializeField]
        SerializableGuid m_AnimationClipGuid;

        public SerializableGuid AnimationClipGuid => m_AnimationClipGuid;

        public string AnimationClipPath => m_AnimationClipGuid.IsSet() ? AssetDatabase.GUIDToAssetPath(m_AnimationClipGuid.GetGuidStr()) : null;

        AnimationClip m_Clip;

        public bool IsClipLoaded => m_Clip != null;

        public AnimationClip GetOrLoadClipSync(bool bDisplayProgress = true)
        {
            if (m_Clip == null)
            {
                m_Clip = LoadClipSync(bDisplayProgress);
            }

            return m_Clip;
        }

        AnimationClip LoadClipSync(bool bDisplayProgress = true)
        {
            AnimationClip clip = null;

            Assert.IsTrue(Valid);
            if (Valid)
            {
                if (bDisplayProgress)
                {
                    EditorUtility.DisplayProgressBar($"Loading clip {ClipName}", string.Empty, 1.0f);
                }

                clip = LoadAnimationClipFromGuid(m_AnimationClipGuid);

                if (bDisplayProgress)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return clip;
        }

        void SetClip(AnimationClip clip)
        {
            if (clip == null)
            {
                m_Clip = null;
                return;
            }

            AnimationClipPostprocessor.RemoveOnImport(m_AnimationClipGuid, ReloadClipAndMarkDirty);
            AnimationClipPostprocessor.RemoveOnDelete(m_AnimationClipGuid, OnDeleteClip);

            m_AnimationClipGuid = SerializableGuidUtility.GetSerializableGuidFromAsset(clip);

            AnimationClipPostprocessor.AddOnImport(m_AnimationClipGuid, ReloadClipAndMarkDirty);
            AnimationClipPostprocessor.AddOnDelete(m_AnimationClipGuid, OnDeleteClip);

            m_Clip = clip;
            m_CachedClipVersion = AnimationClipPostprocessor.GetClipVersion(clip);
            m_CachedClipName = clip.name;
            m_CachedClipDuration = Utility.ComputeAccurateClipDuration(clip);
            m_CachedSampleRate = clip.frameRate;
        }

        internal const string k_MissingClipText = "Missing Animation Clip";

        [SerializeField]
        int m_CachedClipVersion;

        public int ClipVersion => m_CachedClipVersion;

        [SerializeField]
        string m_CachedClipName;

        public string ClipName => Valid ? m_CachedClipName : m_CachedClipName + " (not found)";

        [SerializeField]
        float m_CachedClipDuration;

        public float DurationInSeconds => m_CachedClipDuration;

        [SerializeField]
        float m_CachedSampleRate;

        public float SampleRate => m_CachedSampleRate;

        [SerializeField]
        private Avatar m_RetargetSourceAvatar;

        public Avatar RetargetSourceAvatar
        {
            get { return m_RetargetSourceAvatar; }
            set { m_RetargetSourceAvatar = value; NotifyChanged(); }
        }

        public bool Valid
        {
            get
            {
                if (m_AnimationClipGuid.IsSet())
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(m_AnimationClipGuid.GetGuidStr());
                    return !string.IsNullOrEmpty(assetPath) && File.Exists(assetPath);
                }

                return false;
            }
        }

        [SerializeField]
        List<TagAnnotation> tags = new List<TagAnnotation>();
        public List<TagAnnotation> Tags
        {
            get { return tags ?? (tags = new List<TagAnnotation>()); }
        }

        [SerializeField]
        List<MarkerAnnotation> markers = new List<MarkerAnnotation>();
        public List<MarkerAnnotation> Markers
        {
            get { return markers ?? (markers = new List<MarkerAnnotation>()); }
        }

        public static List<TaggedAnimationClip> BuildFromClips(List<AnimationClip> animationClips, Asset asset, ETagImportOption tagImportOption)
        {
            var taggedClips = new List<TaggedAnimationClip>();

            if (animationClips == null)
            {
                return taggedClips;
            }

            for (int i = 0; i < animationClips.Count; ++i)
            {
                var animationClip = animationClips[i];
                TaggedAnimationClip taggedClip = BuildFromClip(animationClip, asset, tagImportOption);
                taggedClips.Add(taggedClip);
            }

            return taggedClips;
        }

        void SerializeGuidStr(AnimationClip clip, ref SerializableGuid guid)
        {
            if (clip == null)
            {
                guid = new SerializableGuid();
                return;
            }

            string guidStr = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));
            if (string.IsNullOrEmpty(guidStr))
            {
                throw new InvalidOperationException();
            }

            if (!guidStr.Equals(guid.ToString()))
            {
                guid.SetGuidStr(guidStr);
                Asset.MarkDirty();
            }
        }

        public TaggedAnimationClip Clip
        {
            get { return this; }
        }

        public static TaggedAnimationClip BuildFromClip(AnimationClip animationClip, Asset asset, ETagImportOption tagImportOption)
        {
            return new TaggedAnimationClip(animationClip, asset, tagImportOption);
        }

        public Avatar GetSourceAvatar(Avatar defaultAvatar)
        {
            if (RetargetSourceAvatar != null)
            {
                return RetargetSourceAvatar;
            }

            return defaultAvatar;
        }

        [SerializeField]
        Asset m_Asset;

        internal Asset Asset
        {
            get { return m_Asset; }
        }

        TaggedAnimationClip(AnimationClip animationClip, Asset asset, ETagImportOption tagImportOption)
        {
            m_Asset = asset;
            SetClip(animationClip);

            foreach (var method in Utility.GetExtensionMethods(GetType()))
            {
                method.Invoke(null, new object[] { this, tagImportOption });
            }

            if (tagImportOption == ETagImportOption.AddDefaultTag)
            {
                object tag = DefaultTagAttribute.CreateDefaultTag();
                if (tag != null)
                {
                    TagAnnotation tagAnnotation = TagAnnotation.Create(tag.GetType(), 0.0f, DurationInSeconds);
                    tagAnnotation.payload.SetValueObject(tag);
                    AddTag(tagAnnotation);
                }
            }

            if (animationClip.isLooping)
            {
                TaggedPreBoundaryClip = this;
                TaggedPostBoundaryClip = this;
            }

            // Obsolete V1 variables
            m_PreBoundaryClip = null;
            m_PostBoundaryClip = null;
        }

        public int NumFrames
        {
            get { return Missing.truncToInt(SampleRate * DurationInSeconds); }
        }

        public int ClampedTimeInSecondsToIndex(float timeInSeconds)
        {
            int frameIndex = Missing.roundToInt(timeInSeconds * SampleRate);
            return math.clamp(frameIndex, 0, NumFrames - 1);
        }

        public int ClampedDurationInSecondsToFrames(float durationInSeconds)
        {
            int frameIndex = Missing.roundToInt(durationInSeconds * SampleRate);
            return math.clamp(frameIndex, 0, NumFrames);
        }

        public float IndexToTimeInSeconds(int index)
        {
            return index / SampleRate;
        }

        public int ClipFramesToAssetFrames(Asset asset, int numClipFrames)
        {
            float sourceSampleRate = SampleRate;
            float targetSampleRate = asset.SampleRate;
            float sampleRateRatio = sourceSampleRate / targetSampleRate;

            int numFramesDestination = Missing.roundToInt(numClipFrames / sampleRateRatio);

            return numFramesDestination;
        }

        public int AssetFramesToClipFrames(Asset asset, int numAssetFrames)
        {
            float sourceSampleRate = asset.SampleRate;
            float targetSampleRate = SampleRate;
            float sampleRateRatio = sourceSampleRate / targetSampleRate;

            int numFramesDestination = Missing.roundToInt(numAssetFrames / sampleRateRatio);

            return numFramesDestination;
        }

        public void AddTag(Type tagType, float startTime = 0f, float duration = -1f)
        {
            if (!Valid)
            {
                return;
            }

            if (duration < 0)
            {
                duration = DurationInSeconds - startTime;
            }

            int existing = tags.Count(t => t.Type == tagType &&
                FloatComparer.AreEqual(t.startTime, startTime, FloatComparer.kEpsilon) &&
                FloatComparer.AreEqual(t.duration, duration, FloatComparer.kEpsilon));
            if (existing > 0)
            {
                return;
            }

            Undo.RecordObject(m_Asset, $"Add {TagAttribute.GetDescription(tagType)} tag");

            TagAnnotation newTag = TagAnnotation.Create(tagType, startTime, duration);
            tags.Add(newTag);

            NotifyChanged();
        }

        public event Action<TagAnnotation> TagAdded;
        public event Action<TagAnnotation> TagRemoved;

        public void AddTag(TagAnnotation tag)
        {
            if (!Valid)
            {
                return;
            }

            Undo.RecordObject(m_Asset, $"Add {TagAttribute.GetDescription(tag.Type)} tag");
            tags.Add(tag);
            TagAdded?.Invoke(tag);
            NotifyChanged();
        }

        public void RemoveTag(Type type)
        {
            var matchingTags = tags.Where(t =>
                t.Type == type && FloatComparer.s_ComparerWithDefaultTolerance.Equals(t.startTime, 0f) && FloatComparer.s_ComparerWithDefaultTolerance.Equals(t.duration, Clip.DurationInSeconds)).ToList();
            if (matchingTags.Any())
            {
                Undo.RecordObject(m_Asset, $"Remove {matchingTags.Count} {TagAttribute.GetDescription(type)} tag(s)");
                foreach (var t in matchingTags)
                {
                    tags.Remove(t);
                    t.Dispose();
                }

                NotifyChanged();
            }
        }

        public void RemoveTag(TagAnnotation tag)
        {
            Undo.RecordObject(m_Asset, $"Remove {TagAttribute.GetDescription(tag.Type)} tag");
            tags.RemoveAll(x => x == tag);
            tag.Dispose();
            TagRemoved?.Invoke(tag);
            NotifyChanged();
        }

        public void RemoveTags(List<TagAnnotation> toRemove)
        {
            bool removed = false;
            foreach (TagAnnotation ta in toRemove)
            {
                int index = tags.IndexOf(ta);
                if (index >= 0)
                {
                    if (!removed)
                    {
                        Undo.RecordObject(m_Asset, $"Remove tags");
                        removed = true;
                    }

                    ta.Dispose();
                    tags.RemoveAt(index);
                }
            }
        }

        public event Action DataChanged;

        internal void NotifyChanged()
        {
            DataChanged?.Invoke();
        }

        public event Action<MarkerAnnotation> MarkerAdded;

        public MarkerAnnotation AddMarker(Type type, float timeInSeconds)
        {
            MarkerAnnotation marker = MarkerAnnotation.Create(type, timeInSeconds);
            AddMarker(marker);
            return marker;
        }

        public void AddMarker(MarkerAnnotation marker)
        {
            if (!Valid)
            {
                return;
            }

            Undo.RecordObject(m_Asset, $"Add {TagAttribute.GetDescription(marker.payload.Type)}");
            markers.Add(marker);
            MarkerAdded?.Invoke(marker);
            NotifyChanged();
        }

        public event Action<MarkerAnnotation> MarkerRemoved;

        public void RemoveMarker(MarkerAnnotation marker)
        {
            int index = markers.IndexOf(marker);
            if (index >= 0)
            {
                Undo.RecordObject(m_Asset, $"Remove {MarkerAttribute.GetDescription(marker.GetType())}");
                MarkerRemoved?.Invoke(marker);
                marker.Dispose();
                markers.RemoveAt(index);
                NotifyChanged();
            }
        }

        public void RemoveMarkers(List<MarkerAnnotation> toRemove)
        {
            bool removed = false;
            foreach (var ma in toRemove)
            {
                int index = markers.IndexOf(ma);
                if (index >= 0)
                {
                    if (!removed)
                    {
                        Undo.RecordObject(m_Asset, $"Remove markers.");
                        removed = true;
                    }

                    ma.Dispose();
                    markers.RemoveAt(index);
                }
            }
        }

        public IEnumerable<TagAnnotation> FindTagsCoveringTime(float timeInSeconds)
        {
            foreach (TagAnnotation tag in tags)
            {
                if (tag.DoesCoverTime(timeInSeconds))
                {
                    yield return tag;
                }
            }
        }

        public (int, TagAnnotation) GetMetricAndTagCoveringTime(float timeInSeconds)
        {
            foreach (var tag in FindTagsCoveringTime(timeInSeconds))
            {
                int index = GetMetricIndexCoveringTimeOnTag(tag);
                if (index >= 0)
                {
                    return (index, tag);
                }
            }

            return (-1, null);
        }

        public int GetMetricIndexCoveringTimeOnTag(TagAnnotation tag)
        {
            return GetMetricIndexCoveringTimeOnTag(TagAnnotation.GetTagTypeFullName(tag));
        }

        public int GetMetricIndexCoveringTimeOnTag(string tagTypeName)
        {
            if (!string.IsNullOrEmpty(tagTypeName))
            {
                int metricCount = Asset.Metrics.Count;
                for (int metricIndex = 0; metricIndex < metricCount; ++metricIndex)
                {
                    if (Asset.GetMetric(metricIndex).TagTypes.Contains(tagTypeName))
                    {
                        return metricIndex;
                    }
                }
            }

            return -1;
        }

        public void Dispose()
        {
            foreach (var marker in markers)
            {
                marker.Dispose();
            }

            markers.Clear();

            foreach (var tag in tags)
            {
                tag.Dispose();
            }

            tags.Clear();

            AnimationClipPostprocessor.RemoveOnImport(m_AnimationClipGuid, ReloadClipAndMarkDirty);
            AnimationClipPostprocessor.RemoveOnDelete(m_AnimationClipGuid, OnDeleteClip);
        }

        static AnimationClip LoadAnimationClipFromGuid(SerializableGuid guid)
        {
            if (guid.IsSet())
            {
                string path = AssetDatabase.GUIDToAssetPath(guid.GetGuidStr());
                if (!string.IsNullOrEmpty(path))
                {
                    return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                }
            }

            return null;
        }

        public struct ComparerByAvatar : IComparer<TaggedAnimationClip>
        {
            public int Compare(TaggedAnimationClip clip1, TaggedAnimationClip clip2)
            {
                if (clip1.RetargetSourceAvatar == clip2.RetargetSourceAvatar)
                {
                    return 0;
                }
                else if (clip1.RetargetSourceAvatar == null)
                {
                    return 1;
                }
                else if (clip2.RetargetSourceAvatar == null)
                {
                    return -1;
                }
                else
                {
                    return clip1.RetargetSourceAvatar.GetInstanceID().CompareTo(clip2.RetargetSourceAvatar.GetInstanceID());
                }
            }
        }
    }
}
