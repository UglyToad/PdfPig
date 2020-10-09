namespace UglyToad.PdfPig.SystemDrawing
{
    using System;
    using System.Drawing.Drawing2D;
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// MatrixExtensions
    /// </summary>
    public static class MatrixExtensions
    {
        /// <summary>
        /// not in place
        /// </summary>
        public static Matrix Multiply(this Matrix matrix, double value)
        {
            return Multiply(matrix, (float)value);
        }

        /// <summary>
        /// not in place
        /// </summary>
        public static Matrix Multiply(this Matrix matrix, float value)
        {
            return new Matrix(matrix.Elements[0] * value, matrix.Elements[1] * value,
                                       matrix.Elements[2] * value, matrix.Elements[3] * value,
                                       matrix.Elements[4] * value, matrix.Elements[5] * value);
        }

        /// <summary>
        /// Create a new <see cref="Matrix"/> from the values.
        /// </summary>
        /// <param name="values">Either all 9 values of the matrix, 6 values in the default PDF order or the 4 values of the top left square.</param>
        /// <returns></returns>
        public static Matrix FromArray(double[] values)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.drawing2d.matrix?view=dotnet-plat-ext-3.1
            if (values.Length == 9)
            {
                if (values[2] == 0 && values[5] == 0 && values[8] == 1)
                {
                    // ignore last column
                    var mat = new Matrix((float)values[0], (float)values[1],
                                         (float)values[3], (float)values[4],
                                         (float)values[6], (float)values[7]);
                    //mat.Translate(0, -pageHeight);
                    return mat;
                }
                throw new ArgumentException("MatrixFromArray", nameof(values));
                //return new TransformationMatrix(values);
            }
            else if (values.Length == 6)
            {
                var mat = new Matrix((float)values[0], (float)values[1],
                                     (float)values[2], (float)values[3],
                                     (float)values[4], (float)values[5]);
                //mat.Translate(0, -pageHeight);
                return mat;
                //return new TransformationMatrix(values[0], values[1], 0,
                //    values[2], values[3], 0,
                //    values[4], values[5], 1);
            }
            else if (values.Length == 4)
            {
                var mat = new Matrix((float)values[0], (float)values[1],
                                     (float)values[2], (float)values[3],
                                     0f, 0f);
                //mat.Translate(0, -pageHeight);
                return mat;
                //return new TransformationMatrix(values[0], values[1], 0,
                //    values[2], values[3], 0,
                //    0, 0, 1);
            }

            throw new ArgumentException("The array must either define all 9 elements of the matrix or all 6 key elements. Instead array was: " + values);
        }

        /// <summary>
        /// Create a new <see cref="Matrix"/> from the 6 values provided in the default PDF order.
        /// </summary>
        public static Matrix FromValues(double a, double b, double c, double d, double e, double f)
            => new Matrix((float)a, (float)b,
                          (float)c, (float)d,
                          (float)e, (float)f);

        /// <summary>
        /// Create a new <see cref="Matrix"/> from the 4 values provided in the default PDF order.
        /// </summary>
        public static Matrix FromValues(double a, double b, double c, double d)
            => new Matrix((float)a, (float)b,
                          (float)c, (float)d,
                          (float)0, (float)0);

        /// <summary>
        /// The default <see cref="Matrix"/>.
        /// </summary>
        public static Matrix Identity => new Matrix(1, 0,
                                                    0, 1,
                                                    0, 0);

        /// <summary>
        /// Create a new <see cref="Matrix"/> with the X and Y translation values set.
        /// </summary>
        public static Matrix GetTranslationMatrix(float x, float y) => new Matrix(1, 0,
                                                                                  0, 1,
                                                                                  x, y);

        /// <summary>
        /// Create a new <see cref="Matrix"/> with the X and Y scaling values set.
        /// </summary>
        public static Matrix GetScaleMatrix(float scaleX, float scaleY) => new Matrix(scaleX, 0,
            0, scaleY,
            0, 0);

        /// <summary>
        /// Create a new <see cref="Matrix"/> with the X and Y scaling values set.
        /// </summary>
        public static Matrix GetRotationMatrix(float degreesCounterclockwise)
        {
            float cos;
            float sin;

            switch (degreesCounterclockwise)
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
                    cos = (float)Math.Cos(degreesCounterclockwise * (Math.PI / 180));
                    sin = (float)Math.Sin(degreesCounterclockwise * (Math.PI / 180));
                    break;
            }

            return new Matrix(cos, sin,
                -sin, cos,
                0, 0);
        }
    }
}
