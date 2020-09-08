using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal partial class Asset : ScriptableObject, ISerializationCallbackReceiver
    {
        internal float TimeHorizon
        {
            get { return m_Data.timeHorizon; }
            set
            {
                Undo.RecordObject(this, "Change Time Horizon");
                m_Data.timeHorizon = value;
                MarkDirty();
            }
        }

        internal float SampleRate
        {
            get { return m_Data.sampleRate; }
            set
            {
                Undo.RecordObject(this, "Change Sample Rate");
                m_Data.sampleRate = value;
                MarkDirty();
            }
        }

        internal List<Metric> Metrics
        {
            get { return m_Data.metrics; }
            set { m_Data.metrics = value; }
        }

        internal Avatar DestinationAvatar
        {
            get { return m_Data.destinationAvatar; }
            set
            {
                if (DestinationAvatar != value)
                {
                    Undo.RecordObject(this, "Change Destination Avatar");
                    m_Data.destinationAvatar = value;
                    AvatarModified?.Invoke();
                    MarkDirty();
                }
            }
        }

        internal delegate void AssetModifiedHandler(Asset asset);
        internal event AssetModifiedHandler AssetWasDeserialized;

        internal Avatar[] GetAvatars()
        {
            var avatars = new List<Avatar>();

            foreach (var animationClip in AnimationLibrary)
            {
                var avatar =
                    animationClip.GetSourceAvatar(
                        DestinationAvatar);

                if (!avatars.Contains(avatar))
                {
                    avatars.Add(avatar);
                }
            }

            return avatars.ToArray();
        }

        internal TaggedAnimationClip[] GetAnimationClips()
        {
            return AnimationLibrary.ToArray();
        }

        internal TaggedAnimationClip[] GetAnimationClips(Avatar avatar)
        {
            var defaultAvatar = DestinationAvatar;

            return AnimationLibrary.Where(
                clip => clip.GetSourceAvatar(defaultAvatar) == avatar).ToArray();
        }

        /// <summary>
        /// Sets the metric in the Asset.
        /// </summary>
        /// <param name="metric"></param>
        /// <remarks>If a metric with the same name exists in the asset, then it will be replaced with metric supplied as argument.</remarks>
        internal void SetMetric(Metric metric)
        {
            int count = Metrics.Count;

            int index = Metrics.FindIndex((Metric setting) => { return setting.name == metric.name; });
            if (index >= 0)
            {
                Metrics[index] = metric;
            }
            else
            {
                Metrics.Add(metric);
            }
        }

        internal bool AddMetric(Metric metric)
        {
            foreach (var setting in Metrics)
            {
                if (metric.name == setting.name)
                {
                    metric.name = ObjectNames.GetUniqueName(Metrics.Select(m => m.name).ToArray(), metric.name);
                }
            }

            Metrics.Add(metric);
            MetricsModified?.Invoke();

            MarkDirty();

            return true;
        }

        internal void RemoveMetric(int index)
        {
            if (Metrics.Count <= index)
            {
                return;
            }

            Metrics.RemoveAt(index);
            MetricsModified?.Invoke();

            MarkDirty();
        }

        internal List<TaggedAnimationClip> AnimationLibrary
        {
            get => m_Data.animationLibrary;
        }

        internal Metric GetMetric(int index)
        {
            if (index < 0 || index > m_Data.metrics.Count)
            {
                throw new System.ArgumentOutOfRangeException("index");
            }

            return Metrics[index];
        }

        internal void AssignTagToMetric(int metricIndex, Type t)
        {
            if (TagAttribute.IsTagType(t) && metricIndex >= 0 && metricIndex < Metrics.Count)
            {
                string fullName = t.FullName;

                Metric metric = Metrics[metricIndex];

                if (!metric.TagTypes.Contains(fullName))
                {
                    metric.TagTypes.Add(fullName);
                    if (metricIndex >= 0)
                    {
                        UnAssignTagFromMetricWithoutNotify(-1, t);
                    }

                    for (int i = 0; i < Metrics.Count; ++i)
                    {
                        if (i == metricIndex)
                        {
                            continue;
                        }

                        UnAssignTagFromMetricWithoutNotify(i, t);
                    }

                    MarkDirty();
                    MetricsModified?.Invoke();
                }
            }
        }

        internal Metric GetMetricForPayloadType(Type payloadType)
        {
            return GetMetricForTagType(TagAttribute.FindTypeByPayloadArgumentType(payloadType), true);
        }

        internal Metric GetMetricForTagType(Type tagType, bool bWarnMetricNotFound = false)
        {
            Type type = tagType;
            if (type != null)
            {
                string fullName = type.FullName;
                foreach (Metric m in Metrics)
                {
                    if (m.TagTypes.Contains(fullName))
                    {
                        return m;
                    }
                }

                if (bWarnMetricNotFound)
                {
                    Debug.LogWarning($"Tag type {fullName} is not associated to any metric in the asset, using default metric instead.");
                }
            }
            else
            {
                Debug.LogWarning($"Tag type {tagType} is not part of the asset, using default metric instead.");
            }

            return null;
        }

        internal void RemoveTagFromMetric(int metricIndex, Type t)
        {
            if (metricIndex >= 0 && metricIndex < Metrics.Count)
            {
                Metrics[metricIndex].TagTypes.Remove(t.FullName);
                MarkDirty();
                MetricsModified?.Invoke();
            }
        }

        internal void UnAssignTagFromMetricWithoutNotify(int metricIndex, Type t)
        {
            if (metricIndex >= 0 && metricIndex < Metrics.Count)
            {
                Metrics[metricIndex].TagTypes.Remove(t.FullName);
            }
        }

        internal BinaryReference GetBinaryReference()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            string assetGuidStr = AssetDatabase.AssetPathToGUID(assetPath);
            return new BinaryReference(assetGuidStr);
        }

        internal bool BinaryUpToDate
        {
            get
            {
                return m_Data.syncedWithBinary;
            }
        }

        //TODO - remove when UpdateTitle is removed from BuilderWindow
        internal event Action MarkedDirty;
        internal event Action AvatarModified;
        internal event Action MetricsModified;

        private List<TaggedAnimationClip> GetTaggedClipsSortedByAvatar()
        {
            List<TaggedAnimationClip> sortedClips = AnimationLibrary.ConvertAll(clip => clip);
            sortedClips.Sort(new TaggedAnimationClip.ComparerByAvatar());
            return sortedClips;
        }

        internal void RecordUndo(string text)
        {
            Undo.RecordObject(this, text);
            MarkDirty();
        }

        void OnDisable()
        {
            foreach (var clip in AnimationLibrary)
            {
                clip.DataChanged -= MarkDirty;
            }
        }

        void OnEnable()
        {
            UpdateObsoleteClips();

            foreach (var clip in AnimationLibrary)
            {
                clip.DataChanged += MarkDirty;
            }
        }

        // Check if some animation clips have been reimported while the asset was not used.
        // If so, we must update concerned tagged clip cache information
        void UpdateObsoleteClips()
        {
            foreach (TaggedAnimationClip taggedClip in AnimationLibrary)
            {
                string clipPath = taggedClip.AnimationClipPath;
                if (clipPath != null)
                {
                    int clipVersion = AnimationClipPostprocessor.GetClipVersion(clipPath);
                    if (taggedClip.ClipVersion != clipVersion)
                    {
                        taggedClip.ReloadClipAndMarkDirty();
                    }
                }
            }
        }

        internal class DoCreateKinematicaAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = ScriptableObject.CreateInstance<Asset>();
                AssetDatabase.CreateAsset(asset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }
        [MenuItem("Assets/Create/Kinematica/Asset")]
        private static void CreateAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateKinematicaAsset>(), "New Kinematica Asset.asset", null, null);
        }

        internal bool CanBuild(ref string error)
        {
            List<string> errors = new List<string>();
            if (DestinationAvatar == null)
            {
                errors.Add("No avatar rig chosen. Unable to Build Asset");
            }

            if (AnimationLibrary.Count == 0)
            {
                errors.Add("No clips added. Unable to Build Asset");
            }

            bool invalidFound = false;

            MissingAnnotationSet missingTagTypes = MissingAnnotationSet.Create();
            MissingAnnotationSet missingMarkerTypes = MissingAnnotationSet.Create();

            foreach (TaggedAnimationClip tc in AnimationLibrary)
            {
                if (!invalidFound && !tc.Valid)
                {
                    errors.Add("At least one Animation Clip is missing. Please remove any missing clips before building");
                    invalidFound = true;
                }

                foreach (var tag in tc.Tags)
                {
                    if (tag.payload.Type == null)
                    {
                        missingTagTypes.Add(tag.payload.SimplifiedTypeName, tc.ClipName, tag.startTime, tag.duration);
                    }
                }

                foreach (var marker in tc.Markers)
                {
                    if (marker.payload.Type == null)
                    {
                        missingMarkerTypes.Add(marker.payload.SimplifiedTypeName, tc.ClipName, marker.timeInSeconds, 0.0f);
                    }
                }
            }

            foreach ((string tagTypeName, List<MissingAnnotationSet.Occurence> occurences) in missingTagTypes.Annotations)
            {
                string[] occurenceStrings = occurences.Select(o => $"{o.clipName} on interval [{o.startTime},{o.startTime + o.duration}]").ToArray();
                string occurencesMsg = string.Join(", ", occurenceStrings);
                errors.Add($"{tagTypeName} tag definition missing but still used in clip(s) {occurencesMsg}. Please restore Tag type definition or remove references");
            }

            foreach ((string markerTypeName, List<MissingAnnotationSet.Occurence> occurences) in missingMarkerTypes.Annotations)
            {
                string[] occurenceStrings = occurences.Select(o => $"{o.clipName} at time {o.startTime}").ToArray();
                string occurencesMsg = string.Join(", ", occurenceStrings);
                errors.Add($"{markerTypeName} marker definition missing but still used in clip(s) {occurencesMsg}. Please restore Marker type definition or remove references");
            }

            string binaryPath = BinaryPath;

            if (File.Exists(binaryPath) && !AssetDatabase.MakeEditable(binaryPath))
            {
                errors.Add($"Cannot write to Kinematica binary asset : {binaryPath}");
            }

            string debugFilePath = Path.ChangeExtension(AssetPath, ".debug.xml");
            if (File.Exists(debugFilePath) && !AssetDatabase.MakeEditable(debugFilePath))
            {
                errors.Add($"Unable to write to Kinematica debug file : {debugFilePath}");
            }

            if (errors.Any())
            {
                error = string.Join("\n", errors);
                return false;
            }

            return true;
        }

        internal void Populate()
        {
            foreach (var method in Utility.GetExtensionMethods(GetType()))
            {
                method.Invoke(null, new object[] { this });
            }

            MarkDirty();

            EditorUtility.ClearProgressBar();
        }

        internal void Clear()
        {
            TimeHorizon = 0;
            SampleRate = 0;
            foreach (var clip in AnimationLibrary)
            {
                clip.Dispose();
            }

            AnimationLibrary.Clear();
            Metrics.Clear();
        }

        internal void MarkDirty()
        {
            MarkDirty(true);
        }

        void MarkDirty(bool desync)
        {
            if (this == null)
            {
                //TODO - this is to prevent certain Undo tests from failing - once we know why the Asset is appearing as null we should remove this
                return;
            }

            EditorUtility.SetDirty(this);
            if (desync)
            {
                m_Data.syncedWithBinary = false;
            }

            MarkedDirty?.Invoke();
        }

        internal void AddClips(IEnumerable<TaggedAnimationClip> clips)
        {
            Undo.RecordObject(this, "Add Animation Clips");
            foreach (var clip in clips)
            {
                if (AnimationLibrary.Contains(clip))
                {
                    continue;
                }

                AnimationLibrary.Add(clip);
                clip.DataChanged += MarkDirty;
            }

            MarkDirty();
        }

        internal void RemoveClips(IEnumerable<TaggedAnimationClip> clips)
        {
            Undo.RecordObject(this, "Remove Animation Clips");
            foreach (var selection in clips)
            {
                selection.DataChanged -= MarkDirty;
                selection.Dispose();
                AnimationLibrary.Remove(selection);
            }

            MarkDirty();
        }
    }
}
