using System;
using System.Reflection;
using System.Collections.Generic;

using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    /// <summary>
    /// Data types are identified by annotating a user-defined struct
    /// with the data attribute. All data types are collected at startup
    /// in the data type collection.
    /// </summary>
    /// <seealso cref="DataAttribute"/>
    internal class DataType
    {
        internal class Field
        {
            public string name;
            public FieldInfo info;
            public Type type;
            public int offset;
            public bool selfNodeOnly;

            public static Field Create(FieldInfo info, Type type, string name, bool selfNodeOnly)
            {
                var offset =
                    UnsafeUtility.GetFieldOffset(info);

                return new Field
                {
                    info = info,
                    name = name,
                    type = type,
                    offset = offset,
                    selfNodeOnly = selfNodeOnly
                };
            }
        }

        internal Type type;
        internal int hashCode;

        internal Field[] inputFields;
        internal Field[] outputFields;
        internal Field[] propertyFields;

        internal int numInputFields => inputFields.Length;
        internal int numOutputFields => outputFields.Length;
        internal int numPropertyFields => propertyFields.Length;

        DataType(Type type)
        {
            Assert.IsTrue(HasDataAttribute(type));

            var fields = type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var inputFields = new List<Field>();
            var outputFields = new List<Field>();
            var propertyFields = new List<Field>();

            foreach (var field in fields)
            {
                string name = field.Name;

                var inputAttribute = field.GetCustomAttribute<InputAttribute>();

                if (inputAttribute != null)
                {
                    if (field.FieldType != typeof(DebugIdentifier))
                    {
                        throw new ArgumentException($"Data type {type.FullName} has input {name} of type {field.FieldType.FullName}, all inputs should be of type {nameof(DebugIdentifier)}", "type");
                    }

                    var inputType = typeof(DebugIdentifier);

                    if (!string.IsNullOrEmpty(inputAttribute.name))
                    {
                        name = inputAttribute.name;
                    }

                    inputFields.Add(Field.Create(field, inputType, name, false));
                }

                var outputAttribute = field.GetCustomAttribute<OutputAttribute>();

                if (outputAttribute != null)
                {
                    if (field.FieldType != typeof(DebugIdentifier))
                    {
                        throw new ArgumentException($"Data type {type.FullName} has output {name} of type {field.FieldType.FullName}, all outputs should be of type {nameof(DebugIdentifier)}", "type");
                    }

                    var outputType = typeof(DebugIdentifier);

                    if (!string.IsNullOrEmpty(outputAttribute.name))
                    {
                        name = outputAttribute.name;
                    }

                    outputFields.Add(Field.Create(field, outputType, name, outputAttribute.AcceptOnlySelfNode));
                }

                var propertyAttribute = field.GetCustomAttribute<PropertyAttribute>();

                if (propertyAttribute != null)
                {
                    if (!string.IsNullOrEmpty(propertyAttribute.name))
                    {
                        name = propertyAttribute.name;
                    }

                    propertyFields.Add(Field.Create(field, field.FieldType, name, inputAttribute.AcceptOnlySelfNode));
                }
            }

            hashCode = BurstRuntime.GetHashCode32(type);

            this.type = type;

            this.inputFields = inputFields.ToArray();
            this.outputFields = outputFields.ToArray();
            this.propertyFields = propertyFields.ToArray();
        }

        static bool HasDataAttribute(Type type)
        {
            return type.GetCustomAttributes(typeof(DataAttribute), true).Length > 0;
        }

        internal static DataType Create(Type type)
        {
            return new DataType(type);
        }

        static DataType[] types;

        internal static DataType GetDataType(Type type)
        {
            foreach (var dataType in types)
            {
                if (dataType.type == type)
                {
                    return dataType;
                }
            }

            return null;
        }

        internal static DataType[] Types
        {
            get
            {
                if (types == null)
                {
                    var dataTypes = new List<DataType>();

                    foreach (var type in GetAllTypes())
                    {
                        if (HasDataAttribute(type))
                        {
                            dataTypes.Add(Create(type));
                        }
                    }

                    types = dataTypes.ToArray();
                }

                return types;
            }
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
