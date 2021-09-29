namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The MultiAim constraint data.
    /// </summary>
    [System.Serializable]
    public struct MultiAimConstraintData : IAnimationJobData, IMultiAimConstraintData
    {
        /// <summary>
        /// Axis type for MultiAimConstraint.
        /// </summary>
        public enum Axis
        {
            /// <summary>Positive X Axis (1, 0, 0)</summary>
            X,
            /// <summary>Negative X Axis (-1, 0, 0)</summary>
            X_NEG,
            /// <summary>Positive Y Axis (0, 1, 0)</summary>
            Y,
            /// <summary>Negative Y Axis (0, -1, 0)</summary>
            Y_NEG,
            /// <summary>Positive Z Axis (0, 0, 1)</summary>
            Z,
            /// <summary>Negative Z Axis (0, 0, -1)</summary>
            Z_NEG
        }

        /// <summary>
        /// Specifies how the world up vector used by the Multi-Aim constraint is defined.
        /// </summary>
        public enum WorldUpType
        {
            /// <summary>Neither defines nor uses a world up vector.</summary>
            None,
            /// <summary>Uses and defines the world up vector as the Unity Scene up vector (the Y axis).</summary>
            SceneUp,
            /// <summary>Uses and defines the world up vector as a vector from the constrained object, in the direction of the up object.</summary>
            ObjectUp,
            /// <summary>Uses and defines the world up vector as relative to the local space of the object.</summary>
            ObjectRotationUp,
            /// <summary>Uses and defines the world up vector as a vector specified by the user.</summary>
            Vector
        };

        [SerializeField] Transform m_ConstrainedObject;

        [SyncSceneToStream, SerializeField, Range(0, 1)] WeightedTransformArray m_SourceObjects;
        [SyncSceneToStream, SerializeField] Vector3 m_Offset;
        [SyncSceneToStream, SerializeField, Range(-180f, 180f)] float m_MinLimit;
        [SyncSceneToStream, SerializeField, Range(-180f, 180f)] float m_MaxLimit;

        [NotKeyable, SerializeField] Axis m_AimAxis;
        [NotKeyable, SerializeField] Axis m_UpAxis;

        [NotKeyable, SerializeField] WorldUpType m_WorldUpType;
        [SyncSceneToStream, SerializeField] Transform m_WorldUpObject;
        [NotKeyable, SerializeField] Axis m_WorldUpAxis;

        [NotKeyable, SerializeField] bool m_MaintainOffset;
        [NotKeyable, SerializeField] Vector3Bool m_ConstrainedAxes;

        /// <inheritdoc />
        public Transform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }

        /// <inheritdoc />
        public WeightedTransformArray sourceObjects
        {
            get => m_SourceObjects;
            set => m_SourceObjects = value;
        }

        /// <inheritdoc />
        public bool maintainOffset { get => m_MaintainOffset; set => m_MaintainOffset = value; }
        /// <summary>
        /// Post-Rotation offset applied to the constrained Transform.
        /// </summary>
        public Vector3 offset { get => m_Offset; set => m_Offset = value; }

        /// <summary>
        /// Minimum and maximum value of the rotation permitted for the constraint. The values are in degrees.
        /// </summary>
        public Vector2 limits
        {
            get => new Vector2(m_MinLimit, m_MaxLimit);

            set
            {
                m_MinLimit = Mathf.Clamp(value.x, -180f, 180f);
                m_MaxLimit = Mathf.Clamp(value.y, -180f, 180f);
            }
        }

        /// <inheritdoc cref="IMultiAimConstraintData.aimAxis"/>
        public Axis aimAxis { get => m_AimAxis; set => m_AimAxis = value; }
        /// <inheritdoc cref="IMultiAimConstraintData.upAxis"/>
        public Axis upAxis { get => m_UpAxis; set => m_UpAxis = value; }


        /// <inheritdoc cref="IMultiAimConstraintData.worldUpType"/>
        public WorldUpType worldUpType { get => m_WorldUpType; set => m_WorldUpType = value; }
        /// <inheritdoc cref="IMultiAimConstraintData.aimAxis"/>
        public Axis worldUpAxis { get => m_WorldUpAxis; set => m_WorldUpAxis = value; }
        /// <inheritdoc />
        public Transform worldUpObject { get => m_WorldUpObject; set => m_WorldUpObject = value; }

        /// <inheritdoc />
        public bool constrainedXAxis { get => m_ConstrainedAxes.x; set => m_ConstrainedAxes.x = value; }
        /// <inheritdoc />
        public bool constrainedYAxis { get => m_ConstrainedAxes.y; set => m_ConstrainedAxes.y = value; }
        /// <inheritdoc />
        public bool constrainedZAxis { get => m_ConstrainedAxes.z; set => m_ConstrainedAxes.z = value; }


        /// <inheritdoc />
        Vector3 IMultiAimConstraintData.aimAxis => Convert(m_AimAxis);
        /// <inheritdoc />
        Vector3 IMultiAimConstraintData.upAxis => Convert(m_UpAxis);
        /// <inheritdoc />
        int IMultiAimConstraintData.worldUpType => (int) m_WorldUpType;
        /// <inheritdoc />
        Vector3 IMultiAimConstraintData.worldUpAxis => Convert(m_WorldUpAxis);
        /// <inheritdoc />
        string IMultiAimConstraintData.offsetVector3Property => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_Offset));
        /// <inheritdoc />
        string IMultiAimConstraintData.minLimitFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_MinLimit));
        /// <inheritdoc />
        string IMultiAimConstraintData.maxLimitFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_MaxLimit));
        /// <inheritdoc />
        string IMultiAimConstraintData.sourceObjectsProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_SourceObjects));

        /// <inheritdoc />
        bool IAnimationJobData.IsValid()
        {
            if (m_ConstrainedObject == null || m_SourceObjects.Count == 0)
                return false;

            foreach (var src in m_SourceObjects)
                if (src.transform == null)
                    return false;

            return true;
        }

        /// <inheritdoc />
        void IAnimationJobData.SetDefaultValues()
        {
            m_ConstrainedObject = null;
            m_UpAxis = Axis.Y;
            m_AimAxis = Axis.Z;
            m_WorldUpType = WorldUpType.None;
            m_WorldUpAxis = Axis.Y;
            m_WorldUpObject = null;
            m_SourceObjects.Clear();
            m_MaintainOffset = false;
            m_Offset = Vector3.zero;
            m_ConstrainedAxes = new Vector3Bool(true);
            m_MinLimit = -180f;
            m_MaxLimit = 180f;
        }

        static Vector3 Convert(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return Vector3.right;
                case Axis.X_NEG:
                    return Vector3.left;
                case Axis.Y:
                    return Vector3.up;
                case Axis.Y_NEG:
                    return Vector3.down;
                case Axis.Z:
                    return Vector3.forward;
                case Axis.Z_NEG:
                    return Vector3.back;
                default:
                    return Vector3.up;
            }
        }
    }

    /// <summary>
    /// MultiAim constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Multi-Aim Constraint")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/constraints/MultiAimConstraint.html")]
    public class MultiAimConstraint : RigConstraint<
        MultiAimConstraintJob,
        MultiAimConstraintData,
        MultiAimConstraintJobBinder<MultiAimConstraintData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [NotKeyable, SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [NotKeyable, SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}
