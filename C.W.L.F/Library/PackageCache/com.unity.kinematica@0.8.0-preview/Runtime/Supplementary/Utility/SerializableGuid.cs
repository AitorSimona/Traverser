using System;

namespace Unity.Kinematica
{
    [Serializable]
    internal struct SerializableGuid
    {
        public int val0;
        public int val1;
        public int val2;
        public int val3;

        public static SerializableGuid CreateInvalid()
        {
            return new SerializableGuid()
            {
                val0 = 0,
                val1 = 0,
                val2 = 0,
                val3 = 0
            };
        }

        public void SetGuid(Guid guid)
        {
            byte[] guidBytes = guid.ToByteArray();
            val0 = System.BitConverter.ToInt32(guidBytes, 0);
            val1 = System.BitConverter.ToInt32(guidBytes, 4);
            val2 = System.BitConverter.ToInt32(guidBytes, 8);
            val3 = System.BitConverter.ToInt32(guidBytes, 12);
        }

        public void SetGuidStr(string guidStr)
        {
            SetGuid(new Guid(guidStr));
        }

        public Guid GetGuid()
        {
            byte[] guidBytes = new byte[16];

            byte[] buf;
            buf = System.BitConverter.GetBytes(val0);
            System.Array.Copy(buf, 0, guidBytes, 0, 4);
            buf = System.BitConverter.GetBytes(val1);
            System.Array.Copy(buf, 0, guidBytes, 4, 4);
            buf = System.BitConverter.GetBytes(val2);
            System.Array.Copy(buf, 0, guidBytes, 8, 4);
            buf = System.BitConverter.GetBytes(val3);
            System.Array.Copy(buf, 0, guidBytes, 12, 4);

            return new Guid(guidBytes);
        }

        public string GetGuidStr()
        {
            return GetGuid().ToString("N");
        }

        public bool IsSet()
        {
            return val0 != 0 || val1 != 0 || val2 != 0 || val3 != 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SerializableGuid))
            {
                return false;
            }

            return this == (SerializableGuid)obj;
        }

        public override int GetHashCode()
        {
            return val0 ^ val1 ^ val2 ^ val3;
        }

        public static bool operator==(SerializableGuid lhs, SerializableGuid rhs)
        {
            return lhs.val0 == rhs.val0 && lhs.val1 == rhs.val1 && lhs.val2 == rhs.val2 && lhs.val3 == rhs.val3;
        }

        public static bool operator!=(SerializableGuid lhs, SerializableGuid rhs)
        {
            return !(lhs == rhs);
        }
    }
}
