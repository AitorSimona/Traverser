using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// The TwistChain inverse constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct TwistChainInverseConstraintJob : IWeightedAnimationJob
    {
        /// <summary>The Transform handle for the root Transform of the chain.</summary>
        public ReadOnlyTransformHandle root;
        /// <summary>The Transform handle for the tip Transform of the chain.</summary>
        public ReadOnlyTransformHandle tip;

        /// <summary>The Transform handle for the root target Transform.</summary>
        public ReadWriteTransformHandle rootTarget;
        /// <summary>The Transform handle for the tip target Transform.</summary>
        public ReadWriteTransformHandle tipTarget;

        /// <inheritdoc />
        public FloatProperty jobWeight { get; set; }

        /// <summary>
        /// Defines what to do when processing the root motion.
        /// </summary>
        /// <param name="stream">The animation stream to work on.</param>
        public void ProcessRootMotion(AnimationStream stream) { }

        /// <summary>
        /// Defines what to do when processing the animation.
        /// </summary>
        /// <param name="stream">The animation stream to work on.</param>
        public void ProcessAnimation(AnimationStream stream)
        {
            jobWeight.Set(stream, 1f);

            rootTarget.SetPosition(stream, root.GetPosition(stream));
            rootTarget.SetRotation(stream, root.GetRotation(stream));

            tipTarget.SetPosition(stream, tip.GetPosition(stream));
            tipTarget.SetRotation(stream, tip.GetRotation(stream));
        }
    }

    /// <summary>
    /// The TwistChain inverse constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class TwistChainInverseConstraintJobBinder<T> : AnimationJobBinder<TwistChainInverseConstraintJob, T>
        where T : struct, IAnimationJobData, ITwistChainConstraintData
    {
        /// <inheritdoc />
        public override TwistChainInverseConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new TwistChainInverseConstraintJob();

            job.root = ReadOnlyTransformHandle.Bind(animator, data.root);
            job.tip = ReadOnlyTransformHandle.Bind(animator, data.tip);

            job.rootTarget = ReadWriteTransformHandle.Bind(animator, data.rootTarget);
            job.tipTarget = ReadWriteTransformHandle.Bind(animator, data.tipTarget);

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(TwistChainInverseConstraintJob job)
        {
        }
    }
}
