using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// The [InverseRigConstraint] attribute allows to match an inverse constraint (inverse solve) to its
    /// base constraint (forward solve) counterpart.  This is used in bi-directional baking to override
    /// constraints when baking animations to constraints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class InverseRigConstraintAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="targetBinderType">The base constraint type.</param>
        public InverseRigConstraintAttribute(Type targetBinderType)
        {
            if (targetBinderType == null || !typeof(IRigConstraint).IsAssignableFrom(targetBinderType))
                Debug.LogError("Invalid constraint for InverseRigConstraint attribute.");

            this.baseConstraint = targetBinderType;
        }

        /// <summary>
        /// Retrieves the base constraint type.
        /// </summary>
        public Type baseConstraint { get; }
    }
}
