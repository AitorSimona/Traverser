using Unity.Collections;

namespace UnityEngine.Animations.Rigging
{
    using TransformSyncer = RigSyncSceneToStreamJob.TransformSyncer;
    using PropertySyncer  = RigSyncSceneToStreamJob.PropertySyncer;

    [Unity.Burst.BurstCompile]
    internal struct RigSyncSceneToStreamJob : IAnimationJob
    {
        public struct TransformSyncer : System.IDisposable
        {
            public NativeArray<TransformSceneHandle> sceneHandles;
            public NativeArray<TransformStreamHandle> streamHandles;

            public static TransformSyncer Create(int size)
            {
                return new TransformSyncer() {
                    sceneHandles = new NativeArray<TransformSceneHandle>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                    streamHandles = new NativeArray<TransformStreamHandle>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)
                };
            }

            public void Dispose()
            {
                if (sceneHandles.IsCreated)
                    sceneHandles.Dispose();

                if (streamHandles.IsCreated)
                    streamHandles.Dispose();
            }

            public void BindAt(int index, Animator animator, Transform transform)
            {
                sceneHandles[index] = animator.BindSceneTransform(transform);
                streamHandles[index] = animator.BindStreamTransform(transform);
            }

            public void Sync(ref AnimationStream stream)
            {
                for (int i = 0, count = sceneHandles.Length; i < count; ++i)
                {
                    var sceneHandle = sceneHandles[i];
                    if (!sceneHandle.IsValid(stream))
                        continue;

                    var streamHandle = streamHandles[i];
                    sceneHandle.GetLocalTRS(stream, out Vector3 scenePos, out Quaternion sceneRot, out Vector3 sceneScale);
                    streamHandle.SetLocalTRS(stream, scenePos, sceneRot, sceneScale, true);

                    streamHandles[i] = streamHandle;
                    sceneHandles[i] = sceneHandle;
                }
            }
        }

        internal struct PropertySyncer : System.IDisposable
        {
            public NativeArray<PropertySceneHandle> sceneHandles;
            public NativeArray<PropertyStreamHandle> streamHandles;
            public NativeArray<float> buffer;

            public static PropertySyncer Create(int size)
            {
                return new PropertySyncer() {
                    sceneHandles = new NativeArray<PropertySceneHandle>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                    streamHandles = new NativeArray<PropertyStreamHandle>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                    buffer = new NativeArray<float>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)
                };
            }

            public void Dispose()
            {
                if (sceneHandles.IsCreated)
                    sceneHandles.Dispose();

                if (streamHandles.IsCreated)
                    streamHandles.Dispose();

                if (buffer.IsCreated)
                    buffer.Dispose();
            }

            public void BindAt(int index, Animator animator, Component component, string property)
            {
                sceneHandles[index] = animator.BindSceneProperty(component.transform, component.GetType(), property);
                streamHandles[index] = animator.BindStreamProperty(component.transform, component.GetType(), property);
            }

            public void Sync(ref AnimationStream stream)
            {
                AnimationSceneHandleUtility.ReadFloats(stream, sceneHandles, buffer);
                AnimationStreamHandleUtility.WriteFloats(stream, streamHandles, buffer, true);
            }

            public NativeArray<float> StreamValues(ref AnimationStream stream)
            {
                AnimationStreamHandleUtility.ReadFloats(stream, streamHandles, buffer);
                return buffer;
            }
        }

        public TransformSyncer transformSyncer;
        public PropertySyncer propertySyncer;
        public PropertySyncer rigWeightSyncer;
        public PropertySyncer constraintWeightSyncer;

        public NativeArray<float> rigStates;
        public NativeArray<int> rigConstraintEndIdx;

        public NativeArray<PropertyStreamHandle> modulatedConstraintWeights;

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            transformSyncer.Sync(ref stream);
            propertySyncer.Sync(ref stream);
            rigWeightSyncer.Sync(ref stream);
            constraintWeightSyncer.Sync(ref stream);

            var currRigWeights = rigWeightSyncer.StreamValues(ref stream);
            var currConstraintWeights = constraintWeightSyncer.StreamValues(ref stream);

            int rigIndex = 0;
            for (int i = 0, count = currConstraintWeights.Length; i < count; ++i)
            {
                if (i >= rigConstraintEndIdx[rigIndex])
                    rigIndex++;

                currConstraintWeights[i] *= (currRigWeights[rigIndex] * rigStates[rigIndex]);
            }

            AnimationStreamHandleUtility.WriteFloats(stream, modulatedConstraintWeights, currConstraintWeights, false);
        }
    }

    internal struct SyncableProperties
    {
        public RigProperties rig;
        public ConstraintProperties[] constraints;
    }

    internal interface IRigSyncSceneToStreamData
    {
        Transform[] syncableTransforms { get; }
        SyncableProperties[] syncableProperties { get; }

        bool[] rigStates { get; }
    }

    internal class RigSyncSceneToStreamJobBinder<T> : AnimationJobBinder<RigSyncSceneToStreamJob, T>
        where T : struct, IAnimationJobData, IRigSyncSceneToStreamData
    {
        internal static string[] s_PropertyElementNames = new string[] {".x", ".y", ".z", ".w"};

        public override RigSyncSceneToStreamJob Create(Animator animator, ref T data, Component component)
        {
            var job = new RigSyncSceneToStreamJob();

            var transforms = data.syncableTransforms;
            if (transforms != null)
            {
                job.transformSyncer = TransformSyncer.Create(transforms.Length);
                for (int i = 0; i < transforms.Length; ++i)
                    job.transformSyncer.BindAt(i, animator, transforms[i]);
            }

            var properties = data.syncableProperties;
            if (properties != null)
            {
                int rigCount = properties.Length, constraintCount = 0, propertyCount = 0;
                for (int i = 0; i < properties.Length; ++i)
                {
                    constraintCount += properties[i].constraints.Length;
                    for (int j = 0; j < properties[i].constraints.Length; ++j)
                        for (int k = 0; k < properties[i].constraints[j].properties.Length; ++k)
                            propertyCount += properties[i].constraints[j].properties[k].descriptor.size;
                }

                job.propertySyncer = PropertySyncer.Create(propertyCount);
                job.rigWeightSyncer = PropertySyncer.Create(rigCount);
                job.constraintWeightSyncer = PropertySyncer.Create(constraintCount);
                job.rigStates = new NativeArray<float>(rigCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                job.rigConstraintEndIdx = new NativeArray<int>(rigCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                job.modulatedConstraintWeights = new NativeArray<PropertyStreamHandle>(constraintCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                int constraintIdx = 0, propertyIdx = 0;
                for (int i = 0; i < properties.Length; ++i)
                {
                    job.rigWeightSyncer.BindAt(i, animator, properties[i].rig.component, RigProperties.s_Weight);
                    job.rigStates[i] = data.rigStates[i] ? 1f : 0f;

                    var constraints = properties[i].constraints;
                    for (int j = 0; j < constraints.Length; ++j)
                    {
                        ref var constraint = ref constraints[j];

                        job.constraintWeightSyncer.BindAt(constraintIdx, animator, constraint.component, ConstraintProperties.s_Weight);
                        job.modulatedConstraintWeights[constraintIdx++] = animator.BindCustomStreamProperty(
                            PropertyUtils.ConstructCustomPropertyName(constraint.component, ConstraintProperties.s_Weight),
                            CustomStreamPropertyType.Float
                            );

                        for (int k = 0; k < constraint.properties.Length; ++k)
                        {
                            ref var property = ref constraint.properties[k];
                            if (property.descriptor.size == 1)
                                job.propertySyncer.BindAt(propertyIdx++, animator, constraint.component, property.name);
                            else
                            {
                                Debug.Assert(property.descriptor.size <= 4);
                                for (int l = 0; l < property.descriptor.size; ++l)
                                    job.propertySyncer.BindAt(propertyIdx++, animator, constraint.component, property.name + s_PropertyElementNames[l]);
                            }
                        }
                    }

                    job.rigConstraintEndIdx[i] = constraintIdx;
                }
            }

            return job;
        }

        public override void Destroy(RigSyncSceneToStreamJob job)
        {
            job.transformSyncer.Dispose();
            job.propertySyncer.Dispose();
            job.rigWeightSyncer.Dispose();
            job.constraintWeightSyncer.Dispose();

            if (job.rigStates.IsCreated)
                job.rigStates.Dispose();

            if (job.rigConstraintEndIdx.IsCreated)
                job.rigConstraintEndIdx.Dispose();

            if (job.modulatedConstraintWeights.IsCreated)
                job.modulatedConstraintWeights.Dispose();
        }

        public override void Update(RigSyncSceneToStreamJob job, ref T data)
        {
            int count = Mathf.Min(job.rigStates.Length, data.rigStates.Length);
            for (int i = 0; i < count; ++i)
                job.rigStates[i] = data.rigStates[i] ? 1f : 0f;
        }
    }
}
