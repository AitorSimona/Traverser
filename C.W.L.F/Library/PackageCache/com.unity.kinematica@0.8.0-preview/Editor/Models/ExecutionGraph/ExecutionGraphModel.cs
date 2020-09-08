using System;
using System.Collections.Generic;


namespace Unity.Kinematica.Editor
{
    internal struct ExecutionGroupModel
    {
        public int groupIndex;
        public List<DebugReference> references;

        public string title;

        public static ExecutionGroupModel Create(int groupIndex)
        {
            return new ExecutionGroupModel()
            {
                groupIndex = groupIndex,
                references = new List<DebugReference>()
            };
        }

        public void FindTitle(DebugMemory memory)
        {
            foreach (DebugReference reference in references)
            {
                if (DataTypes.IsValidType(reference.identifier.typeHashCode))
                {
                    Type type = DataTypes.GetTypeFromHashCode(reference.identifier.typeHashCode).Item1;
                    if (typeof(IMotionMatchingQuery).IsAssignableFrom(type))
                    {
                        object debugObject = memory.ReadObjectGeneric(reference);
                        title = (debugObject as IMotionMatchingQuery).DebugTitle;

                        if (debugObject is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        break;
                    }
                }
            }
        }
    }

    internal struct ExecutionGraphModel
    {
        public List<ExecutionGroupModel> groups;

        public static ExecutionGraphModel Create(DebugMemory? memory)
        {
            List<ExecutionGroupModel> groups = new List<ExecutionGroupModel>();

            if (memory.HasValue)
            {
                ExecutionGroupModel group = ExecutionGroupModel.Create(0);

                for (DebugReference reference = memory.Value.FirstOrDefault; reference.IsValid; reference = memory.Value.Next(reference))
                {
                    if (reference.dataOnly)
                    {
                        continue;
                    }

                    if (reference.group != group.groupIndex)
                    {
                        if (group.references.Count > 0)
                        {
                            group.FindTitle(memory.Value);
                            groups.Add(group);
                        }

                        group = ExecutionGroupModel.Create(reference.group);
                    }

                    group.references.Add(reference);
                }

                if (group.references.Count > 0)
                {
                    group.FindTitle(memory.Value);
                    groups.Add(group);
                }
            }

            return new ExecutionGraphModel()
            {
                groups = groups
            };
        }
    }
}
