using System;
using Unity.Mathematics;
using UnityEngine;
using Unity.Kinematica.Editor;

[Marker("Anchor", "Green")]
[Serializable]
public struct AnchorMarker : Payload<Anchor>
{
    [MoveManipulator]
    public Vector3 position;
    
    [RotateManipulator]
    public Quaternion rotation;

    public Anchor Build(PayloadBuilder builder)
    {
        return new Anchor
        {
            transform = new AffineTransform(position, rotation)
        };
    }
}
