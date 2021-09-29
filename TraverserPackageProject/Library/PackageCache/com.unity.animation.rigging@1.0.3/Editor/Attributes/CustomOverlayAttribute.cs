using System;
using UnityEngine;

namespace UnityEditor.Animations.Rigging
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class CustomOverlayAttribute : Attribute
    {
        private Type m_EffectorType;

        public CustomOverlayAttribute(Type effectorType)
        {
            m_EffectorType = effectorType;
        }

        public Type effectorType { get => m_EffectorType; }
    }
}
