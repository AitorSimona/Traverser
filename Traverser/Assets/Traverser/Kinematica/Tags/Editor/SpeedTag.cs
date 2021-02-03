using System;
using Unity.Kinematica.Editor;

[Serializable]
[Tag("Speed", "#FFFF66")]
public struct SpeedTag : Payload<Speed>
{
    public Speed.Type type;

    public Speed Build(PayloadBuilder builder)
    {
        return Speed.Create(type);
    }
}