using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The MultiParent constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiParentConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadWriteTransformHandle driven;
        /// <summary>The Transform handle for the constrained object parent Transform.</summary>
        public ReadOnlyTransformHandle drivenParent;

        /// <summary>List of Transform handles for the source objects.</summary>
        public NativeArray<ReadOnlyTransformHandle> sourceTransforms;
        /// <summary>List of weights for the source objects.</summary>
        public NativeArray<PropertyStreamHandle> sourceWeights;
        /// <summary>List of offsets to apply to source rotations if maintainOffset is enabled.</summary>
        public NativeArray<AffineTransform> sourceOffsets;

        /// <summary>Buffer used to store weights during job execution.</summary>
        public NativeArray<float> weightBuffer;

        /// <summary>Position axes mask. Position will apply on the local axis for a value of 1.0, and will be kept as is for a value of 0.0.</summary>
        public Vector3 positionAxesMask;
        /// <summary>Rotation axes mask. Rotation will apply on the local axis for a value of 1.0, and will be kept as is for a value of 0.0.</summary>
        public Vector3 rotationAxesMask;

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
                AnimationStreamHandleUtility.ReadFloats(stream, sourceWeights, weightBuffer);

                float sumWeights = AnimationRuntimeUtils.Sum(weightBuffer);
                if (sumWeights < k_Epsilon)
                {
                    AnimationRuntimeUtils.PassThrough(stream, driven);
                    return;
                }

                float weightScale = sumWeights > 1f ? 1f / sumWeights : 1f;

                float accumWeights = 0f;
                var accumTx = new AffineTransform(Vector3.zero, QuaternionExt.zero);
                for (int i = 0; i < sourceTransforms.Length; ++i)
                {
                    ReadOnlyTransformHandle sourceTransform = sourceTransforms[i];
                    var normalizedWeight = weightBuffer[i] * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    sourceTransform.GetGlobalTR(stream, out Vector3 srcWPos, out Quaternion srcWRot);
                    var sourceTx = new AffineTransform(srcWPos, srcWRot);

                    sourceTx *= sourceOffsets[i];

                    accumTx.translation += sourceTx.translation * normalizedWeight;
                    accumTx.rotation = QuaternionExt.Add(accumTx.rotation, QuaternionExt.Scale(sourceTx.rotation, normalizedWeight));

                    // Required to update handles with binding info.
                    sourceTransforms[i] = sourceTransform;
                    accumWeights += normalizedWeight;
                }

                accumTx.rotation = QuaternionExt.NormalizeSafe(accumTx.rotation);
                if (accumWeights < 1f)
                {
                    driven.GetGlobalTR(stream, out Vector3 currentWPos, out Quaternion currentWRot);
                    accumTx.translation += currentWPos * (1f - accumWeights);
                    accumTx.rotation = Quaternion.Lerp(currentWRot, accumTx.rotation, accumWeights);
                }

                // Convert accumTx to local space
                if (drivenParent.IsValid(stream))
                {
                    drivenParent.GetGlobalTR(stream, out Vector3 parentWPos, out Quaternion parentWRot);
                    var parentTx = new AffineTransform(parentWPos, parentWRot);
                    accumTx = parentTx.InverseMul(accumTx);
                }

                driven.GetLocalTRS(stream, out Vector3 currentLPos, out Quaternion currentLRot, out Vector3 currentLScale);
                if (Vector3.Dot(positionAxesMask, positionAxesMask) < 3f)
                    accumTx.translation = AnimationRuntimeUtils.Lerp(currentLPos, accumTx.translation, positionAxesMask);
                if (Vector3.Dot(rotationAxesMask, rotationAxesMask) < 3f)
                    accumTx.rotation = Quaternion.Euler(AnimationRuntimeUtils.Lerp(currentLRot.eulerAngles, accumTx.rotation.eulerAngles, rotationAxesMask));

                driven.SetLocalTRS(
                    stream,
                    Vector3.Lerp(currentLPos, accumTx.translation, w),
                    Quaternion.Lerp(currentLRot, accumTx.rotation, w),
                    currentLScale
                    );
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the MultiParent constraint.
    /// </summary>
    public interface IMultiParentConstraintData
    {
        /// <summary>The Transform affected by the constraint Source Transforms.</summary>
        Transform constrainedObject { get; }
        /// <summary>
        /// The list of Transforms that influence the constrained Transform rotation.
        /// Each source has a weight from 0 to 1.
        /// </summary>
        WeightedTransformArray sourceObjects { get; }
        /// <summary>This is used to maintain the current position offset from the constrained GameObject to the source GameObjects.</summary>
        bool maintainPositionOffset { get; }
        /// <summary>This is used to maintain the current rotation offset from the constrained GameObject to the source GameObjects.</summary>
        bool maintainRotationOffset { get; }

        /// <summary>Toggles whether the constrained transform will translate along the X axis.</summary>
        bool constrainedPositionXAxis { get; }
        /// <summary>Toggles whether the constrained transform will translate along the Y axis.</summary>
        bool constrainedPositionYAxis { get; }
        /// <summary>Toggles whether the constrained transform will translate along the Z axis.</summary>
        bool constrainedPositionZAxis { get; }
        /// <summary>Toggles whether the constrained transform will rotate along the X axis.</summary>
        bool constrainedRotationXAxis { get; }
        /// <summary>Toggles whether the constrained transform will rotate along the Y axis.</summary>
        bool constrainedRotationYAxis { get; }
        /// <summary>Toggles whether the constrained transform will rotate along the Z axis.</summary>
        bool constrainedRotationZAxis { get; }

        /// <summary>The path to the source objects property in the constraint component.</summary>
        string sourceObjectsProperty { get; }
    }

    /// <summary>
    /// The MultiParent constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiParentConstraintJobBinder<T> : AnimationJobBinder<MultiParentConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiParentConstraintData
    {
        /// <inheritdoc />
        public override MultiParentConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiParentConstraintJob();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);

            WeightedTransformArray sourceObjects = data.sourceObjects;

            WeightedTransformArrayBinder.BindReadOnlyTransforms(animator, component, sourceObjects, out job.sourceTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, sourceObjects, data.sourceObjectsProperty, out job.sourceWeights);

            job.sourceOffsets = new NativeArray<AffineTransform>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            job.weightBuffer = new NativeArray<float>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var drivenTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
            for (int i = 0; i < sourceObjects.Count; ++i)
            {
                var sourceTransform = sourceObjects[i].transform;

                var srcTx = new AffineTransform(sourceTransform.position, sourceTransform.rotation);
                var srcOffset = AffineTransform.identity;
                var tmp = srcTx.InverseMul(drivenTx);

                if (data.maintainPositionOffset)
                    srcOffset.translation = tmp.translation;
                if (data.maintainRotationOffset)
                    srcOffset.rotation = tmp.rotation;

                job.sourceOffsets[i] = srcOffset;
            }

            job.positionAxesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedPositionXAxis),
                System.Convert.ToSingle(data.constrainedPositionYAxis),
                System.Convert.ToSingle(data.constrainedPositionZAxis)
                );
            job.rotationAxesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedRotationXAxis),
                System.Convert.ToSingle(data.constrainedRotationYAxis),
                System.Convert.ToSingle(data.constrainedRotationZAxis)
                );

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiParentConstraintJob job)
        {
            job.sourceTransforms.Dispose();
            job.sourceWeights.Dispose();
            job.sourceOffsets.Dispose();
            job.weightBuffer.Dispose();
        }
    }
}
