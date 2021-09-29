using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The MultiAim constraint job.
    /// </summary>
    [Unity.Burst.BurstCompile]
    public struct MultiAimConstraintJob : IWeightedAnimationJob
    {
        const float k_Epsilon = 1e-5f;

        /// <summary>
        /// Specifies how the world up vector used by the Multi-Aim constraint is defined.
        /// </summary>
        public enum WorldUpType
        {
            /// <summary>Neither defines nor uses a world up vector.</summary>
            None,
            /// <summary>Uses and defines the world up vector as the Unity Scene up vector (the Y axis).</summary>
            SceneUp,
            /// <summary>Uses and defines the world up vector as a vector from the constrained object, in the direction of the up object.</summary>
            ObjectUp,
            /// <summary>Uses and defines the world up vector as relative to the local space of the object.</summary>
            ObjectRotationUp,
            /// <summary>Uses and defines the world up vector as a vector specified by the user.</summary>
            Vector
        };

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

        /// <summary>Local aim axis of the constrained object Transform.</summary>
        public Vector3 aimAxis;
        /// <summary>Local up axis of the constrained object Transform.</summary>
        public Vector3 upAxis;

        /// <summary>
        /// Specifies which mode to use to keep the upward direction of the constrained Object.
        /// </summary>
        public WorldUpType worldUpType;
        /// <summary>
        /// A static vector in world coordinates that is the general upward direction. This is used when World Up Type is set to WorldUpType.Vector.
        /// </summary>
        public Vector3 worldUpAxis;
        /// <summary>
        /// The Transform handle for the world up object. This is used when World Up Type is set to WorldUpType.ObjectUp or WorldUpType.ObjectRotationUp.
        /// </summary>
        public ReadOnlyTransformHandle worldUpObject;

        /// <summary>Axes mask. Rotation will apply on the local axis for a value of 1.0, and will be kept as is for a value of 0.0.</summary>
        public Vector3 axesMask;

        /// <summary>Minimum rotation value.</summary>
        public FloatProperty minLimit;
        /// <summary>Maximum rotation value.</summary>
        public FloatProperty maxLimit;

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

                var worldUpVector = ComputeWorldUpVector(stream);
                var upVector = AnimationRuntimeUtils.Select(Vector3.zero, upAxis, axesMask);

                float weightScale = sumWeights > 1f ? 1f / sumWeights : 1f;

                Vector2 minMaxAngles = new Vector2(minLimit.Get(stream), maxLimit.Get(stream));

                var drivenWPos = driven.GetPosition(stream);
                var drivenLRot = driven.GetLocalRotation(stream);
                var drivenParentInvRot = Quaternion.Inverse(drivenParent.GetRotation(stream));
                Quaternion accumDeltaRot = QuaternionExt.zero;
                float accumWeights = 0f;
                for (int i = 0; i < sourceTransforms.Length; ++i)
                {
                    var normalizedWeight = weightBuffer[i] * weightScale;
                    if (normalizedWeight < k_Epsilon)
                        continue;

                    ReadOnlyTransformHandle sourceTransform = sourceTransforms[i];

                    var fromDir = drivenLRot * aimAxis;
                    var toDir = drivenParentInvRot * (sourceTransform.GetPosition(stream) - drivenWPos);
                    if (toDir.sqrMagnitude < k_Epsilon)
                        continue;

                    var crossDir = Vector3.Cross(fromDir, toDir).normalized;
                    if (Vector3.Dot(axesMask, axesMask) < 3f)
                    {
                        crossDir = AnimationRuntimeUtils.Select(Vector3.zero, crossDir, axesMask).normalized;
                        if (Vector3.Dot(crossDir, crossDir) > k_Epsilon)
                        {
                            fromDir = AnimationRuntimeUtils.ProjectOnPlane(fromDir, crossDir);
                            toDir = AnimationRuntimeUtils.ProjectOnPlane(toDir, crossDir);
                        }
                        else
                        {
                            toDir = fromDir;
                        }
                    }

                    var rotToSource = Quaternion.AngleAxis(
                        Mathf.Clamp(Vector3.Angle(fromDir, toDir), minMaxAngles.x, minMaxAngles.y),
                        crossDir
                    );

                    if (worldUpType != WorldUpType.None && Vector3.Dot(upVector, upVector) > k_Epsilon)
                    {
                        var wupProject = Vector3.Cross(Vector3.Cross(drivenParentInvRot * worldUpVector, toDir).normalized, toDir).normalized;
                        var rupProject = Vector3.Cross(Vector3.Cross(drivenLRot * rotToSource * upVector, toDir).normalized, toDir).normalized;

                        rotToSource = QuaternionExt.FromToRotation(rupProject, wupProject) * rotToSource;
                    }

                    accumDeltaRot = QuaternionExt.Add(
                        accumDeltaRot,
                        QuaternionExt.Scale(sourceOffsets[i] * rotToSource, normalizedWeight)
                        );

                    // Required to update handles with binding info.
                    sourceTransforms[i] = sourceTransform;
                    accumWeights += normalizedWeight;
                }

                accumDeltaRot = QuaternionExt.NormalizeSafe(accumDeltaRot);
                if (accumWeights < 1f)
                    accumDeltaRot = Quaternion.Lerp(Quaternion.identity, accumDeltaRot, accumWeights);

                Quaternion newRot = accumDeltaRot * drivenLRot;
                if (Vector3.Dot(axesMask, axesMask) < 3f)
                    newRot = Quaternion.Euler(AnimationRuntimeUtils.Select(drivenLRot.eulerAngles, newRot.eulerAngles, axesMask));

                var offset = drivenOffset.Get(stream);
                if (Vector3.Dot(offset, offset) > 0f)
                    newRot *= Quaternion.Euler(offset);

                driven.SetLocalRotation(stream, Quaternion.Lerp(drivenLRot, newRot, w));
            }
            else
                AnimationRuntimeUtils.PassThrough(stream, driven);
        }

        Vector3 ComputeWorldUpVector(AnimationStream stream)
        {
            var result = Vector3.up;
            switch (worldUpType)
            {
                case WorldUpType.None:
                    result = Vector3.zero;
                    break;
                case WorldUpType.SceneUp:
                    // the scene Up vector and the World Up vector are the same thing
                    break;
                case WorldUpType.ObjectUp:
                {
                    // the target's Up vector points to the up object
                    var referencePos = Vector3.zero;
                    if (worldUpObject.IsValid(stream))
                        referencePos = worldUpObject.GetPosition(stream);
                    var targetPos = driven.GetPosition(stream);

                    result = (referencePos - targetPos).normalized;
                    break;
                }
                case WorldUpType.ObjectRotationUp:
                {
                    var upRotation = Quaternion.identity;
                    if (worldUpObject.IsValid(stream))
                        upRotation = worldUpObject.GetRotation(stream);

                    // if no object is specified, the up vector is defined relative to the scene world space
                    result = upRotation * worldUpAxis;
                    break;
                }
                case WorldUpType.Vector:
                    result = worldUpAxis;
                    break;
                default:
                    break;
            }

            return result;
        }
    }

    /// <summary>
    /// This interface defines the data mapping for the MultiAim constraint.
    /// </summary>
    public interface IMultiAimConstraintData
    {
        /// <summary>The Transform affected by the constraint Source Transforms.</summary>
        Transform constrainedObject { get; }
        /// <summary>
        /// The list of Transforms that influence the constrained Transform orientation.
        /// Each source has a weight from 0 to 1.
        /// </summary>
        WeightedTransformArray sourceObjects { get; }
        /// <summary>
        /// This is used to maintain the current rotation offset from the constrained GameObject to the source GameObjects.
        /// </summary>
        bool maintainOffset { get; }

        /// <summary>Specifies the local aim axis of the constrained Transform to use in order to orient itself to the Source Transforms.</summary>
        Vector3 aimAxis { get; }
        /// <summary>Specified the local up axis of the constrained Transform to use in order to orient itself to the Source Transforms.</summary>
        Vector3 upAxis { get; }


        /// <summary>
        /// Specifies which mode to use to keep the upward direction of the constrained Object.
        /// </summary>
        /// <seealso cref="MultiAimConstraintJob.WorldUpType"/>
        int worldUpType { get; }
        /// <summary>
        /// A static vector in world coordinates that is the general upward direction. This is used when World Up Type is set to WorldUpType.Vector.
        /// </summary>
        Vector3 worldUpAxis { get; }
        /// <summary>
        /// The Transform used to calculate the upward direction. This is used when World Up Type is set to WorldUpType.ObjectUp or WorldUpType.ObjectRotationUp.
        /// </summary>
        Transform worldUpObject { get;  }

        /// <summary>Toggles whether the constrained Transform will rotate along the X axis.</summary>
        bool constrainedXAxis { get; }
        /// <summary>Toggles whether the constrained Transform will rotate along the Y axis.</summary>
        bool constrainedYAxis { get; }
        /// <summary>Toggles whether the constrained Transform will rotate along the Z axis.</summary>
        bool constrainedZAxis { get; }

        /// <summary>The path to the offset property in the constraint component.</summary>
        string offsetVector3Property { get; }
        /// <summary>The path to the minimum limit property in the constraint component.</summary>
        string minLimitFloatProperty { get; }
        /// <summary>The path to the maximum limit property in the constraint component.</summary>
        string maxLimitFloatProperty { get; }
        /// <summary>The path to the source objects property in the constraint component.</summary>
        string sourceObjectsProperty { get; }
    }

    /// <summary>
    /// The MultiAim constraint job binder.
    /// </summary>
    /// <typeparam name="T">The constraint data type</typeparam>
    public class MultiAimConstraintJobBinder<T> : AnimationJobBinder<MultiAimConstraintJob, T>
        where T : struct, IAnimationJobData, IMultiAimConstraintData
    {
        /// <inheritdoc />
        public override MultiAimConstraintJob Create(Animator animator, ref T data, Component component)
        {
            var job = new MultiAimConstraintJob();

            job.driven = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.drivenParent = ReadOnlyTransformHandle.Bind(animator, data.constrainedObject.parent);
            job.aimAxis = data.aimAxis;
            job.upAxis = data.upAxis;

            job.worldUpType = (MultiAimConstraintJob.WorldUpType)data.worldUpType;
            job.worldUpAxis = data.worldUpAxis;
            job.worldUpObject = ReadOnlyTransformHandle.Bind(animator, data.worldUpObject);

            WeightedTransformArray sourceObjects = data.sourceObjects;

            WeightedTransformArrayBinder.BindReadOnlyTransforms(animator, component, sourceObjects, out job.sourceTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, sourceObjects, data.sourceObjectsProperty, out job.sourceWeights);

            job.sourceOffsets = new NativeArray<Quaternion>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            job.weightBuffer = new NativeArray<float>(sourceObjects.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < sourceObjects.Count; ++i)
            {
                if (data.maintainOffset)
                {
                    var constrainedAim = data.constrainedObject.rotation * data.aimAxis;
                    job.sourceOffsets[i] = QuaternionExt.FromToRotation(
                        sourceObjects[i].transform.position - data.constrainedObject.position,
                        constrainedAim
                        );
                }
                else
                    job.sourceOffsets[i] = Quaternion.identity;
            }

            job.minLimit = FloatProperty.Bind(animator, component, data.minLimitFloatProperty);
            job.maxLimit = FloatProperty.Bind(animator, component, data.maxLimitFloatProperty);
            job.drivenOffset = Vector3Property.Bind(animator, component, data.offsetVector3Property);

            job.axesMask = new Vector3(
                System.Convert.ToSingle(data.constrainedXAxis),
                System.Convert.ToSingle(data.constrainedYAxis),
                System.Convert.ToSingle(data.constrainedZAxis)
                );

            return job;
        }

        /// <inheritdoc />
        public override void Destroy(MultiAimConstraintJob job)
        {
            job.sourceTransforms.Dispose();
            job.sourceWeights.Dispose();
            job.sourceOffsets.Dispose();
            job.weightBuffer.Dispose();
        }
    }
}
