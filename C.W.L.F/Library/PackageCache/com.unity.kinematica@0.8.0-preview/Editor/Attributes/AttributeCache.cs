using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica.Editor
{
    internal static class AttributeCache<T>
    {
        static IEnumerable<Type> PopulateTypes(Assembly assembly)
        {
            foreach (Type type in SnapshotDebugger.ReflectionUtility.GetTypesFromAssembly(assembly))
            {
                if (IsTType(type))
                {
                    yield return type;
                }
            }
        }

        public static List<Type> PopulateTypes()
        {
            List<Type> types = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                types.AddRange(PopulateTypes(assembly));
            }

            return TrimDuplicates(types);
        }

        public static List<Type> TrimDuplicates(List<Type> types)
        {
            Assembly packageAssembly = typeof(Asset).Assembly;
            var grouped = types.GroupBy(x => x.FullName);
            var result = new List<Type>();

            foreach (var group in grouped)
            {
                Type overridenType = group.FirstOrDefault(t => t.Assembly != packageAssembly);
                if (overridenType != null)
                {
                    result.Add(overridenType);
                }
                else
                {
                    result.Add(group.FirstOrDefault());
                }
            }

            return result;
        }

        static bool IsTType(Type type)
        {
            Assert.IsTrue(type != null);

            return type.GetCustomAttributes(typeof(T), true).Length > 0;
        }
    }
}
