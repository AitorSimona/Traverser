namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// Use this class to define an override rig constraint on another rig constraint.
    /// While the overriden constraint data remains the same, the job is overriden.
    /// </summary>
    /// <typeparam name="TConstraint">The base constraint to override</typeparam>
    /// <typeparam name="TJob">The override job</typeparam>
    /// <typeparam name="TData">The constraint data</typeparam>
    /// <typeparam name="TBinder">The override constraint job binder</typeparam>
    public class OverrideRigConstraint<TConstraint, TJob, TData, TBinder> : IRigConstraint
        where TConstraint : MonoBehaviour, IRigConstraint
        where TJob        : struct, IWeightedAnimationJob
        where TData       : struct, IAnimationJobData
        where TBinder     : AnimationJobBinder<TJob, TData>, new()
    {
        /// <summary>
        /// The base constraint.
        /// </summary>
        [SerializeField]
        protected TConstraint m_Constraint;

        static readonly TBinder s_Binder = new TBinder();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseConstraint">The constraint to override.</param>
        public OverrideRigConstraint(TConstraint baseConstraint)
        {
            m_Constraint = baseConstraint;
        }

        /// <summary>
        /// Creates the animation job for this constraint.
        /// </summary>
        /// <param name="animator">The animated hierarchy Animator component.</param>
        /// <returns>Returns the newly instantiated job.</returns>
        public IAnimationJob CreateJob(Animator animator)
        {
            IAnimationJobBinder binder = (IAnimationJobBinder)s_Binder;
            TJob job = (TJob)binder.Create(animator, m_Constraint.data, m_Constraint);

            // Bind constraint job weight property
            job.jobWeight = FloatProperty.BindCustom(
                animator,
                PropertyUtils.ConstructCustomPropertyName(m_Constraint, ConstraintProperties.s_Weight)
                );

            return job;
        }

        /// <summary>
        /// Frees the specified job memory.
        /// </summary>
        /// <param name="job"></param>
        public void DestroyJob(IAnimationJob job) => s_Binder.Destroy((TJob)job);

        /// <summary>
        /// Updates the specified job data.
        /// </summary>
        /// <param name="job"></param>
        public void UpdateJob(IAnimationJob job)
        {
            IAnimationJobBinder binder = (IAnimationJobBinder)s_Binder;
            binder.Update(job, m_Constraint.data);
        }

        /// <summary>
        /// Retrieves the constraint valid state.
        /// </summary>
        /// <returns>Returns true if constraint data can be successfully evaluated. Returns false otherwise.</returns>
        public bool IsValid()
        {
            return m_Constraint.IsValid();
        }

        /// <summary>
        /// The job binder for the constraint.
        /// </summary>
        IAnimationJobBinder IRigConstraint.binder => s_Binder;
        /// <summary>
        /// The data container for the constraint.
        /// </summary>
        IAnimationJobData IRigConstraint.data => m_Constraint.data;
        /// <summary>
        /// The component for the constraint.
        /// </summary>
        Component IRigConstraint.component => m_Constraint.component;

        /// <summary>
        /// The constraint weight. This is a value between 0 and 1.
        /// </summary>
        public float weight { get => m_Constraint.weight; set => m_Constraint.weight = value; }
    }
}
