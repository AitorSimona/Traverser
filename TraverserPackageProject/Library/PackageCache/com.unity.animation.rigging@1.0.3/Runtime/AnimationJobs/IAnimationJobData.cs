namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This interface is used to represent all constraint data structs.
    /// </summary>
    public interface IAnimationJobData
    {
        /// <summary>
        /// Retrieves the data valid state.
        /// </summary>
        /// <returns>Returns true if data can be successfully used in a constraint. Returns false otherwise.</returns>
        bool IsValid();
        /// <summary>
        /// Resets values to defaults.
        /// </summary>
        void SetDefaultValues();
    }
}
