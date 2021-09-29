// For some reasons Unity.Burst.LowLevel is not part of UnityEngine in 2018.2 but only in UnityEditor
// In 2018.3 It should be fine

#if !UNITY_DOTSPLAYER && !NET_DOTS && ((UNITY_2018_2_OR_NEWER && UNITY_EDITOR) || UNITY_2018_3_OR_NEWER)
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Unity.Burst
{
    /// <summary>
    /// The burst compiler runtime frontend.
    /// </summary>
    public static class BurstCompiler
    {
        private static readonly object GlobalLock = new object();
        private static BurstCompilerOptions _global = null;

        /// <summary>
        /// Gets the global options for the burst compiler.
        /// </summary>
        public static BurstCompilerOptions Options
        {
            get
            {
                // We only create it when it is used, not when this class is initialized
                lock (GlobalLock)
                {
                    // Make sure to late initialize the settings
                    return _global ?? (_global = new BurstCompilerOptions(true));
                }
            }
        }

#if UNITY_2019_3_OR_NEWER
        /// <summary>
        /// Sets the execution mode for all jobs spawned from now on.
        /// </summary>
        /// <param name="mode">Specifiy the required execution mode</param>
        public static void SetExecutionMode(BurstExecutionEnvironment mode)
        {
            Burst.LowLevel.BurstCompilerService.SetCurrentExecutionMode((uint)mode);
        }
        /// <summary>
        /// Retrieve the current execution mode that is configured.
        /// </summary>
        /// <returns>Currently configured execution mode</returns>
        public static BurstExecutionEnvironment GetExecutionMode()
        {
            return (BurstExecutionEnvironment)Burst.LowLevel.BurstCompilerService.GetCurrentExecutionMode();
        }
#endif

        /// <summary>
        /// Compile the following delegate with burst and return a new delegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegateMethod"></param>
        /// <returns></returns>
        /// <remarks>NOT AVAILABLE, unsafe to use</remarks>
        internal static unsafe T CompileDelegate<T>(T delegateMethod) where T : class
        {
            // We have added support for runtime CompileDelegate in 2018.2+
            void* function = Compile(delegateMethod, false);
            object res = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer((IntPtr)function, delegateMethod.GetType());
            return (T)res;
        }

        /// <summary>
        /// Compile the following delegate into a function pointer with burst, invokable from a Burst Job or from regular C#.
        /// </summary>
        /// <typeparam name="T">Type of the delegate of the function pointer</typeparam>
        /// <param name="delegateMethod">The delegate to compile</param>
        /// <returns>A function pointer invokable from a Burst Job or from regular C#</returns>
        public static unsafe FunctionPointer<T> CompileFunctionPointer<T>(T delegateMethod) where T : class
        {
            // We have added support for runtime CompileDelegate in 2018.2+
            void* function = Compile(delegateMethod, true);
            return new FunctionPointer<T>(new IntPtr(function));
        }

        private static unsafe void* Compile<T>(T delegateObj, bool isFunctionPointer) where T : class
        {
            if (delegateObj == null) throw new ArgumentNullException(nameof(delegateObj));
            if (!(delegateObj is Delegate)) throw new ArgumentException("object instance must be a System.Delegate", nameof(delegateObj));

            var delegateMethod = (Delegate)(object)delegateObj;
            if (!delegateMethod.Method.IsStatic)
            {
                throw new InvalidOperationException($"The method `{delegateMethod.Method}` must be static. Instance methods are not supported");
            }
            if (delegateMethod.Method.IsGenericMethod)
            {
                throw new InvalidOperationException($"The method `{delegateMethod.Method}` must be a non-generic method");
            }

#if ENABLE_IL2CPP
            if (isFunctionPointer &&
                delegateMethod.Method.GetCustomAttributes().All(s => s.GetType().Name != "MonoPInvokeCallbackAttribute"))
            {
                UnityEngine.Debug.Log($"The method `{delegateMethod.Method}` must have `MonoPInvokeCallback` attribute to be compatible with IL2CPP!");
            }
#endif

            void* function;

#if BURST_INTERNAL
            // Internally in Burst tests, we callback the C# method instead
            function = (void*)Marshal.GetFunctionPointerForDelegate(delegateMethod);
#else

#if UNITY_EDITOR
            string defaultOptions;

            // In case Burst is disabled entirely from the command line
            if (BurstCompilerOptions.ForceDisableBurstCompilation)
            {
                GCHandle.Alloc(delegateMethod);
                function = (void*)Marshal.GetFunctionPointerForDelegate(delegateMethod);
                return function;
            }

            if (isFunctionPointer)
            {
                defaultOptions = "--" + BurstCompilerOptions.OptionJitIsForFunctionPointer + "\n";
                // Make sure that the delegate will never be collected
                GCHandle.Alloc(delegateMethod);
                var managedFunctionPointer = Marshal.GetFunctionPointerForDelegate(delegateMethod);
                defaultOptions += "--" + BurstCompilerOptions.OptionJitManagedFunctionPointer + "0x" + managedFunctionPointer.ToInt64().ToString("X16");
            }
            else
            {
                defaultOptions = "--" + BurstCompilerOptions.OptionJitEnableSynchronousCompilation;
            }

            string extraOptions;
            // The attribute is directly on the method, so we recover the underlying method here
            if (Options.TryGetOptions(delegateMethod.Method, true, out extraOptions))
            {
                if (!string.IsNullOrWhiteSpace(extraOptions))
                {
                    defaultOptions += "\n" + extraOptions;
                }

                var delegateMethodId = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(delegateObj, defaultOptions);
                function = Unity.Burst.LowLevel.BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodId);
            }
#else
            // The attribute is directly on the method, so we recover the underlying method here
            if (BurstCompilerOptions.HasBurstCompileAttribute(delegateMethod.Method))
            {
                if (Options.EnableBurstCompilation && BurstCompilerHelper.IsBurstGenerated)
                {
                    var delegateMethodId = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(delegateObj, string.Empty);
                    function = Unity.Burst.LowLevel.BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodId);
                }
                else
                {
                    // Make sure that the delegate will never be collected
                    GCHandle.Alloc(delegateMethod);
                    // If we are in a standalone player, and burst is disabled and we are actually
                    // trying to load a function pointer, in that case we need to support it
                    // so we are then going to use the managed function directly
                    // NOTE: When running under IL2CPP, this could lead to a `System.NotSupportedException : To marshal a managed method, please add an attribute named 'MonoPInvokeCallback' to the method definition.`
                    // so in that case, the method needs to have `MonoPInvokeCallback`
                    // but that's a requirement for IL2CPP, not an issue with burst
                    function = (void*)Marshal.GetFunctionPointerForDelegate(delegateMethod);
                }
            }
#endif
            else
            {
                throw new InvalidOperationException($"Burst cannot compile the function pointer `{delegateMethod.Method}` because the `[BurstCompile]` attribute is missing");
            }
#endif
            // Should not happen but in that case, we are still trying to generated an error
            // It can be null if we are trying to compile a function in a standalone player
            // and the function was not compiled. In that case, we need to output an error
            if (function == null)
            {
                throw new InvalidOperationException($"Burst failed to compile the function pointer `{delegateMethod.Method}`");
            }

            // When burst compilation is disabled, we are still returning a valid stub function pointer (the a pointer to the managed function)
            // so that CompileFunctionPointer actually returns a delegate in all cases
            return function;
        }

        /// <summary>
        /// Lets the compiler service know we are shutting down, called by the event EditorApplication.quitting
        /// </summary>
        internal static void Shutdown()
        {
#if UNITY_EDITOR
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandShutdown);
#endif
        }

#if UNITY_EDITOR
        internal static void DomainReload()
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandDomainReload);
        }

        internal static string VersionNotify(string version)
        {
            return SendCommandToCompiler(BurstCompilerOptions.CompilerCommandVersionNotification, version);
        }

        internal static void UpdateAssemblerFolders(List<string> folders)
        {
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandUpdateAssemblyFolders, $"{string.Join(";", folders)}");
        }
#endif

        /// <summary>
        /// Cancel any compilation being processed by the JIT Compiler in the background.
        /// </summary>
        internal static void Cancel()
        {
#if UNITY_EDITOR
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandCancel);
#endif
        }

        internal static void Enable()
        {
#if UNITY_EDITOR
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandEnableCompiler);
#endif
        }

        internal static void Disable()
        {
#if UNITY_EDITOR
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandDisableCompiler);
#endif
        }

        internal static void TriggerRecompilation()
        {
#if UNITY_EDITOR
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandTriggerRecompilation, Options.GetOptions(true));
#endif
        }

        internal static void Reset()
        {
#if UNITY_EDITOR
            SendCommandToCompiler(BurstCompilerOptions.CompilerCommandReset);
#endif
        }

#if UNITY_EDITOR
        private static string SendCommandToCompiler(string commandName, string commandArgs = null)
        {
            if (commandName == null) throw new ArgumentNullException(nameof(commandName));

            var compilerOptions = commandName;
            if (commandArgs != null)
            {
                compilerOptions += " " + commandArgs;
            }

            var results = Unity.Burst.LowLevel.BurstCompilerService.GetDisassembly(DummyMethodInfo, compilerOptions);
            if (!string.IsNullOrEmpty(results))
                return results.TrimStart('\n');
            return "";
        }

        private static readonly MethodInfo DummyMethodInfo = typeof(BurstCompiler).GetMethod(nameof(DummyMethod), BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Dummy empty method for being able to send a command to the compiler
        /// </summary>
        private static void DummyMethod() { }
#else
        /// <summary>
        /// Internal class to detect at standalone player time if AOT settings were enabling burst.
        /// </summary>
        [BurstCompile]
        internal static class BurstCompilerHelper
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate bool IsBurstEnabledDelegate();
            private static readonly IsBurstEnabledDelegate IsBurstEnabledImpl = new IsBurstEnabledDelegate(IsBurstEnabled);

            [BurstCompile]
            [AOT.MonoPInvokeCallback(typeof(IsBurstEnabledDelegate))]
            private static bool IsBurstEnabled()
            {
                bool result = true;
                DiscardedMethod(ref result);
                return result;
            }

            [BurstDiscard]
            private static void DiscardedMethod(ref bool value)
            {
                value = false;
            }

            private static unsafe bool IsCompiledByBurst(Delegate del)
            {
                var delegateMethodId = Unity.Burst.LowLevel.BurstCompilerService.CompileAsyncDelegateMethod(del, string.Empty);
                // We don't try to run the method, having a pointer is already enough to tell us that burst was active for AOT settings
                return Unity.Burst.LowLevel.BurstCompilerService.GetAsyncCompiledAsyncDelegateMethod(delegateMethodId) != (void*)0;
            }

            /// <summary>
            /// Gets a boolean indicating whether burst was enabled for standalone player, used only at runtime.
            /// </summary>
            public static readonly bool IsBurstGenerated = IsCompiledByBurst(IsBurstEnabledImpl);
        }
#endif

    }
}
#endif
