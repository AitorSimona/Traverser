namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The TwoBoneIK constraint data.
    /// </summary>
    [System.Serializable]
    public struct TwoBoneIKConstraintData : IAnimationJobData, ITwoBoneIKConstraintData
    {
        [SerializeField] Transform m_Root;
        [SerializeField] Transform m_Mid;
        [SerializeField] Transform m_Tip;

        [SyncSceneToStream, SerializeField] Transform m_Target;
        [SyncSceneToStream, SerializeField] Transform m_Hint;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_TargetPositionWeight;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_TargetRotationWeight;
        [SyncSceneToStream, SerializeField, Range(0f, 1f)] float m_HintWeight;

        [NotKeyable, SerializeField] bool m_MaintainTargetPositionOffset;
        [NotKeyable, SerializeField] bool m_MaintainTargetRotationOffset;

        /// <inheritdoc />
        public Transform root { get => m_Root; set => m_Root = value; }
        /// <inheritdoc />
        public Transform mid { get => m_Mid; set => m_Mid = value; }
        /// <inheritdoc />
        public Transform tip { get => m_Tip; set => m_Tip = value; }
        /// <inheritdoc />
        public Transform target { get => m_Target; set => m_Target = value; }
        /// <inheritdoc />
        public Transform hint { get => m_Hint; set => m_Hint = value; }

        /// <summary>The weight for which target position has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public float targetPositionWeight { get => m_TargetPositionWeight; set => m_TargetPositionWeight = Mathf.Clamp01(value); }
        /// <summary>The weight for which target rotation has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public float targetRotationWeight { get => m_TargetRotationWeight; set => m_TargetRotationWeight = Mathf.Clamp01(value); }
        /// <summary>The weight for which hint transform has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public float hintWeight { get => m_HintWeight; set => m_HintWeight = Mathf.Clamp01(value); }

        /// <inheritdoc />
        public bool maintainTargetPositionOffset { get => m_MaintainTargetPositionOffset; set => m_MaintainTargetPositionOffset = value; }
        /// <inheritdoc />
        public bool maintainTargetRotationOffset { get => m_MaintainTargetRotationOffset; set => m_MaintainTargetRotationOffset = value; }

        /// <inheritdoc />
        string ITwoBoneIKConstraintData.targetPositionWeightFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_TargetPositionWeight));
        /// <inheritdoc />
        string ITwoBoneIKConstraintData.targetRotationWeightFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_TargetRotationWeight));
        /// <inheritdoc />
        string ITwoBoneIKConstraintData.hintWeightFloatProperty => PropertyUtils.ConstructConstraintDataPropertyName(nameof(m_HintWeight));

        /// <inheritdoc />
        bool IAnimationJobData.IsValid() => (m_Tip != null && m_Mid != null && m_Root != null && m_Target != null && m_Tip.IsChildOf(m_Mid) && m_Mid.IsChildOf(m_Root));

        /// <inheritdoc />
        void IAnimationJobData.SetDefaultValues()
        {
            m_Root = null;
            m_Mid = null;
            m_Tip = null;
            m_Target = null;
            m_Hint = null;
            m_TargetPositionWeight = 1f;
            m_TargetRotationWeight = 1f;
            m_HintWeight = 1f;
            m_MaintainTargetPositionOffset = false;
            m_MaintainTargetRotationOffset = false;
        }
    }

    /// <summary>
    /// TwoBoneIK constraint
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Two Bone IK Constraint")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/constraints/TwoBoneIKConstraint.html")]
    public class TwoBoneIKConstraint : RigConstraint<
        TwoBoneIKConstraintJob,
        TwoBoneIKConstraintData,
        TwoBoneIKConstraintJobBinder<TwoBoneIKConstraintData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
        [NotKeyable, SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
        [NotKeyable, SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}
