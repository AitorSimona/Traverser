namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The TwoBoneIK constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct TwoBoneIKConstraintJob : IWeightedAnimationJob
    {
        /// <summary>The transform handle for the root transform.</summary>
        public ReadWriteTransformHandle root;
        /// <summary>The transform handle for the mid transform.</summary>
        public ReadWriteTransformHandle mid;
        /// <summary>The transform handle for the tip transform.</summary>
        public ReadWriteTransformHandle tip;

        /// <summary>The transform handle for the hint transform.</summary>
        public ReadOnlyTransformHandle hint;
        /// <summary>The transform handle for the target transform.</summary>
        public ReadOnlyTransformHandle target;

        /// <summary>The offset applied to the target transform if maintainTargetPositionOffset or maintainTargetRotationOffset is enabled.</summary>
        public AffineTransform targetOffset;

        /// <summary>The weight for which target position has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public FloatProperty targetPositionWeight;
        /// <summary>The weight for which target rotation has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public FloatProperty targetRotationWeight;
        /// <summary>The weight for which hint transform has an effect on IK calculations. This is a value in between 0 and 1.</summary>
        public FloatProperty hintWeight;

        /// <summary>The main weight given to the constraint. This is a value in between 0 and 1.</summary>
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
            float w = jobWeight.Get(stream);
            if (w > 0f)
            {
                AnimationRuntimeUtils.SolveTwoBoneIK(
                    stream, root, mid, tip, target, hint,
                    targetPositionWeight.Get(stream) * w,
                    targetRotationWeight.Get(stream) * w,
                    hintWeight.Get(stream) * w,
                    targetOffset
                    );
            }
            else
            {
                AnimationRuntimeUtils.PassThrough(stream, root);
                AnimationRuntimeUtils.PassThrough(stream, mid);
                AnimationRuntimeUtils.PassThrough(stream, tip);
            }
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the TwoBoneIK constraint.
    /// </summary>
    public interface ITwoBoneIKConstraintData
    {
        /// <summary>The root transform of the two bones hierarchy.</summary>
        Transform root { get; }
        /// <summary>The mid transform of the two bones hierarchy.</summary>
        Transform mid { get; }
        /// <summary>The tip transform of the two bones hierarchy.</summary>
        Transform tip { get; }
        /// <summary>The IK target transform.</summary>
        Transform target { get; }
        /// <summary>The IK hint transform.</summary>
        Transform hint { get; }

        /// <summary>This is used to maintain the offset of the tip position to the target position.</summary>
        bool maintainTargetPositionOffset { get; }
        /// <summary>This is used to maintain the offset of the tip rotation to the target rotation.</summary>
        bool maintainTargetRotationOffset { get; }

        /// <summary>The path to the target position weight property in the constraint component.</summary>
        string targetPositionWeightFloatProperty { get; }
        /// <summary>The path to the target rotation weight property in the constraint component.</summary>
        string targetRotationWeightFloatProperty { get; }
        /// <summary>The path to the hint weight property in the constraint component.</summary>
        string hintWeightFloatProperty { get; }
    }

    /// <summary>
    /// The TwoBoneIK constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class TwoBoneIKConstraintJobBinder<T> : AnimationJobBinder<TwoBoneIKConstraintJob, T>
        where T : struct, IAnimationJobData, ITwoBoneIKConstraintData
    {
        /// <summary>
        /// Creates the animation job.
        /// </summary>
        /// <param name="animator">The animated hierarchy Animator component.</param>
        /// <param name="data">The constraint data.</param>
        /// <param name="component">The constraint component.</param>
        /// <returns>Returns a new job interface.</returns>
        public override TwoBoneIKConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new TwoBoneIKConstraintJob();

            job.root = ReadWriteTransformHandle.Bind(animator, data.root);
            job.mid = ReadWriteTransformHandle.Bind(animator, data.mid);
            job.tip = ReadWriteTransformHandle.Bind(animator, data.tip);
            job.target = ReadOnlyTransformHandle.Bind(animator, data.target);

            if (data.hint != null)
                job.hint = ReadOnlyTransformHandle.Bind(animator, data.hint);

            job.targetOffset = AffineTransform.identity;
            if (data.maintainTargetPositionOffset)
                job.targetOffset.translation = data.tip.position - data.target.position;
            if (data.maintainTargetRotationOffset)
                job.targetOffset.rotation = Quaternion.Inverse(data.target.rotation) * data.tip.rotation;

            job.targetPositionWeight = FloatProperty.Bind(animator, component, data.targetPositionWeightFloatProperty);
            job.targetRotationWeight = FloatProperty.Bind(animator, component, data.targetRotationWeightFloatProperty);
            job.hintWeight = FloatProperty.Bind(animator, component, data.hintWeightFloatProperty);

            return job;
        }

        /// <summary>
        /// Destroys the animation job.
        /// </summary>
        /// <param name="job">The animation job to destroy.</param>
        public override void Destroy(TwoBoneIKConstraintJob job)
        {
        }
    }
}
