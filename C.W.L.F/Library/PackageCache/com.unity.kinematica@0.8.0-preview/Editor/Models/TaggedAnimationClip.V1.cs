using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    partial class TaggedAnimationClip
    {
        // Obsolete V1 variables
        [SerializeField]
        AnimationClip m_PreBoundaryClip;

        [SerializeField]
        AnimationClip m_PostBoundaryClip;

        [SerializeField]
        LazyLoadReference<AnimationClip> m_AnimationClipReference;
    }
}
