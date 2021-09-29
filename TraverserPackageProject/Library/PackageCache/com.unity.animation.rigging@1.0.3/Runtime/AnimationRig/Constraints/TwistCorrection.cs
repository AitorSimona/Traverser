namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The TwistCorrection constraint data.
    /// </summary>
    [System.Serializable]
    public struct TwistCorrectionData : IAnimationJobData, ITwistCorrectionData
    {
        /// <summary>
        /// Axis type for TwistCorrection.
        /// </summary>
        public enum Axis
        {
            /// <summary>X Axis.</summary>
            X,
            /// <summary>Y Axis.</summary>
            Y,
            /// <summary>Z Axis.</summary>
            Z
        }

        [SyncSceneToStream, SerializeField] Transform m_Source;

        [NotKeyable, SerializeField] Axis m_TwistAxis;
        [SyncSceneToStream, SerializeField, Range(-1, 1)] WeightedTransformArray m_TwistNodes;

        /// <summary>The source Transform that influences the twist nodes.</summary>
        public Transform sourceObject { get => m_Source; set => m_Source = value; }

        /// <inheritdoc />
        public WeightedTransformArray twistNodes
        {
            get => m_TwistNodes;
            set => m_TwistNodes = value;
        }

        /// <inheritdoc />
        public Axis twistAxis { get => m_TwistAxis; set => m_TwistAxis = value; }

        /// <inheritdoc />
        Transform ITwistCorrectionData.source => m_Source;
        /// <inheritdoc />
        Vector3 ITwistCorrectionData.twistAxis => Convert(m_TwistAxis);

        /// <inheritdoc />
        string ITwistCorrectionData.twistNodesProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_TwistNodes));

        static Vector3 Convert(Axis axis)
        {
            if (axis == Axis.X)
                return Vector3.right;

            if (axis == Axis.Y)
                return Vector3.up;

            return Vector3.forward;
        }

        /// <inheritdoc />
        bool IAnimationJobData.IsValid()
        {
            if (m_Source == null)
                return false;

            for (int i = 0; i < m_TwistNodes.Count; ++i)
                if (m_TwistNodes[i].transform == null)
                    return false;

            return true;
        }

        /// <inheritdoc />
        void IAnimationJobData.SetDefaultValues()
        {
            m_Source = null;
            m_TwistAxis = Axis.Z;
            m_TwistNodes.Clear();
        }
    }

    /// <summary>
    /// TwistCorrection constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Twist Correction")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/constraints/TwistCorrection.html")]
    public class TwistCorrection : RigConstraint<
        TwistCorrectionJob,
        TwistCorrectionData,
        TwistCorrectionJobBinder<TwistCorrectionData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [NotKeyable, SerializeField, HideInInspector] bool m_TwistNodesGUIToggle;
    #endif
    }
}
