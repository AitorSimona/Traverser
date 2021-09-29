using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// The MultiParent inverse constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiParentInverseConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadOnlyTransformHandle driven;
        /// <summary>The Transform handle for the constrained object parent Transform.</summary>
        public ReadOnlyTransformHandle drivenParent;

        /// <summary>List of Transform handles for the source objects.</summary>
        public NativeArray<ReadWriteTransformHandle> sourceTransforms;
        /// <summary>List of weights for the source objects.</summary>
        public NativeArray<PropertyStreamHandle> sourceWeights;
        /// <summary>List of offsets to apply to source rotations if maintainOffset is enabled.</summary>
        public NativeArray<AffineTransform> sourceOffsets;

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

            var drivenLocalTx = new AffineTransform(driven.GetLocalPosition(stream), driven.GetLocalRotation(stream));
            var parentTx = new AffineTransform();

            // Convert accumTx to local space
            if (drivenParent.IsValid(stream))
            {
                drivenParent.GetGlobalTR(stream, out Vector3 parentWPos, out Quaternion parentWRot);
                parentTx = new AffineTransform(parentWPos, parentWRot);
            }

            var drivenTx = parentTx * drivenLocalTx;

            for (int i = 0; i < sourceTransforms.Length; ++i)
            {
                sourceWeights[i].SetFloat(stream, 1f);

                var sourceTransform = sourceTransforms[i];
                sourceTransform.GetGlobalTR(stream, out var sourcePosition, out var sourceRotation);

                var result = drivenTx;
                result *= sourceOffsets[i];

                sourceTransform.SetGlobalTR(stream, result.translation, result.rotation);
                sourceTransforms[i] = sourceTransform;
            }
        }
    }

    /// <summary>
    /// The MultiParent inverse constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiParentInverseConstraintJobBinder<T> : AnimationJobBinder<MultiParentInverseConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiParentConstraintData
    {
        /// <inheritdoc />
        public override MultiParentInverseConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiParentInverseConstraintJob();

            job.driven = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);

            WeightedTransformArray sourceObjects = data.sourceObjects;

            WeightedTransformArrayBinder.BindReadWriteTransforms(animator, component, sourceObjects, out job.sourceTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, sourceObjects, data.sourceObjectsProperty, out job.sourceWeights);

            job.sourceOffsets = new NativeArray<AffineTransform>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

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

                srcOffset = srcOffset.Inverse();

                job.sourceOffsets[i] = srcOffset;
            }

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiParentInverseConstraintJob job)
        {
            job.sourceTransforms.Dispose();
            job.sourceWeights.Dispose();
            job.sourceOffsets.Dispose();
        }
    }
}
