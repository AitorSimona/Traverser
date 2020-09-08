using System;
using Unity.Kinematica;
using Unity.Kinematica.Editor;

[Trait]
public struct RunTrait
{
    public static RunTrait Trait => new RunTrait();
}

[Serializable]
[Tag("RunTag", "#4850d2")]

internal struct RunTag : Payload<RunTrait>
{
    public static RunTag CreateDefaultTag()
    {
        return new RunTag();
    }

    public RunTrait Build(PayloadBuilder builder)
    {
        return RunTrait.Trait;
    }
}

