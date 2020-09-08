using System;
using System.Reflection;
using UnityEngine;

namespace Unity.Kinematica.Editor.GenericStruct
{
    internal static class GenericStructHelper
    {
        public static ScriptableObject Wrap(Type type)
        {
            var method = typeof(GenericStructHelper).GetMethod(nameof(WrapDefault), BindingFlags.Static | BindingFlags.Public);
            var genericMethod = method.MakeGenericMethod(type);
            return genericMethod.Invoke(null, null) as ScriptableObject;
        }

        public static string Serialize(this ScriptableObject scriptableObject)
        {
            Type type = GetType(scriptableObject);
            var method = typeof(GenericStructHelper).GetMethod(nameof(SerializeGeneric), BindingFlags.Static | BindingFlags.Public);
            var genericMethod = method.MakeGenericMethod(type);
            return genericMethod.Invoke(null, new object[] { scriptableObject }) as string;
        }

        public static string SerializeGeneric<T>(this ScriptableObject scriptableObject) where T : struct
        {
            IValueStore<T> valueStore = scriptableObject as IValueStore<T>;
            return JsonUtility.ToJson(valueStore.val);
        }

        public static string Deserialize(this ScriptableObject scriptableObject, string serializedData)
        {
            Type type = GetType(scriptableObject);
            var method = typeof(GenericStructHelper).GetMethod(nameof(DeserializeGeneric), BindingFlags.Static | BindingFlags.Public);
            var genericMethod = method.MakeGenericMethod(type);
            return genericMethod.Invoke(null, new object[] { scriptableObject, serializedData }) as string;
        }

        public static void DeserializeGeneric<T>(this ScriptableObject scriptableObject, string serializedData) where T : struct
        {
            IValueStore<T> valueStore = scriptableObject as IValueStore<T>;
            valueStore.val = JsonUtility.FromJson<T>(serializedData);
        }

        public static ScriptableObject WrapDefault<T>() where T : struct
        {
            Type genericType = typeof(GenericStructWrapper<>).MakeGenericType(typeof(T));
            Type dynamicType = DynamicTypeBuilder.MakeDerivedType<T>(genericType);

            var dynamicTypeInstance = ScriptableObject.CreateInstance(dynamicType);
            IValueStore<T> valueStore = dynamicTypeInstance as IValueStore<T>;
            if (valueStore == null)
            {
                return null;
            }

            valueStore.val = default(T);

            return dynamicTypeInstance;
        }

        public static Type GetType(ScriptableObject scriptableObject)
        {
            foreach (Type type in scriptableObject.GetType().GetInterfaces())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValueStore<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }
    }
}
