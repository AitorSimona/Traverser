using System;
using System.Collections.Generic;
using Unity.Burst;

namespace Unity.Kinematica.Editor
{
    public class GraphNodeAttribute : Attribute
    {
        Type type;

        static Dictionary<int, Type> nodeTypes;

        public Type RuntimeType => type;

        public GraphNodeAttribute(Type type)
        {
            this.type = type;
        }

        public static Type Type(Type type)
        {
            var attribute = GetAttribute(type);

            if (attribute == null)
            {
                return null;
            }

            return attribute.type;
        }

        static GraphNodeAttribute GetAttribute(Type type)
        {
            var attributes =
                type.GetCustomAttributes(
                    typeof(GraphNodeAttribute), false);

            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes[0] as GraphNodeAttribute;
        }

        static void AddNodeType(Type nodeType, Type runtimeType)
        {
            if (runtimeType != null)
            {
                if (runtimeType.GetCustomAttributes(typeof(DataAttribute), false).Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Type {runtimeType.FullName} needs to have [Data] attribute to be instantiated as a node in the execution graph.");
                }

                bool isGraphNode = nodeType == typeof(GraphNode);
                bool isGraphNodeChild = nodeType.IsSubclassOf(typeof(GraphNode));

                if (!isGraphNode && !isGraphNodeChild)
                {
                    throw new InvalidOperationException(
                        $"Type {nodeType.FullName} needs to inherit from {typeof(GraphNode).FullName}.");
                }

                int typeHashCode = BurstRuntime.GetHashCode32(runtimeType);

                if (!nodeTypes.ContainsKey(typeHashCode) || isGraphNodeChild)
                {
                    nodeTypes[typeHashCode] = nodeType;
                }
            }
        }

        static void ComputeTypes()
        {
            nodeTypes = new Dictionary<int, Type>();

            foreach (var type in GetAllTypes())
            {
                GraphNodeAttribute attribute = GetAttribute(type);
                if (attribute != null)
                {
                    AddNodeType(type, attribute.type);
                }
                else if (DataAttribute.IsGraphNode(type))
                {
                    AddNodeType(typeof(GraphNode), type);
                }
            }
        }

        public static Type GetNodeType(int hashCode)
        {
            if (nodeTypes == null)
            {
                ComputeTypes();
            }

            if (nodeTypes.TryGetValue(hashCode, out Type nodeType))
            {
                return nodeType;
            }

            return null;
        }

        static IEnumerable<Type> GetAllTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in SnapshotDebugger.ReflectionUtility.GetTypesFromAssembly(assembly))
                {
                    if (!type.IsAbstract)
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
