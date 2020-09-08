namespace Unity.SnapshotDebugger
{
    public struct FrameDebugProviderInfo
    {
        public int uniqueIdentifier;
        public string displayName;
        public IFrameDebugProvider provider;

        public bool IsAlive => provider != null;

        public static FrameDebugProviderInfo CreateFromProvider(IFrameDebugProvider provider)
        {
            return new FrameDebugProviderInfo()
            {
                uniqueIdentifier = provider.GetUniqueIdentifier(),
                displayName = provider.GetDisplayName(),
                provider = provider
            };
        }
    }
}
