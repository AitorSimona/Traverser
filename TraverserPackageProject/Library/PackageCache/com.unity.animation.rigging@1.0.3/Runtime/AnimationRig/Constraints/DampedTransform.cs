namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The DampedTransform constraint data.
    /// </summary>
    [System.Serializable]
    public struct DampedTransformData : IAnimationJobData, IDampedTransformData
    {
        [SerializeField] Transform m_ConstrainedObject;

        [SyncSceneToStream, SerializeField] Transform m_Source;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_DampPosition;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_DampRotation;

        [NotKeyable, SerializeField] bool m_MaintainAim;

        /// <inheritdoc />
        public Transform constrainedObject { get => m_ConstrainedObject; set => m_ConstrainedObject = value; }
        /// <inheritdoc />
        public Transform sourceObject { get => m_Source; set => m_Source = value; }
        /// <summary>
        /// Damp position weight. Defines how much of constrained object position follows source object position.
        /// Constrained position will closely follow source object when set to 0, and will
        /// not move when set to 1.
        /// </summary>
        public float dampPosition { get => m_DampPosition; set => m_DampPosition = Mathf.Clamp01(value); }
        /// <summary>
        /// Damp rotation weight. Defines how much of constrained object rotation follows source object rotation.
        /// Constrained rotation will closely follow source object when set to 0, and will
        /// not move when set to 1.
        /// </summary>
        public float dampRotation { get => m_DampRotation; set => m_DampRotation = Mathf.Clamp01(value); }
        /// <inheritdoc />
        public bool maintainAim { get => m_MaintainAim; set => m_MaintainAim = value; }

        /// <inheritdoc />
        string IDampedTransformData.dampPositionFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_DampPosition));
        /// <inheritdoc />
        string IDampedTransformData.dampRotationFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_DampRotation));

        /// <inheritdoc />
        bool IAnimationJobData.IsValid() => !(m_ConstrainedObject == null || m_Source == null);

        /// <inheritdoc />
        void IAnimationJobData.SetDefaultValues()
        {
            m_ConstrainedObject = null;
            m_Source = null;
            m_DampPosition = 0.5f;
            m_DampRotation = 0.5f;
            m_MaintainAim = true;
        }
    }

    /// <summary>
    /// DampedTransform constraint.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Damped Transform")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/constraints/DampedTransform.html")]
    public class DampedTransform : RigConstraint<
        DampedTransformJob,
        DampedTransformData,
        DampedTransformJobBinder<DampedTransformData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [NotKeyable, SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [NotKeyable, SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}
