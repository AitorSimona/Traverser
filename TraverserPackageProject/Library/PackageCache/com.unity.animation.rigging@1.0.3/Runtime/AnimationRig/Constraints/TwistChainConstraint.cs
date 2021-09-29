namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The TwistChain constraint data.
    /// </summary>
    [System.Serializable]
    public struct TwistChainConstraintData : IAnimationJobData, ITwistChainConstraintData
    {
        [SerializeField] private Transform m_Root;
        [SerializeField] private Transform m_Tip;

        [SyncSceneToStream, SerializeField] private Transform m_RootTarget;
        [SyncSceneToStream, SerializeField] private Transform m_TipTarget;

        [SerializeField] private AnimationCurve m_Curve;

        /// <inheritdoc />
        public Transform root { get => m_Root; set => m_Root = value; }
        /// <inheritdoc />
        public Transform tip { get => m_Tip; set => m_Tip = value; }

        /// <inheritdoc />
        public Transform rootTarget { get => m_RootTarget; set => m_RootTarget = value; }
        /// <inheritdoc />
        public Transform tipTarget { get => m_TipTarget; set => m_TipTarget = value; }

        /// <inheritdoc />
        public AnimationCurve curve { get => m_Curve; set => m_Curve = value; }

        /// <inheritdoc />
        bool IAnimationJobData.IsValid() => !(root == null || tip == null || !tip.IsChildOf(root) || rootTarget == null || tipTarget == null || curve == null);

        /// <inheritdoc />
        void IAnimationJobData.SetDefaultValues()
        {
            root = tip = rootTarget = tipTarget = null;
            curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }

    /// <summary>
    /// TwistChain constraint
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Twist Chain Constraint")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/constraints/TwistChainConstraint.html")]
    public class TwistChainConstraint : RigConstraint<
        TwistChainConstraintJob,
        TwistChainConstraintData,
        TwistChainConstraintJobBinder<TwistChainConstraintData>
        >
    {
    #if UNITY_EDITOR
    #pragma warning disable 0414
            [NotKeyable, SerializeField, HideInInspector] bool m_SourceObjectsGUIToggle;
            [NotKeyable, SerializeField, HideInInspector] bool m_SettingsGUIToggle;
    #endif
    }
}
