using Unity.Kinematica;

[Trait]
public struct Parkour
{
    // TODO: This values assume that the relevant layers are setup in these specific numbers!!!
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
        // TODO: This values assume that the relevant layers are setup in these specific numbers!!!
        return Create((Type)(layer - 2));
    }
}
