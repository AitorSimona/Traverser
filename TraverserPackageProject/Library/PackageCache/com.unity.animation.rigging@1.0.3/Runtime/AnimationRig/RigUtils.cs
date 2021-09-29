using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.Animations.Rigging
{
    static class RigUtils
    {
        internal static readonly Dictionary<Type, PropertyDescriptor> s_SupportedPropertyTypeToDescriptor = new Dictionary<Type, PropertyDescriptor>
        {
            { typeof(float)      , new PropertyDescriptor{ size = 1, type = PropertyType.Float } },
            { typeof(int)        , new PropertyDescriptor{ size = 1, type = PropertyType.Int   } },
            { typeof(bool)       , new PropertyDescriptor{ size = 1, type = PropertyType.Bool  } },
            { typeof(Vector2)    , new PropertyDescriptor{ size = 2, type = PropertyType.Float } },
            { typeof(Vector3)    , new PropertyDescriptor{ size = 3, type = PropertyType.Float } },
            { typeof(Vector4)    , new PropertyDescriptor{ size = 4, type = PropertyType.Float } },
            { typeof(Quaternion) , new PropertyDescriptor{ size = 4, type = PropertyType.Float } },
            { typeof(Vector3Int) , new PropertyDescriptor{ size = 3, type = PropertyType.Int   } },
            { typeof(Vector3Bool), new PropertyDescriptor{ size = 3, type = PropertyType.Bool  } }
        };

        public static IRigConstraint[] GetConstraints(Rig rig)
        {
            IRigConstraint[] constraints = rig.GetComponentsInChildren<IRigConstraint>();
            if (constraints.Length == 0)
                return null;

            List<IRigConstraint> tmp = new List<IRigConstraint>(constraints.Length);
            foreach (var constraint in constraints)
            {
                if (constraint.IsValid())
                    tmp.Add(constraint);
            }

            return tmp.Count == 0 ? null : tmp.ToArray();
        }

        private static Transform[] GetSyncableRigTransforms(Animator animator)
        {
            RigTransform[] rigTransforms = animator.GetComponentsInChildren<RigTransform>();
            if (rigTransforms.Length == 0)
                return null;

            Transform[] transforms = new Transform[rigTransforms.Length];
            for (int i = 0; i < transforms.Length; ++i)
                transforms[i] = rigTransforms[i].transform;

            return transforms;
        }

        private static bool ExtractTransformType(
            Animator animator,
            FieldInfo field,
            ref IAnimationJobData data,
            List<Transform> syncableTransforms
            )
        {
            bool handled = true;

            Type fieldType = field.FieldType;
            if (fieldType == typeof(Transform))
            {
                var value = (Transform)field.GetValue(data);
                if (value != null && value.IsChildOf(animator.transform))
                    syncableTransforms.Add(value);
            }
            else if (fieldType == typeof(Transform[]) || fieldType == typeof(List<Transform>))
            {
                var list = (IEnumerable<Transform>)field.GetValue(data);
                foreach (var element in list)
                    if (element != null && element.IsChildOf(animator.transform))
                        syncableTransforms.Add(element);
            }
            else
                handled = false;

            return handled;
        }

        private static bool ExtractPropertyType(
            FieldInfo field,
            ref IAnimationJobData data,
            List<Property> syncableProperties
            )
        {
            if (!s_SupportedPropertyTypeToDescriptor.TryGetValue(field.FieldType, out PropertyDescriptor descriptor))
                return false;

            syncableProperties.Add(
                new Property { name = PropertyUtils.ConstructConstraintDataPropertyName(field.Name), descriptor = descriptor }
                );

            return true;
        }

        private static bool ExtractWeightedTransforms(
                Animator animator,
                FieldInfo field,
                ref IAnimationJobData data,
                List<Transform> syncableTransforms,
                List<Property> syncableProperties)
        {
            bool handled = true;

            Type fieldType = field.FieldType;
            if (fieldType == typeof(WeightedTransform))
            {
                var value = ((WeightedTransform)field.GetValue(data)).transform;
                if (value != null && value.IsChildOf(animator.transform))
                    syncableTransforms.Add(value);

                syncableProperties.Add(
                    new Property { name = PropertyUtils.ConstructConstraintDataPropertyName(field.Name + ".weight"), descriptor = s_SupportedPropertyTypeToDescriptor[typeof(float)] }
                    );
            }
            else if (fieldType == typeof(WeightedTransformArray))
            {
                var list = (IEnumerable<WeightedTransform>)field.GetValue(data);
                int index = 0;
                foreach (var element in list)
                {
                    if (element.transform != null && element.transform.IsChildOf(animator.transform))
                        syncableTransforms.Add(element.transform);

                    syncableProperties.Add(
                        new Property { name = PropertyUtils.ConstructConstraintDataPropertyName(field.Name + ".m_Item" + index + ".weight"), descriptor = s_SupportedPropertyTypeToDescriptor[typeof(float)] }
                        );

                    ++index;
                }
            }
            else
                handled = false;

            return handled;
        }

        private static void ExtractAllSyncableData(Animator animator, IList<IRigLayer> layers, out List<Transform> syncableTransforms, out List<SyncableProperties> syncableProperties)
        {
            syncableTransforms = new List<Transform>();
            syncableProperties = new List<SyncableProperties>(layers.Count);

            Dictionary<Type, FieldInfo[]> typeToSyncableFields = new Dictionary<Type, FieldInfo[]>();
            foreach (var layer in layers)
            {
                if (!layer.IsValid())
                    continue;

                var constraints = layer.constraints;

                List<ConstraintProperties> allConstraintProperties = new List<ConstraintProperties>(constraints.Length);

                foreach (var constraint in constraints)
                {
                    var data = constraint.data;
                    var dataType = constraint.data.GetType();
                    if (!typeToSyncableFields.TryGetValue(dataType, out FieldInfo[] syncableFields))
                    {
                        FieldInfo[] allFields = dataType.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                            );

                        List<FieldInfo> filteredFields = new List<FieldInfo>(allFields.Length);
                        foreach (var field in allFields)
                            if (field.GetCustomAttribute<SyncSceneToStreamAttribute>() != null)
                                filteredFields.Add(field);

                        syncableFields = filteredFields.ToArray();
                        typeToSyncableFields[dataType] = syncableFields;
                    }

                    List<Property> properties = new List<Property>(syncableFields.Length);
                    foreach (var field in syncableFields)
                    {
                        if (ExtractWeightedTransforms(animator, field, ref data, syncableTransforms, properties))
                            continue;
                        if (ExtractTransformType(animator, field, ref data, syncableTransforms))
                            continue;
                        if (ExtractPropertyType(field, ref data, properties))
                            continue;

                        throw new NotSupportedException("Field type [" + field.FieldType + "] is not a supported syncable property type.");
                    }

                    allConstraintProperties.Add(
                        new ConstraintProperties {
                            component = constraint.component,
                            properties = properties.ToArray()
                        }
                    );
                }

                syncableProperties.Add(
                    new SyncableProperties {
                        rig = new RigProperties { component = layer.rig as Component },
                        constraints = allConstraintProperties.ToArray()
                    }
                );
            }

            var extraTransforms = GetSyncableRigTransforms(animator);
            if (extraTransforms != null)
                syncableTransforms.AddRange(extraTransforms);
        }

        public static IAnimationJob[] CreateAnimationJobs(Animator animator, IRigConstraint[] constraints)
        {
            if (constraints == null || constraints.Length == 0)
                return null;

            IAnimationJob[] jobs = new IAnimationJob[constraints.Length];
            for (int i = 0; i < constraints.Length; ++i)
                jobs[i] = constraints[i].CreateJob(animator);

            return jobs;
        }

        public static void DestroyAnimationJobs(IRigConstraint[] constraints, IAnimationJob[] jobs)
        {
            if (jobs == null || jobs.Length != constraints.Length)
                return;

            for (int i = 0; i < constraints.Length; ++i)
                constraints[i].DestroyJob(jobs[i]);
        }

        private struct RigSyncSceneToStreamData : IAnimationJobData, IRigSyncSceneToStreamData
        {
            public RigSyncSceneToStreamData(Transform[] transforms, SyncableProperties[] properties, int rigCount)
            {
                if (transforms != null && transforms.Length > 0)
                {
                    var unique = UniqueTransformIndices(transforms);
                    if (unique.Length != transforms.Length)
                    {
                        syncableTransforms = new Transform[unique.Length];
                        for (int i = 0; i < unique.Length; ++i)
                            syncableTransforms[i] = transforms[unique[i]];
                    }
                    else
                        syncableTransforms = transforms;
                }
                else
                    syncableTransforms = null;

                syncableProperties = properties;

                rigStates = rigCount > 0 ? new bool[rigCount] : null;

                m_IsValid = !(((syncableTransforms == null || syncableTransforms.Length == 0) &&
                    (syncableProperties == null || syncableProperties.Length == 0) &&
                    rigStates == null));
            }

            static int[] UniqueTransformIndices(Transform[] transforms)
            {
                if (transforms == null || transforms.Length == 0)
                    return null;

                HashSet<int> instanceIDs = new HashSet<int>();
                List<int> unique = new List<int>(transforms.Length);

                for (int i = 0; i < transforms.Length; ++i)
                    if (instanceIDs.Add(transforms[i].GetInstanceID()))
                        unique.Add(i);

                return unique.ToArray();
            }

            public Transform[] syncableTransforms { get; private set; }
            public SyncableProperties[] syncableProperties { get; private set; }
            public bool[] rigStates { get; set; }
            private readonly bool m_IsValid;

            bool IAnimationJobData.IsValid() => m_IsValid;

            void IAnimationJobData.SetDefaultValues()
            {
                syncableTransforms = null;
                syncableProperties = null;
                rigStates = null;
            }
        }

        internal static IAnimationJobData CreateSyncSceneToStreamData(Animator animator, IList<IRigLayer> layers)
        {
            ExtractAllSyncableData(animator, layers, out List<Transform> syncableTransforms, out List<SyncableProperties> syncableProperties);
            return new RigSyncSceneToStreamData(syncableTransforms.ToArray(), syncableProperties.ToArray(), layers.Count);
        }

        public static IAnimationJobBinder syncSceneToStreamBinder { get; } = new RigSyncSceneToStreamJobBinder<RigSyncSceneToStreamData>();
    }
}
