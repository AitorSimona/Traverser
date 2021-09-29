using UnityEditor;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Responsible to synchronize <see cref="BurstCompiler.Options"/> with the menu
    /// </summary>
    internal static class BurstEditorOptions
    {
        // Properties stored in SessionState (survive between domain reloads, but stays alive only during the life of the editor)
        private const string EnableBurstSafetyChecksText = "BurstSafetyChecks";

        // Properties stored in EditorPrefs (survive between editor restart)
        private const string EnableBurstCompilationText = "BurstCompilation";
        private const string EnableBurstTimingsText = "BurstShowTimings";
        private const string EnableBurstCompileSynchronouslyText = "BurstCompileSynchronously";
        private const string EnableBurstDebugText = "BurstDebug";

        /// <summary>
        /// <c>true</c> if the menu options are synchronized with <see cref="BurstCompiler.Options"/>
        /// </summary>
        private static bool _isSynchronized;

        public static void EnsureSynchronized()
        {
            GetGlobalOptions();
        }

        public static bool EnableBurstCompilation
        {
            get => GetGlobalOptions().EnableBurstCompilation;
            set => GetGlobalOptions().EnableBurstCompilation = value;
        }

        public static bool EnableBurstSafetyChecks
        {
            get => GetGlobalOptions().EnableBurstSafetyChecks;
            set => GetGlobalOptions().EnableBurstSafetyChecks = value;
        }

        public static bool EnableBurstCompileSynchronously
        {
            get => GetGlobalOptions().EnableBurstCompileSynchronously;
            set => GetGlobalOptions().EnableBurstCompileSynchronously = value;
        }

        public static bool EnableBurstTimings
        {
            get => GetGlobalOptions().EnableBurstTimings;
            set => GetGlobalOptions().EnableBurstTimings = value;
        }

        public static bool EnableBurstDebug
        {
            get => GetGlobalOptions().EnableBurstDebug;
            set => GetGlobalOptions().EnableBurstDebug = value;
        }

        private static BurstCompilerOptions GetGlobalOptions()
        {
            var global = BurstCompiler.Options;
            // If options are not synchronize with our global instance, setup the sync
            if (!_isSynchronized)
            {
                // setup the synchronization
                global.EnableBurstCompilation = EditorPrefs.GetBool(EnableBurstCompilationText, true);
                global.EnableBurstCompileSynchronously = EditorPrefs.GetBool(EnableBurstCompileSynchronouslyText, false);
                global.EnableBurstTimings = EditorPrefs.GetBool(EnableBurstTimingsText, false);
                global.EnableBurstDebug = EditorPrefs.GetBool(EnableBurstDebugText, false);

                // Session only properties
                global.EnableBurstSafetyChecks = SessionState.GetBool(EnableBurstSafetyChecksText, true);
                
                global.OptionsChanged += GlobalOnOptionsChanged;
                _isSynchronized = true;
            }

            return global;
        }

        private static void GlobalOnOptionsChanged()
        {
            var global = BurstCompiler.Options;
            // We are not optimizing anything here, so whenever one option is set, we reset all of them
            EditorPrefs.SetBool(EnableBurstCompilationText, global.EnableBurstCompilation);
            EditorPrefs.SetBool(EnableBurstCompileSynchronouslyText, global.EnableBurstCompileSynchronously);
            EditorPrefs.SetBool(EnableBurstTimingsText, global.EnableBurstTimings);
            EditorPrefs.SetBool(EnableBurstDebugText, global.EnableBurstDebug);

            // Session only properties
            SessionState.SetBool(EnableBurstSafetyChecksText, global.EnableBurstSafetyChecks);
        }
    }
}