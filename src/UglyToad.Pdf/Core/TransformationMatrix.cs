namespace UglyToad.Pdf.Core
{
    using System;
    using Geometry;

    /// <summary>
    /// Specifies the conversion from the transformed coordinate space to the original untransformed coordinate space.
    /// </summary>
    internal struct TransformationMatrix
    {
        public static TransformationMatrix Default = new TransformationMatrix(new decimal[]
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

        public override string ToString()
        {
            return $"{A}, {B}, 0\r\n{C}, {D}, 0\r\n{E}, {F}, 1";
        }
    }
}
