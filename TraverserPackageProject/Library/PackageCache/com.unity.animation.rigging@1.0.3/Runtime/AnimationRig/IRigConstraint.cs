namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This interface is used to represent all constraints classes.
    /// </summary>
    public interface IRigConstraint
    {
        /// <summary>
        /// Retrieves the constraint valid state.
        /// </summary>
        /// <returns>Returns true if constraint data can be successfully evaluated. Returns false otherwise.</returns>
        bool IsValid();

        /// <summary>
        /// Creates the animation job for this constraint.
        /// </summary>
        /// <param name="animator">The animated hierarchy Animator component.</param>
        /// <returns>Returns the newly instantiated job.</returns>
        IAnimationJob CreateJob(Animator animator);
        /// <summary>
        /// Updates the specified job data.
        /// </summary>
        /// <param name="job">The job to update.</param>
        void UpdateJob(IAnimationJob job);
        /// <summary>
        /// Frees the specified job memory.
        /// </summary>
        /// <param name="job">The job to destroy.</param>
        void DestroyJob(IAnimationJob job);

        /// <summary>
        /// The data container for the constraint.
        /// </summary>
        IAnimationJobData data { get; }
        /// <summary>
        /// The job binder for the constraint.
        /// </summary>
        IAnimationJobBinder binder { get; }

        /// <summary>
        /// The component for the constraint.
        /// </summary>
        Component component { get; }

        /// <summary>
        /// The constraint weight. This is a value in between 0 and 1.
        /// </summary>
        float weight { get; set; }
    }
}

