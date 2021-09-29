using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// MultiRotation inverse constraint.
    /// </summary>
    /// <seealso cref="MultiRotationConstraint"/>
    [InverseRigConstraint(typeof(MultiRotationConstraint))]
    public class MultiRotationInverseConstraint : OverrideRigConstraint<
        MultiRotationConstraint,
        MultiRotationInverseConstraintJob,
        MultiRotationConstraintData,
        MultiRotationInverseConstraintJobBinder<MultiRotationConstraintData>
        >
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseConstraint">Base constraint to override.</param>
        public MultiRotationInverseConstraint(MultiRotationConstraint baseConstraint) : base(baseConstraint) {}
    }
}

