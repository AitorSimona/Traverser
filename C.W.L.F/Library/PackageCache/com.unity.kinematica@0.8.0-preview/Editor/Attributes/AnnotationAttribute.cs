using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.WSA;
using ColorUtility = Unity.SnapshotDebugger.ColorUtility;

namespace Unity.Kinematica.Editor
{
    [AttributeUsage(AttributeTargets.Struct)]
    public abstract class AnnotationAttribute : Attribute
    {
        protected string m_DisplayName;
        protected Color m_Color;

        protected AnnotationAttribute(string displayName, string color)
        {
            m_Color = ColorUtility.FromHtmlString(color);
            m_DisplayName = displayName;
        }

        public static Color GetColor(Type type)
        {
            if (type == null)
            {
                return Color.gray;
            }

            AnnotationAttribute attribute = type.GetCustomAttributes(typeof(AnnotationAttribute), true).FirstOrDefault() as AnnotationAttribute;

            return attribute.m_Color;
        }

        protected delegate string GetDisplayNameDelegate(Type t);

        protected static string GetDisplayName<T>(Type type) where T : AnnotationAttribute
        {
            if (type == null)
            {
                return null;
            }

            T[] attributes = (T[])type.GetCustomAttributes(typeof(T), false);
            if (attributes.Length == 0)
            {
                return type.Name;
            }

            return attributes[0].m_DisplayName;
        }

        protected static HashSet<Type> GetTypesWithDuplicateDescription(IEnumerable<Type> types, GetDisplayNameDelegate getDisplayName)
        {
            HashSet<Type> duplicates = new HashSet<Type>();
            IEnumerable<IGrouping<string, Type>> grouped = types.GroupBy(t => getDisplayName(t));
            foreach (IGrouping<string, Type> g in grouped)
            {
                var results = g.ToList();
                if (results.Count > 1)
                {
                    foreach (Type result in results)
                    {
                        duplicates.Add(result);
                    }
                }
            }

            return duplicates;
        }

        protected static string GetFullDisplayName(HashSet<Type> typesWithDuplicateClassName, Type type, string displayName)
        {
            if (typesWithDuplicateClassName.Contains(type))
            {
                int lastSection = type.FullName.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
                if (lastSection >= 0)
                {
                    return $"{type.FullName.Substring(0, lastSection)}.{displayName}";
                }
            }

            return displayName;
        }
    }
}
