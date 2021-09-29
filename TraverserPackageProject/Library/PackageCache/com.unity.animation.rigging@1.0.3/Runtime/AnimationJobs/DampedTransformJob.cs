namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The DampedTransform job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct DampedTransformJob : IWeightedAnimationJob
    {
        const float k_FixedDt = 0.01667f; // 60Hz simulation step
        const float k_DampFactor = 40f;

        /// <summary>The Transform handle for the constrained object Transform.</summary>
        public ReadWriteTransformHandle driven;
        /// <summary>The Transform handle for the source object Transform.</summary>
        public ReadOnlyTransformHandle source;

        /// <summary>Initial TR offset from source to constrained object.</summary>
        public AffineTransform localBindTx;

        /// <summary>Aim axis used to adjust constrained object rotation if maintainAim is enabled.</summary>
        public Vector3 aimBindAxis;

        /// <summary>Previous frame driven Transform TR components.</summary>
        public AffineTransform prevDrivenTx;

        /// <summary>
        /// Defines how much of constrained object position follows source object position.
        /// constrained position will closely follow source object when set to 0, and will
        /// not move when set to 1.
        /// </summary>
        public FloatProperty dampPosition;
        /// <summary>
        /// Defines how much of constrained object rotation follows source object rotation.
        /// constrained rotation will closely follow source object when set to 0, and will
        /// not move when set to 1.
        /// </summary>
        public FloatProperty dampRotation;

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
            float streamDt = Mathf.Abs(stream.deltaTime);
            driven.GetGlobalTR(stream, out Vector3 drivenPos, out Quaternion drivenRot);

            if (w > 0f && streamDt > 0f)
            {
                source.GetGlobalTR(stream, out Vector3 sourcePos, out Quaternion sourceRot);
                var sourceTx = new AffineTransform(sourcePos, sourceRot);
                var targetTx = sourceTx * localBindTx;
                targetTx.translation = Vector3.Lerp(drivenPos, targetTx.translation, w);
                targetTx.rotation = Quaternion.Lerp(drivenRot, targetTx.rotation, w);

                var dampPosW = AnimationRuntimeUtils.Square(1f - dampPosition.Get(stream));
                var dampRotW = AnimationRuntimeUtils.Square(1f - dampRotation.Get(stream));
                bool doAimAjustements = Vector3.Dot(aimBindAxis, aimBindAxis) > 0f;

                while (streamDt > 0f)
                {
                    float factoredDt = k_DampFactor * Mathf.Min(k_FixedDt, streamDt);

                    prevDrivenTx.translation +=
                        (targetTx.translation - prevDrivenTx.translation) * dampPosW * factoredDt;

                    prevDrivenTx.rotation *= Quaternion.Lerp(
                        Quaternion.identity,
                        Quaternion.Inverse(prevDrivenTx.rotation) * targetTx.rotation,
                        dampRotW * factoredDt
                        );

                    if (doAimAjustements)
                    {
                        var fromDir = prevDrivenTx.rotation * aimBindAxis;
                        var toDir = sourceTx.translation - prevDrivenTx.translation;
                        prevDrivenTx.rotation =
                            Quaternion.AngleAxis(Vector3.Angle(fromDir, toDir), Vector3.Cross(fromDir, toDir).normalized) * prevDrivenTx.rotation;
                    }

                    streamDt -= k_FixedDt;
                }

                driven.SetGlobalTR(stream, prevDrivenTx.translation, prevDrivenTx.rotation);
            }
            else
            {
                prevDrivenTx.Set(drivenPos, drivenRot);
                AnimationRuntimeUtils.PassThrough(stream, driven);
            }
        }
    }

    /// <summary>
    /// This interface defines the data mapping for DampedTransform.
    /// </summary>
    public interface IDampedTransformData
    {
        /// <summary>The Transform affected by the constraint Source Transform.</summary>
        Transform constrainedObject { get; }
        /// <summary>The source Transform.</summary>
        Transform sourceObject { get; }

        /// <summary>Toggles whether damping will enforces aim.</summary>
        bool maintainAim { get; }

        /// <summary>The path to the damp position weight property in the constraint component.</summary>
        string dampPositionFloatProperty { get; }
        /// <summary>The path to the damp rotation weight property in the constraint component.</summary>
        string dampRotationFloatProperty { get; }
    }

    /// <summary>
    /// The DampedTransform job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class DampedTransformJobBinder<T> : AnimationJobBinder<DampedTransformJob, T>
        where T : struct, IAnimationJobData, IDampedTransformData
    {
        /// <inheritdoc />
        public override DampedTransformJob Create(Animator animator, ref T data, Component component)
        {
            var job = new DampedTransformJob();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.source = ReadOnlyTransformHandle.Bind(animator, data.sourceObject);

            var drivenTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
            var sourceTx = new AffineTransform(data.sourceObject.position, data.sourceObject.rotation);

            job.localBindTx = sourceTx.InverseMul(drivenTx);
            job.prevDrivenTx = drivenTx;

            job.dampPosition = FloatProperty.Bind(animator, component, data.dampPositionFloatProperty);
            job.dampRotation = FloatProperty.Bind(animator, component, data.dampRotationFloatProperty);

            if (data.maintainAim && AnimationRuntimeUtils.SqrDistance(data.constrainedObject.position, data.sourceObject.position) > 0f)
                job.aimBindAxis = Quaternion.Inverse(data.constrainedObject.rotation) * (sourceTx.translation - drivenTx.translation).normalized;
            else
                job.aimBindAxis = Vector3.zero;

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(DampedTransformJob job)
        {
        }
    }
}
