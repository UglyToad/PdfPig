namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;

    internal static class Utils
    {
        public static int HighestOneBit(this int number)
        {
            return (int)Math.Pow(2, Convert.ToString(number, 2).Length - 1);
        }

        public static int GetMinY(this Jbig2Rectangle r)
        {
            return r.Y;
        }

        public static int GetMaxY(this Jbig2Rectangle r)
        {
            return r.Y + r.Height;
        }

        public static int GetMaxX(this Jbig2Rectangle r)
        {
            return r.X + r.Width;
        }

        public static int GetMinX(this Jbig2Rectangle r)
        {
            return r.X;
        }
    }
}
