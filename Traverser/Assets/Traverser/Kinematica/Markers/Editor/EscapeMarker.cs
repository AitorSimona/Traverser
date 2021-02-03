using System;
using Unity.Kinematica.Editor;

[Marker("Escape", "Brown")]
[Serializable]
public struct EscapeMarker : Payload<Escape>
{
    public Escape Build(PayloadBuilder builder)
    {
        return Escape.Default;
    }
}
