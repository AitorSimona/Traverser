namespace Unity.Kinematica
{
    internal struct DataTypeIndex
    {
        internal short value;

        public bool IsValid => value != Invalid;

        public bool Equals(DataTypeIndex index)
        {
            return value == index.value;
        }

        public static implicit operator short(DataTypeIndex index)
        {
            return index.value;
        }

        public static implicit operator DataTypeIndex(short index)
        {
            return Create(index);
        }

        internal static DataTypeIndex Create(short index)
        {
            return new DataTypeIndex
            {
                value = index
            };
        }

        public static DataTypeIndex Invalid => - 1;
    }
}
