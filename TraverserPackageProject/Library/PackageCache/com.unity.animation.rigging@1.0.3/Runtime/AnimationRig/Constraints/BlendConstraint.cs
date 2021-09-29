namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The Blend constraint data.
    /// </summary>
    [System.Serializable]
    public struct BlendConstraintData : IAnimationJobData, IBlendConstraintData
    {
        [SerializeField] Transform m_ConstrainedObject;

        [SyncSceneToStream, SerializeField] Transform m_SourceA;
        [SyncSceneToStream, SerializeField] Transform m_SourceB;
        [SyncSceneToStream, SerializeField] bool m_BlendPosition;
        [SyncSceneToStream, SerializeField] bool m_BlendRotation;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_PositionWeight;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_RotationWeight;

        [NotKeyable, SerializeField] bool m_MaintainPositionOffsets;
        [NotKeyable, SerializeField] bool m_MaintainRotationOffsets;

        /// <inheritdoc />
        public Transform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }
        /// <inheritdoc />
        public Transform sourceObjectA { get => m_SourceA; set => m_SourceA = value; }
        /// <inheritdoc />
        public Transform sourceObjectB { get => m_SourceB; set => m_SourceB = value; }
        /// <summary>Toggles whether position is blended in the constraint.</summary>
        public bool blendPosition { get => m_BlendPosition; set => m_BlendPosition = value; }
        /// <summary>Toggles whether rotation is blended in the constraint.</summary>
        public bool blendRotation { get => m_BlendRotation; set => m_BlendRotation = value; }
        /// <summary>
        /// Specifies the weight with which to blend position.
        /// A weight of zero will result in the position of sourceObjectA, while a weight of one will result in the position of sourceObjectB.
        /// </summary>
        public float positionWeight { get => m_PositionWeight; set => m_PositionWeight = Mathf.Clamp01(value); }
        /// <summary>
        /// Specifies the weight with which to blend rotation.
        /// A weight of zero will result in the rotation of sourceObjectA, while a weight of one will result in the rotation of sourceObjectB.
        /// </summary>
        public float rotationWeight { get => m_RotationWeight; set => m_RotationWeight = Mathf.Clamp01(value); }
        /// <inheritdoc />
        public bool maintainPositionOffsets { get => m_MaintainPositionOffsets; set => m_MaintainPositionOffsets = value; }
        /// <inheritdoc />
        public bool maintainRotationOffsets { get => m_MaintainRotationOffsets; set => m_MaintainRotationOffsets = value; }

        /// <inheritdoc />
        string IBlendConstraintData.blendPositionBoolProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_BlendPosition));
        /// <inheritdoc />
        string IBlendConstraintData.blendRotationBoolProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_BlendRotation));
        /// <inheritdoc />
        string IBlendConstraintData.positionWeightFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_PositionWeight));
        /// <inheritdoc />
        string IBlendConstraintData.rotationWeightFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_RotationWeight));

        /// <inheritdoc />
        bool IAnimationJobData.IsValid() => !(m_ConstrainedObject == null || m_SourceA == null || m_SourceB == null);

        /// <inheritdoc />
        void IAnimationJobData.SetDefaultValues()
        {
            m_ConstrainedObject = null;
            m_SourceA = null;
            m_SourceB = null;
            m_BlendPosition = true;
            m_BlendRotation = true;
            m_PositionWeight = 0.5f;
            m_RotationWeight = 0.5f;
            m_MaintainPositionOffsets = false;
            m_MaintainRotationOffsets = false;
        }
    }

    /// <summary>
    /// Blend constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Blend Constraint")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/constraints/BlendConstraint.html")]
    public class BlendConstraint : RigConstraint<
        BlendConstraintJob,
        BlendConstraintData,
        BlendConstraintJobBinder<BlendConstraintData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [NotKeyable, SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [NotKeyable, SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}
