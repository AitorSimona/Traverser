using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Unity.Kinematica.Editor
{
    class AnimationClipPostprocessor : AssetPostprocessor
    {
        class CallbackSet
        {
            public event Action onImport;
            public event Action onDelete;

            public void AddOnImport(Action onImportDelegate)
            {
                onImport += onImportDelegate;
            }

            public void AddOnDelete(Action onDeleteDelegate)
            {
                onDelete += onDeleteDelegate;
            }

            public void RemoveOnImport(Action onImportDelegate)
            {
                onImport -= onImportDelegate;
            }

            public void RemoveOnDelete(Action onDeleteDelegate)
            {
                onDelete -= onDeleteDelegate;
            }

            public void OnImport()
            {
                onImport?.Invoke();
            }

            public void OnDelete()
            {
                onDelete?.Invoke();
            }
        }

        const string kVersionPrefix = "ClipVersion=";

        static Dictionary<SerializableGuid, CallbackSet> s_CallbackSetsPerClip = new Dictionary<SerializableGuid, CallbackSet>();
        static CallbackSet s_GlobalCallbackSet = new CallbackSet();

        public static void AddOnImport(Action onImport)
        {
            s_GlobalCallbackSet.AddOnImport(onImport);
        }

        public static void AddOnImport(SerializableGuid clipGuid, Action onImport)
        {
            ChangeCallbackSet(clipGuid, set => set.AddOnImport(onImport));
        }

        public static void AddOnDelete(Action onDelete)
        {
            s_GlobalCallbackSet.AddOnDelete(onDelete);
        }

        public static void AddOnDelete(SerializableGuid clipGuid, Action onDelete)
        {
            ChangeCallbackSet(clipGuid, set => set.AddOnDelete(onDelete));
        }

        public static void RemoveOnImport(Action onImport)
        {
            s_GlobalCallbackSet.RemoveOnImport(onImport);
        }

        public static void RemoveOnImport(SerializableGuid clipGuid, Action onImport)
        {
            ChangeCallbackSet(clipGuid, set => set.RemoveOnImport(onImport));
        }

        public static void RemoveOnDelete(Action onDelete)
        {
            s_GlobalCallbackSet.RemoveOnDelete(onDelete);
        }

        public static void RemoveOnDelete(SerializableGuid clipGuid, Action onDelete)
        {
            ChangeCallbackSet(clipGuid, set => set.RemoveOnDelete(onDelete));
        }

        /// <summary>
        /// returns version of clip without loading it
        /// </summary>
        /// <param name="clipPath"></param>
        /// <returns></returns>
        public static int GetClipVersion(string clipPath)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(clipPath);
            if (assetImporter == null)
            {
                return 0;
            }

            return ParseVersionFromUserData(assetImporter.userData).Item1;
        }

        /// <summary>
        /// returns version of clip without loading it
        /// </summary>
        /// <param name="clipPath"></param>
        /// <returns></returns>
        public static int GetClipVersion(AnimationClip animationClip)
        {
            string assetPath = AssetDatabase.GetAssetPath(animationClip);
            if (string.IsNullOrEmpty(assetPath))
            {
                return 0;
            }

            return GetClipVersion(assetPath);
        }

        /// <summary>
        /// Return clip importer user data stripped of versioning
        /// </summary>
        /// <param name="clipPath"></param>
        public static string GetUserData(string clipPath)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(clipPath);
            if (assetImporter == null)
            {
                return null;
            }

            return ParseVersionFromUserData(assetImporter.userData).Item2;
        }

        void OnPreprocessAnimation()
        {
            // increment version on clip (while preserving user data)
            (int version, string userData) = ParseVersionFromUserData(assetImporter.userData);
            assetImporter.userData = $"{kVersionPrefix}{version + 1},{userData}";
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            s_GlobalCallbackSet.OnImport();
            s_GlobalCallbackSet.OnDelete();

            foreach (string path in importedAssets)
            {
                SerializableGuid guid = SerializableGuidUtility.GetSerializableGuidFromAssetPath(path);

                CallbackSet callbacks;
                if (s_CallbackSetsPerClip.TryGetValue(guid, out callbacks))
                {
                    callbacks.OnImport();
                }
            }

            foreach (string path in deletedAssets)
            {
                SerializableGuid guid = SerializableGuidUtility.GetSerializableGuidFromAssetPath(path);

                CallbackSet callbacks;
                if (s_CallbackSetsPerClip.TryGetValue(guid, out callbacks))
                {
                    callbacks.OnDelete();
                }
            }
        }

        static (int, string) ParseVersionFromUserData(string userData)
        {
            if (string.IsNullOrEmpty(userData))
            {
                return (0, "");
            }

            if (!userData.StartsWith(kVersionPrefix))
            {
                return (0, userData);
            }

            int endVersionIndex = userData.IndexOf(',');
            if (endVersionIndex < 0)
            {
                return (0, userData);
            }

            string versionString = userData.Substring(kVersionPrefix.Length, endVersionIndex - kVersionPrefix.Length);
            int version = Int32.Parse(versionString);

            return (version, userData.Substring(endVersionIndex + 1, userData.Length - (endVersionIndex + 1)));
        }

        static void ChangeCallbackSet(SerializableGuid clipGuid, Action<CallbackSet> modification)
        {
            if (!clipGuid.IsSet())
            {
                return;
            }

            CallbackSet callbackSet;
            if (!s_CallbackSetsPerClip.TryGetValue(clipGuid, out callbackSet))
            {
                callbackSet = new CallbackSet();
                s_CallbackSetsPerClip.Add(clipGuid, callbackSet);
            }

            modification?.Invoke(callbackSet);
        }
    }
}
