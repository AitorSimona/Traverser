using Unity.Kinematica;
using Unity.Mathematics;

[Trait]
public struct Contact
{
    // Transform at which we make contact with the environment
    // (in the original animation) relative to the
    // root transform of that particular pose
    public AffineTransform transform;

    public static Contact Default
    {
        get
        {
            return new Contact
            {
                transform = AffineTransform.identity
            };
        }
    }
}
