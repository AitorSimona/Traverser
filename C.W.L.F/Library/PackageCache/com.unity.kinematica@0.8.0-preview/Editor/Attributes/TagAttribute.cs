using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica.Editor
{
    public class TagAttribute : AnnotationAttribute
    {
        public const string k_UnknownTagType = "Unknown Tag Type";

        public TagAttribute(string displayName, string color) : base(displayName, color) {}

        public static bool IsTagType(Type type)
        {
            Assert.IsTrue(type != null);
            return HasTagAttribute(type) && PayloadUtilities.ImplementsPayloadInterface(type);
        }

        public static bool HasTagAttribute(Type type)
        {
            return type.GetCustomAttributes(typeof(TagAttribute), true).Length > 0;
        }

        static List<Type> k_TypesCache;
        static List<Type> k_VisibleTypesCache;

        public static IEnumerable<Type> GetTypes()
        {
            return k_TypesCache ?? (k_TypesCache = AttributeCache<TagAttribute>.PopulateTypes());
        }

        public static IEnumerable<Type> GetVisibleTypesInInspector()
        {
            if (k_VisibleTypesCache == null)
            {
                k_VisibleTypesCache = new List<Type>();

                foreach (Type type in GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(HideInInspector), true).Length == 0)
                    {
                        k_VisibleTypesCache.Add(type);
                    }
                }
            }

            return k_VisibleTypesCache;
        }

        public static Type TypeFromName(string name)
        {
            foreach (Type type in GetTypes())
            {
                if (GetDescription(type) == name)
                {
                    return type;
                }
            }

            return null;
        }

        static Dictionary<string, Type> k_PayloadArgTypeToTagType;

        public static Type FindTypeByPayloadArgumentType(Type payloadArgType)
        {
            if (k_PayloadArgTypeToTagType == null)
            {
                k_PayloadArgTypeToTagType = new Dictionary<string, Type>();
                foreach (Type type in GetTypes())
                {
                    Type argType = PayloadUtilities.GenericArgumentTypeFromTagInterface(type);
                    if (k_PayloadArgTypeToTagType.ContainsKey(argType.Name))
                    {
                        // log error that two tag types have the same payload argument
                        continue;
                    }


                    k_PayloadArgTypeToTagType.Add(argType.Name, type);
                }
            }

            k_PayloadArgTypeToTagType.TryGetValue(payloadArgType.FullName, out Type tagType);
            return tagType;
        }

        protected static HashSet<Type> k_TypesWithDuplicates;
        static HashSet<Type> TypesWithDuplicateClassName
        {
            get { return k_TypesWithDuplicates ?? (k_TypesWithDuplicates = GetTypesWithDuplicateDescription(GetVisibleTypesInInspector(), GetDisplayName)); }
        }

        static string GetDisplayName(Type type)
        {
            string name = GetDisplayName<TagAttribute>(type);
            if (name == null)
            {
                return k_UnknownTagType;
            }

            return name;
        }

        public static string GetDescription(Type type)
        {
            if (type == null)
            {
                return k_UnknownTagType;
            }

            return GetFullDisplayName(TypesWithDuplicateClassName, type, GetDisplayName(type));
        }

        static List<string> k_Descriptions;

        public static List<string> GetAllDescriptions()
        {
            if (k_Descriptions == null)
            {
                k_Descriptions = new List<string>();
                foreach (Type type in GetVisibleTypesInInspector())
                {
                    k_Descriptions.Add(GetDescription(type));
                }
            }

            return k_Descriptions;
        }
    }
}
