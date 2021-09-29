using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// Attribute that can be placed on BakeParameters. The attribute is used to declare to which RigConstraint the BakeParameters belong.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class BakeParametersAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="constraintType">The RigConstraint to which the BakeParameters belong.</param>
        public BakeParametersAttribute(Type constraintType)
        {
            if (constraintType == null || !typeof(IRigConstraint).IsAssignableFrom(constraintType))
                Debug.LogError("Invalid constraint for InverseRigConstraint attribute.");

            this.constraintType = constraintType;
        }

        /// <summary>
        /// The RigConstraint to which the BakeParameters belong.
        /// </summary>
        public Type constraintType { get; }
    }

    /// <summary>
    /// Class that holds bi-directional baking capabilities and curve bindings of a RigConstraint.
    /// </summary>
    /// <typeparam name="T">The Type of RigConstraint the parameters belong to.</typeparam>
    public abstract class BakeParameters<T> : IBakeParameters
        where T : IRigConstraint
    {
        /// <summary>
        /// Boolean used to determine if the RigConstraint can transfer motion from itself to the skeleton
        /// </summary>
        public abstract bool canBakeToSkeleton { get; }
        /// <summary>
        /// Boolean used to determine if the RigConstraint can transfer motion to itself from the skeleton.
        /// </summary>
        public abstract bool canBakeToConstraint { get; }

        /// <summary>
        /// Collects the editor curve bindings for all the properties that this RigConstraint modifies when transferring motion to the skeleton.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder which the constraint is part of.</param>
        /// <param name="constraint">The RigConstraint for which the bindings should be collected.</param>
        /// <returns></returns>
        public abstract IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, T constraint);
        /// <summary>
        /// Collects the editor curve bindings for all the properties that this RigConstraint modifies when transferring motion to this constraint.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder which the constraint is part of.</param>
        /// <param name="constraint">The RigConstraint for which the bindings should be collected.</param>
        /// <returns></returns>
        public abstract IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, T constraint);


        /// <inheritdoc />
        bool IBakeParameters.canBakeToSkeleton => canBakeToSkeleton;
        /// <inheritdoc />
        bool IBakeParameters.canBakeToConstraint => canBakeToConstraint;

        /// <inheritdoc />
        IEnumerable<EditorCurveBinding> IBakeParameters.GetSourceCurveBindings(RigBuilder rigBuilder, IRigConstraint constraint)
        {
            Debug.Assert(constraint is T);
            T tConstraint = (T)constraint;
            return GetSourceCurveBindings(rigBuilder, tConstraint);
        }

        /// <inheritdoc />
        IEnumerable<EditorCurveBinding> IBakeParameters.GetConstrainedCurveBindings(RigBuilder rigBuilder, IRigConstraint constraint)
        {
            Debug.Assert(constraint is T);
            T tConstraint = (T)constraint;
            return GetConstrainedCurveBindings(rigBuilder, tConstraint);
        }
    }

    /// <summary>
    /// This is the base interface for BakeParameters.
    /// </summary>
    public interface IBakeParameters
    {
        /// <summary>
        /// Boolean used to determine if the RigConstraint can transfer motion from itself to the skeleton
        /// </summary>
        bool canBakeToSkeleton { get; }
        /// <summary>
        /// Boolean used to determine if the RigConstraint can transfer motion to itself from the skeleton.
        /// </summary>
        bool canBakeToConstraint { get; }

        /// <summary>
        /// Collects the editor curve bindings for all the properties that this RigConstraint modifies when transferring motion to the skeleton.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder which the constraint is part of.</param>
        /// <param name="constraint">The RigConstraint for which the bindings should be collected.</param>
        /// <returns></returns>
        IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, IRigConstraint constraint);
        /// <summary>
        /// Collects the editor curve bindings for all the properties that this RigConstraint modifies when transferring motion to this constraint.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder which the constraint is part of.</param>
        /// <param name="constraint">The RigConstraint for which the bindings should be collected.</param>
        /// <returns></returns>
        IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, IRigConstraint constraint);
    }

}
