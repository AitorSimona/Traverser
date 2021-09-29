namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// Interface for rig layers.
    /// </summary>
    public interface IRigLayer
    {
        /// <summary>The Rig associated to the IRigLayer</summary>
        Rig rig { get; }

        /// <summary>The list of constraints associated with the IRigLayer.</summary>
        IRigConstraint[] constraints { get; }
        /// <summary>The list of jobs built from constraints associated with the IRigLayer.</summary>
        IAnimationJob[] jobs { get; }

        /// <summary>The active state. True if the IRigLayer is active, false otherwise.</summary>
        bool active { get; }
        /// <summary>The IRigLayer name.</summary>
        string name { get; }

        /// <summary>
        /// Initializes the IRigLayer
        /// </summary>
        /// <param name="animator">The Animator used to animate the IRigLayer constraints.</param>
        /// <returns>True if IRigLayer was initialized properly, false otherwise.</returns>
        bool Initialize(Animator animator);
        /// <summary>
        /// Updates the IRigLayer jobs.
        /// </summary>
        void Update();
        /// <summary>
        /// Resets the IRigLayer.
        /// </summary>
        void Reset();

        /// <summary>
        /// Queries whether the IRigLayer is valid.
        /// </summary>
        /// <returns>True if IRigLayer is valid, false otherwise.</returns>
        bool IsValid();
    }
}
