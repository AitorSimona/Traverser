using System;

namespace Unity.Kinematica.Editor
{
    [Marker("Default", "Brown")]
    [Serializable]
    internal struct DefaultMarker : Payload<Default>
    {
        public Default Build(PayloadBuilder builder)
        {
            return Default.Trait;
        }
    }
}
