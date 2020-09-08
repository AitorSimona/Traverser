using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    partial class TaggedAnimationClip
    {
        internal void ConvertToV2()
        {
            if (!m_AnimationClipReference.isBroken)
            {
                SerializeGuidStr(m_AnimationClipReference.asset, ref m_AnimationClipGuid);
            }

            if (m_PreBoundaryClip != null)
            {
                SerializeGuidStr(m_PreBoundaryClip, ref m_PreBoundaryClipGuid);
            }

            if (m_PostBoundaryClip != null)
            {
                SerializeGuidStr(m_PostBoundaryClip, ref m_PostBoundaryClipGuid);
            }
        }

        internal void ConvertToV3()
        {
            AnimationClip clip = LoadAnimationClipFromGuid(m_AnimationClipGuid);
            if (clip != null)
            {
                SetClip(clip);
            }
        }

        internal void OnDeserialize()
        {
            AnimationClipPostprocessor.AddOnImport(m_AnimationClipGuid, ReloadClipAndMarkDirty);
            AnimationClipPostprocessor.AddOnDelete(m_AnimationClipGuid, OnDeleteClip);
        }

        internal void ReloadClipAndMarkDirty()
        {
            AnimationClip clip = LoadClipSync();
            SetClip(clip); // update cache info
            NotifyChanged();
        }

        internal void OnDeleteClip()
        {
            SetClip(null);
            NotifyChanged();
        }
    }
}
