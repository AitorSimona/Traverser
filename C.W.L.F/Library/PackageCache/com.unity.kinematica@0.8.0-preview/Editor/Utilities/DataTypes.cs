using System;
using System.Collections.Generic;
using Unity.Burst;

namespace Unity.Kinematica.Editor
{
    internal class DataTypes
    {
        static Dictionary<int, (Type, DataType)> k_Types = null;

        static Dictionary<int, (Type, DataType)> Types
        {
            get
            {
                if (k_Types == null)
                {
                    k_Types = new Dictionary<int, (Type, DataType)>();

                    List<Type> types = AttributeCache<DataAttribute>.PopulateTypes();
                    foreach (Type type in types)
                    {
                        int code = BurstRuntime.GetHashCode32(type);
                        k_Types[code] = (type, DataType.Create(type));
                    }
                }

                return k_Types;
            }
        }

        internal static bool IsValidType(int hashCode)
        {
            return Types.ContainsKey(hashCode);
        }

        internal static (Type, DataType) GetTypeFromHashCode(int hashCode)
        {
            return Types[hashCode];
        }
    }
}
