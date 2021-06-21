namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;
    using System.Drawing;

    internal static class Utils
    {
        public static int HighestOneBit(this int number)
        {
            return (int)Math.Pow(2, Convert.ToString(number, 2).Length - 1);
        }

        public static int GetMinY(this Rectangle r)
        {
            return r.Y;
        }

        public static int GetMaxY(this Rectangle r)
        {
            return r.Y + r.Height;
        }

        public static int GetMaxX(this Rectangle r)
        {
            return r.X + r.Width;
        }

        public static int GetMinX(this Rectangle r)
        {
            return r.X;
        }
    }
}
