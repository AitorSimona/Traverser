using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using ManagedTraitType = Unity.Kinematica.TraitType;
using TypeIndex = Unity.Kinematica.Binary.TypeIndex;

namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer
    {
        internal struct TraitType
        {
            public int hashCode;

            public IntPtr executeFunction;

            public TypeIndex typeIndex;

            TraitType(ManagedTraitType source)
            {
                hashCode = source.hashCode;

                executeFunction = source.executeFunction;

                typeIndex = TypeIndex.Invalid;
            }

            public static TraitType Create(ManagedTraitType source)
            {
                return new TraitType(source);
            }

            public static TraitType Default
            {
                get => new TraitType();
            }
        }

        static void ReserveTraitTypes(ref ArrayMemory memory)
        {
            int numTraitTypes = ManagedTraitType.Types.Length;
            memory.Reserve<TraitType>(numTraitTypes);
        }

        static NativeSlice<TraitType> ConstructTraitTypes(ref ArrayMemory memory, ref Binary binary)
        {
            int numTraitTypes = ManagedTraitType.Types.Length;

            NativeSlice<TraitType> traitTypes = memory.CreateSlice<TraitType>(numTraitTypes);

            for (int i = 0; i < numTraitTypes; ++i)
            {
                var traitType =
                    TraitType.Create(
                        ManagedTraitType.Types[i]);

                var typeIndex =
                    binary.GetTypeIndex(
                        traitType.hashCode);

                if (typeIndex.IsValid)
                {
                    if (binary.GetType(typeIndex).numBytes != UnsafeUtility.SizeOf(ManagedTraitType.Types[i].type))
                    {
                        throw new Exception($"Failed to match trait {ManagedTraitType.Types[i].type.Name} in binary.");
                    }
                }

                traitType.typeIndex = typeIndex;

                traitTypes[i] = traitType;
            }

            return traitTypes;
        }
    }
}
