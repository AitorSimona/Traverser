namespace Unity.Kinematica
{
    public struct DebugReference
    {
        public DebugIdentifier identifier;
        public int address;
        public int group;
        public bool dataOnly;

        public static DebugReference Invalid => new DebugReference() { identifier = DebugIdentifier.Invalid, address = 0, group = 0, dataOnly = false };

        public bool IsValid => identifier.IsValid;

        public static implicit operator DebugIdentifier(DebugReference debugRef) => debugRef.identifier;
    }
}
