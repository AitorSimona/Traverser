using System;
using Unity.Kinematica.Editor;

namespace Navigation
{
    [Serializable]
    [Tag("Idle", "#0ab266")]
    public struct IdleTag : Payload<Idle>
    {
        public Idle Build(PayloadBuilder builder)
        {
            return Idle.Default;
        }
    }
}
