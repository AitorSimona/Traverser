namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The Blend constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct BlendConstraintJob : IWeightedAnimationJob
    {
        private const int k_BlendTranslationMask = 1 << 0;
        private const int k_BlendRotationMask = 1 << 1;

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadWriteTransformHandle driven;
        /// <summary>The Transform handle for sourceA Transform.</summary>
        public ReadOnlyTransformHandle sourceA;
        /// <summary>The Transform handle for sourceB Transform.</summary>
        public ReadOnlyTransformHandle sourceB;
        /// <summary>TR offset to apply to sourceA if maintainOffset is enabled.</summary>
        public AffineTransform sourceAOffset;
        /// <summary>TR offset to apply to sourceB if maintainOffset is enabled.</summary>
        public AffineTransform sourceBOffset;

        /// <summary>Toggles whether to blend position in the job.</summary>
        public BoolProperty blendPosition;
        /// <summary>Toggles whether to blend rotation in the job.</summary>
        public BoolProperty blendRotation;
        /// <summary>
        /// Specifies the weight with which to blend position.
        /// A weight of zero will result in the position of sourceA, while a weight of one will result in the position of sourceB.
        /// </summary>
        public FloatProperty positionWeight;
        /// <summary>
        /// Specifies the weight with which to blend rotation.
        /// A weight of zero will result in the rotation of sourceA, while a weight of one will result in the rotation of sourceB.
        /// </summary>
        public FloatProperty rotationWeight;

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
            float w = jobWeight.Get(stream);
            if (w > 0f)
            {
                if (blendPosition.Get(stream))
                {
                    Vector3 posBlend = Vector3.Lerp(
                        sourceA.GetPosition(stream) + sourceAOffset.translation,
                        sourceB.GetPosition(stream) + sourceBOffset.translation,
                        positionWeight.Get(stream)
                        );
                    driven.SetPosition(stream, Vector3.Lerp(driven.GetPosition(stream), posBlend, w));
                }
                else
                    driven.SetLocalPosition(stream, driven.GetLocalPosition(stream));

                if (blendRotation.Get(stream))
                {
                    Quaternion rotBlend = Quaternion.Lerp(
                        sourceA.GetRotation(stream) * sourceAOffset.rotation,
                        sourceB.GetRotation(stream) * sourceBOffset.rotation,
                        rotationWeight.Get(stream)
                        );
                    driven.SetRotation(stream, Quaternion.Lerp(driven.GetRotation(stream), rotBlend, w));
                }
                else
                    driven.SetLocalRotation(stream, driven.GetLocalRotation(stream));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the Blend constraint.
    /// </summary>
    public interface IBlendConstraintData
    {
        /// <summary>The Transform affected by the two source Transforms.</summary>
        Transform constrainedObject { get; }
        /// <summary>First Transform in blend.</summary>
        Transform sourceObjectA { get; }
        /// <summary>Second Transform in blend.</summary>
        Transform sourceObjectB { get; }

        /// <summary>This is used to maintain the current position offset from the constrained GameObject to the source GameObjects.</summary>
        bool maintainPositionOffsets { get; }
        /// <summary>This is used to maintain the current rotation offset from the constrained GameObject to the source GameObjects.</summary>
        bool maintainRotationOffsets { get; }

        /// <summary>The path to the blend position property in the constraint component.</summary>
        string blendPositionBoolProperty { get; }
        /// <summary>The path to the blend rotation property in the constraint component.</summary>
        string blendRotationBoolProperty { get; }
        /// <summary>The path to the position weight property in the constraint component.</summary>
        string positionWeightFloatProperty { get; }
        /// <summary>The path to the rotation weight property in the constraint component.</summary>
        string rotationWeightFloatProperty { get; }
    }

    /// <summary>
    /// The Blend constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class BlendConstraintJobBinder<T> : AnimationJobBinder<BlendConstraintJob, T>
        where T : struct, IAnimationJobData, IBlendConstraintData
    {
        /// <inheritdoc />
        public override BlendConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new BlendConstraintJob();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.sourceA = ReadOnlyTransformHandle.Bind(animator, data.sourceObjectA);
            job.sourceB = ReadOnlyTransformHandle.Bind(animator, data.sourceObjectB);

            job.sourceAOffset = job.sourceBOffset = AffineTransform.identity;
            if (data.maintainPositionOffsets)
            {
                var drivenPos = data.constrainedObject.position;
                job.sourceAOffset.translation = drivenPos - data.sourceObjectA.position;
                job.sourceBOffset.translation = drivenPos - data.sourceObjectB.position;
            }

            if (data.maintainRotationOffsets)
            {
                var drivenRot = data.constrainedObject.rotation;
                job.sourceAOffset.rotation = Quaternion.Inverse(data.sourceObjectA.rotation) * drivenRot;
                job.sourceBOffset.rotation = Quaternion.Inverse(data.sourceObjectB.rotation) * drivenRot;
            }

            job.blendPosition = BoolProperty.Bind(animator, component, data.blendPositionBoolProperty);
            job.blendRotation = BoolProperty.Bind(animator, component, data.blendRotationBoolProperty);
            job.positionWeight = FloatProperty.Bind(animator, component, data.positionWeightFloatProperty);
            job.rotationWeight = FloatProperty.Bind(animator, component, data.rotationWeightFloatProperty);

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(BlendConstraintJob job)
        {
        }
    }
}
