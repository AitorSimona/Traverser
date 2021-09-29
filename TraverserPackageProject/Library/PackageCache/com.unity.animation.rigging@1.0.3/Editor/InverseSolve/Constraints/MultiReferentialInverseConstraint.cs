using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// MultiReferential inverse constraint.
    /// </summary>
    [InverseRigConstraint(typeof(MultiReferentialConstraint))]
    public class MultiReferentialInverseConstraint : OverrideRigConstraint<
        MultiReferentialConstraint,
        MultiReferentialInverseConstraintJob,
        MultiReferentialConstraintData,
        MultiReferentialInverseConstraintJobBinder<MultiReferentialConstraintData>
        >
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseConstraint">Base constraint to override.</param>
        public MultiReferentialInverseConstraint(MultiReferentialConstraint baseConstraint) : base(baseConstraint) {}
    }
}

