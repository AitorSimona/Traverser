using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The MultiRotation constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiRotationConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadWriteTransformHandle driven;
        /// <summary>The Transform handle for the constrained object parent Transform.</summary>
        public ReadOnlyTransformHandle drivenParent;
        /// <summary>The post-rotation offset applied to the constrained object.</summary>
        public Vector3Property drivenOffset;

        /// <summary>List of Transform handles for the source objects.</summary>
        public NativeArray<ReadOnlyTransformHandle> sourceTransforms;
        /// <summary>List of weights for the source objects.</summary>
        public NativeArray<PropertyStreamHandle> sourceWeights;
        /// <summary>List of offsets to apply to source rotations if maintainOffset is enabled.</summary>
        public NativeArray<Quaternion> sourceOffsets;

        /// <summary>Buffer used to store weights during job execution.</summary>
        public NativeArray<float> weightBuffer;

        /// <summary>Axes mask. Rotation will apply on the local axis for a value of 1.0, and will be kept as is for a value of 0.0.</summary>
        public Vector3 axesMask;

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
                AnimationStreamHandleUtility.ReadFloats(stream, sourceWeights, weightBuffer);

                float sumWeights = AnimationRuntimeUtils.Sum(weightBuffer);
                if (sumWeights < k_Epsilon)
                {
                    AnimationRuntimeUtils.PassThrough(stream, driven);
                    return;
                }

                float weightScale = sumWeights > 1f ? 1f / sumWeights : 1f;

                float accumWeights = 0f;
                Quaternion accumRot = QuaternionExt.zero;
                for (int i = 0; i < sourceTransforms.Length; ++i)
                {
                    var normalizedWeight = weightBuffer[i] * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    ReadOnlyTransformHandle sourceTransform = sourceTransforms[i];
                    accumRot = QuaternionExt.Add(accumRot, QuaternionExt.Scale(sourceTransform.GetRotation(stream) * sourceOffsets[i], normalizedWeight));

                    // Required to update handles with binding info.
                    sourceTransforms[i] = sourceTransform;
                    accumWeights += normalizedWeight;
                }

                accumRot = QuaternionExt.NormalizeSafe(accumRot);
                if (accumWeights < 1f)
                    accumRot = Quaternion.Lerp(driven.GetRotation(stream), accumRot, accumWeights);

                // Convert accumRot to local space
                if (drivenParent.IsValid(stream))
                    accumRot = Quaternion.Inverse(drivenParent.GetRotation(stream)) * accumRot;

                Quaternion currentLRot = driven.GetLocalRotation(stream);
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    accumRot = Quaternion.Euler(AnimationRuntimeUtils.Lerp(currentLRot.eulerAngles, accumRot.eulerAngles, axesMask));

                var offset = drivenOffset.Get(stream);
                if (Vector3.Dot(offset, offset) > 0f)
                    accumRot *= Quaternion.Euler(offset);

                driven.SetLocalRotation(stream, Quaternion.Lerp(currentLRot, accumRot, w));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the MultiRotation constraint.
    /// </summary>
    public interface IMultiRotationConstraintData
    {
        /// <summary>The Transform affected by the constraint Source Transforms.</summary>
        Transform constrainedObject { get; }

        /// <summary>
        /// The list of Transforms that influence the constrained Transform rotation.
        /// Each source has a weight from 0 to 1.
        /// </summary>
        WeightedTransformArray sourceObjects { get; }
        /// <summary>This is used to maintain the current rotation offset from the constrained GameObject to the source GameObjects.</summary>
        bool maintainOffset { get; }

        /// <summary>The path to the offset property in the constraint component.</summary>
        string offsetVector3Property { get; }
        /// <summary>The path to the source objects property in the constraint component.</summary>
        string sourceObjectsProperty { get; }

        /// <summary>Toggles whether the constrained transform will rotate along the X axis.</summary>
        bool constrainedXAxis { get; }
        /// <summary>Toggles whether the constrained transform will rotate along the Y axis.</summary>
        bool constrainedYAxis { get; }
        /// <summary>Toggles whether the constrained transform will rotate along the Z axis.</summary>
        bool constrainedZAxis { get; }
    }

    /// <summary>
    /// The MultiRotation constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiRotationConstraintJobBinder<T> : AnimationJobBinder<MultiRotationConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiRotationConstraintData
    {
        /// <inheritdoc />
        public override MultiRotationConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiRotationConstraintJob();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffset = Vector3Property.Bind(animator, component, data.offsetVector3Property);

            WeightedTransformArray sourceObjects = data.sourceObjects;

            WeightedTransformArrayBinder.BindReadOnlyTransforms(animator, component, sourceObjects, out job.sourceTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, sourceObjects, data.sourceObjectsProperty, out job.sourceWeights);

            job.sourceOffsets = new NativeArray<Quaternion>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            job.weightBuffer = new NativeArray<float>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            Quaternion drivenRot = data.constrainedObject.rotation;
            for (int i = 0; i < sourceObjects.Count; ++i)
            {
                job.sourceOffsets[i] = data.maintainOffset ?
                    (Quaternion.Inverse(sourceObjects[i].transform.rotation) * drivenRot) : Quaternion.identity;
            }

            job.axesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedXAxis),
                System.Convert.ToSingle(data.constrainedYAxis),
                System.Convert.ToSingle(data.constrainedZAxis)
                );

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiRotationConstraintJob job)
        {
            job.sourceTransforms.Dispose();
            job.sourceWeights.Dispose();
            job.sourceOffsets.Dispose();
            job.weightBuffer.Dispose();
        }
    }
}
