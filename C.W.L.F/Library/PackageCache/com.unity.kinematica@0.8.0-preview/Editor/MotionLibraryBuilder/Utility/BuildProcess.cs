using UnityEngine;
using System.Collections;

using System.IO;
using Unity.Kinematica;
using UnityEditor;
using System;

namespace Unity.Kinematica.Editor
{
    internal class BuildProcess : IDisposable
    {
        Asset m_Asset;
        Builder m_Builder;
        IEnumerator m_BuildState;
        bool m_bFinished;

        public Builder Builder => m_Builder;

        public bool IsFinished => m_bFinished;

        public BuildProcess(Asset asset)
        {
            try
            {
                m_Asset = asset;
                m_Builder = Builder.Create(asset);

                string assetPath = asset.AssetPath;
                string filePath = asset.BinaryPath;

                var fileDirectory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }

                m_BuildState = m_Builder.BuildAsync(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message} : {e.InnerException}");

                m_Asset = null;
                m_Builder = null;
                m_BuildState = null;
                m_bFinished = true;
            }
        }

        public event Action BuildStopped;

        public void Dispose()
        {
            if (m_Builder != null)
            {
                m_Builder.Dispose();
            }

            BuildStopped?.Invoke();
        }

        public void FrameUpdate()
        {
            if (m_bFinished)
            {
                return;
            }

            try
            {
                m_bFinished = !m_BuildState.MoveNext();
                if (m_bFinished && !m_Builder.Cancelled)
                {
                    m_Asset.OnAssetBuilt();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message} : {e.InnerException}");
                m_bFinished = true;
            }
        }

        public void Cancel()
        {
            m_Builder?.Cancel();
        }
    }
}
