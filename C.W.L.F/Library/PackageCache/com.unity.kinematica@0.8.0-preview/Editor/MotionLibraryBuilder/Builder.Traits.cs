using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine.Assertions;

using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public struct Trait
        {
            public Type type;
            public byte[] byteArray;

            public static Trait Create<T>(T value) where T : struct
            {
                if (!UnsafeUtility.IsUnmanaged<T>())
                {
                    throw new Exception($"{typeof(T).Name} must be an unmanaged type");
                }

                var sizeOfType = UnsafeUtility.SizeOf<T>();

                var result = new Trait
                {
                    type = typeof(T),
                    byteArray = sizeOfType > 0 ? new byte[sizeOfType] : null
                };

                unsafe
                {
                    fixed(void* destination = result.byteArray)
                    {
                        UnsafeUtility.CopyStructureToPtr(ref value, destination);
                    }
                }

                return result;
            }

            public bool Equals(Trait trait)
            {
                if (type == trait.type)
                {
                    if (byteArray.SequenceEqual(trait.byteArray))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        int RegisterTrait(Trait trait)
        {
            for (int i = 0; i < traits.Count; ++i)
            {
                if (traits[i].Equals(trait))
                {
                    return i;
                }
            }

            traits.Add(trait);

            return traits.Count - 1;
        }

        Trait BuildTrait(Payload payload, PayloadBuilder builder)
        {
            return InvokeGenericMethod(payload.Type,
                nameof(BuildTraitGeneric), payload, builder);
        }

        Trait InvokeGenericMethod(Type type, string name, object parameter, object parameter2)
        {
            var method = GetType().GetMethod(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(method != null);
            var genericMethod = method.MakeGenericMethod(type);
            return (Trait)genericMethod.Invoke(this, new object[] { parameter, parameter2 });
        }

        Trait BuildTraitGeneric<T>(Payload payload, PayloadBuilder builder) where T : struct
        {
            T value = payload.GetValue<T>();

            Type runtimeType =
                PayloadUtilities.GenericArgumentTypeFromTagInterface(
                    typeof(T));

            return InvokeGenericMethod(runtimeType,
                nameof(BuildTraitGenericPayload), value, builder);
        }

        static bool HasTraitAttribute<T>() where T : struct
        {
            return typeof(T).GetCustomAttributes(typeof(TraitAttribute), true).Length > 0;
        }

        Trait BuildTraitGenericPayload<T>(Payload<T> payload, PayloadBuilder builder) where T : struct
        {
            var trait = Trait.Create(payload.Build(builder));

            bool isValueType = trait.type.IsValueType;
            bool isBlittable = UnsafeUtility.IsBlittable(trait.type);

            if (!isValueType || !isBlittable)
            {
                throw new ArgumentException($"Trait {typeof(T).Name} generated a non-blittable instance of type {trait.type.Name}.");
            }

            if (!HasTraitAttribute<T>())
            {
                throw new ArgumentException($"Trait {trait.type.Name} must have the 'Trait' attribute.");
            }

            RegisterType(trait.type);

            return trait;
        }

        List<Trait> traits = new List<Trait>();
    }
}
