namespace UglyToad.PdfPig.Util
{
    using System;

    internal static class InternalStringExtensions
    {
        public static bool StartsWithOffset(this string value, string start, int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset cannot be negative: {offset}");
            }

            if (value is null)
            {
                if (start is null && offset == 0)
                {
                    return true;
                }

                return false;
            }

            if (offset > value.Length - 1)
            {
                return false;
            }

            return value.Substring(offset).StartsWith(start);
        }
    }
}
