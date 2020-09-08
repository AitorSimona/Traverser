using System.Text;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public class StringTable
        {
            public int GetStringIndex(string value)
            {
                int numStrings = strings.Count;
                for (int i = 0; i < numStrings; ++i)
                {
                    if (string.Equals(strings[i], value))
                    {
                        return i;
                    }
                }

                return -1;
            }

            public int RegisterString(string value)
            {
                int stringIndex = GetStringIndex(value);
                if (stringIndex >= 0)
                {
                    return stringIndex;
                }

                strings.Add(value);
                return strings.Count - 1;
            }

            public string this[int index]
            {
                get { return strings[index]; }
            }

            public IEnumerable<string> Strings => strings;

            public int NumStrings => strings.Count;

            public static StringTable Create()
            {
                return new StringTable();
            }

            private List<string> strings = new List<string>();
        }

        public unsafe void BuildStringTable()
        {
            int numBytes = 0;

            foreach (string name in stringTable.Strings)
            {
                numBytes += name.Length;
            }

            ref Binary binary = ref Binary;

            allocator.Allocate(stringTable.NumStrings, ref binary.stringTable);
            allocator.Allocate(numBytes, ref binary.stringBuffer);

            int stringIndex = 0;
            int writeOffset = 0;

            byte* destinationBuffer = (byte*)binary.stringBuffer.GetUnsafePtr();

            foreach (string name in stringTable.Strings)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(name);

                binary.stringTable[stringIndex].offset = writeOffset;
                binary.stringTable[stringIndex].count = buffer.Length;

                fixed(byte* ptr = &buffer[0])
                {
                    UnsafeUtility.MemCpy(destinationBuffer, ptr, buffer.Length);
                    destinationBuffer += buffer.Length;
                }

                stringIndex++;
                writeOffset += buffer.Length;
            }

            Assert.IsTrue(stringIndex == stringTable.NumStrings);
        }
    }
}
