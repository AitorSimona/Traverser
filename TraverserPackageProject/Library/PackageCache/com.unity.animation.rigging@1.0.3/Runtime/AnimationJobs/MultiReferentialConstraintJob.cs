using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The MultiReferential constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiReferentialConstraintJob : IWeightedAnimationJob
    {
        /// <summary>The driver index.</summary>
        public IntProperty driver;
        /// <summary>The list of Transforms that are affected by the specified driver.</summary>
        public NativeArray<ReadWriteTransformHandle> sources;
        /// <summary>Cache of AffineTransform representing the source objects initial positions.</summary>
        public NativeArray<AffineTransform> sourceBindTx;
        /// <summary>List of AffineTransform to apply to driven source objects.</summary>
        public NativeArray<AffineTransform> offsetTx;

        private int m_PrevDriverIdx;

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
                var driverIdx = driver.Get(stream);
                if (driverIdx != m_PrevDriverIdx)
                    UpdateOffsets(driverIdx);

                sources[driverIdx].GetGlobalTR(stream, out Vector3 driverWPos, out Quaternion driverWRot);
                var driverTx = new AffineTransform(driverWPos, driverWRot);

                int offset = 0;
                for (int i = 0; i < sources.Length; ++i)
                {
                    if (i == driverIdx)
                        continue;

                    var tx = driverTx * offsetTx[offset];

                    var src = sources[i];
                    src.GetGlobalTR(stream, out Vector3 srcWPos, out Quaternion srcWRot);
                    src.SetGlobalTR(stream, Vector3.Lerp(srcWPos, tx.translation, w), Quaternion.Lerp(srcWRot, tx.rotation, w));
                    offset++;

                    sources[i] = src;
                }
            }
            else
            {
                for (int i = 0; i < sources.Length; ++i)
                    AnimationRuntimeUtils.PassThrough(stream, sources[i]);
            }
        }

        internal void UpdateOffsets(int driver)
        {
            driver = Mathf.Clamp(driver, 0, sources.Length - 1);

            int offset = 0;
            var invDriverTx = sourceBindTx[driver].Inverse();
            for (int i = 0; i < sourceBindTx.Length; ++i)
            {
                if (i == driver)
                    continue;

                offsetTx[offset] = invDriverTx * sourceBindTx[i];
                offset++;
            }

            m_PrevDriverIdx = driver;
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the MultiReferential constraint.
    /// </summary>
    public interface IMultiReferentialConstraintData
    {
        /// <summary>The driver index. This is a value in between 0 and the number of sourceObjects.</summary>
        int driverValue { get; }
        /// <summary>The path to the driver property in the constraint component.</summary>
        string driverIntProperty { get; }
        /// <summary>The list of Transforms that can act as drivers for the constrained Transform.</summary>
        Transform[] sourceObjects { get; }
    }

    /// <summary>
    /// The MultiReferential constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiReferentialConstraintJobBinder<T> : AnimationJobBinder<MultiReferentialConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiReferentialConstraintData
    {
        /// <inheritdoc />
        public override MultiReferentialConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiReferentialConstraintJob();

            var sources = data.sourceObjects;
            job.driver = IntProperty.Bind(animator, component, data.driverIntProperty);
            job.sources = new NativeArray<ReadWriteTransformHandle>(sources.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.sourceBindTx = new NativeArray<AffineTransform>(sources.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            job.offsetTx = new NativeArray<AffineTransform>(sources.Length - 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < sources.Length; ++i)
            {
                job.sources[i] = ReadWriteTransformHandle.Bind(animator, sources[i].transform);
                job.sourceBindTx[i] = new AffineTransform(sources[i].position, sources[i].rotation);
            }

            job.UpdateOffsets(data.driverValue);

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiReferentialConstraintJob job)
        {
            job.sources.Dispose();
            job.sourceBindTx.Dispose();
            job.offsetTx.Dispose();
        }
    }
}
