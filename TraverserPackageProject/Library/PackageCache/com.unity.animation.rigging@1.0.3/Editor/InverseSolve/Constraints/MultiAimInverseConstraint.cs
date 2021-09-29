using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// MultiAim inverse constraint.
    /// </summary>
    /// <seealso cref="MultiAimConstraint"/>
    [InverseRigConstraint(typeof(MultiAimConstraint))]
    public class MultiAimInverseConstraint : OverrideRigConstraint<
        MultiAimConstraint,
        MultiAimInverseConstraintJob,
        MultiAimConstraintData,
        MultiAimInverseConstraintJobBinder<MultiAimConstraintData>
        >
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseConstraint">Base constraint to override.</param>
        public MultiAimInverseConstraint(MultiAimConstraint baseConstraint) : base(baseConstraint) {}
    }
}

