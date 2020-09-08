using System;

namespace Unity.Kinematica.Editor
{
    [Serializable]
    [Tag("Default", "#4850d2"), DefaultTag]
    internal struct DefaultTag : Payload<Default>
    {
        public static DefaultTag CreateDefaultTag()
        {
            return new DefaultTag();
        }

        public Default Build(PayloadBuilder builder)
        {
            return Default.Trait;
        }
    }
}
