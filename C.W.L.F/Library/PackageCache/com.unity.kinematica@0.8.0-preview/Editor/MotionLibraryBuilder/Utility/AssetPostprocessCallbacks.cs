using System;
using UnityEditor;

namespace Unity.Kinematica.Editor
{
    class AssetPostprocessCallbacks : AssetPostprocessor, IDisposable
    {
        public delegate void AssetPostprocessDelegate();

        public AssetPostprocessDelegate importDelegate;
        public AssetPostprocessDelegate deleteDelegate;

        public AssetPostprocessCallbacks()
        {
            m_AssetPath = null;
        }

        public AssetPostprocessCallbacks(UnityEngine.Object asset)
        {
            m_AssetPath = AssetDatabase.GetAssetPath(asset);
            staticDelegates += OnPostProcessAsset;
        }

        public void Dispose()
        {
            staticDelegates -= OnPostProcessAsset;
        }

        bool IsValid => m_AssetPath != null;

        void OnPostProcessAsset(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!IsValid)
            {
                return;
            }

            for (int i = 0; i < movedAssets.Length; ++i)
            {
                if (movedFromAssetPaths[i] == m_AssetPath)
                {
                    m_AssetPath = movedAssets[i];
                    break;
                }
            }

            if (Array.Exists(importedAssets, s => s.Equals(m_AssetPath)))
            {
                importDelegate?.Invoke();
            }

            if (Array.Exists(deletedAssets, s => s.Equals(m_AssetPath)))
            {
                deleteDelegate?.Invoke();
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            staticDelegates?.Invoke(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        delegate void StaticAssetDelegate(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths);

        static StaticAssetDelegate staticDelegates;

        string m_AssetPath;
    }
}
