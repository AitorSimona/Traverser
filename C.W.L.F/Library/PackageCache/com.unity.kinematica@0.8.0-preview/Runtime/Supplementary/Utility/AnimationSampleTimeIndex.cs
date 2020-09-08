namespace Unity.Kinematica
{
    internal struct AnimationSampleTimeIndex
    {
        public static AnimationSampleTimeIndex  CreateInvalid()
        {
            return new AnimationSampleTimeIndex()
            {
                clipGuid = new SerializableGuid(),
                clipName = null,
                animFrameIndex = -1
            };
        }

        public bool IsValid => clipGuid.IsSet() && clipName != null && animFrameIndex >= 0;

        internal SerializableGuid clipGuid;
        public string           clipName;
        public int              animFrameIndex;
    }
}
