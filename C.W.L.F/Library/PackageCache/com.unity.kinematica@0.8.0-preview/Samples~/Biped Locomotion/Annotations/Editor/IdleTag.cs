using System;
using Unity.Kinematica.Editor;

namespace BipedLocomotion
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
