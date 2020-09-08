using System.Collections.Generic;
using System.Linq;

namespace Unity.Kinematica.Editor
{
    internal static class BitMask
    {
        public static int MaskFlags(List<bool> boolValues)
        {
            if (boolValues.Count > 32)
            {
                return 0;
            }

            int intValue = 0;

            TagAttribute.GetVisibleTypesInInspector();

            for (int i = 0; i < boolValues.Count; i++)
            {
                if (boolValues[i])
                {
                    intValue += 1 << i;
                }
            }

            return intValue;
        }

        public static List<bool> Unmasked(int mask)
        {
            var values = new bool[32];

            for (int i = 0; i < 32; ++i)
            {
                if ((mask & 1 << i) != 0)
                {
                    values[i] = true;
                }
            }

            return values.ToList();
        }
    }
}
