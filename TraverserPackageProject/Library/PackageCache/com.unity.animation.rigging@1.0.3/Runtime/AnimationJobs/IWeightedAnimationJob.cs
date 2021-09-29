namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This interface represents an animation job with a weight value.
    /// </summary>
    public interface IWeightedAnimationJob : IAnimationJob
    {
        /// <summary>The main weight given to the constraint. This is a value in between 0 and 1.</summary>
        FloatProperty jobWeight { get; set; }
    }
}
