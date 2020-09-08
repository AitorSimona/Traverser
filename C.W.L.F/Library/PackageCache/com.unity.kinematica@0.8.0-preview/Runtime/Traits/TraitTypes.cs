using System;
using System.Reflection;
using System.Collections.Generic;

using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    internal class TraitType
    {
        public Type type;

        public int hashCode;

        public IntPtr executeFunction;

        TraitType(Type type)
        {
            Assert.IsTrue(HasTraitAttribute(type));

            if (!UnsafeUtility.IsUnmanaged(type) || !UnsafeUtility.IsBlittable(type))
            {
                throw new InvalidOperationException(
                    $"Trait type {type.FullName} must be unmanaged and blittable.");
            }

            if (ImplementsTraitInterface(type))
            {
                //
                // TODO: Find a way to call Trait.Execute directly
                //

                var executeSelf =
                    type.GetMethod(nameof(ExecuteSelf),
                        BindingFlags.Static | BindingFlags.Public);

                if (executeSelf == null)
                {
                    throw new InvalidOperationException(
                        $"Trait type {type.FullName} must implement 'ExecuteSelf'.");
                }

                executeFunction = CompileFunction(type, executeSelf);

                Assert.IsTrue(executeFunction != IntPtr.Zero);
            }
            else
            {
                executeFunction = IntPtr.Zero;
            }

            hashCode = BurstRuntime.GetHashCode32(type);

            this.type = type;
        }

        static MethodInfo compileGeneric;

        public static IntPtr CompileFunction(Type type, MethodInfo executeSelf)
        {
            if (compileGeneric == null)
            {
                compileGeneric = typeof(TraitType).GetMethod(
                    nameof(CompileFunctionGeneric),
                    BindingFlags.Static | BindingFlags.NonPublic);
            }

            if (executeSelf.GetCustomAttribute(typeof(BurstCompileAttribute)) == null)
            {
                throw new Exception($"{executeSelf.Name} function from class {type.Name} is missing [BurstCompile] attribute.");
            }

            if (System.Attribute.GetCustomAttribute(type, typeof(BurstCompileAttribute)) == null)
            {
                throw new Exception($"{type.Name} class is missing [BurstCompile] attribute.");
            }

            Assert.IsTrue(compileGeneric != null);

            var genericMethod = compileGeneric.MakeGenericMethod(type);

            try
            {
                return (IntPtr)genericMethod.Invoke(null, new object[] { executeSelf });
            }
            catch (Exception e)
            {
                throw new Exception($"Compilation of function {executeSelf.Name} from class {type.Name} failed ({e.Message})");
            }
        }

        delegate void ExecuteDelegate<T>(ref T self, ref MotionSynthesizer synthesizer);

        [BurstCompile]
        static void ExecuteSelf<T>(ref T self, ref MotionSynthesizer synthesizer) where T : Trait
        {
            //
            // TODO: Unfortunately Burst doesn't accept instance methods
            //       or generics when calling 'CompileFunctionPointer'...
            //
            // var executeThis = typeof(TraitType).GetMethod(
            //     nameof(ExecuteSelf), BindingFlags.Static | BindingFlags.NonPublic);

            // var genericExecuteThis = executeThis.MakeGenericMethod(typeof(T));
            //

            self.Execute(ref synthesizer);
        }

        unsafe static IntPtr CompileFunctionGeneric<T>(MethodInfo executeSelf) where T : struct
        {
            var executeDelegate = (ExecuteDelegate<T>)
                Delegate.CreateDelegate(
                typeof(ExecuteDelegate<T>), executeSelf);

            var functionPointer =
                BurstCompiler.CompileFunctionPointer(
                    executeDelegate);

            return *(IntPtr*)UnsafeUtility.AddressOf(ref functionPointer);
        }

        static bool ImplementsTraitInterface(Type type)
        {
            return typeof(Trait).IsAssignableFrom(type);
        }

        static bool HasTraitAttribute(Type type)
        {
            return type.GetCustomAttributes(typeof(TraitAttribute), true).Length > 0;
        }

        public static TraitType Create(Type type)
        {
            return new TraitType(type);
        }

        static TraitType[] types;

        public static TraitType GetTraitType(Type type)
        {
            foreach (var traitType in types)
            {
                if (traitType.type == type)
                {
                    return traitType;
                }
            }

            return null;
        }

        public static TraitType[] Types
        {
            get
            {
                if (types == null)
                {
                    var traitTypes = new List<TraitType>();

                    foreach (var type in GetAllTypes())
                    {
                        if (HasTraitAttribute(type))
                        {
                            traitTypes.Add(Create(type));
                        }
                    }

                    types = traitTypes.ToArray();
                }

                return types;
            }
        }

        static IEnumerable<Type> GetAllTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in SnapshotDebugger.ReflectionUtility.GetTypesFromAssembly(assembly))
                {
                    if (!type.IsAbstract)
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
