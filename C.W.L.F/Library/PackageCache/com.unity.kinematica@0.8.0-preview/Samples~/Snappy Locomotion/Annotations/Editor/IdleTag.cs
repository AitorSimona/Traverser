using System;
using Unity.Kinematica.Editor;

namespace SnappyLocomotion
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
