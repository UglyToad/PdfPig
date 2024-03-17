using System;
using System.Collections;
using System.Collections.Generic;

namespace UglyToad.PdfPig.Util
{
    internal sealed class Matrix3x3 : IEnumerable<double>, IEquatable<Matrix3x3>
    {
        /// <summary>
        /// The identity matrix. The result of multiplying a matrix with
        /// the identity matrix is the matrix itself.
        /// </summary>
        public static readonly Matrix3x3 Identity = new Matrix3x3(
            1, 0, 0,
            0, 1, 0,
            0, 0, 1);

        private readonly double m11;
        private readonly double m12;
        private readonly double m13;

        private readonly double m21;
        private readonly double m22;
        private readonly double m23;

        private readonly double m31;
        private readonly double m32;
        private readonly double m33;

        /// <summary>
        /// Creates a 3x3 matrix with the following layout:
        /// 
        /// | m11  m12  m13 |
        /// | m21  m22  m23 |
        /// | m31  m32  m33 |
        /// 
        /// </summary>
        public Matrix3x3(double m11, double m12, double m13, double m21, double m22, double m23, double m31, double m32, double m33)
        {
            this.m11 = m11;
            this.m12 = m12;
            this.m13 = m13;

            this.m21 = m21;
            this.m22 = m22;
            this.m23 = m23;

            this.m31 = m31;
            this.m32 = m32;
            this.m33 = m33;
        }

        public IEnumerator<double> GetEnumerator()
        {
            yield return m11;
            yield return m12;
            yield return m13;
            yield return m21;
            yield return m22;
            yield return m23;
            yield return m31;
            yield return m32;
            yield return m33;
        }

        /// <summary>
        /// Returns a new matrix that is the inverse of this matrix (i.e. multiplying a matrix with
        /// its inverse matrix yields the identity matrix).
        ///
        /// If an inverse matrix does not exist, null is returned.
        /// </summary>
        public Matrix3x3 Inverse()
        {
            var determinant = GetDeterminant();

            // No inverse matrix exists when determinant is zero
            if (determinant == 0)
            {
                throw new InvalidOperationException("May not inverse a matrix with a determinant of 0.");
            }

            var transposed = Transpose();
            var minorm11 = (transposed.m22 * transposed.m33) - (transposed.m23 * transposed.m32);
            var minorm12 = (transposed.m21 * transposed.m33) - (transposed.m23 * transposed.m31);
            var minorm13 = (transposed.m21 * transposed.m32) - (transposed.m22 * transposed.m31);

            var minorm21 = (transposed.m12 * transposed.m33) - (transposed.m13 * transposed.m32);
            var minorm22 = (transposed.m11 * transposed.m33) - (transposed.m13 * transposed.m31);
            var minorm23 = (transposed.m11 * transposed.m32) - (transposed.m12 * transposed.m31);

            var minorm31 = (transposed.m12 * transposed.m23) - (transposed.m13 * transposed.m22);
            var minorm32 = (transposed.m11 * transposed.m23) - (transposed.m13 * transposed.m21);
            var minorm33 = (transposed.m11 * transposed.m22) - (transposed.m12 * transposed.m21);

            var adjugate = new Matrix3x3(
                minorm11, -minorm12, minorm13,
                -minorm21, minorm22, -minorm23,
                minorm31, -minorm32, minorm33);

            return adjugate.Multiply(1 / determinant);
        }

        /// <summary>
        /// Returns a new matrix with each element being a mulitple of the supplied factor.
        /// </summary>
        public Matrix3x3 Multiply(double factor)
        {
            return new Matrix3x3(
                m11 * factor, m12 * factor, m13 * factor,
                m21 * factor, m22 * factor, m23 * factor,
                m31 * factor, m32 * factor, m33 * factor);
        }

        /// <summary>
        /// Multiplies this matrix with the supplied 3-element vector
        /// and returns a new 3-element vector as the result.
        /// </summary>
        public (double, double, double) Multiply((double, double, double) vector)
        {
            return (
                m11 * vector.Item1 + m12 * vector.Item2 + m13 * vector.Item3,
                m21 * vector.Item1 + m22 * vector.Item2 + m23 * vector.Item3,
                m31 * vector.Item1 + m32 * vector.Item2 + m33 * vector.Item3);
        }

        /// <summary>
        /// Returns a new matrix that is the 'dot product' of this matrix
        /// and the supplied matrix.
        /// </summary>
        public Matrix3x3 Multiply(Matrix3x3 matrix)
        {
            return new Matrix3x3(
                m11 * matrix.m11 + m12 * matrix.m21 + m13 * matrix.m31,
                m11 * matrix.m12 + m12 * matrix.m22 + m13 * matrix.m32,
                m11 * matrix.m13 + m12 * matrix.m23 + m13 * matrix.m33,
                m21 * matrix.m11 + m22 * matrix.m21 + m23 * matrix.m31,
                m21 * matrix.m12 + m22 * matrix.m22 + m23 * matrix.m32,
                m21 * matrix.m13 + m22 * matrix.m23 + m23 * matrix.m33,
                m31 * matrix.m11 + m32 * matrix.m21 + m33 * matrix.m31,
                m31 * matrix.m12 + m32 * matrix.m22 + m33 * matrix.m32,
                m31 * matrix.m13 + m32 * matrix.m23 + m33 * matrix.m33);
        }

        /// <summary>
        /// Returns a new matrix that is the transpose of this matrix
        /// (i.e. the tranpose of a matrix, is a matrix with its rows
        /// and column interchanged)
        /// </summary>
        public Matrix3x3 Transpose()
        {
            return new Matrix3x3(
                m11, m21, m31,
                m12, m22, m32,
                m13, m23, m33);
        }

        public override bool Equals(object? obj)
        {
            return obj is Matrix3x3 other && Equals(other);
        }

        public bool Equals(Matrix3x3? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                m11 == other.m11 &&
                m12 == other.m12 &&
                m13 == other.m13 &&
                m21 == other.m21 &&
                m22 == other.m22 &&
                m23 == other.m23 &&
                m31 == other.m31 &&
                m32 == other.m32 &&
                m33 == other.m33;
        }

        public override int GetHashCode()
        {
            return (m11, m12, m13, m21, m22, m23, m31, m32, m33).GetHashCode();
        }

        private double GetDeterminant()
        {
            var minorM11 = (m22 * m33) - (m23 * m32);
            var minorM12 = (m21 * m33) - (m23 * m31);
            var minorM13 = (m21 * m32) - (m22 * m31);

            return m11 * minorM11 - m12 * minorM12 + m13 * minorM13;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
