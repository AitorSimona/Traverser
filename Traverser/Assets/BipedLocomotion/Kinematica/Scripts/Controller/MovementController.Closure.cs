using UnityEngine;

using Unity.Collections;
using Unity.SnapshotDebugger;
using Unity.Mathematics;
using UnityEngine.Assertions;

public partial class MovementController : SnapshotProvider
{

    public struct Closure
    {
        /// <summary>
        /// Denotes the current world space position of the controller.
        /// </summary>
        public float3 position;

        /// <summary>
        /// Denotes the current world space linear velocity of the controller.
        /// </summary>
        public float3 velocity;

        /// <summary>
        /// Denotes the current kinematic displacement.
        /// </summary>
        /// <remarks>
        /// The kinematic displacement represents the accumulated
        /// displacements per frame, see <see cref="MovementController.Move"/>.
        /// </remarks>
        /// <seealso cref="MovementController.Move"/>
        public float3 kinematicDisplacement;

        /// <summary>
        /// Denotes the current displacement required to resolve penetrations.
        /// </summary>
        public float3 penetrationDisplacement;

        /// <summary>
        /// Denotes the current displacement due to external forces.
        /// </summary>
        /// <remarks>
        /// The dynamics displacement represents the accumulated
        /// displacement due to external forces exerted on the
        /// controller, see <see cref="MovementController.AddForce"/>.
        /// </remarks>
        /// <seealso cref="MovementController.AddForce"/>
        /// <seealso cref="MovementController.AddImpulse"/>
        public float3 dynamicsDisplacement;

        /// <summary>
        /// Denotes the current displacement required to resolve collisions.
        /// </summary>
        /// <remarks>
        /// The collision displacement includes displacements due to
        /// deflections, i.e. sliding along collision shapes.
        /// </remarks>
        public float3 collisionDisplacement;

        /// <summary>
        /// Denotes whether or not the controller is grounded.
        /// </summary>
        /// <remarks>
        /// The controller is considered to be grounded if it either rests
        /// or slides along a collision surface.
        /// </remarks>
        /// <seealso cref="ground"/>
        public bool isGrounded;

        /// <summary>
        /// Denotes the transform of the object the controller is grounded to.
        /// </summary>
        /// <remarks>
        /// The controller is considered to be grounded if it either rests
        /// or slides along a collision surface. This attribute refers to
        /// the transform of the corresponding object.
        /// </remarks>
        /// <seealso cref="isGrounded"/>
        public Transform ground;

        /// <summary>
        /// Denotes whether or not the controller's movement was truncated
        /// due to a collision resolution.
        /// </summary>
        /// <seealso cref="collider"/>
        /// <seealso cref="colliderContactPoint"/>
        /// <seealso cref="colliderContactNormal"/>
        public bool isColliding;

        /// <summary>
        /// Reference to the collider that was responsible for a collision resolution.
        /// </summary>
        /// <seealso cref="isColliding"/>
        /// <seealso cref="colliderContactPoint"/>
        /// <seealso cref="colliderContactNormal"/>
        public Collider collider;

        /// <summary>
        /// Denotes the contact point at which a collision occurred.
        /// </summary>
        /// <seealso cref="isColliding"/>
        /// <seealso cref="collider"/>
        /// <seealso cref="colliderContactNormal"/>
        public float3 colliderContactPoint;

        /// <summary>
        /// Denotes the normal of the contact point at which a collision occurred.
        /// </summary>
        /// <seealso cref="isColliding"/>
        /// <seealso cref="collider"/>
        /// <seealso cref="colliderContactPoint"/>
        public float3 colliderContactNormal;

        internal static Closure Create()
        {
            return new Closure()
            {
                position = float3.zero,
                velocity = float3.zero,

                kinematicDisplacement = float3.zero,
                penetrationDisplacement = float3.zero,
                dynamicsDisplacement = float3.zero,
                collisionDisplacement = float3.zero,

                isGrounded = false,
                ground = null,

                isColliding = false,
                collider = null,
                colliderContactPoint = float3.zero,
                colliderContactNormal = float3.zero,
            };
        }

        internal void WriteToStream(Buffer buffer)
        {
            buffer.Write(position);
            buffer.Write(velocity);
            buffer.Write(kinematicDisplacement);
            buffer.Write(penetrationDisplacement);
            buffer.Write(dynamicsDisplacement);
            buffer.Write(collisionDisplacement);

            buffer.Write(isGrounded);
            buffer.WriteTransform(ground);

            buffer.Write(isColliding);
            buffer.WriteComponent(collider);

            buffer.Write(colliderContactPoint);
            buffer.Write(colliderContactNormal);
        }

        internal void ReadFromStream(Buffer buffer)
        {
            position = buffer.ReadVector3();
            velocity = buffer.ReadVector3();
            kinematicDisplacement = buffer.ReadVector3();
            penetrationDisplacement = buffer.ReadVector3();
            dynamicsDisplacement = buffer.ReadVector3();
            collisionDisplacement = buffer.ReadVector3();

            isGrounded = buffer.ReadBoolean();
            ground = buffer.ReadTransform();

            isColliding = buffer.ReadBoolean();
            collider = buffer.ReadComponent<Collider>();

            colliderContactPoint = buffer.ReadVector3();
            colliderContactNormal = buffer.ReadVector3();
        }

        internal Vector3 InverseTransformPoint(Vector3 p)
        {
            var q =
                Missing.conjugate(
                    quaternion.identity);

            return Missing.rotateVector(q, p) -
                Missing.rotateVector(q, position);
        }
    }
}
