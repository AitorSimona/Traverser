using Unity.Kinematica;

[Trait]
public struct Ledge
{
    public enum Type
    {
        Mount,
        Dismount,
        PullUp,
        DropDown
    }

    public Type type;

    public bool IsType(Type type)
    {
        return this.type == type;
    }

    public static Ledge Create(Type type)
    {
        return new Ledge
        {
            type = type
        };
    }

    public static Ledge Create(int type)
    {
        return Create((Type)type);
    }
}
