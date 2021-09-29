using System;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This class holds the serialized data necessary to build
    /// a rig effector. This is saved in either the RigBuilder, or the Rig component.
    /// </summary>
    [Serializable]
    public class RigEffectorData
    {
        /// <summary>
        /// The effector visual style.
        /// </summary>
        [Serializable]
        public struct Style
        {
            /// <summary>The effector shape. This is represented by a Mesh.</summary>
            public Mesh shape;
            /// <summary>The effector main color.</summary>
            public Color color;
            /// <summary>The</summary>
            public float size;
            /// <summary>The position offset applied to the effector.</summary>
            public Vector3 position;
            /// <summary>The rotation offset applied to the effector.</summary>
            public Vector3 rotation;
        };

        [SerializeField] private Transform m_Transform;
        [SerializeField] private Style m_Style = new Style();
        [SerializeField] private bool m_Visible = true;

        /// <summary>The Transform represented by the effector.</summary>
        public Transform transform { get => m_Transform; }
        /// <summary>The visual style of the effector.</summary>
        public Style style { get => m_Style; }
        /// <summary>The visibility state of the effector. True if visible, false otherwise.</summary>
        public bool visible { get => m_Visible; set => m_Visible = value; }

        /// <summary>
        /// Initializes the effector with a Transform and a default style.
        /// </summary>
        /// <param name="transform">The Transform represented by the effector.</param>
        /// <param name="style">The visual style of the effector.</param>
        public void Initialize(Transform transform, Style style)
        {
            m_Transform = transform;
            m_Style = style;
        }
    }
}
