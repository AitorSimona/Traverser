using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// The MultiPosition inverse constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiPositionInverseConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadOnlyTransformHandle driven;
        /// <summary>The Transform handle for the constrained object parent Transform.</summary>
        public ReadOnlyTransformHandle drivenParent;
        /// <summary>The post-translation offset applied to the constrained object.</summary>
        public Vector3Property drivenOffset;

        /// <summary>List of Transform handles for the source objects.</summary>
        public NativeArray<ReadWriteTransformHandle> sourceTransforms;
        /// <summary>List of weights for the source objects.</summary>
        public NativeArray<PropertyStreamHandle> sourceWeights;
        /// <summary>List of offsets to apply to source rotations if maintainOffset is enabled.</summary>
        public NativeArray<Vector3> sourceOffsets;

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

            var drivenPos = driven.GetLocalPosition(stream);
            var offset = drivenOffset.Get(stream);

            var lPos = drivenPos - offset;

            var parentTx = new AffineTransform();
            if (drivenParent.IsValid(stream))
            {
                drivenParent.GetGlobalTR(stream, out Vector3 parentWPos, out Quaternion parentWRot);
                parentTx = new AffineTransform(parentWPos, parentWRot);
            }

            var wPos = parentTx.Transform(lPos);
            for (int i = 0; i < sourceTransforms.Length; ++i)
            {
                sourceWeights[i].SetFloat(stream, 1f);

                ReadWriteTransformHandle sourceTransform = sourceTransforms[i];

                sourceTransform.SetPosition(stream, wPos - sourceOffsets[i]);

                // Required to update handles with binding info.
                sourceTransforms[i] = sourceTransform;
            }
        }
    }

    /// <summary>
    /// The MultiPosition inverse constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiPositionInverseConstraintJobBinder<T> : AnimationJobBinder<MultiPositionInverseConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiPositionConstraintData
    {
        /// <inheritdoc />
        public override MultiPositionInverseConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiPositionInverseConstraintJob();

            job.driven = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);
            job.drivenOffset = Vector3Property.Bind(animator, component, data.offsetVector3Property);

            WeightedTransformArray sourceObjects = data.sourceObjects;

            WeightedTransformArrayBinder.BindReadWriteTransforms(animator, component, sourceObjects, out job.sourceTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, sourceObjects, data.sourceObjectsProperty, out job.sourceWeights);

            job.sourceOffsets = new NativeArray<Vector3>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            Vector3 drivenPos = data.constrainedObject.position;
            for (int i = 0; i < sourceObjects.Count; ++i)
            {
                job.sourceOffsets[i] = data.maintainOffset ? (drivenPos - sourceObjects[i].transform.position) : Vector3.zero;
            }

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiPositionInverseConstraintJob job)
        {
            job.sourceTransforms.Dispose();
            job.sourceWeights.Dispose();
            job.sourceOffsets.Dispose();
        }
    }
}
