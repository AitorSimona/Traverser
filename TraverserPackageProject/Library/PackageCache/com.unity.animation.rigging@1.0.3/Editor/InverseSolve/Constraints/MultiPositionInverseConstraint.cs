using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// MultiPosition inverse constraint.
    /// </summary>
    /// <seealso cref="MultiPositionConstraint"/>
    [InverseRigConstraint(typeof(MultiPositionConstraint))]
    public class MultiPositionInverseConstraint : OverrideRigConstraint<
        MultiPositionConstraint,
        MultiPositionInverseConstraintJob,
        MultiPositionConstraintData,
        MultiPositionInverseConstraintJobBinder<MultiPositionConstraintData>
        >
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseConstraint">Base constraint to override.</param>
        public MultiPositionInverseConstraint(MultiPositionConstraint baseConstraint) : base(baseConstraint) {}
    }
}

