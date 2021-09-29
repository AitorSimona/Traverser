namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The OverrideTransform job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct OverrideTransformJob : IWeightedAnimationJob
    {
        /// <summary>
        /// The override space controls how the override source Transform
        /// is copied unto constrained Transform.
        /// </summary>
        public enum Space
        {
            /// <summary>Copy override world TR components into world TR components of the constrained Transform.</summary>
            World = 0,
            /// <summary>Copy override local TR components into local TR components of the constrained Transform.</summary>
            Local = 1,
            /// <summary>Add override local TR components to local TR components of the constrained Transform. </summary>
            Pivot = 2
        }

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadWriteTransformHandle driven;
        /// <summary>The Transform handle for the override source object Transform.</summary>
        public ReadOnlyTransformHandle source;
        /// <summary>Cached inverse local TR used in space switching calculations.</summary>
        public AffineTransform sourceInvLocalBindTx;

        /// <summary>Cached source to world coordinates quaternion. Used when Space is set to World.</summary>
        public Quaternion sourceToWorldRot;
        /// <summary>Cached source to local coordinates quaternion. Used when Space is set to Local.</summary>
        public Quaternion sourceToLocalRot;
        /// <summary>Cached source to pivot coordinates quaternion. Used when Space is set to Pivot.</summary>
        public Quaternion sourceToPivotRot;

        /// <summary>CacheIndex to the override space.</summary>
        /// <seealso cref="AnimationJobCache"/>
        public CacheIndex spaceIdx;
        /// <summary>CacheIndex to source to space quaternion used in current space.</summary>
        /// <seealso cref="AnimationJobCache"/>
        public CacheIndex sourceToCurrSpaceRotIdx;

        /// <summary>The override position.</summary>
        public Vector3Property position;
        /// <summary>The override rotation.</summary>
        public Vector3Property rotation;
        /// <summary>The weight for which override position has an effect on the constrained Transform. This is a value in between 0 and 1.</summary>
        public FloatProperty positionWeight;
        /// <summary>The weight for which override rotation has an effect on the constrained Transform. This is a value in between 0 and 1.</summary>
        public FloatProperty rotationWeight;

        /// <summary>Cache for static properties in the job.</summary>
        public AnimationJobCache cache;

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
                AffineTransform overrideTx;
                if (source.IsValid(stream))
                {
                    source.GetLocalTRS(stream, out Vector3 srcLPos, out Quaternion srcLRot, out _);
                    var sourceLocalTx = new AffineTransform(srcLPos, srcLRot);
                    var sourceToSpaceRot = cache.Get<Quaternion>(sourceToCurrSpaceRotIdx);
                    overrideTx = Quaternion.Inverse(sourceToSpaceRot) * (sourceInvLocalBindTx * sourceLocalTx) * sourceToSpaceRot;
                }
                else
                    overrideTx = new AffineTransform(position.Get(stream), Quaternion.Euler(rotation.Get(stream)));

                Space overrideSpace = (Space)cache.GetRaw(spaceIdx);
                var posW = positionWeight.Get(stream) * w;
                var rotW = rotationWeight.Get(stream) * w;
                switch (overrideSpace)
                {
                    case Space.World:
                        {
                            driven.GetGlobalTR(stream, out Vector3 drivenWPos, out Quaternion drivenWRot);
                            driven.SetGlobalTR(
                                stream,
                                Vector3.Lerp(drivenWPos, overrideTx.translation, posW),
                                Quaternion.Lerp(drivenWRot, overrideTx.rotation, rotW)
                                );
                        }
                        break;
                    case Space.Local:
                        {
                            driven.GetLocalTRS(stream, out Vector3 drivenLPos, out Quaternion drivenLRot, out Vector3 drivenLScale);
                            driven.SetLocalTRS(
                                stream,
                                Vector3.Lerp(drivenLPos, overrideTx.translation, posW),
                                Quaternion.Lerp(drivenLRot, overrideTx.rotation, rotW),
                                drivenLScale
                                );
                        }
                        break;
                    case Space.Pivot:
                        {
                            driven.GetLocalTRS(stream, out Vector3 drivenLPos, out Quaternion drivenLRot, out Vector3 drivenLScale);
                            var drivenLocalTx = new AffineTransform(drivenLPos, drivenLRot);
                            overrideTx = drivenLocalTx * overrideTx;

                            driven.SetLocalTRS(
                                stream,
                                Vector3.Lerp(drivenLocalTx.translation, overrideTx.translation, posW),
                                Quaternion.Lerp(drivenLocalTx.rotation, overrideTx.rotation, rotW),
                                drivenLScale
                                );
                        }
                        break;
                    default:
                        break;
                }
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }

        internal void UpdateSpace(int space)
        {
            if ((int)cache.GetRaw(spaceIdx) == space)
                return;

            cache.SetRaw(space, spaceIdx);

            Space currSpace = (Space)space;
            if (currSpace == Space.Pivot)
                cache.Set(sourceToPivotRot, sourceToCurrSpaceRotIdx);
            else if (currSpace == Space.Local)
                cache.Set(sourceToLocalRot, sourceToCurrSpaceRotIdx);
            else
                cache.Set(sourceToWorldRot, sourceToCurrSpaceRotIdx);
        }
    }

    /// <summary>
    /// This interface defines the data mapping for OverrideTransform.
    /// </summary>
    public interface IOverrideTransformData
    {
        /// <summary>The Transform affected by the constraint source Transform.</summary>
        Transform constrainedObject { get; }
        /// <summary>The override source Transform.</summary>
        Transform sourceObject { get; }
        /// <summary>The override space.</summary>
        int space { get; }

        /// <summary>The path to the position weight property in the constraint component.</summary>
        string positionWeightFloatProperty { get; }
        /// <summary>The path to the rotation weight property in the constraint component.</summary>
        string rotationWeightFloatProperty { get; }
        /// <summary>The path to the override position property in the constraint component.</summary>
        string positionVector3Property { get; }
        /// <summary>The path to the override rotation property in the constraint component.</summary>
        string rotationVector3Property { get; }
    }

    /// <summary>
    /// The OverrideTransform job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class OverrideTransformJobBinder<T> : AnimationJobBinder<OverrideTransformJob, T>
        where T : struct, IAnimationJobData, IOverrideTransformData
    {
        /// <inheritdoc />
        public override OverrideTransformJob Create(Animator animator, ref T data, Component component)
        {
            var job = new OverrideTransformJob();
            var cacheBuilder = new AnimationJobCacheBuilder();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);

            if (data.sourceObject != null)
            {
                // Cache source to possible space rotation offsets (world, local and pivot)
                // at bind time so we can switch dynamically between them at runtime.

                job.source = ReadOnlyTransformHandle.Bind(animator, data.sourceObject);
                var sourceLocalTx = new AffineTransform(data.sourceObject.localPosition, data.sourceObject.localRotation);
                job.sourceInvLocalBindTx = sourceLocalTx.Inverse();

                var sourceWorldTx = new AffineTransform(data.sourceObject.position, data.sourceObject.rotation);
                var drivenWorldTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
                job.sourceToWorldRot = sourceWorldTx.Inverse().rotation;
                job.sourceToPivotRot = sourceWorldTx.InverseMul(drivenWorldTx).rotation;

                var drivenParent = data.constrainedObject.parent;
                if (drivenParent != null)
                {
                    var drivenParentWorldTx = new AffineTransform(drivenParent.position, drivenParent.rotation);
                    job.sourceToLocalRot = sourceWorldTx.InverseMul(drivenParentWorldTx).rotation;
                }
                else
                    job.sourceToLocalRot = job.sourceToPivotRot;
            }

            job.spaceIdx = cacheBuilder.Add(data.space);
            if (data.space == (int)OverrideTransformJob.Space.Pivot)
                job.sourceToCurrSpaceRotIdx = cacheBuilder.Add(job.sourceToPivotRot);
            else if (data.space == (int)OverrideTransformJob.Space.Local)
                job.sourceToCurrSpaceRotIdx = cacheBuilder.Add(job.sourceToLocalRot);
            else
                job.sourceToCurrSpaceRotIdx = cacheBuilder.Add(job.sourceToWorldRot);

            job.position = Vector3Property.Bind(animator, component, data.positionVector3Property);
            job.rotation = Vector3Property.Bind(animator, component, data.rotationVector3Property);
            job.positionWeight = FloatProperty.Bind(animator, component, data.positionWeightFloatProperty);
            job.rotationWeight = FloatProperty.Bind(animator, component, data.rotationWeightFloatProperty);

            job.cache = cacheBuilder.Build();

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(OverrideTransformJob job)
        {
            job.cache.Dispose();
        }

        /// <inheritdoc />
        public override void Update(OverrideTransformJob job, ref T data)
        {
            job.UpdateSpace(data.space);
        }
    }
}
