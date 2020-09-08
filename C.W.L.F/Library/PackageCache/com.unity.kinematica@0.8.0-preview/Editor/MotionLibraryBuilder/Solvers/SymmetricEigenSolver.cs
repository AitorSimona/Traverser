using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    internal struct SymmetricEigenSolver
    {
        public double3x3 evec;
        public double3 eval;

        public SymmetricEigenSolver(double a00, double a01, double a02, double a11, double a12, double a22)
        {
            evec = new double3x3();
            eval = new double3();

            //
            // Precondition the matrix by factoring out the maximum absolute value
            // of the components. This guards against floating-point overflow when
            // computing the eigenvalues.
            //

            var max0 = math.max(math.abs(a00), math.abs(a01));
            var max1 = math.max(math.abs(a02), math.abs(a11));
            var max2 = math.max(math.abs(a12), math.abs(a22));

            var maxAbsElement = math.max(math.max(max0, max1), max2);

            if (maxAbsElement == 0)
            {
                //
                // A is the zero matrix.
                //

                eval[0] = 0;
                eval[1] = 0;
                eval[2] = 0;

                evec[0] = new double3(1, 0, 0);
                evec[1] = new double3(0, 1, 0);
                evec[2] = new double3(0, 0, 1);

                return;
            }

            var invMaxAbsElement = 1 / maxAbsElement;

            a00 *= invMaxAbsElement;
            a01 *= invMaxAbsElement;
            a02 *= invMaxAbsElement;
            a11 *= invMaxAbsElement;
            a12 *= invMaxAbsElement;
            a22 *= invMaxAbsElement;

            var norm = a01 * a01 + a02 * a02 + a12 * a12;

            if (norm > 0)
            {
                //
                // Compute the eigenvalues of A. The acos(z) function requires |z| <= 1,
                // but will fail silently and return NaN if the input is larger than 1 in
                // magnitude. To avoid this condition due to rounding errors, the halfDet
                // value is clamped to [-1,1].
                //

                var traceDiv3 = (a00 + a11 + a22) / 3;

                var b00 = a00 - traceDiv3;
                var b11 = a11 - traceDiv3;
                var b22 = a22 - traceDiv3;

                var denom = math.sqrt((b00 * b00 + b11 * b11 + b22 * b22 + norm * 2) / 6);

                var c00 = b11 * b22 - a12 * a12;
                var c01 = a01 * b22 - a12 * a02;
                var c02 = a01 * a12 - b11 * a02;

                var det = (b00 * c00 - a01 * c01 + a02 * c02) / (denom * denom * denom);

                var halfDet = det * 0.5;

                halfDet = math.min(math.max(halfDet, -1), 1);

                //
                // The eigenvalues of B are ordered as beta0 <= beta1 <= beta2.  The
                // number of digits in twoThirdsPi is chosen so that, whether float or
                // double, the floating-point number is the closest to theoretical 2*pi/3.
                //

                var angle = math.acos(halfDet) / 3;
                var twoThirdsPi = 2.09439510239319549;
                var beta2 = math.cos(angle) * 2;
                var beta0 = math.cos(angle + twoThirdsPi) * 2;
                var beta1 = -(beta0 + beta2);

                //
                // The eigenvalues of A are ordered as alpha0 <= alpha1 <= alpha2.
                //

                eval[0] = traceDiv3 + denom * beta0;
                eval[1] = traceDiv3 + denom * beta1;
                eval[2] = traceDiv3 + denom * beta2;

                //
                // The index i0 corresponds to the root guaranteed to have multiplicity 1
                // and goes with either the maximum root or the minimum root. The index
                // i2 goes with the root of the opposite extreme. Root beta2 is always
                // between beta0 and beta1.
                //

                int i0, i2, i1 = 1;

                if (halfDet >= 0)
                {
                    i0 = 2;
                    i2 = 0;
                }
                else
                {
                    i0 = 0;
                    i2 = 2;
                }

                //
                // Compute the eigenvectors. The set { evec[0], evec[1], evec[2] } is
                // right handed and orthonormal.
                //

                evec[i0] = ComputeEigenvector0(a00, a01, a02, a11, a12, a22, eval[i0]);
                evec[i1] = ComputeEigenvector1(a00, a01, a02, a11, a12, a22, evec[i0], eval[i1]);
                evec[i2] = math.cross(evec[i0], evec[i1]);
            }
            else
            {
                //
                // The matrix is diagonal.
                //

                eval[0] = a00;
                eval[1] = a11;
                eval[2] = a22;

                evec[0] = new double3(1, 0, 0);
                evec[1] = new double3(0, 1, 0);
                evec[2] = new double3(0, 0, 1);
            }

            //
            // The preconditioning scaled the matrix A, which scales the eigenvalues.
            // Revert the scaling.
            //

            eval[0] *= maxAbsElement;
            eval[1] *= maxAbsElement;
            eval[2] *= maxAbsElement;
        }

        double3 ComputeEigenvector0(double a00, double a01, double a02, double a11, double a12, double a22, double eval0)
        {
            //
            // Compute a unit-length eigenvector for eigenvalue[i0]. The matrix is
            // rank 2, so two of the rows are linearly independent. For a robust
            // computation of the eigenvector, select the two rows whose cross product
            // has largest length of all pairs of rows.
            //

            var row0 = new double3(a00 - eval0, a01, a02);
            var row1 = new double3(a01, a11 - eval0, a12);
            var row2 = new double3(a02, a12, a22 - eval0);

            var r0xr1 = math.cross(row0, row1);
            var r0xr2 = math.cross(row0, row2);
            var r1xr2 = math.cross(row1, row2);

            var d0 = math.dot(r0xr1, r0xr1);
            var d1 = math.dot(r0xr2, r0xr2);
            var d2 = math.dot(r1xr2, r1xr2);

            var dmax = d0;
            int imax = 0;

            if (d1 > dmax)
            {
                dmax = d1;
                imax = 1;
            }

            if (d2 > dmax)
            {
                imax = 2;
            }

            if (imax == 0)
            {
                return r0xr1 / math.sqrt(d0);
            }
            else if (imax == 1)
            {
                return r0xr2 / math.sqrt(d1);
            }
            else
            {
                return r1xr2 / math.sqrt(d2);
            }
        }

        double3 ComputeEigenvector1(double a00, double a01, double a02, double a11, double a12, double a22, double3 evec0, double eval1)
        {
            //
            // Robustly compute a right-handed orthonormal set { U, V, evec0 }.
            //
            double3 U;
            double3 V;

            ComputeOrthogonalComplement(evec0, out U, out V);

            //
            // Let e be eval1 and let E be a corresponding eigenvector which is a
            // solution to the linear system (A - e*I)*E = 0. The matrix (A - e*I)
            // is 3x3, not invertible (so infinitely many solutions), and has rank 2
            // when eval1 and eval are different. It has rank 1 when eval1 and eval2
            // are equal. Numerically, it is difficult to compute robustly the rank
            // of a matrix. Instead, the 3x3 linear system is reduced to a 2x2 system
            // as follows. Define the 3x2 matrix J = [U V] whose columns are the U
            // and V computed previously. Define the 2x1 vector X = J*E. The 2x2
            // system is 0 = M * X = (J^T * (A - e*I) * J) * X where J^T is the
            // transpose of J and M = J^T * (A - e*I) * J is a 2x2 matrix. The system
            // may be written as
            //     +-                        -++-  -+       +-  -+
            //     | U^T*A*U - e  U^T*A*V     || x0 | = e * | x0 |
            //     | V^T*A*U      V^T*A*V - e || x1 |       | x1 |
            //     +-                        -++   -+       +-  -+
            // where X has row entries x0 and x1.
            //

            var AU = new double3
                (
                a00 * U[0] + a01 * U[1] + a02 * U[2],
                a01 * U[0] + a11 * U[1] + a12 * U[2],
                a02 * U[0] + a12 * U[1] + a22 * U[2]
                );

            var AV = new double3
                (
                a00 * V[0] + a01 * V[1] + a02 * V[2],
                a01 * V[0] + a11 * V[1] + a12 * V[2],
                a02 * V[0] + a12 * V[1] + a22 * V[2]
                );

            var m00 = U[0] * AU[0] + U[1] * AU[1] + U[2] * AU[2] - eval1;
            var m01 = U[0] * AV[0] + U[1] * AV[1] + U[2] * AV[2];
            var m11 = V[0] * AV[0] + V[1] * AV[1] + V[2] * AV[2] - eval1;

            //
            // For robustness, choose the largest-length row of M to compute the
            // eigenvector. The 2-tuple of coefficients of U and V in the
            // assignments to eigenvector[1] lies on a circle, and U and V are
            // unit length and perpendicular, so eigenvector[1] is unit length
            // (within numerical tolerance).
            //

            var absM00 = math.abs(m00);
            var absM01 = math.abs(m01);
            var absM11 = math.abs(m11);

            double maxAbsComp;
            if (absM00 >= absM11)
            {
                maxAbsComp = math.max(absM00, absM01);
                if (maxAbsComp > 0)
                {
                    if (absM00 >= absM01)
                    {
                        m01 /= m00;
                        m00 = 1 / math.sqrt(1 + m01 * m01);
                        m01 *= m00;
                    }
                    else
                    {
                        m00 /= m01;
                        m01 = 1 / math.sqrt(1 + m00 * m00);
                        m00 *= m01;
                    }
                    return (U * m01) - (V * m00);
                }
                else
                {
                    return U;
                }
            }
            else
            {
                maxAbsComp = math.max(absM11, absM01);
                if (maxAbsComp > 0)
                {
                    if (absM11 >= absM01)
                    {
                        m01 /= m11;
                        m11 = 1 / math.sqrt(1 + m01 * m01);
                        m01 *= m11;
                    }
                    else
                    {
                        m11 /= m01;
                        m01 = 1 / math.sqrt(1 + m11 * m11);
                        m11 *= m01;
                    }
                    return (U * m11) - (V * m01);
                }
                else
                {
                    return U;
                }
            }
        }

        void ComputeOrthogonalComplement(double3 W, out double3 U, out double3 V)
        {
            //
            // Robustly compute a right-handed orthonormal set { U, V, W }. The
            // vector W is guaranteed to be unit-length, in which case there is no
            // need to worry about a division by zero when computing invLength.
            //

            if (math.abs(W[0]) > math.abs(W[1]))
            {
                //
                // The component of maximum absolute value is either W[0] or W[2].
                //

                var invLength = 1 / math.sqrt(W[0] * W[0] + W[2] * W[2]);
                U = new double3(-W[2] * invLength, 0, +W[0] * invLength);
                V = math.cross(W, U);
            }
            else
            {
                //
                // The component of maximum absolute value is either W[1] or W[2].
                //

                var invLength = 1 / math.sqrt(W[1] * W[1] + W[2] * W[2]);
                U = new double3(0, +W[2] * invLength, -W[1] * invLength);
                V = math.cross(W, U);
            }
        }
    }
}
