using System;
using System.Reflection;

using UnityEngine;
using UnityEditor;

namespace Unity.SnapshotDebugger.Editor
{
    [InitializeOnLoad]
    internal static class VariableValidator
    {
        static VariableValidator()
        {
            foreach (Type type in SnapshotDebugger.ReflectionUtility.GetTypesFromAssembly(Assembly.GetAssembly(typeof(SnapshotAttribute))))
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.IsDefined(typeof(SnapshotAttribute), false) == true)
                    {
                        if (typeof(SnapshotProvider).IsAssignableFrom(field.DeclaringType) == false)
                        {
                            Debug.LogWarning($"Field '{field.Name}' defined in type '{type.FullName}' is marked with the 'Snapshot' attribute but the declaring type does not inherit from 'SnapshotProvider'. The 'Snapshot' attribute will be ignored");
                        }
                    }
                }
            }
        }
    }
}
