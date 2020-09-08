using System;
using Unity.Kinematica.Editor;

namespace HelloWorld
{
    [Serializable]
    [Tag("Locomotion", "#4850d2")]
    public struct LocomotionTag : Payload<Locomotion>
    {
        public Locomotion Build(PayloadBuilder builder)
        {
            return Locomotion.Default;
        }
    }
}
