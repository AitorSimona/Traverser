using System;
using Unity.Kinematica.Editor;

[Serializable]
[Tag("Parkour", "#5048d2")]
public struct ParkourTag : Payload<Parkour>
{
    public Parkour.Type type;

    public Parkour Build(PayloadBuilder builder)
    {
        return Parkour.Create(type);
    }

    public static ParkourTag Create(Parkour.Type type)
    {
        return new ParkourTag
        {
            type = type
        };
    }
}
