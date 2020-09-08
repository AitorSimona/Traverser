using System.Runtime.InteropServices;

namespace Unity.SnapshotDebugger
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Common32
    {
        static Common32 conversion = new Common32();

        [FieldOffset(0)]
        public float single;

        [FieldOffset(0)]
        public int integer;

        public static float ToSingle(int value)
        {
            conversion.integer = value;
            return conversion.single;
        }

        public static int ToInteger(float value)
        {
            conversion.single = value;
            return conversion.integer;
        }
    }

    internal static class BitConverter
    {
        public static void GetBytes(byte[] buffer, short value)
        {
            unchecked
            {
                buffer[0] = (byte)((value >> 8) & 0xFF);
                buffer[1] = (byte)(value & 0xFF);
            }
        }

        public static void GetBytes(byte[] buffer, int value)
        {
            unchecked
            {
                buffer[0] = (byte)((value >> 24) & 0xFF);
                buffer[1] = (byte)((value >> 16) & 0xFF);
                buffer[2] = (byte)((value >> 8) & 0xFF);
                buffer[3] = (byte)(value & 0xFF);
            }
        }

        public static void GetBytes(byte[] buffer, float value)
        {
            unchecked
            {
                GetBytes(buffer, Common32.ToInteger(value));
            }
        }

        public static void GetBytes(byte[] buffer, bool value)
        {
            unchecked
            {
                buffer[0] = (byte)((value == true) ? 1 : 0);
            }
        }

        public static short GetShort(byte[] buffer)
        {
            unchecked
            {
                return (short)((buffer[0] << 8)
                    | buffer[1]);
            }
        }

        public static int GetInt(byte[] buffer)
        {
            unchecked
            {
                return (int)((buffer[0] << 24)
                    | (buffer[1] << 16)
                    | (buffer[2] << 8)
                    | buffer[3]);
            }
        }

        public static float GetFloat(byte[] buffer)
        {
            unchecked
            {
                return Common32.ToSingle(GetInt(buffer));
            }
        }

        public static bool GetBool(byte[] buffer)
        {
            unchecked
            {
                return buffer[0] != 0;
            }
        }
    }
}
