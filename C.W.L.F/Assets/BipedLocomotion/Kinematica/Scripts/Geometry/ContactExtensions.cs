using System;
using System.Collections.Generic;
using Unity;
using Unity.Collections;
using Unity.Kinematica;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

[Flags]
internal enum SegmentCollisionCheck
{
    // --- We perform bit shift to use the enum as a flag container ---
    // we shift each flag to its "position"
    InsideGeometry = 1 << 0, // binary 0001
    AboveGround = 1 << 1 // binary 0010
}

internal static class TagExtensions
{
    public static bool IsAxis(Collider collider, AffineTransform contactTransform, float3 axis)
    {
        float3 localNormal = Missing.rotateVector(
            Missing.conjugate(collider.transform.rotation),
                Missing.zaxis(contactTransform.q));

        return math.abs(math.dot(localNormal, axis)) >= 0.95f;
    }

    public static AffineTransform GetClosestTransform(float3 v0, float3 v1, float3 p)
    {
        float3 closestPoint = ClosestPoint.FromPosition(p, v0, v1);

        float3 up = Missing.up;
        float3 edge = math.normalize(v1 - v0);
        float3 n = math.cross(edge, up);

        quaternion q = math.quaternion(math.float3x3(-edge, up, -n));

        return new AffineTransform(closestPoint, q);
    }

    public static QueryResult GetPoseSequence<T, T2>(ref Binary binary, AffineTransform contactTransform, T value, T2 value2, float contactThreshold, SegmentCollisionCheck collisionCheck = SegmentCollisionCheck.InsideGeometry | SegmentCollisionCheck.AboveGround) where T : struct where T2 : struct
    {
        var queryResult = QueryResult.Create();

        NativeArray<OBB> obbs =
            GetBoundsFromContactPoints(ref binary,
                contactTransform, value, value2, contactThreshold);

        Binary.TraitIndex tagTraitIndex = binary.GetTraitIndex(value);
        Binary.TraitIndex tagTraitIndex2 = binary.GetTraitIndex(value2);

        bool doubleTag = !tagTraitIndex.Equals(tagTraitIndex2);

        int numIntervals = binary.numIntervals;

        for (int i = 0; i < numIntervals; ++i)
        {
            ref Binary.Interval interval = ref binary.GetInterval(i);
            Binary.TagList tag_list = binary.GetTagList(interval.tagListIndex);

            bool isIntervalValid = false; 

            if(doubleTag)
            {
                if (tag_list.numIndices == 2)
                {
                    isIntervalValid = binary.Contains(ref tag_list, tagTraitIndex);

                    if(isIntervalValid)
                       isIntervalValid = binary.Contains(ref tag_list, tagTraitIndex2);
                }
            }
            else
            {
                isIntervalValid = binary.Contains(ref tag_list, tagTraitIndex);
            }

            if (isIntervalValid)
            {
                ref var segment = ref binary.GetSegment(interval.segmentIndex);

                if (IsSegmentEndValidPosition(ref binary, interval.segmentIndex, contactTransform, contactThreshold, collisionCheck))
                {
                    queryResult.Add(i, interval.firstFrame, interval.numFrames);
                }

            }
        }

        obbs.Dispose();

        return queryResult;
    }

    public static NativeArray<OBB> GetBoundsFromContactPoints<T, T2>(ref Binary binary, AffineTransform contactTransform, T value, T2 value2, float contactThreshold) where T : struct where T2 : struct
    {
        Bounds bounds =
            GetBoundsForContactPoints(
                ref binary, value, value2);

        float3 extents =
            Missing.Convert(bounds.extents) +
                new float3(contactThreshold);

        float3 position = contactTransform.transform(bounds.center);

        Collider[] colliders = Physics.OverlapBox(position, extents, contactTransform.q);

        int numColliders = GetNumBoxColliders(colliders);

        NativeArray<OBB> obbs = new NativeArray<OBB>(numColliders, Allocator.Temp);

        int writeIndex = 0;

        foreach (Collider collider in colliders)
        {
            BoxCollider boxCollider = collider as BoxCollider;
            if (boxCollider != null)
            {
                OBB obb = OBBFromBoxCollider(boxCollider);
                obb.transform = contactTransform.inverseTimes(obb.transform);
                obbs[writeIndex++] = obb;
            }
        }

        Assert.IsTrue(writeIndex == numColliders);

        return obbs;
    }

    private static int GetNumBoxColliders(Collider[] colliders)
    {
        int result = 0;
        foreach (Collider collider in colliders)
        {
            BoxCollider boxCollider = collider as BoxCollider;
            if (boxCollider != null)
            {
                result++;
            }
        }
        return result;
    }

    public static Bounds GetBoundsForContactPoints<T, T2>(ref Binary binary, T value, T2 value2) where T : struct where T2 : struct
    {
        NativeArray<float3> contactPoints =
            GetContactPoints(ref binary, value, value2);

        Bounds bounds = new Bounds();

        bounds.SetMinMax(
            new float3(float.MaxValue),
                new float3(float.MinValue));

        for (int i = 0; i < contactPoints.Length; ++i)
        {
            bounds.Encapsulate(contactPoints[i]);
        }

        contactPoints.Dispose();

        return bounds;
    }

    public static Binary.MarkerIndex GetMarkerOfType(ref Binary binary, Binary.SegmentIndex segmentIndex, Binary.TypeIndex typeIndex)
    {
        ref var segment = ref binary.GetSegment(segmentIndex);

        var numMarkers = segment.numMarkers;

        for (int i = 0; i < numMarkers; ++i)
        {
            var markerIndex = segment.markerIndex + i;

            if (binary.IsType(markerIndex, typeIndex))
            {
                return markerIndex;
            }
        }

        return Binary.MarkerIndex.Invalid;
    }

    public static NativeArray<float3> GetContactPoints<T, T2>(ref Binary binary, T value, T2 value2) where T : struct where T2 : struct
    {
        int numContacts = 0;

        Binary.TraitIndex tagTraitIndex = binary.GetTraitIndex(value);
        Binary.TraitIndex tagTraitIndex2 = binary.GetTraitIndex(value2);

        var contactTypeIndex = binary.GetTypeIndex<Contact>();

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        for (int i = 0; i < binary.numTags; ++i)
        {
            ref var tag = ref binary.GetTag(i);

            if (tag.traitIndex == tagTraitIndex
                || tag.traitIndex == tagTraitIndex2)
            {
                var segmentIndex = tag.segmentIndex;

                ref var segment = ref binary.GetSegment(segmentIndex);

                var numMarkers = segment.numMarkers;

                for (int j=0; j<numMarkers; ++j)
                {
                    var markerIndex = segment.markerIndex + j;

                    if (binary.IsType(markerIndex, contactTypeIndex))
                    {
                        numContacts++;
                    }
                }
            }
        }

        int writeIndex = 0;

        NativeArray<float3> contactPoints = new NativeArray<float3>(numContacts, Allocator.Temp);

        for (int i = 0; i < binary.numTags; ++i)
        {
            ref var tag = ref binary.GetTag(i);

            if (tag.traitIndex == tagTraitIndex
                || tag.traitIndex == tagTraitIndex2)
            {
                var segmentIndex = tag.segmentIndex;

                ref var segment = ref binary.GetSegment(segmentIndex);

                var anchorIndex = GetMarkerOfType(
                    ref binary, segmentIndex, anchorTypeIndex);
                Assert.IsTrue(anchorIndex.IsValid);

                ref Binary.Marker anchorMarker =
                    ref binary.GetMarker(anchorIndex);

                AffineTransform anchorTransform =
                    binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

                var firstFrame = segment.destination.firstFrame;

                int anchorFrame = firstFrame + anchorMarker.frameIndex;

                AffineTransform referenceTransform = anchorTransform *
                    binary.GetTrajectoryTransformBetween(
                        anchorFrame, -anchorMarker.frameIndex);

                for (int j = 0; j < segment.numMarkers; ++j)
                {
                    var markerIndex = segment.markerIndex + j;

                    if (binary.IsType(markerIndex, contactTypeIndex))
                    {
                        ref Binary.Marker marker =
                            ref binary.GetMarker(markerIndex);

                        AffineTransform rootTransformAtContact = referenceTransform *
                            binary.GetTrajectoryTransformBetween(firstFrame, marker.frameIndex);

                        AffineTransform contactTransform =
                            rootTransformAtContact * binary.GetPayload<Contact>(
                                marker.traitIndex).transform;

                        contactPoints[writeIndex++] = contactTransform.t;
                    }
                }
            }
        }

        Assert.IsTrue(writeIndex == numContacts);

        return contactPoints;
    }

    public static void DebugDraw(float3 position, float scale, Color color)
    {
        float3 x = Missing.right * scale * 0.5f;
        float3 y = Missing.up * scale * 0.5f;
        float3 z = Missing.forward * scale * 0.5f;

        Debug.DrawLine(position - x, position + x, color);
        Debug.DrawLine(position - y, position + y, color);
        Debug.DrawLine(position - z, position + z, color);
    }

    public static void DebugDraw(BoxCollider collider, Color color)
    {
        Transform transform = collider.transform;

        Vector3 center = collider.center;
        Vector3 size = collider.size;

        Vector3[] vertices = new Vector3[8];

        vertices[0] = transform.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);
        vertices[1] = transform.TransformPoint(center + new Vector3(size.x, size.y, size.z) * 0.5f);
        vertices[2] = transform.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);
        vertices[3] = transform.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);

        vertices[4] = transform.TransformPoint(center + new Vector3(-size.x, -size.y, size.z) * 0.5f);
        vertices[5] = transform.TransformPoint(center + new Vector3(size.x, -size.y, size.z) * 0.5f);
        vertices[6] = transform.TransformPoint(center + new Vector3(size.x, -size.y, -size.z) * 0.5f);
        vertices[7] = transform.TransformPoint(center + new Vector3(-size.x, -size.y, -size.z) * 0.5f);

        Debug.DrawLine(vertices[0], vertices[1], color);
        Debug.DrawLine(vertices[1], vertices[2], color);
        Debug.DrawLine(vertices[2], vertices[3], color);
        Debug.DrawLine(vertices[3], vertices[0], color);

        Debug.DrawLine(vertices[4], vertices[5], color);
        Debug.DrawLine(vertices[5], vertices[6], color);
        Debug.DrawLine(vertices[6], vertices[7], color);
        Debug.DrawLine(vertices[7], vertices[4], color);

        Debug.DrawLine(vertices[0], vertices[4], color);
        Debug.DrawLine(vertices[1], vertices[5], color);
        Debug.DrawLine(vertices[2], vertices[6], color);
        Debug.DrawLine(vertices[3], vertices[7], color);
    }

    public struct OBB
    {
        public AffineTransform transform;
        public float3 size;

        public float3 transformPoint(float3 position)
        {
            return transform.transform(position);
        }

        public bool Contains(float3 position, float radius)
        {
            float3 p = transform.inverseTransform(position);
            float3 halfSize = (size * 0.5f) + new float3(radius);

            if (p.x < -halfSize.x) return false;
            if (p.x > halfSize.x) return false;
            if (p.y < -halfSize.y) return false;
            if (p.y > halfSize.y) return false;
            if (p.z < -halfSize.z) return false;
            if (p.z > halfSize.z) return false;

            return true;
        }
    }

    public static unsafe void DebugDraw(OBB obb, Color color)
    {
        float3* vertices = stackalloc float3[8];

        float3 extents = obb.size * 0.5f;

        vertices[0] = obb.transformPoint(new float3(-extents.x, extents.y, extents.z));
        vertices[1] = obb.transformPoint(new float3(extents.x, extents.y, extents.z));
        vertices[2] = obb.transformPoint(new float3(extents.x, extents.y, -extents.z));
        vertices[3] = obb.transformPoint(new float3(-extents.x, extents.y, -extents.z));

        vertices[4] = obb.transformPoint(new float3(-extents.x, -extents.y, extents.z));
        vertices[5] = obb.transformPoint(new float3(extents.x, -extents.y, extents.z));
        vertices[6] = obb.transformPoint(new float3(extents.x, -extents.y, -extents.z));
        vertices[7] = obb.transformPoint(new float3(-extents.x, -extents.y, -extents.z));

        Debug.DrawLine(vertices[0], vertices[1], color);
        Debug.DrawLine(vertices[1], vertices[2], color);
        Debug.DrawLine(vertices[2], vertices[3], color);
        Debug.DrawLine(vertices[3], vertices[0], color);

        Debug.DrawLine(vertices[4], vertices[5], color);
        Debug.DrawLine(vertices[5], vertices[6], color);
        Debug.DrawLine(vertices[6], vertices[7], color);
        Debug.DrawLine(vertices[7], vertices[4], color);

        Debug.DrawLine(vertices[0], vertices[4], color);
        Debug.DrawLine(vertices[1], vertices[5], color);
        Debug.DrawLine(vertices[2], vertices[6], color);
        Debug.DrawLine(vertices[3], vertices[7], color);
    }

    public static OBB OBBFromBoxCollider(BoxCollider collider)
    {
        AffineTransform baseTransform = Missing.Convert(collider.transform);

        float3 transformScale = collider.transform.lossyScale;
        float3 colliderScale = collider.size;

        float3 center = baseTransform.transform(
            collider.center * transformScale);

        return new OBB
        {
            transform = new AffineTransform(center, baseTransform.q),
            size = transformScale * colliderScale
        };
    }

    public static bool SphereObbsInterset(float3 position, float radius, NativeArray<OBB> obbs)
    {
        for (int i = 0; i < obbs.Length; ++i)
        {
            if (obbs[i].Contains(position, radius))
            {
                return true;
            }
        }

        return false;
    }

    public static bool AllContactsValid(ref Binary binary, ref Binary.Segment segment, NativeArray<OBB> obbs, float contactThreshold)
    {
    //    ref Binary.Marker anchorMarker =
    //        ref binary.GetMarker(tag.markerIndex);
    //    Assert.IsTrue(anchorMarker.typeIndex == Anchor.typeIndex);

    //    int anchorFrame = tag.firstFrame + anchorMarker.frameIndex;

    //    AffineTransform anchorTransform =
    //        binary.GetPayload<Anchor>(anchorMarker.payload).transform;

    //    AffineTransform referenceTransform = anchorTransform *
    //        binary.GetTrajectoryTransformBetween(
    //            anchorFrame, -anchorMarker.frameIndex);

    //    for (int i = 1; i < tag.numMarkers; ++i)
    //    {
    //        ref Binary.Marker contactMarker =
    //            ref binary.GetMarker(tag.markerIndex + i);

    //        if (contactMarker.typeIndex == Contact.typeIndex)
    //        {
    //            AffineTransform rootTransformAtContact = referenceTransform *
    //                binary.GetTrajectoryTransformBetween(
    //                    tag.firstFrame, contactMarker.frameIndex);

    //            AffineTransform contactTransform =
    //                binary.GetPayload<Contact>(contactMarker.payload).transform;

    //            AffineTransform contactWorldSpaceTransform =
    //                rootTransformAtContact * contactTransform;

    //            if (!SphereObbsInterset(contactWorldSpaceTransform.t, contactThreshold, obbs))
    //            {
    //                return false;
    //            }
    //        }
    //    }

        return true;
    }

    
    //private static int ms_EnvironmentCollisionMask = -1;

    //// TODO: Remove from here
    //public static int EnvironmentCollisionMask
    //{
    //    get
    //    {
    //        if (ms_EnvironmentCollisionMask < 0)
    //        {
    //            string[] layerNames = new string[] { "Default", "Wall", "Ledge", "Platform", "Table" };
    //            ms_EnvironmentCollisionMask = LayerMask.GetMask(layerNames);
    //        }

    //        return ms_EnvironmentCollisionMask;
    //    }
    //}

    public static bool IsSegmentEndValidPosition(ref Binary binary, Binary.SegmentIndex segmentIndex, AffineTransform contactTransform, float contactThreshold, SegmentCollisionCheck collisionCheck)
    {
        ref var segment = ref binary.GetSegment(segmentIndex);

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        var escapeTypeIndex = binary.GetTypeIndex<Escape>();

        var anchorIndex = GetMarkerOfType(
            ref binary, segmentIndex, anchorTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref var anchorMarker = ref binary.GetMarker(anchorIndex);

        var escapeIndex = GetMarkerOfType(
            ref binary, segmentIndex, escapeTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref var escapeMarker = ref binary.GetMarker(escapeIndex);

        var firstFrame = segment.destination.firstFrame;

        int anchorFrame = firstFrame + anchorMarker.frameIndex;

        AffineTransform anchorTransform =
            binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

        AffineTransform anchorWorldSpaceTransform =
            contactTransform * anchorTransform;

        AffineTransform worldRootTransform = anchorWorldSpaceTransform *
            binary.GetTrajectoryTransformBetween(
                anchorFrame, -anchorMarker.frameIndex) *
            binary.GetTrajectoryTransformBetween(
                firstFrame, escapeMarker.frameIndex);

        float collisionRadius = 0.1f;
        bool bValidPosition = true;

        // check character isn't inside geometry
        if ((collisionCheck & SegmentCollisionCheck.InsideGeometry) > 0)
        {
            bValidPosition = !Physics.CheckSphere(worldRootTransform.t + new float3(0.0f, 2.0f * collisionRadius, 0.0f), collisionRadius, CWLF.CollisionLayer.EnvironmentCollisionMask);
        }

        // check character is on the ground
        if ((collisionCheck & SegmentCollisionCheck.AboveGround) > 0)
        {
            bValidPosition = bValidPosition && Physics.CheckSphere(worldRootTransform.t, collisionRadius, CWLF.CollisionLayer.EnvironmentCollisionMask);
        }

        return bValidPosition;
    }

    public static void DebugDrawContacts(ref Binary binary, ref Binary.Tag tag, AffineTransform trajectoryContactTransform, NativeArray<OBB> obbs, float contactThreshold)
    {
        var segmentIndex = tag.segmentIndex;

        ref var segment = ref binary.GetSegment(segmentIndex);

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        var contactTypeIndex = binary.GetTypeIndex<Contact>();

        var anchorIndex = GetMarkerOfType(
            ref binary, segmentIndex, anchorTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref Binary.Marker anchorMarker =
            ref binary.GetMarker(anchorIndex);

        var firstFrame = segment.destination.firstFrame;

        int anchorFrame = firstFrame + anchorMarker.frameIndex;

        AffineTransform anchorTransform =
            binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

        AffineTransform referenceTransform = anchorTransform *
            binary.GetTrajectoryTransformBetween(
                anchorFrame, -anchorMarker.frameIndex);

        for (int i = 0; i < segment.numMarkers; ++i)
        {
            var markerIndex = segment.markerIndex + i;

            if (binary.IsType(markerIndex, contactTypeIndex))
            {
                ref Binary.Marker contactMarker =
                    ref binary.GetMarker(markerIndex);

                AffineTransform rootTransformAtContact = referenceTransform *
                    binary.GetTrajectoryTransformBetween(
                        firstFrame, contactMarker.frameIndex);

                AffineTransform contactTransform =
                    binary.GetPayload<Contact>(
                        contactMarker.traitIndex).transform;

                AffineTransform contactWorldSpaceTransform =
                    rootTransformAtContact * contactTransform;

                bool contactPointInContact =
                    SphereObbsInterset(contactWorldSpaceTransform.t, contactThreshold, obbs);

                Color color = contactPointInContact ? Color.green : Color.red;

                AffineTransform worldSpaceContactTransform =
                    trajectoryContactTransform * contactWorldSpaceTransform;

                DebugDraw(worldSpaceContactTransform.t, 0.25f, color);
            }
        }
    }

    public static void DebugDrawPoseAndTrajectory(ref Binary binary, ref Binary.Tag tag, AffineTransform contactTransform, int poseIndex)
    {
        var segmentIndex = tag.segmentIndex;

        ref var segment = ref binary.GetSegment(segmentIndex);

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        var anchorIndex = GetMarkerOfType(
            ref binary, segmentIndex, anchorTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref Binary.Marker anchorMarker =
            ref binary.GetMarker(anchorIndex);

        var firstFrame = segment.destination.firstFrame;

        int anchorFrame = firstFrame + anchorMarker.frameIndex;

        AffineTransform anchorTransform =
            binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

        AffineTransform anchorWorldSpaceTransform =
            contactTransform * anchorTransform;

        AffineTransform referenceTransform = anchorWorldSpaceTransform *
            binary.GetTrajectoryTransformBetween(
                anchorFrame, -anchorMarker.frameIndex);

        binary.DebugDrawTrajectory(referenceTransform,
            firstFrame, segment.destination.numFrames, Color.yellow);

        referenceTransform *=
            binary.GetTrajectoryTransformBetween(
                firstFrame, poseIndex);

        binary.DebugDrawPoseWorldSpace(referenceTransform,
            firstFrame + poseIndex, Color.magenta);
        
        Binary.DebugDrawTransform(referenceTransform, 0.2f);
    }

    public static void DebugDrawTrajectory(ref Binary binary, ref Binary.Tag tag, AffineTransform contactTransform)
    {
        var segmentIndex = tag.segmentIndex;

        ref var segment = ref binary.GetSegment(segmentIndex);

        var anchorTypeIndex = binary.GetTypeIndex<Anchor>();

        var anchorIndex = GetMarkerOfType(
            ref binary, segmentIndex, anchorTypeIndex);
        Assert.IsTrue(anchorIndex.IsValid);

        ref Binary.Marker anchorMarker =
            ref binary.GetMarker(anchorIndex);

        var firstFrame = segment.destination.firstFrame;

        int anchorFrame = firstFrame + anchorMarker.frameIndex;

        AffineTransform anchorTransform =
            binary.GetPayload<Anchor>(anchorMarker.traitIndex).transform;

        AffineTransform anchorWorldSpaceTransform =
            contactTransform * anchorTransform;

        AffineTransform referenceTransform = anchorWorldSpaceTransform *
            binary.GetTrajectoryTransformBetween(
                anchorFrame, -anchorMarker.frameIndex);

        binary.DebugDrawTrajectory(referenceTransform,
            firstFrame, tag.numFrames, Color.yellow);
    }
}
