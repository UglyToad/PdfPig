using System;

namespace UglyToad.PdfPig.Util;

internal static class StringExtensions
{
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
