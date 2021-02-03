using System;
using Unity.Kinematica.Editor;
using Unity.Mathematics;
using UnityEngine;

[Marker("Contact", "Purple")]
[Serializable]
public struct ContactMarker : Payload<Contact>
{
    [NormalManipulator]
    public Vector3 normal;

    public string jointName;

    public Contact Build(PayloadBuilder builder)
    {
        if (!string.IsNullOrEmpty(jointName))
        {
            int jointIndex = builder.GetJointIndexForName(jointName);

            AffineTransform jointTransform = builder.GetJointTransformCharacterSpace(jointIndex);

            if (!Missing.equalEps(Missing.zero, normal, 1e-4f))
            {
                AffineTransform rootTransform = builder.GetRootTransform();

                float3 forward = Missing.zaxis(rootTransform.q);

                // Rotate trajectory forward axis such that it coincides with
                // the desired normal vector for this contact.
                quaternion q = Missing.forRotation(forward, normal);

                // Construct world space rotation, i.e. a rotation that
                // rotates the cardinal coordinate frame into the contact frame.
                quaternion qw = math.mul(q, rootTransform.q);

                // Inverse transform 'qw' w.r.t. the root transform, i.e. we
                // can reconstruct the world space contact transform by
                // concatenating the trajectory and contact transform.
                quaternion ql = math.mul(Missing.conjugate(rootTransform.q), qw);

                float3 t = jointTransform.t;

                return new Contact
                {
                    transform = new AffineTransform(t, ql)
                };
            }
        }

        return Contact.Default;
    }
}
