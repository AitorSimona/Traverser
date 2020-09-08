using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Unity.Kinematica
{
    internal static class EnumUtility
    {
        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }

            return string.Empty;
        }

        public static IEnumerable<T> GetAllValues<T>() where T : IConvertible
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static List<string> GetAllDescriptions(this Type type)
        {
            var list = new List<string>();
            Array values = Enum.GetValues(type);

            foreach (int val in values)
            {
                var memInfo = type.GetMember(type.GetEnumName(val));
                var descriptionAttribute = memInfo[0]
                    .GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .FirstOrDefault() as DescriptionAttribute;

                if (descriptionAttribute != null)
                {
                    list.Add(descriptionAttribute.Description);
                }
            }

            return list;
        }

        public static T TypeFromName<T>(string name) where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(v => v.GetDescription() == name);
        }
    }
}
