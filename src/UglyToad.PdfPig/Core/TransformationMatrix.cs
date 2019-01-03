namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using Geometry;

    /// <summary>
    /// Specifies the conversion from the transformed coordinate space to the original untransformed coordinate space.
    /// </summary>
    public struct TransformationMatrix
    {
        /// <summary>
        /// The default <see cref="TransformationMatrix"/>.
        /// </summary>
        public static TransformationMatrix Identity = new TransformationMatrix(new decimal[]
        {
            1,0,0,
            0,1,0,
            0,0,1
        });

        private readonly decimal[] value;

        /// <summary>
        /// The scale for the X dimension.
        /// </summary>
        public decimal A => value[0];
        /// <summary>
        /// The value at (0, 1).
        /// </summary>
        public decimal B => value[1];
        /// <summary>
        /// The value at (1, 0).
        /// </summary>
        public decimal C => value[3];
        /// <summary>
        /// The scale for the Y dimension.
        /// </summary>
        public decimal D => value[4];
        /// <summary>
        /// The value at (2, 0) - translation in X.
        /// </summary>
        public decimal E => value[6];
        /// <summary>
        /// The value at (2, 1) - translation in Y.
        /// </summary>
        public decimal F => value[7];

        /// <summary>
        /// Get the value at the specific row and column.
        /// </summary>
        public decimal this[int row, int col]
        {
            get
            {
                if (row >= Rows)
                {
                    throw new ArgumentOutOfRangeException(nameof(row), $"The transformation matrix only contains {Rows} rows and is zero indexed, you tried to access row {row}.");
                }

                if (row < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(row), "Cannot access negative rows in a matrix.");
                }

                if (col >= Columns)
                {
                    throw new ArgumentOutOfRangeException(nameof(col), $"The transformation matrix only contains {Columns} columns and is zero indexed, you tried to access column {col}.");
                }

                if (col < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(col), "Cannot access negative columns in a matrix.");
                }

                var resultIndex = row * Rows + col;

                if (resultIndex > value.Length - 1)
                {
                    throw new ArgumentOutOfRangeException($"Trying to access {row}, {col} mapped to the index {resultIndex} which was not in the value array.");
                }

                return value[resultIndex];
            }
        }

        /// <summary>
        /// The number of rows in the matrix.
        /// </summary>
        public const int Rows = 3;
        /// <summary>
        /// The number of columns in the matrix.
        /// </summary>
        public const int Columns = 3;
        
        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/>.
        /// </summary>
        /// <param name="value">The 9 values of the matrix.</param>
        public TransformationMatrix(decimal[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != 9)
            {
                throw new ArgumentException("The constructor for the PDF transformation matrix must contain 9 elements. Instead got: " + value);
            }

            this.value = value;
        }
        
        /// <summary>
        /// Transform a point using this transformation matrix.
        /// </summary>
        /// <param name="original">The original point.</param>
        /// <returns>A new point which is the result of applying this transformation matrix.</returns>
        [Pure]
        public PdfPoint Transform(PdfPoint original)
        {
            var x = A * original.X + C * original.Y + E;
            var y = B * original.X + D * original.Y + F;

            return new PdfPoint(x, y);
        }

        /// <summary>
        /// Transform an X coordinate using this transformation matrix.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <returns>The transformed X coordinate.</returns>
        [Pure]
        internal decimal TransformX(decimal x)
        {
            var xt = A * x + C * 0 + E;

            return xt;
        }

        /// <summary>
        /// Transform a vector using this transformation matrix.
        /// </summary>
        /// <param name="original">The original vector.</param>
        /// <returns>A new vector which is the result of applying this transformation matrix.</returns>
        [Pure]
        internal PdfVector Transform(PdfVector original)
        {
            var x = A * original.X + C * original.Y + E;
            var y = B * original.X + D * original.Y + F;

            return new PdfVector(x, y);
        }

        /// <summary>
        /// Transform a rectangle using this transformation matrix.
        /// </summary>
        /// <param name="original">The original rectangle.</param>
        /// <returns>A new rectangle which is the result of applying this transformation matrix.</returns>
        [Pure]
        public PdfRectangle Transform(PdfRectangle original)
        {
            return new PdfRectangle(
                Transform(original.TopLeft),
                Transform(original.TopRight),
                Transform(original.BottomLeft),
                Transform(original.BottomRight)
            );
        }

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> from the 6 values provided in the default PDF order.
        /// </summary>
        public static TransformationMatrix FromValues(decimal a, decimal b, decimal c, decimal d, decimal e, decimal f)
            => FromArray(new[] {a, b, c, d, e, f});

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> from the values.
        /// </summary>
        /// <param name="values">Either all 9 values of the matrix, 6 values in the default PDF order or the 4 values of the top left square.</param>
        /// <returns></returns>
        public static TransformationMatrix FromArray(decimal[] values)
        {
            if (values.Length == 9)
            {
                return new TransformationMatrix(values);
            }

            if (values.Length == 6)
            {
                return new TransformationMatrix(new []
                {
                    values[0], values[1], 0,
                    values[2], values[3], 0,
                    values[4], values[5], 1
                });
            }

            if (values.Length == 4)
            {
                return new TransformationMatrix(new []
                {
                    values[0], values[1], 0,
                    values[2], values[3], 0,
                    0, 0, 1
                });
            }

            throw new ArgumentException("The array must either define all 9 elements of the matrix or all 6 key elements. Instead array was: " + values);
        }

        /// <summary>
        /// Multiplies one transformation matrix by another without modifying either matrix. Order is: (this * matrix).
        /// </summary>
        /// <param name="matrix">The matrix to multiply</param>
        /// <returns>The resulting matrix.</returns>
        [Pure]
        public TransformationMatrix Multiply(TransformationMatrix matrix)
        {
            var result = new decimal[9];

            for (var i = 0; i < Rows; i++)
            {
                var rowIndexPart = i * Rows;

                for (var j = 0; j < Columns; j++)
                {
                    var index = rowIndexPart + j;

                    for (var x = 0; x < Rows; x++)
                    {
                        result[index] += this[i, x] * matrix[x, j];
                    }
                }
            }

            return new TransformationMatrix(result);
        }

        /// <summary>
        /// Multiplies the matrix by a scalar value without modifying this matrix.
        /// </summary>
        /// <param name="scalar">The value to multiply.</param>
        /// <returns>A new matrix which is multiplied by the scalar value.</returns>
        [Pure]
        public TransformationMatrix Multiply(decimal scalar)
        {
            var result = new decimal[9];

            for (var i = 0; i < Rows; i++)
            {
                for (var j = 0; j < Columns; j++)
                {
                    var index = (i * Rows) + j;

                    for (var x = 0; x < Rows; x++)
                    {
                        result[index] += this[i, x] * scalar;
                    }
                }
            }

            return new TransformationMatrix(result);
        }

        /// <summary>
        /// Get the X scaling component of the current matrix.
        /// </summary>
        /// <returns></returns>
        internal decimal GetScalingFactorX()
        {
            var xScale = A;

            /*
             * BM: if the trm is rotated, the calculation is a little more complicated
             *
             * The rotation matrix multiplied with the scaling matrix is:
             * (   x   0   0)    ( cos  sin  0)    ( x*cos x*sin   0)
             * (   0   y   0) *  (-sin  cos  0)  = (-y*sin y*cos   0)
             * (   0   0   1)    (   0    0  1)    (     0     0   1)
             *
             * So, if you want to deduce x from the matrix you take
             * M(0,0) = x*cos and M(0,1) = x*sin and use the theorem of Pythagoras
             *
             * sqrt(M(0,0)^2+M(0,1)^2) =
             * sqrt(x2*cos2+x2*sin2) =
             * sqrt(x2*(cos2+sin2)) = (here is the trick cos2+sin2 = 1)
             * sqrt(x2) =
             * abs(x)
             */
            if (!(B == 0m && C == 0m))
            {
                xScale = (decimal)Math.Sqrt((double)(A*A + B*B));
            }

            return xScale;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is TransformationMatrix m))
            {
                return false;
            }

            return Equals(this, m);
        }

        /// <summary>
        /// Determines whether 2 transformation matrices are equal.
        /// </summary>
        public static bool Equals(TransformationMatrix a, TransformationMatrix b)
        {
            for (var i = 0; i < Rows; i++)
            {
                for (var j = 0; j < Columns; j++)
                {
                    if (a[i, j] != b[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = 1113510858;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<decimal[]>.Default.GetHashCode(value);
            return hashCode;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{A}, {B}, 0\r\n{C}, {D}, 0\r\n{E}, {F}, 1";
        }

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> with the X and Y translation values set.
        /// </summary>
        public static TransformationMatrix GetTranslationMatrix(decimal x, decimal y)
        {
            return new TransformationMatrix(new []
            {
                1, 0, 0,
                0, 1, 0,
                x, y, 1
            });
        }
    }
}
