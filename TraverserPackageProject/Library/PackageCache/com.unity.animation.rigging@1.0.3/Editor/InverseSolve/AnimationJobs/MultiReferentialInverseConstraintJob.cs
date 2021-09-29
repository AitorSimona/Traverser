using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Unity.Collections;

namespace UnityEditor.Animations.Rigging
{
    /// <summary>
    /// The MultiReferential inverse constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiReferentialInverseConstraintJob : IWeightedAnimationJob
    {
        /// <summary>The list of Transforms that are affected by the specified driver.</summary>
        public NativeArray<ReadWriteTransformHandle> sources;
        /// <summary>List of AffineTransform to apply to driven source objects.</summary>
        public NativeArray<AffineTransform> offsetTx;

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

            sources[0].GetGlobalTR(stream, out Vector3 driverWPos, out Quaternion driverWRot);
            var driverTx = new AffineTransform(driverWPos, driverWRot);

            int offset = 0;
            for (int i = 1; i < sources.Length; ++i)
            {
                var tx = driverTx * offsetTx[offset];

                var src = sources[i];
                src.GetGlobalTR(stream, out Vector3 srcWPos, out Quaternion srcWRot);
                src.SetGlobalTR(stream, tx.translation, tx.rotation);
                offset++;

                sources[i] = src;
            }
        }
    }

    /// <summary>
    /// The MultiReferential inverse constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiReferentialInverseConstraintJobBinder<T> : AnimationJobBinder<MultiReferentialInverseConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiReferentialConstraintData
    {
        /// <inheritdoc />
        public override MultiReferentialInverseConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiReferentialInverseConstraintJob();

            var sources = data.sourceObjects;
            job.sources = new NativeArray<ReadWriteTransformHandle>(sources.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.offsetTx = new NativeArray<AffineTransform>(sources.Length - 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sourceBindTx = new AffineTransform[sources.Length];

            for (int i = 0; i < sources.Length; ++i)
            {
                job.sources[i] = ReadWriteTransformHandle.Bind(animator, sources[i].transform);
                sourceBindTx[i] = new AffineTransform(sources[i].position, sources[i].rotation);
            }

            int offset = 0;
            var invDriverTx = sourceBindTx[0].Inverse();
            for (int i = 1; i < sourceBindTx.Length; ++i)
            {
                job.offsetTx[offset] = invDriverTx * sourceBindTx[i];
                offset++;
            }

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiReferentialInverseConstraintJob job)
        {
            job.sources.Dispose();
            job.offsetTx.Dispose();
        }
    }
}
