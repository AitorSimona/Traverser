using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Unity.Mathematics;
using Unity.SnapshotDebugger;

public partial class MovementController : SnapshotProvider
{
    [Snapshot]
    State state;

    [Snapshot]
    State snapshotState;

    CollisionShape collisionShape;

    //
    // Numerical error values
    //

    const float epsilon = 0.0001f;
    const float epsilonSquared = 0.00000001f;
    const float oneOverCos45 = 1.41421356237f;

    //
    // Miscellaneous constants
    //

    const float extraSpacing = 0.001f;

    void Start()
    {
        foreach (var collider in GetComponents<Collider>())
        {
            if (collisionShape != null)
            {
                throw new ArgumentException($"Movement controller only supports a single 'CapsuleCollider' or 'SphereCollider' for '{name}'.");
            }
            else if (collider as CapsuleCollider)
            {
                collisionShape =
                    new CapsuleCollisionShape(
                        collider as CapsuleCollider);
            }
            else if (collider as SphereCollider)
            {
                collisionShape =
                    new SphereCollisionShape(
                        collider as SphereCollider);
            }
        }

        if (collisionShape == null)
        {
            throw new ArgumentException($"Movement controller requires a 'CapsuleCollider' or 'SphereCollider' for '{name}'.");
        }
    }

    void UpdateVelocity(float deltaTime)
    {
        var accumulatedForce = CalculateAccumulatedForce(deltaTime);

        if (state.current.isGrounded)
        {
            var verticalAccumulatedVelocity =
                Missing.project(state.accumulatedVelocity, math.up());

            if (math.dot(math.normalize(verticalAccumulatedVelocity), math.up()) <= 0.0f)
            {
                var lateralAccumulatedVelocity =
                    state.accumulatedVelocity - verticalAccumulatedVelocity;

                state.accumulatedVelocity = lateralAccumulatedVelocity;
            }
        }

        state.accumulatedVelocity += accumulatedForce / mass;
    }

    void UpdateMovement(float deltaTime)
    {
        state.current.kinematicDisplacement =
            state.desiredVelocity * deltaTime + state.desiredDisplacement;

        state.desiredDisplacement = Vector3.zero;

        if (gravityEnabled)
        {
            float3 gravity = Physics.gravity;
            state.accumulatedVelocity += gravity * deltaTime;
            state.current.dynamicsDisplacement = state.accumulatedVelocity * deltaTime;
        }

        var verticalDisplacement = Missing.project(state.current.dynamicsDisplacement, math.up());
        var verticalDisplacementDot = math.dot(verticalDisplacement, math.up());
        if (state.previous.isGrounded && verticalDisplacementDot < 0.0f)
        {
            state.current.dynamicsDisplacement -= verticalDisplacement;
            verticalDisplacement = float3.zero;
        }

        var groundDistance = 0.0f;

        var currentPosition = Position;

        var desiredDisplacement =
            state.current.kinematicDisplacement +
                state.current.dynamicsDisplacement;

        var desiredDistance = math.length(desiredDisplacement);
        var desiredDirection = math.normalizesafe(desiredDisplacement);

        var actualMovement = float3.zero;
        var actualDistance = 0.0f;

        var stepDistance = 0.1f;

        while (true)
        {
            actualDistance = actualDistance + stepDistance;
            if (actualDistance > desiredDistance)
            {
                stepDistance = stepDistance - (actualDistance - desiredDistance);
                actualDistance = desiredDistance;
            }

            actualMovement = actualMovement + (desiredDirection * stepDistance);

            var positionForGrounding =
                Position + actualMovement +
                    state.current.penetrationDisplacement +
                        state.current.collisionDisplacement;

            groundDistance = GetGroundDistance(positionForGrounding);

            if (groundDistance < groundTolerance)
            {
                state.current.isGrounded = true;

                if ((groundSnap && groundDistance > 0.0f) || (resolveGroundPenetration && groundDistance < 0.0f))
                {
                    state.current.penetrationDisplacement += -math.up() * groundDistance;
                }
            }
            else if (groundSnap && resolveGroundPenetration)
            {
                if (verticalDisplacementDot < epsilon)
                {
                    var groundProbeLength = (groundSnapDistance > 0.0f ? groundSnapDistance : groundTolerance);

                    if (state.previous.isGrounded && groundDistance < groundProbeLength)
                    {
                        state.current.isGrounded = true;
                        state.current.penetrationDisplacement -= math.up() * groundDistance;
                    }
                }
            }

            if (collisionEnabled)
            {
                var candidatePosition = Position + actualMovement +
                    state.current.penetrationDisplacement + state.current.collisionDisplacement;

                var stepDisplacement = candidatePosition - currentPosition;
                var safeDisplacement = stepDisplacement;

                var remainingDisplacement = float3.zero;

                if (ProcessCollisions(currentPosition, ref safeDisplacement, ref remainingDisplacement))
                {
                    state.current.isColliding = true;
                }

                state.current.collisionDisplacement -= remainingDisplacement;

                if (math.lengthsq(remainingDisplacement) > epsilonSquared)
                {
                    var deflectedDisplacement = remainingDisplacement -
                        Missing.project(remainingDisplacement, state.current.colliderContactNormal);

                    if (state.current.isGrounded)
                    {
                        var verticalDeflected = Missing.project(deflectedDisplacement, math.up());
                        var verticalDeflectedDot = math.dot(verticalDeflected, math.up());

                        if (verticalDeflectedDot > 0.0f)
                        {
                            deflectedDisplacement -= verticalDeflected;
                        }
                        else if (verticalDeflectedDot < 0f)
                        {
                            if (math.length(state.current.penetrationDisplacement) < groundTolerance)
                            {
                                deflectedDisplacement -= verticalDeflected;
                            }
                        }
                    }

                    var normalizedKinematicDisplacement =
                        math.normalizesafe(state.current.kinematicDisplacement);

                    var deflectedProjection =
                        Missing.project(deflectedDisplacement,
                            normalizedKinematicDisplacement);

                    if (math.dot(math.normalizesafe(deflectedProjection), normalizedKinematicDisplacement) < 0.0f)
                    {
                        deflectedDisplacement -= deflectedProjection;
                    }

                    var projectedColliderNormal =
                        Missing.project(state.current.colliderContactNormal, math.up());

                    if (math.dot(projectedColliderNormal, math.up()) < -0.05f)
                    {
                        var verticalAccumulatedVelocity =
                            Missing.project(state.accumulatedVelocity, math.up());

                        if (math.dot(math.normalizesafe(verticalAccumulatedVelocity), math.up()) > 0f)
                        {
                            state.accumulatedVelocity = float3.zero;
                        }
                    }

                    desiredDirection = math.normalizesafe(deflectedDisplacement);

                    desiredDistance -= math.length(remainingDisplacement) - math.length(deflectedDisplacement);

                    remainingDisplacement = float3.zero;
                    ProcessCollisions(currentPosition + safeDisplacement, ref deflectedDisplacement, ref remainingDisplacement);

                    state.current.collisionDisplacement += deflectedDisplacement;
                }
            }

            currentPosition =
                Position + actualMovement +
                    state.current.penetrationDisplacement +
                        state.current.collisionDisplacement;

            if (actualDistance >= desiredDistance)
            {
                break;
            }
        }

        var desiredPosition = Position + state.current.kinematicDisplacement +
            state.current.dynamicsDisplacement + state.current.penetrationDisplacement +
                state.current.collisionDisplacement;

        state.current.collisionDisplacement += currentPosition - desiredPosition;

        groundDistance = GetGroundDistance(currentPosition);

        var finalDisplacement = currentPosition - Position;

        if (resolveGroundPenetration && groundDistance < 0f)
        {
            state.current.isGrounded = true;

            finalDisplacement -= math.up() * groundDistance;
        }

        var finalPosition = Position + finalDisplacement;

        state.current.position = finalPosition;

        state.current.velocity = (finalPosition - state.previous.position) / deltaTime;

        if (state.current.isGrounded)
        {
            var verticalAccumulatedVelocity =
                Missing.project(state.accumulatedVelocity, math.up());

            state.accumulatedVelocity -= verticalAccumulatedVelocity;
        }
    }

    bool ProcessCollisions(float3 position, ref float3 displacement, ref float3 remainingDisplacement)
    {
        var result = new CollisionShape.Contact();

        result.contactDistance = float.MaxValue;
        result.shape = null;

        var normalizedDisplacement = math.normalizesafe(displacement);

        var numContacts = collisionShape.CollisionCastAll(
            position, normalizedDisplacement,
                math.length(displacement), layerMask);

        for (int i = 0; i < numContacts; i++)
        {
            var contact = collisionShape[i];

            if (contact.shape == null)
            {
                continue;
            }

            if (math.dot(normalizedDisplacement, contact.contactNormal) > -epsilon)
            {
                continue;
            }

            if (contact.contactDistance < result.contactDistance)
            {
                result = contact;
            }
        }

        if (result.shape != null)
        {
            state.current.isColliding = true;

            state.current.collider = result.contactCollider;
            state.current.colliderContactPoint = result.contactPoint;
            state.current.colliderContactNormal = result.contactNormal;

            var adjustedDisplacement = float3.zero;

            if (result.contactDistance > extraSpacing)
            {
                adjustedDisplacement =
                    math.normalizesafe(displacement) *
                        math.min(result.contactDistance -
                            extraSpacing, math.length(displacement));
            }
            else if (result.contactDistance < extraSpacing)
            {
                adjustedDisplacement =
                    math.normalizesafe(result.contactPoint - result.contactOrigin) *
                        (result.contactDistance - extraSpacing);
            }

            remainingDisplacement += displacement - adjustedDisplacement;

            displacement = adjustedDisplacement;

            return true;
        }

        return false;
    }

    void InitializeSupport(float3 position)
    {
        var rayStartPosition = position + (math.up() * groundProbeOffset);
        var rayDirection = -math.up();
        var rayLength = groundProbeOffset + groundProbeLength;

        RaycastHit raycast;

        bool isGrounded = Raycast.ClosestHit(
            rayStartPosition, rayDirection, out raycast,
                rayLength, layerMask, transform);

        if (isGrounded)
        {
            isGrounded = (raycast.distance - groundProbeOffset < groundTolerance + epsilon);
            state.current.ground = raycast.collider.gameObject.transform;
            rayLength = raycast.distance + groundSupport;
        }

        if (!isGrounded)
        {
            float3 closestPoint = raycast.point;

            rayStartPosition = position + (math.up() * groundSupport);

            Collider[] colliders = null;

            var numContacts = Intersection.OverlapSphere(
                rayStartPosition, groundSupport * oneOverCos45,
                    out colliders, layerMask, transform);

            bool ignore = false;

            if (colliders == null || numContacts == 0)
            {
                ignore = true;
                isGrounded = false;
            }
            else if (numContacts == 1)
            {
                var result = ClosestPoint.FromPosition(
                    rayStartPosition, colliders[0]);

                if (!Missing.IsNaN(result))
                {
                    closestPoint = result;

                    var closestPointLocal =
                        state.current.InverseTransformPoint(
                            closestPoint);

                    closestPointLocal.y = 0f;

                    if (closestPointLocal.magnitude > (groundSupport * 0.25f))
                    {
                        ignore = true;
                        isGrounded = false;
                    }
                }
            }
            else
            {
                for (int i = 0; i < numContacts; i++)
                {
                    if (colliders[i] == raycast.collider)
                    {
                        continue;
                    }

                    var candidatePoint = ClosestPoint.FromPosition(
                        rayStartPosition, colliders[i]);

                    if (Missing.IsNaN(candidatePoint))
                    {
                        continue;
                    }

                    var candidatePointLocal = state.current.InverseTransformPoint(candidatePoint);

                    if (candidatePointLocal.y >= groundSupport + epsilon)
                    {
                        continue;
                    }

                    if (-candidatePointLocal.y > groundTolerance + epsilon)
                    {
                        continue;
                    }

                    candidatePointLocal.y = 0f;

                    if (math.length(candidatePointLocal) < (groundSupport * 0.25f))
                    {
                        closestPoint = candidatePoint;

                        isGrounded = true;

                        break;
                    }
                }
            }

            if (!ignore)
            {
                var closestPointToRayStart = closestPoint - rayStartPosition;

                rayDirection = math.normalizesafe(closestPointToRayStart);
                rayLength = math.length(closestPointToRayStart) + groundTolerance;

                var rayCastResult = Raycast.ClosestHit(
                    rayStartPosition, rayDirection, out raycast,
                        rayLength, layerMask, transform);

                if (rayCastResult)
                {
                    float groundDistance = raycast.distance - groundSupport;
                    isGrounded = groundDistance < (groundTolerance + epsilon) * oneOverCos45;
                    state.current.ground = raycast.collider.gameObject.transform;
                }
            }
        }

        state.current.isGrounded = isGrounded;
    }

    float GetGroundDistance(float3 position)
    {
        var rayStartPosition = position + (math.up() * groundProbeOffset);
        var rayDirection = -math.up();
        var rayLength = groundProbeOffset + groundProbeLength;

        bool isGrounded = false;

        RaycastHit raycast;

        var rayCastResult = Raycast.ClosestHit(
            rayStartPosition, rayDirection, out raycast,
                rayLength, layerMask, transform);

        var distance = float.MaxValue;

        if (rayCastResult)
        {
            var tolerance = (groundSnap ? groundSnapDistance : groundTolerance);

            isGrounded = raycast.distance - groundProbeOffset < tolerance + epsilon;
            distance = raycast.distance - groundProbeOffset;
            state.current.ground = raycast.collider.transform;
        }

        if (!isGrounded)
        {
            float3 closestPoint = raycast.point;

            rayStartPosition = position + (math.up() * groundSupport);

            Collider[] colliders = null;

            int numContacts = Intersection.OverlapSphere(
                rayStartPosition, groundSupport * oneOverCos45,
                    out colliders, layerMask, transform);

            bool ignore = false;

            if (colliders == null || numContacts == 0)
            {
                ignore = true;
                isGrounded = false;
            }
            else if (numContacts == 1)
            {
                var result = ClosestPoint.FromPosition(
                    position + math.up() * 0.05f, colliders[0]);

                if (!Missing.IsNaN(result))
                {
                    closestPoint = result;

                    var closestPointLocal =
                        state.current.InverseTransformPoint(closestPoint);

                    closestPointLocal.y = 0f;

                    if (closestPointLocal.magnitude < groundSupport)
                    {
                        ignore = false;
                        isGrounded = true;
                    }
                    else
                    {
                        ignore = true;
                        isGrounded = false;
                    }
                }
            }
            else
            {
                for (int i = 0; i < numContacts; i++)
                {
                    if (colliders[i] == raycast.collider)
                    {
                        continue;
                    }

                    var candidatePoint = ClosestPoint.FromPosition(
                        position + math.up() * 0.05f, colliders[i]);

                    if (Missing.IsNaN(candidatePoint))
                    {
                        continue;
                    }

                    var candidatePointLocal = state.current.InverseTransformPoint(candidatePoint);

                    if (candidatePointLocal.y >= groundSupport + epsilon)
                    {
                        continue;
                    }

                    candidatePointLocal.y = 0f;

                    if (math.length(candidatePointLocal) < groundSupport)
                    {
                        closestPoint = candidatePoint;

                        isGrounded = true;

                        break;
                    }
                }
            }

            if (!ignore)
            {
                var closestPointLocal = state.current.InverseTransformPoint(closestPoint);

                rayStartPosition = closestPoint + (math.up() * groundSupport);

                rayDirection = -math.up();
                rayLength = groundSupport + groundTolerance;

                rayCastResult = Raycast.ClosestHit(
                    rayStartPosition, rayDirection, out raycast,
                        rayLength, layerMask, transform);

                if (rayCastResult)
                {
                    distance = raycast.distance - groundSupport - closestPointLocal.y;
                    isGrounded = distance < (groundTolerance + epsilon) * oneOverCos45;
                    state.current.ground = raycast.collider.gameObject.transform;
                }
            }
        }

        return distance;
    }

    float3 CalculateAccumulatedForce(float deltaTime)
    {
        float3 accumulatedForce = float3.zero;

        for (int i = state.appliedForces.Length - 1; i >= 0; i--)
        {
            Force force = state.appliedForces[i];

            accumulatedForce += force.value;

            force.remainingTimeInSeconds -= deltaTime;

            if (force.remainingTimeInSeconds <= 0.0f)
            {
                state.appliedForces.RemoveAtSwapBack(i);
            }
            else
            {
                state.appliedForces[i] = force;
            }
        }

        return accumulatedForce;
    }
}
