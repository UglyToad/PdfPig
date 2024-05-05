namespace UglyToad.PdfPig.Core
{
    using System.Diagnostics.Contracts;
    using static UglyToad.PdfPig.Core.PdfSubpath;

    /// <summary>
    /// Specifies the conversion from the transformed coordinate space to the original untransformed coordinate space.
    /// </summary>
    public readonly struct TransformationMatrix
    {
        /// <summary>
        /// The default <see cref="TransformationMatrix"/>.
        /// </summary>
        public static readonly TransformationMatrix Identity = new TransformationMatrix(1, 0, 0,
            0, 1, 0,
            0, 0, 1);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> with the X and Y translation values set.
        /// </summary>
        public static TransformationMatrix GetTranslationMatrix(double x, double y) => new TransformationMatrix(1, 0, 0,
            0, 1, 0,
            x, y, 1);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> with the X and Y scaling values set.
        /// </summary>
        public static TransformationMatrix GetScaleMatrix(double scaleX, double scaleY) => new TransformationMatrix(
            scaleX, 0, 0,
            0, scaleY, 0,
            0, 0, 1);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> with the X and Y scaling values set.
        /// </summary>
        public static TransformationMatrix GetRotationMatrix(double degreesCounterclockwise)
        {
            double cos;
            double sin;

            var deg = degreesCounterclockwise % 360;
            if (deg < 0)
            {
                deg += 360;
            }

            switch (deg)
            {
                case 0:
                case 360:
                    cos = 1;
                    sin = 0;
                    break;
                case 90:
                    cos = 0;
                    sin = 1;
                    break;
                case 180:
                    cos = -1;
                    sin = 0;
                    break;
                case 270:
                    cos = 0;
                    sin = -1;
                    break;
                default:
                    cos = Math.Cos(degreesCounterclockwise * (Math.PI / 180));
                    sin = Math.Sin(degreesCounterclockwise * (Math.PI / 180));
                    break;
            }

            return new TransformationMatrix(cos, sin, 0,
                -sin, cos, 0,
                0, 0, 1);
        }

        private readonly double row1;
        private readonly double row2;
        private readonly double row3;

        /// <summary>
        /// The value at (0, 0) - The scale for the X dimension.
        /// </summary>
        public readonly double A;

        /// <summary>
        /// The value at (0, 1).
        /// </summary>
        public readonly double B;

        /// <summary>
        /// The value at (1, 0).
        /// </summary>
        public readonly double C;

        /// <summary>
        /// The value at (1, 1) - The scale for the Y dimension.
        /// </summary>
        public readonly double D;

        /// <summary>
        /// The value at (2, 0) - translation in X.
        /// </summary>
        public readonly double E;

        /// <summary>
        /// The value at (2, 1) - translation in Y.
        /// </summary>
        public readonly double F;

        /// <summary>
        /// Get the value at the specific row and column.
        /// </summary>
        public double this[int row, int col]
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

                return row switch {
                    0 => col switch {
                        0 => A,
                        1 => B,
                        2 => row1,
                        _ => throw new ArgumentOutOfRangeException($"Trying to access {row}, {col} which was not in the value array.")
                    },
                    1 => col switch {
                        0 => C,
                        1 => D,
                        2 => row2,
                        _ => throw new ArgumentOutOfRangeException($"Trying to access {row}, {col} which was not in the value array.")
                    },
                    2 => col switch {
                        0 => E,
                        1 => F,
                        2 => row3,
                        _ => throw new ArgumentOutOfRangeException($"Trying to access {row}, {col} which was not in the value array.")
                    },
                    _ => throw new ArgumentOutOfRangeException($"Trying to access {row}, {col} which was not in the value array.")
                };
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
        public TransformationMatrix(ReadOnlySpan<double> value) : this(value[0], value[1], value[2], value[3], value[4], value[5], value[6], value[7], value[8])
        {
        }

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/>.
        /// </summary>
        public TransformationMatrix(double a, double b, double r1, double c, double d, double r2, double e, double f, double r3)
        {
            A = a;
            B = b;
            row1 = r1;
            C = c;
            D = d;
            row2 = r2;
            E = e;
            F = f;
            row3 = r3;
        }

        /// <summary>
        /// Transform a point using this transformation matrix.
        /// </summary>
        /// <param name="original">The original point.</param>
        /// <returns>A new point which is the result of applying this transformation matrix.</returns>
        [Pure]
        public PdfPoint Transform(PdfPoint original)
        {
            (double x, double y) xy = Transform(original.X, original.Y);
            return new PdfPoint(xy.x, xy.y);
        }

        /// <summary>
        /// Transform a point using this transformation matrix.
        /// </summary>
        /// <param name="x">The original point X coordinate.</param>
        /// <param name="y">The original point Y coordinate.</param>
        /// <returns>A new point which is the result of applying this transformation matrix.</returns>
        [Pure]
        public (double x, double y) Transform(double x, double y)
        {
            return (A * x + C * y + E, B * x + D * y + F);
        }

        /// <summary>
        /// Transform an X coordinate using this transformation matrix.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <returns>The transformed X coordinate.</returns>
        [Pure]
        public double TransformX(double x)
        {
            var xt = A * x + C * 0 + E;

            return xt;
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
        /// Transform a subpath using this transformation matrix.
        /// </summary>
        /// <param name="subpath">The original subpath.</param>
        /// <returns>A new subpath which is the result of applying this transformation matrix.</returns>
        public PdfSubpath Transform(PdfSubpath subpath)
        {
            var trSubpath = new PdfSubpath();
            foreach (var c in subpath.Commands)
            {
                if (c is Move move)
                {
                    var loc = Transform(move.Location);
                    trSubpath.MoveTo(loc.X, loc.Y);
                }
                else if (c is Line line)
                {
                    //var from = Transform(line.From);
                    var to = Transform(line.To);
                    trSubpath.LineTo(to.X, to.Y);
                }
                else if (c is CubicBezierCurve cubic)
                {
                    var first = Transform(cubic.FirstControlPoint);
                    var second = Transform(cubic.SecondControlPoint);
                    var end = Transform(cubic.EndPoint);
                    trSubpath.BezierCurveTo(first.X, first.Y, second.X, second.Y, end.X, end.Y);
                }
                else if (c is QuadraticBezierCurve quadratic)
                {
                    var control = Transform(quadratic.ControlPoint);
                    var end = Transform(quadratic.EndPoint);
                    trSubpath.BezierCurveTo(control.X, control.Y, end.X, end.Y);
                }
                else if (c is Close)
                {
                    trSubpath.CloseSubpath();
                }
                else
                {
                    throw new Exception("Unknown PdfSubpath type");
                }
            }
            return trSubpath;
        }

        /// <summary>
        /// Transform a path using this transformation matrix.
        /// </summary>
        /// <param name="path">The original path.</param>
        /// <returns>A new path which is the result of applying this transformation matrix.</returns>
        public IEnumerable<PdfSubpath> Transform(IEnumerable<PdfSubpath> path)
        {
            foreach (var subpath in path)
            {
                yield return Transform(subpath);
            }
        }

        /// <summary>
        /// Generate a <see cref="TransformationMatrix"/> translated by the specified amount.
        /// </summary>
        [Pure]
        public TransformationMatrix Translate(double x, double y)
        {
            var a = A;
            var b = B;
            var r1 = row1;

            var c = C;
            var d = D;
            var r2 = row2;

            var e = (x * A) + (y * C) + E;
            var f = (x * B) + (y * D) + F;
            var r3 = (x * row1) + (y * row2) + row3;

            return new TransformationMatrix(a, b, r1,
                c, d, r2,
                e, f, r3);
        }

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> from the 6 values provided in the default PDF order.
        /// </summary>
        public static TransformationMatrix FromValues(double a, double b, double c, double d, double e, double f)
            => new TransformationMatrix(a, b, 0, c, d, 0, e, f, 1);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> from the 4 values provided in the default PDF order.
        /// </summary>
        public static TransformationMatrix FromValues(double a, double b, double c, double d)
            => new TransformationMatrix(a, b, 0, c, d, 0, 0, 0, 1);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> from the values.
        /// </summary>
        /// <param name="values">Either all 9 values of the matrix, 6 values in the default PDF order or the 4 values of the top left square.</param>
        /// <returns></returns>
        public static TransformationMatrix FromArray(ReadOnlySpan<double> values)
        {
            if (values.Length == 9)
            {
                return new TransformationMatrix(values);
            }

            if (values.Length == 6)
            {
                return new TransformationMatrix(values[0], values[1], 0,
                    values[2], values[3], 0,
                    values[4], values[5], 1);
            }

            if (values.Length == 4)
            {
                return new TransformationMatrix(values[0], values[1], 0,
                    values[2], values[3], 0,
                    0, 0, 1);
            }

            throw new ArgumentException("The array must either define all 9 elements of the matrix or all 6 key elements. Instead array was: " + string.Join(", ", values.ToArray()));
        }

        /// <summary>
        /// Multiplies one transformation matrix by another without modifying either matrix. Order is: (this * matrix).
        /// </summary>
        /// <param name="matrix">The matrix to multiply</param>
        /// <returns>The resulting matrix.</returns>
        [Pure]
        public TransformationMatrix Multiply(TransformationMatrix matrix)
        {
            var a = (A * matrix.A) + (B * matrix.C) + (row1 * matrix.E);
            var b = (A * matrix.B) + (B * matrix.D) + (row1 * matrix.F);
            var r1 = (A * matrix.row1) + (B * matrix.row2) + (row1 * matrix.row3);

            var c = (C * matrix.A) + (D * matrix.C) + (row2 * matrix.E);
            var d = (C * matrix.B) + (D * matrix.D) + (row2 * matrix.F);
            var r2 = (C * matrix.row1) + (D * matrix.row2) + (row2 * matrix.row3);

            var e = (E * matrix.A) + (F * matrix.C) + (row3 * matrix.E);
            var f = (E * matrix.B) + (F * matrix.D) + (row3 * matrix.F);
            var r3 = (E * matrix.row1) + (F * matrix.row2) + (row3 * matrix.row3);

            return new TransformationMatrix(a, b, r1,
                c, d, r2,
                e, f, r3);
        }

        /// <summary>
        /// Multiplies the matrix by a scalar value without modifying this matrix.
        /// </summary>
        /// <param name="scalar">The value to multiply.</param>
        /// <returns>A new matrix which is multiplied by the scalar value.</returns>
        [Pure]
        public TransformationMatrix Multiply(double scalar)
        {
            return new TransformationMatrix(A * scalar, B * scalar, row1 * scalar,
                C * scalar, D * scalar, row2 * scalar,
                E * scalar, F * scalar, row3 * scalar);
        }

        /// <summary>
        /// Get the inverse of the current matrix.
        /// </summary>
        public TransformationMatrix Inverse()
        {
            var a = (D * row3 - row2 * F);
            var c = -(C * row3 - row2 * E);
            var e = (C * F - D * E);

            var b = -(B * row3 - row1 * F);
            var d = (A * row3 - row1 * E);
            var f = -(A * F - B * E);

            var r1 = (B * row2 - row1 * D);
            var r2 = -(A * row2 - row1 * C);
            var r3 = (A * D - B * C);
            var det = A * a + B * c + row1 * e;

            return new TransformationMatrix(a / det, b / det, r1 / det,
                c / det, d / det, r2 / det,
                e / det, f / det, r3 / det);
        }

        /// <summary>
        /// Get the X scaling component of the current matrix.
        /// </summary>
        /// <returns>The scaling factor for the x dimension in this matrix.</returns>
        internal double GetScalingFactorX()
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
            if (!(B == 0 && C == 0))
            {
                xScale = Math.Sqrt(A * A + B * B);
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
            var hashCode = new HashCode();

            hashCode.Add(row1);
            hashCode.Add(row2);
            hashCode.Add(row3);
            hashCode.Add(A);
            hashCode.Add(B);
            hashCode.Add(C);
            hashCode.Add(D);
            hashCode.Add(E);
            hashCode.Add(F);

            return hashCode.ToHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{A}, {B}, {row1}\r\n{C}, {D}, {row2}\r\n{E}, {F}, {row3}";
        }

        /// <inheritdoc/>
        public static bool operator ==(TransformationMatrix left, TransformationMatrix right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(TransformationMatrix left, TransformationMatrix right)
        {
            return !(left == right);
        }
    }
}
