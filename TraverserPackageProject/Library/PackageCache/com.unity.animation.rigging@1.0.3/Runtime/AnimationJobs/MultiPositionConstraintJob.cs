using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The MultiPosition constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiPositionConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadWriteTransformHandle driven;
        /// <summary>The Transform handle for the constrained object parent Transform.</summary>
        public ReadOnlyTransformHandle drivenParent;
        /// <summary>The post-translation offset applied to the constrained object.</summary>
        public Vector3Property drivenOffset;

        /// <summary>List of Transform handles for the source objects.</summary>
        public NativeArray<ReadOnlyTransformHandle> sourceTransforms;
        /// <summary>List of weights for the source objects.</summary>
        public NativeArray<PropertyStreamHandle> sourceWeights;
        /// <summary>List of offsets to apply to source rotations if maintainOffset is enabled.</summary>
        public NativeArray<Vector3> sourceOffsets;

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

                Vector3 currentWPos = driven.GetPosition(stream);
                Vector3 accumPos = currentWPos;
                for (int i = 0; i < sourceTransforms.Length; ++i)
                {
                    var normalizedWeight = weightBuffer[i] * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    ReadOnlyTransformHandle sourceTransform = sourceTransforms[i];
                    accumPos += (sourceTransform.GetPosition(stream) + sourceOffsets[i] - currentWPos) * normalizedWeight;

                    // Required to update handles with binding info.
                    sourceTransforms[i] = sourceTransform;
                }

                // Convert accumPos to local space
                if (drivenParent.IsValid(stream))
                {
                    drivenParent.GetGlobalTR(stream, out Vector3 parentWPos, out Quaternion parentWRot);
                    var parentTx = new AffineTransform(parentWPos, parentWRot);
                    accumPos = parentTx.InverseTransform(accumPos);
                }

                Vector3 currentLPos = driven.GetLocalPosition(stream);
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    accumPos = AnimationRuntimeUtils.Lerp(currentLPos, accumPos, axesMask);

                driven.SetLocalPosition(stream, Vector3.Lerp(currentLPos, accumPos + drivenOffset.Get(stream), w));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the MultiPosition constraint.
    /// </summary>
    public interface IMultiPositionConstraintData
    {
        /// <summary>The Transform affected by the constraint Source Transforms.</summary>
        Transform constrainedObject { get; }
        /// <summary>
        /// The list of Transforms that influence the constrained Transform position.
        /// Each source has a weight from 0 to 1.
        /// </summary>
        WeightedTransformArray sourceObjects { get; }
        /// <summary>This is used to maintain the current position offset from the constrained GameObject to the source GameObjects.</summary>
        bool maintainOffset { get; }

        /// <summary>The path to the offset property in the constraint component.</summary>
        string offsetVector3Property { get; }
        /// <summary>The path to the source objects property in the constraint component.</summary>
        string sourceObjectsProperty { get; }

        /// <summary>Toggles whether the constrained transform will translate along the X axis.</summary>
        bool constrainedXAxis { get; }
        /// <summary>Toggles whether the constrained transform will translate along the Y axis.</summary>
        bool constrainedYAxis { get; }
        /// <summary>Toggles whether the constrained transform will translate along the Z axis.</summary>
        bool constrainedZAxis { get; }
    }

    /// <summary>
    /// The MultiPosition constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiPositionConstraintJobBinder<T> : AnimationJobBinder<MultiPositionConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiPositionConstraintData
    {
        /// <inheritdoc />
        public override MultiPositionConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiPositionConstraintJob();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffset = Vector3Property.Bind(animator, component, data.offsetVector3Property);

            WeightedTransformArray sourceObjects = data.sourceObjects;

            WeightedTransformArrayBinder.BindReadOnlyTransforms(animator, component, sourceObjects, out job.sourceTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, sourceObjects, data.sourceObjectsProperty, out job.sourceWeights);

            job.sourceOffsets = new NativeArray<Vector3>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            job.weightBuffer = new NativeArray<float>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            Vector3 drivenPos = data.constrainedObject.position;
            for (int i = 0; i < sourceObjects.Count; ++i)
            {
                job.sourceOffsets[i] = data.maintainOffset ? (drivenPos - sourceObjects[i].transform.position) : Vector3.zero;
            }

            job.axesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedXAxis),
                System.Convert.ToSingle(data.constrainedYAxis),
                System.Convert.ToSingle(data.constrainedZAxis)
                );

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiPositionConstraintJob job)
        {
            job.sourceTransforms.Dispose();
            job.sourceWeights.Dispose();
            job.sourceOffsets.Dispose();
            job.weightBuffer.Dispose();
        }
    }
}
