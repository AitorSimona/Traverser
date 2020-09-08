using UnityEngine.LowLevel;

using static UnityEngine.LowLevel.PlayerLoopSystem;

namespace Unity.SnapshotDebugger
{
    internal struct UpdateSystem
    {
        public static void Listen<T>(UpdateFunction updateFunction)
        {
            var updateSystems = PlayerLoop.GetCurrentPlayerLoop();
            Listen<T>(ref updateSystems, updateFunction);
            PlayerLoop.SetPlayerLoop(updateSystems);
        }

        public static void Ignore<T>(UpdateFunction updateFunction)
        {
            var updateSystems = PlayerLoop.GetCurrentPlayerLoop();
            Ignore<T>(ref updateSystems, updateFunction);
            PlayerLoop.SetPlayerLoop(updateSystems);
        }

        private static bool Listen<T>(ref PlayerLoopSystem system, UpdateFunction updateFunction)
        {
            if (system.type == typeof(T))
            {
                system.updateDelegate += updateFunction;

                return true;
            }
            else
            {
                if (system.subSystemList != null)
                {
                    for (var i = 0; i < system.subSystemList.Length; i++)
                    {
                        if (Listen<T>(ref system.subSystemList[i], updateFunction))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Ignore<T>(ref PlayerLoopSystem system, UpdateFunction updateFunction)
        {
            if (system.type == typeof(T))
            {
                system.updateDelegate -= updateFunction;

                return true;
            }
            else
            {
                if (system.subSystemList != null)
                {
                    for (var i = 0; i < system.subSystemList.Length; i++)
                    {
                        if (Ignore<T>(ref system.subSystemList[i], updateFunction))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
