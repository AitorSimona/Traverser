using System;
using Unity.Kinematica.Editor;

[Serializable]
[Tag("Ledge", "#48d250")]
public struct LedgeTag : Payload<Ledge>
{
    public Ledge.Type type;

    public Ledge Build(PayloadBuilder builder)
    {
        return Ledge.Create(type);
    }

    public static LedgeTag Create(Ledge.Type type)
    {
        return new LedgeTag
        {
            type = type
        };
    }
}
