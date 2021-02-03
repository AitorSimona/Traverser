using Unity.Kinematica;

[Trait]
public struct Parkour
{
    public enum Type
    {
        Wall = 8,
        Ledge = 9,
        Table = 10,
        Platform = 11,
        DropDown = 12
    }

    public Type type;

    public bool IsType(Type type)
    {
        return this.type == type;
    }

    public static Parkour Create(Type type)
    {
        return new Parkour
        {
            type = type
        };
    }

    public static Parkour Create(int layer)
    {
        return Create((Type)layer);
    }
}
