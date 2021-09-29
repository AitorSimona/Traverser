namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This is the base class for rig constraints.
    /// Inherit from this class to implement custom constraints.
    /// </summary>
    /// <typeparam name="TJob">The constraint job</typeparam>
    /// <typeparam name="TData">The constraint data</typeparam>
    /// <typeparam name="TBinder">The constraint job binder</typeparam>
    public class RigConstraint<TJob, TData, TBinder> : MonoBehaviour, IRigConstraint
        where TJob    : struct, IWeightedAnimationJob
        where TData   : struct, IAnimationJobData
        where TBinder : AnimationJobBinder<TJob, TData>, new()
    {
        /// <summary>
        /// The constraint weight parameter.
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        protected float m_Weight = 1f;

        /// <summary>
        /// The constraint data.
        /// </summary>
        [SerializeField]
        protected TData m_Data;

        static readonly TBinder s_Binder = new TBinder();

        /// <summary>
        /// Resets constraint data to default values.
        /// </summary>
        public void Reset()
        {
            m_Weight = 1f;
            m_Data.SetDefaultValues();
        }

        /// <summary>
        /// Retrieves the constraint valid state.
        /// </summary>
        /// <returns>Returns true if constraint data can be successfully evaluated. Returns false otherwise.</returns>
        public bool IsValid() => m_Data.IsValid();

        /// <summary>
        /// The data container for the constraint.
        /// </summary>
        public ref TData data => ref m_Data;

        /// <summary>
        /// The constraint weight. This is a value in between 0 and 1.
        /// </summary>
        public float weight { get => m_Weight; set => m_Weight = Mathf.Clamp01(value); }

        /// <summary>
        /// Creates the animation job for this constraint.
        /// </summary>
        /// <param name="animator">The animated hierarchy Animator component</param>
        /// <returns>Returns the newly instantiated job.</returns>
        public IAnimationJob CreateJob(Animator animator)
        {
            TJob job = s_Binder.Create(animator, ref m_Data, this);

            // Bind constraint job weight property
            job.jobWeight = FloatProperty.BindCustom(
                animator,
                PropertyUtils.ConstructCustomPropertyName(this, ConstraintProperties.s_Weight)
                );

            return job;
        }

        /// <summary>
        /// Frees the specified job memory.
        /// </summary>
        /// <param name="job">The job to destroy.</param>
        public void DestroyJob(IAnimationJob job) => s_Binder.Destroy((TJob)job);

        /// <summary>
        /// Updates the specified job data.
        /// </summary>
        /// <param name="job">The job to update.</param>
        public void UpdateJob(IAnimationJob job) => s_Binder.Update((TJob)job, ref m_Data);

        /// <summary>
        /// The job binder for the constraint.
        /// </summary>
        IAnimationJobBinder IRigConstraint.binder => s_Binder;
        /// <summary>
        /// The data container for the constraint.
        /// </summary>
        IAnimationJobData IRigConstraint.data => m_Data;
        /// <summary>
        /// The component for the constraint.
        /// </summary>
        Component IRigConstraint.component => (Component)this;
    }
}
