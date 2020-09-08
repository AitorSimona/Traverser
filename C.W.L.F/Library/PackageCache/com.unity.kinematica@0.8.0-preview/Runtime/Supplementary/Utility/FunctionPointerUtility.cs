using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica
{
    internal unsafe struct FunctionPointerUtility
    {
        public static FunctionPointer<TDelegate> CompileStaticMemberFunction<TDelegate>(Type type, string methodName) where TDelegate : class
        {
            if (type.GetCustomAttribute<BurstCompileAttribute>() == null)
            {
                throw new ArgumentException($"Compilation of function {methodName} from {type.Name} failed : class is missing [BurstCompile] attribute.");
            }

            MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                throw new ArgumentException($"Compilation of function {methodName} from {type.Name} failed : method not found.");
            }

            if (method.GetCustomAttribute<BurstCompileAttribute>() == null)
            {
                throw new ArgumentException($"Compilation of function {methodName} from {type.Name} failed : method is missing [BurstCompile] attribute.");
            }

            TDelegate functionDelegate = (TDelegate)(object)method.CreateDelegate(typeof(TDelegate));

            try
            {
                FunctionPointer<TDelegate> functionPointer = BurstCompiler.CompileFunctionPointer<TDelegate>(functionDelegate);
                return functionPointer;
            }
            catch (Exception e)
            {
                throw new Exception($"Compilation of function {methodName} from {type.Name} failed : {e}");
            }
        }

        public static bool IsFunctionPointerValid<TDelegate>(ref FunctionPointer<TDelegate> functionPointer) where TDelegate : class
        {
            return *(IntPtr*)UnsafeUtility.AddressOf(ref functionPointer) != IntPtr.Zero;
        }
    }
}
