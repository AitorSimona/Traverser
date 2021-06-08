using Unity.Kinematica;

[Trait]
public struct Speed
{
    public enum Type
    {
        Slow,
        Normal,
        Fast
    }

    public Type type;

    public bool IsType(Type type)
    {
        return this.type == type;
    }

    public static Speed Create(Type type)
    {
        return new Speed
        {
            type = type
        };
    }
}
