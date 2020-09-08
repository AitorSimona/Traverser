using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    partial class Asset : ScriptableObject
    {
        public string AssetPath
        {
            get { return AssetDatabase.GetAssetPath(this); }
        }

        public string BinaryPath
        {
            get
            {
                string assetGuid = AssetDatabase.AssetPathToGUID(AssetPath);
                return Path.GetFullPath(Binary.GetBinaryFilePathFromAssetGuid(assetGuid));
            }
        }

        /// <summary>
        /// TaggedAnimationClip that corresponds to the supplied AnimationClip
        /// </summary>
        /// <param name=clip>AnimationClip to use</param>
        /// <returns>Returns the TaggedAnimationClip associated with the supplied AnimationClip</returns>
        public bool ContainsAnimationClip(AnimationClip clip)
        {
            SerializableGuid clipGuid = SerializableGuidUtility.GetSerializableGuidFromAsset(clip);
            return AnimationLibrary.Any((TaggedAnimationClip taggedClip) => { return taggedClip.AnimationClipGuid == clipGuid; });
        }

        /// <summary>
        /// Finds the TaggedAnimationClip that corresponds to the supplied AnimationClip
        /// </summary>
        /// <param name=clip>AnimationClip to use</param>
        /// <returns>Returns the TaggedAnimationClip associated with the supplied AnimationClip</returns>
        internal TaggedAnimationClip FindTaggedAnimationClip(AnimationClip clip)
        {
            SerializableGuid clipGuid = SerializableGuidUtility.GetSerializableGuidFromAsset(clip);
            return AnimationLibrary.FirstOrDefault((TaggedAnimationClip taggedClip) => { return taggedClip.AnimationClipGuid == clipGuid; });
        }

        /// <summary>
        /// Creates and adds a new TaggedAnimationClip in the asset from an AnimationClip
        /// </summary>
        /// <completionlist cref=""/>
        /// <param name=clip>AnimationClip to use. Must be an asset on disk</param>
        /// <exception cref="System.ArgumentNullException">Thrown if argument clip is null</exception>
        /// <exception cref="System.ArgumentException">Thrown if argument clip no an asset on disk</exception>
        /// <returns>Returns the TaggedAnimationClip created from the supplied AnimationClip</returns>
        internal TaggedAnimationClip AddAnimationClip(AnimationClip clip)
        {
            if (clip == null)
                throw new ArgumentNullException("clip");

            TaggedAnimationClip taggedAnimationClip = null;

            SerializableGuid clipGuid = SerializableGuidUtility.GetSerializableGuidFromAsset(clip);
            if (!clipGuid.IsSet())
            {
                throw new ArgumentException("argument \"clip\" must be an asset on the disk");
            }

            //Don't add existing AnimationClip to library
            if (AnimationLibrary.Any((TaggedAnimationClip taggedClip) => { return taggedClip.AnimationClipGuid == clipGuid; }))
                return null;

            try
            {
                taggedAnimationClip = TaggedAnimationClip.BuildFromClip(clip, this, ETagImportOption.Import);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("argument \"clip\" must be an asset on disk");
            }

            Undo.RecordObject(this, string.Format("Add Animation Clip {0}", clip.name));
            AnimationLibrary.Add(taggedAnimationClip);
            taggedAnimationClip.DataChanged += MarkDirty;

            MarkDirty();
            return taggedAnimationClip;
        }

        /// <summary>
        /// Removes the TaggedAnimationClip that corresponds to the supplied AnimationClip from the asset
        /// </summary>
        /// <param name=clip>AnimationClip to use</param>
        /// <returns>true if the clip was found and removed, false otherwise</returns>
        public bool RemoveAnimationClip(AnimationClip clip)
        {
            if (clip == null)
                throw new ArgumentNullException("clip");

            SerializableGuid clipGuid = SerializableGuidUtility.GetSerializableGuidFromAsset(clip);

            Undo.RecordObject(this, string.Format("Remove Animation Clip {0}", clip.name));

            int removed = AnimationLibrary.RemoveAll((TaggedAnimationClip tagged) => { return tagged.AnimationClipGuid == clipGuid; });

            if (removed > 0)
            {
                MarkDirty();
            }

            return removed > 0;
        }

        public event Action BuildStarted;
        public event Action BuildStopped;


        BuildProcess m_CurrentBuildProcess;

        public bool BuildInProgress => m_CurrentBuildProcess != null;

        public BuildProcess BuildAsync()
        {
            string errors = null;
            if (!CanBuild(ref errors))
            {
                Debug.LogError(errors);
                return null;
            }

            BuildStarted?.Invoke();

            m_CurrentBuildProcess = new BuildProcess(this);
            m_CurrentBuildProcess.BuildStopped += OnBuildStopped;
            return m_CurrentBuildProcess;
        }

        void OnBuildStopped()
        {
            m_CurrentBuildProcess.BuildStopped -= OnBuildStopped;
            m_CurrentBuildProcess = null;
            BuildStopped?.Invoke();
        }

        /// <summary>
        /// Builds the binary for this asset
        /// </summary>
        public void BuildSync()
        {
            EditorUtility.DisplayProgressBar($"Building Kinematica Asset {name}.asset", "", 0.0f);

            using (BuildProcess buildProcess = BuildAsync())
            {
                BuildStarted?.Invoke();
                buildProcess.Builder.progressFeedback += progressInfo => EditorUtility.DisplayProgressBar($"Building Kinematica Asset {name}.asset", progressInfo.title, progressInfo.progress);

                while (!buildProcess.IsFinished)
                {
                    buildProcess.FrameUpdate();
                }

                BuildStopped?.Invoke();
            }

            EditorUtility.ClearProgressBar();
        }

        public void OnAssetBuilt()
        {
            MarkDirty(false);
            m_Data.syncedWithBinary = true;
        }
    }
}
