using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Kinematica.Editor
{
    public class MarkerAttribute : AnnotationAttribute
    {
        public const string k_UnknownMarkerType = "Unknown Marker Type";

        public MarkerAttribute(string displayName, string color) : base(displayName, color) {}

        public static bool IsMarkerType(Type type)
        {
            Assert.IsTrue(type != null);
            return HasMarkerAttribute(type) && PayloadUtilities.ImplementsPayloadInterface(type);
        }

        public static bool HasMarkerAttribute(Type type)
        {
            return type.GetCustomAttributes(typeof(MarkerAttribute), true).Length > 0;
        }

        static List<Type> k_TypesCache;

        public static IEnumerable<Type> GetMarkerTypes()
        {
            return k_TypesCache ?? (k_TypesCache = AttributeCache<MarkerAttribute>.PopulateTypes());
        }

        protected static HashSet<Type> k_TypesWithDuplicates;
        static HashSet<Type> TypesWithDuplicateClassName
        {
            get { return k_TypesWithDuplicates ?? (k_TypesWithDuplicates = GetTypesWithDuplicateDescription(GetMarkerTypes(), GetDisplayName)); }
        }

        static string GetDisplayName(Type type)
        {
            string name = GetDisplayName<MarkerAttribute>(type);
            if (name == null)
            {
                return k_UnknownMarkerType;
            }

            return name;
        }

        public static string GetDescription(Type type)
        {
            if (type == null)
            {
                return k_UnknownMarkerType;
            }

            return GetFullDisplayName(TypesWithDuplicateClassName, type, GetDisplayName(type));
        }
    }
}
