using System.Text;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        //
        // String table related methods
        //

        internal struct String
        {
            public int offset;
            public int count;
        }

        internal int NumStrings
        {
            get { return stringTable.Length; }
        }

        /// <summary>
        /// Retrieves a string that corresponds to the index passed as argument.
        /// </summary>
        /// <param name="index">Index that identifies the string to be retrieved.</param>
        /// <returns>A string that corresponds to the contents identified by the index passed as argument.</returns>
        public string GetString(int index)
        {
            unsafe
            {
                Assert.IsTrue(index < NumStrings);
                byte* ptr = (byte*)stringBuffer.GetUnsafePtr();
                int offset = stringTable[index].offset;
                int count = stringTable[index].count;
                return Encoding.UTF8.GetString(ptr + offset, count);
            }
        }

        internal int GetStringIndex(string value)
        {
            int numStrings = NumStrings;

            for (int i = 0; i < numStrings; ++i)
            {
                if (string.Equals(value, GetString(i)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
