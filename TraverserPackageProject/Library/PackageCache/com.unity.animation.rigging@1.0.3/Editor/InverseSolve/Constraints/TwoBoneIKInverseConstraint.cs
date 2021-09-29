using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// TwoBone IK inverse constraint.
    /// </summary>
    /// <seealso cref="TwoBoneIKConstraint"/>
    [InverseRigConstraint(typeof(TwoBoneIKConstraint))]
    public class TwoBoneIKInverseConstraint : OverrideRigConstraint<
        TwoBoneIKConstraint,
        TwoBoneIKInverseConstraintJob,
        TwoBoneIKConstraintData,
        TwoBoneIKInverseConstraintJobBinder<TwoBoneIKConstraintData>
        >
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseConstraint">Base constraint to override.</param>
        public TwoBoneIKInverseConstraint(TwoBoneIKConstraint baseConstraint) : base(baseConstraint) {}
    }
}

