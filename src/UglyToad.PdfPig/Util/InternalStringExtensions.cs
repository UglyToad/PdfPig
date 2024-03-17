namespace UglyToad.PdfPig.Util
{
    using System;

    internal static class InternalStringExtensions
    {
        public static bool StartsWithOffset(this string value, ReadOnlySpan<char> start, int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Offset cannot be negative: {offset}");
            }

            if (value is null)
            {
                if (start.Length is 0 && offset == 0)
                {
                    return true;
                }

                return false;
            }

            if (offset > value.Length - 1)
            {
                return false;
            }

            return value.AsSpan(offset).StartsWith(start, StringComparison.Ordinal);
        }

#if NET
    public static ReadOnlySpan<char> AsSpanOrSubstring(this string text, int start)
    {
        return text.AsSpan(start);
    }

    public static ReadOnlySpan<char> AsSpanOrSubstring(this string text, int start, int length)
    {
        return text.AsSpan(start, length);
    }
#else
        public static string AsSpanOrSubstring(this string text, int start)
        {
            return text.Substring(start);
        }

        public static string AsSpanOrSubstring(this string text, int start, int length)
        {
            return text.Substring(start, length);
        }
#endif
    }
}
