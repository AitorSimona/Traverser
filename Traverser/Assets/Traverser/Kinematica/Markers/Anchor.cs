using Unity.Kinematica;
using Unity.Mathematics;

[Trait]
public struct Anchor
{
    // Transform relative to the trajectory transform
    // of the frame at which the anchor has been placed
    public AffineTransform transform;
}
