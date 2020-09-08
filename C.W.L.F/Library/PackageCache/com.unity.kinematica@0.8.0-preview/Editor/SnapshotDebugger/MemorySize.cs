using System;

namespace Unity.SnapshotDebugger.Editor
{
    internal static class MemorySize
    {
        public static string ToString(int memorySize)
        {
            const long oneKiloByte = 1024;
            const long oneMegaByte = oneKiloByte * oneKiloByte;

            if (memorySize > oneMegaByte)
            {
                return string.Format("{0:0.00} Mb",
                    Convert.ToDecimal(memorySize) / oneMegaByte);
            }
            else if (memorySize > oneKiloByte)
            {
                return string.Format("{0:0.00} Kb",
                    Convert.ToDecimal(memorySize) / oneKiloByte);
            }
            else
            {
                return string.Format("{0} Bytes", memorySize);
            }
        }
    }
}
