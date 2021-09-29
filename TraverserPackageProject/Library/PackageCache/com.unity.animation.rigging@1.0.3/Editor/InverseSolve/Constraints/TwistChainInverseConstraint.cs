using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// TwistChain inverse constraint.
    /// </summary>
    /// <seealso cref="TwistChainConstraint"/>
    [InverseRigConstraint(typeof(TwistChainConstraint))]
    public class TwistChainInverseConstraint : OverrideRigConstraint<
        TwistChainConstraint,
        TwistChainInverseConstraintJob,
        TwistChainConstraintData,
        TwistChainInverseConstraintJobBinder<TwistChainConstraintData>
        >
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseConstraint">Base constraint to override.</param>
        public TwistChainInverseConstraint(TwistChainConstraint baseConstraint) : base(baseConstraint) {}
    }
}

