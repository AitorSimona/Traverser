using System;
using System.Reflection;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    [AttributeUsage(AttributeTargets.Struct)]
    internal class DefaultTagAttribute : Attribute
    {
        static MethodInfo   k_CreateDefaultTagMethod;
        static Type         k_DefaultTagType;
        static bool         k_DefaultTagTypeComputed = false;

        public static object CreateDefaultTag()
        {
            if (!k_DefaultTagTypeComputed)
            {
                foreach (Type tagType in TagAttribute.GetVisibleTypesInInspector())
                {
                    if (tagType.IsDefined(typeof(DefaultTagAttribute)))
                    {
                        if (k_CreateDefaultTagMethod == null)
                        {
                            MethodInfo createTagMethod = tagType.GetMethod("CreateDefaultTag", BindingFlags.Static | BindingFlags.Public);
                            if (createTagMethod == null)
                            {
                                Debug.LogWarning($"Default tag type {tagType.FullName} is missing \"public static {tagType.FullName} CreateDefaultTag()\" function, cannot create default tag");
                                return null;
                            }
                            else if (createTagMethod.GetGenericArguments().Length > 0 || createTagMethod.ReturnType != tagType)
                            {
                                Debug.LogWarning($"Default tag type {tagType.FullName} should have \"public static {tagType.FullName} CreateDefaultTag()\" signature, cannot create default tag");
                                return null;
                            }

                            k_CreateDefaultTagMethod = createTagMethod;
                            k_DefaultTagType = tagType;
                        }
                        else
                        {
                            Debug.LogWarning($"Tag type {tagType.FullName} has default tag attribute, but {k_DefaultTagType.FullName} is already defined as the default tag");
                        }
                    }
                }

                k_DefaultTagTypeComputed = true;
            }

            if (k_CreateDefaultTagMethod == null)
            {
                return null;
            }

            return k_CreateDefaultTagMethod.Invoke(null, null);
        }
    }
}
