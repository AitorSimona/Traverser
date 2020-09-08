using UnityEngine;

namespace Unity.SnapshotDebugger
{
    internal abstract class FrameDebugRecord
    {
        public int ProviderIdentifier => m_ProviderIdentifier;

        public string DisplayName => m_DisplayName;

        public virtual void Init(int providerIdentifier, string displayName)
        {
            m_ProviderIdentifier = providerIdentifier;
            m_DisplayName = displayName;
        }

        public abstract bool IsObsolete { get; }

        public abstract void NotifyProviderRemoved();

        public abstract void UpdateRecordEntries(float time, float frameEndTime, FrameDebugProviderInfo provider);

        public abstract void PruneFramesBeforeTimestamp(float startTimeInSeconds);

        int     m_ProviderIdentifier;
        string  m_DisplayName;
    }
}
