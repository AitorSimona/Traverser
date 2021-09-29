using System;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// Provides an accessor to a Transform component reference.
    /// </summary>
    public interface ITransformProvider
    {
        /// <summary>
        /// Reference to a Transform component.
        /// </summary>
        Transform transform { get; set; }
    }

    /// <summary>
    /// Provides an access to a weight value.
    /// </summary>
    public interface IWeightProvider
    {
        /// <summary>
        /// Weight. This is a number in between 0 and 1.
        /// </summary>
        float weight { get; set; }
    }

    /// <summary>
    /// Tuple of a Reference to a Transform component and a weight number.
    /// </summary>
    [System.Serializable]
    public struct WeightedTransform : ITransformProvider, IWeightProvider, IEquatable<WeightedTransform>
    {
        /// <summary>Reference to a Transform component.</summary>
        public Transform transform;
        /// <summary>Weight. This is a number be in between 0 and 1.</summary>
        public float weight;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="transform">Reference to a Transform component.</param>
        /// <param name="weight">Weight. This is a number in between 0 and 1.</param>
        public WeightedTransform(Transform transform, float weight)
        {
            this.transform = transform;
            this.weight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Returns a WeightedTransform object with an null Transform component reference and the specified weight.
        /// </summary>
        /// <param name="weight">Weight. This is a number in between 0 and 1.</param>
        /// <returns>Returns a new WeightedTransform</returns>
        public static WeightedTransform Default(float weight) => new WeightedTransform(null, weight);

        /// <summary>
        /// Compare two WeightedTransform objects for equality.
        /// </summary>
        /// <param name="other">A WeightedTransform object</param>
        /// <returns>Returns true if both WeightedTransform have the same values. False otherwise.</returns>
        public bool Equals(WeightedTransform other)
        {
            if (transform == other.transform && weight == other.weight)
                return true;

            return false;
        }

        /// <inheritdoc />
        Transform ITransformProvider.transform { get => transform; set => transform = value; }
        /// <inheritdoc />
        float IWeightProvider.weight { get => weight; set => weight = Mathf.Clamp01(value); }
    }
}
