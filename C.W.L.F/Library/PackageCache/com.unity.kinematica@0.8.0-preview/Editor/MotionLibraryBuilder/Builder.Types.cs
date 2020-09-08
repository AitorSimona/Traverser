using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine.Assertions;

using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public class RegisteredType
        {
            public Type type;

            public List<RegisteredType> fields;

            public int typeIndex;
            public int nameIndex;
            public int numBytes;
            public int hashCode;

            public RegisteredType(Type type, int nameIndex, int typeIndex)
            {
                this.type = type;
                this.typeIndex = typeIndex;
                this.nameIndex = nameIndex;

                hashCode = BurstRuntime.GetHashCode32(type);

                numBytes = UnsafeUtility.SizeOf(type);

                fields = new List<RegisteredType>();
            }
        }

        public void BuildTypes()
        {
            int numFields = 0;

            foreach (var registeredType in registeredTypes)
            {
                numFields += registeredType.fields.Count;
            }

            ref Binary binary = ref Binary;

            allocator.Allocate(registeredTypes.Count, ref binary.types);
            allocator.Allocate(numFields, ref binary.fields);

            int typeIndex = 0;
            int fieldIndex = 0;

            foreach (var registeredType in registeredTypes)
            {
                binary.types[typeIndex].nameIndex = registeredType.nameIndex;
                binary.types[typeIndex].hashCode = registeredType.hashCode;
                binary.types[typeIndex].numBytes = registeredType.numBytes;
                binary.types[typeIndex].fieldIndex = fieldIndex;
                binary.types[typeIndex].numFields = registeredType.fields.Count;

                foreach (var fieldType in registeredType.fields)
                {
                    binary.fields[fieldIndex].typeIndex = fieldType.typeIndex;

                    fieldIndex++;
                }

                typeIndex++;
            }

            Assert.IsTrue(typeIndex == registeredTypes.Count);
            Assert.IsTrue(fieldIndex == numFields);
        }

        RegisteredType RegisterType(Type type)
        {
            var registeredType = FindRegisteredType(type);

            if (registeredType == null)
            {
                registeredType = CreateRegisteredType(type);

                if (!type.IsPrimitive)
                {
                    var fieldInfos = type.GetFields(
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Public);

                    foreach (var fieldInfo in fieldInfos)
                    {
                        registeredType.fields.Add(
                            RegisterType(fieldInfo.FieldType));
                    }
                }
            }

            return registeredType;
        }

        RegisteredType CreateRegisteredType(Type type)
        {
            int index = registeredTypes.Count;

            var nameIndex =
                stringTable.RegisterString(
                    NameFromType(type));

            var registeredType =
                new RegisteredType(type,
                    nameIndex, index);
            registeredTypes.Add(registeredType);
            return registeredType;
        }

        RegisteredType FindRegisteredType(Type type)
        {
            foreach (RegisteredType registeredType in registeredTypes)
            {
                if (registeredType.type.Equals(type))
                {
                    return registeredType;
                }
            }

            return null;
        }

        static string NameFromType(Type type)
        {
            string typeName = type.FullName;

            int dotIndex = typeName.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                typeName = typeName.Substring(dotIndex + 1);
            }

            return typeName;
        }

        List<RegisteredType> registeredTypes = new List<RegisteredType>();
    }
}
