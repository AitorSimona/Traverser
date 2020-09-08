using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    partial class TaggedAnimationClip
    {
        //TODO - these clips will eventually be applied on a per-segment basis. They are set in the timeline and can be null
        [SerializeField]
        SerializableGuid m_PreBoundaryClipGuid;

        public SerializableGuid PreBoundaryClipGuid => m_PreBoundaryClipGuid;

        [SerializeField]
        SerializableGuid m_PostBoundaryClipGuid;

        public SerializableGuid PostBoundaryClipGuid => m_PostBoundaryClipGuid;

        TaggedAnimationClip m_TaggedPreBoundaryClip;

        public TaggedAnimationClip TaggedPreBoundaryClip
        {
            get
            {
                if (m_TaggedPreBoundaryClip == null && m_PreBoundaryClipGuid.IsSet())
                {
                    if (Asset != null)
                    {
                        m_TaggedPreBoundaryClip = Asset.AnimationLibrary.FirstOrDefault(tc => tc.AnimationClipGuid == m_PreBoundaryClipGuid);
                    }
                }
                else if (m_TaggedPreBoundaryClip != null && !m_PreBoundaryClipGuid.IsSet())
                {
                    m_TaggedPreBoundaryClip = null;
                }

                return m_TaggedPreBoundaryClip;
            }
            set
            {
                Undo.RecordObject(Asset, "Set Boundary Clip");
                m_TaggedPreBoundaryClip = value;
                m_PreBoundaryClipGuid = value == null ? SerializableGuid.CreateInvalid() : value.AnimationClipGuid;
                NotifyChanged();
            }
        }

        TaggedAnimationClip m_TaggedPostBoundaryClip;

        public TaggedAnimationClip TaggedPostBoundaryClip
        {
            get
            {
                if (m_TaggedPostBoundaryClip == null && m_PostBoundaryClipGuid.IsSet())
                {
                    if (Asset != null)
                    {
                        m_TaggedPostBoundaryClip = Asset.AnimationLibrary.FirstOrDefault(tc => tc.AnimationClipGuid == m_PostBoundaryClipGuid);
                    }
                }
                else if (m_TaggedPostBoundaryClip != null && !m_PostBoundaryClipGuid.IsSet())
                {
                    m_TaggedPostBoundaryClip = null;
                }

                return m_TaggedPostBoundaryClip;
            }
            set
            {
                Undo.RecordObject(Asset, "Set Boundary Clip");
                m_TaggedPostBoundaryClip = value;
                m_PostBoundaryClipGuid = value == null ? SerializableGuid.CreateInvalid() : value.AnimationClipGuid;
                NotifyChanged();
            }
        }
    }
}
