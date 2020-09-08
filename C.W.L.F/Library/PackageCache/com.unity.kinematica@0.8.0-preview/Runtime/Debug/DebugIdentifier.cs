namespace Unity.Kinematica
{
    public struct DebugIdentifier
    {
        public ushort index;
        public ushort version;
        public int typeHashCode;

        int GlobalIndex => (version << 16) | index;

        public static DebugIdentifier Invalid => new DebugIdentifier() { index = 0, version = 0, typeHashCode = 0 };

        public bool IsValid => version > 0;

        public bool Equals(DebugIdentifier identifier)
        {
            return index == identifier.index && typeHashCode == identifier.typeHashCode;
        }

        public bool EqualsIndexAndVersion(DebugIdentifier identifier)
        {
            return GlobalIndex == identifier.GlobalIndex && typeHashCode == identifier.typeHashCode;
        }
    }
}
