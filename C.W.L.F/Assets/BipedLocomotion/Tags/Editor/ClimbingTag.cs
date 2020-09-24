using System;
using Unity.Kinematica.Editor;

[Serializable]
[Tag("Climbing", "#d25048")]
public struct ClimbingTag : Payload<Climbing>
{
    public Climbing.Type type;

    public Climbing Build(PayloadBuilder builder)
    {
        return Climbing.Create(type);
    }

    public static ClimbingTag Create(Climbing.Type type)
    {
        return new ClimbingTag
        {
            type = type
        };
    }
}
