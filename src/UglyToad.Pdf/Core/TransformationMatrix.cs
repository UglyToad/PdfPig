namespace UglyToad.Pdf.Core
{
    using System;
    using Geometry;
    using System.Collections.Generic;

    /// <summary>
    /// Specifies the conversion from the transformed coordinate space to the original untransformed coordinate space.
    /// </summary>
    internal struct TransformationMatrix
    {
        public static TransformationMatrix Identity = new TransformationMatrix(new decimal[]
        {
            1,0,0,
            0,1,0,
            0,0,1
        });

        private readonly decimal[] value;

        public decimal A => value[0];
        public decimal B => value[1];
        public decimal C => value[3];
        public decimal D => value[4];
        public decimal E => value[6];
        public decimal F => value[7];

        public decimal this[int row, int col]
        {
            get
            {
                if (row >= Rows)
                {
                    throw new ArgumentOutOfRangeException($"The transformation matrix only contains {Rows} rows and is zero indexed, you tried to access row {row}.");
                }

                if (row < 0)
                {
                    throw new ArgumentOutOfRangeException("Cannot access negative rows in a matrix.");
                }

                if (col >= Columns)
                {
                    throw new ArgumentOutOfRangeException($"The transformation matrix only contains {Columns} columns and is zero indexed, you tried to access column {col}.");
                }

                if (col < 0)
                {
                    throw new ArgumentOutOfRangeException("Cannot access negative columns in a matrix.");
                }

                var resultIndex = row * Rows + col;

                if (resultIndex > value.Length - 1)
                {
                    throw new ArgumentOutOfRangeException($"Trying to access {row}, {col} mapped to the index {resultIndex} which was not in the value array.");
                }

                return value[resultIndex];
            }
        }

        public const int Rows = 3;
        public const int Columns = 3;
        
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
        
        public PdfPoint Transform(PdfPoint original)
        {
            var x = A * original.X + C * original.Y + E;
            var y = B * original.X + D * original.Y + F;

            return new PdfPoint(x, y);
        }

        public static TransformationMatrix FromValues(decimal a, decimal b, decimal c, decimal d, decimal e, decimal f)
            => FromArray(new[] {a, b, c, d, e, f});
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

            throw new ArgumentException("The array must either define all 9 elements of the matrix or all 6 key elements. Instead array was: " + values);
        }

        public TransformationMatrix Multiply(TransformationMatrix matrix)
        {
            var result = new decimal[9];

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    var index = (i * Rows) + j;

                    for (int x = 0; x < Rows; x++)
                    {
                        result[index] += this[i, x] * matrix[x, j];
                    }
                }
            }

            return new TransformationMatrix(result);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TransformationMatrix m))
            {
                return false;
            }

            return Equals(this, m);
        }

        public static bool Equals(TransformationMatrix a, TransformationMatrix b)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    if (a[i, j] != b[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = 1113510858;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<decimal[]>.Default.GetHashCode(value);
            return hashCode;
        }

        public override string ToString()
        {
            return $"{A}, {B}, 0\r\n{C}, {D}, 0\r\n{E}, {F}, 1";
        }
    }
}
