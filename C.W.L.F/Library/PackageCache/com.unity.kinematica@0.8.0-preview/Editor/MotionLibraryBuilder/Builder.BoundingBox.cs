using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public struct BoundingBox
        {
            public AffineTransform transform;
            public float3 extent;
            public float inverseDiagonal;

            public float3 transformPoint(float3 position)
            {
                return transform.transform(position);
            }

            public float3 inverseTransformPoint(float3 position)
            {
                return transform.inverseTransform(position);
            }

            public float3 normalize(float3 position)
            {
                return inverseTransformPoint(position) / extent;
            }

            public float3 inverseNormalize(float3 position)
            {
                return transformPoint(position * extent);
            }

            public static BoundingBox Create(NativeArray<float3> pointCloud)
            {
                //
                // Loop over points to find mean point location.
                //

                float3 mean = float3.zero;

                int numPoints = pointCloud.Length;

                for (int i = 0; i < numPoints; ++i)
                {
                    mean += pointCloud[i];
                }

                var inverseNumPoints = math.rcp(numPoints);

                mean *= inverseNumPoints;

                //
                // Loop over the points again to build the
                // normalized covariance matrix. Note that we only
                // build terms for the upper trianglular portion
                // since the matrix is symmetric.
                //

                double sumXX = 0, sumXY = 0, sumXZ = 0;
                double sumYY = 0, sumYZ = 0, sumZZ = 0;

                for (int i = 0; i < numPoints; ++i)
                {
                    var p = pointCloud[i];

                    var diff = p - mean;

                    sumXX += diff.x * diff.x;
                    sumXY += diff.x * diff.y;
                    sumXZ += diff.x * diff.z;
                    sumYY += diff.y * diff.y;
                    sumYZ += diff.y * diff.z;
                    sumZZ += diff.z * diff.z;
                }

                sumXX *= inverseNumPoints;
                sumXY *= inverseNumPoints;
                sumXZ *= inverseNumPoints;
                sumYY *= inverseNumPoints;
                sumYZ *= inverseNumPoints;
                sumZZ *= inverseNumPoints;

                //
                // Now solve for the eigenvectors of the covariance matrix.
                // We are using the non iterative algorithm described in
                // "A Robust Eigensolver for 3x3 Symmetric Matrices" by David Eberly.
                //

                var solver =
                    new SymmetricEigenSolver(
                        sumXX, sumXY, sumXZ, sumYY, sumYZ, sumZZ);

                var axisX = new float3(solver.evec[0]);
                var axisY = new float3(solver.evec[1]);
                var axisZ = new float3(-solver.evec[2]);

                //
                // Set the rotation matrix using the eigvenvectors.
                //

                var m = math.float3x3(axisX, axisY, axisZ);
                var q = math.quaternion(m);

                //
                // Now build the bounding box extents in the rotated frame.
                //

                var mt = math.transpose(m);

                var min = new float3(float.MaxValue);
                var max = new float3(float.MinValue);

                for (int i = 0; i < numPoints; i++)
                {
                    var p = pointCloud[i];

                    var p_prime = math.mul(mt, p);

                    min = math.min(min, p_prime);
                    max = math.max(max, p_prime);
                }

                //
                // Set the center of the OBB to be the average of the
                // minimum and maximum, and the extents be half of the
                // difference between the minimum and maximum.
                //

                var center = (max + min) * 0.5f;

                var position = math.mul(m, center);

                var extent = (max - min) * 0.5f;

                var numValidDimensions = 3;

                for (int i = 0; i < 3; ++i)
                {
                    if (extent[i] <= 0.001f)
                    {
                        extent[i] = 1.0f;
                        numValidDimensions--;
                    }
                }

                var inverseDiagonal =
                    math.rcp(2.0f * math.sqrt(numValidDimensions));

                var transform =
                    new AffineTransform(position, q);

                return new BoundingBox()
                {
                    transform = transform,
                    extent = extent,
                    inverseDiagonal = inverseDiagonal
                };
            }
        }
    }
}
