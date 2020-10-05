namespace UglyToad.PdfPig.SystemDrawing
{
    using System;
    using System.Drawing.Drawing2D;
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// NOT IN USE
    /// </summary>
    public static class MatrixExtensions
    {
        public static Matrix ToSystemMatrix(this TransformationMatrix transformationMatrix)
        {
            return new Matrix(
                (float)transformationMatrix.A, (float)transformationMatrix.B,
                (float)transformationMatrix.C, (float)transformationMatrix.D,
                (float)transformationMatrix.E, (float)transformationMatrix.F);
        }

        /// <summary>
        /// The default <see cref="TransformationMatrix"/>.
        /// </summary>
        public static Matrix Identity => new Matrix(1, 0,
            0, 1,
            0, 0);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> with the X and Y translation values set.
        /// </summary>
        public static Matrix GetTranslationMatrix(float x, float y) => new Matrix(1, 0,
            0, 1,
            x, y);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> with the X and Y scaling values set.
        /// </summary>
        public static Matrix GetScaleMatrix(float scaleX, float scaleY) => new Matrix(scaleX, 0,
            0, scaleY,
            0, 0);

        /// <summary>
        /// Create a new <see cref="TransformationMatrix"/> with the X and Y scaling values set.
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
