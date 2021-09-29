using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    class SyncSceneToStreamLayer
    {
        public bool Initialize(Animator animator, IList<IRigLayer> layers)
        {
            if (isInitialized)
                return true;

            m_RigIndices = new List<int>(layers.Count);
            for (int i = 0; i < layers.Count; ++i)
            {
                if (!layers[i].IsValid())
                    continue;

                m_RigIndices.Add(i);
            }

            m_Data = RigUtils.CreateSyncSceneToStreamData(animator, layers);
            if (!m_Data.IsValid())
                return false;

            job = RigUtils.syncSceneToStreamBinder.Create(animator, m_Data);

            return (isInitialized = true);
        }

        public void Update(IList<IRigLayer> layers)
        {
            if (!isInitialized || !m_Data.IsValid())
                return;

            IRigSyncSceneToStreamData syncData = (IRigSyncSceneToStreamData)m_Data;
            for (int i = 0, count = m_RigIndices.Count; i < count; ++i)
                syncData.rigStates[i] = layers[m_RigIndices[i]].active;

            RigUtils.syncSceneToStreamBinder.Update(job, m_Data);
        }

        public void Reset()
        {
            if (!isInitialized)
                return;

            if (m_Data != null && m_Data.IsValid())
            {
                RigUtils.syncSceneToStreamBinder.Destroy(job);
                m_Data = null;
            }

            isInitialized = false;
        }

        public bool IsValid() => job != null && m_Data != null;

        public bool isInitialized { get; private set; }

        public IAnimationJob job;

        private IAnimationJobData m_Data;
        private List<int> m_RigIndices;
    }
}
