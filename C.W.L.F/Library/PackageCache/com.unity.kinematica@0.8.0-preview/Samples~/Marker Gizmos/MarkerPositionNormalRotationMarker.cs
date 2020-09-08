using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Kinematica;
using Unity.Kinematica.Editor;
using UnityEngine;


[Trait]
[Serializable]
public struct MarkerPositionNormalRotation {}

[Serializable]
[Marker ("MarkerPositionNormalRotationMarker", "Red")]
public struct MarkerPositionNormalRotationMarker : Payload<MarkerPositionNormalRotation>
{
    [MoveManipulator]
    public Vector3 m_Position;
    [NormalManipulator]
    public Vector3 m_Normal;
    [RotateManipulator]
    public Quaternion m_Rotation;
    public MarkerPositionNormalRotation Build(PayloadBuilder builder)
    {
        return new MarkerPositionNormalRotation();
    }
}
