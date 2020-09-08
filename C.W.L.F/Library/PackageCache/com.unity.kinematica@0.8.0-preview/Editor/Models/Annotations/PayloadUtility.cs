using System;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal static class PayloadUtilities
    {
        public static bool ImplementsPayloadInterface(Type type)
        {
            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericType)
                {
                    if (i.GetGenericTypeDefinition() == typeof(Payload<>))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static Type GenericArgumentTypeFromTagInterface(Type type)
        {
            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericType)
                {
                    if (i.GetGenericTypeDefinition() == typeof(Payload<>))
                    {
                        Debug.Assert(i.GetGenericArguments().Length > 0);

                        return i.GetGenericArguments()[0];
                    }
                }
            }

            return null;
        }
    }
}
