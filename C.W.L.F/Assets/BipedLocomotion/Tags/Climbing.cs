using Unity.Kinematica;

[Trait]
public struct Climbing
{
    public enum Type
    {
        Ledge,
        Wall
    }

    public Type type;

    public bool IsType(Type type)
    {
        return this.type == type;
    }

    public static Climbing Create(Type type)
    {
        return new Climbing
        {
            type = type
        };
    }

    public static Climbing Create(int layer)
    {
        return Create((Type)layer);
    }
}
