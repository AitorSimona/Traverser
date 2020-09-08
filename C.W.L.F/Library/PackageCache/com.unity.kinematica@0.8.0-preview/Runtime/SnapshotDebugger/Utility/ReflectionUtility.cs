using System;
using System.Collections.Generic;
using System.Reflection;

namespace Unity.SnapshotDebugger
{
    public static class ReflectionUtility
    {
        /// <summary>
        /// Get all defined types in a given assembly
        /// </summary>
        public static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            foreach (Type type in types)
            {
                if (type != null)
                {
                    yield return type;
                }
            }
        }
    }
}
