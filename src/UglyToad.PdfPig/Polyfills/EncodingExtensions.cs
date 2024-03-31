#if NETFRAMEWORK || NETSTANDARD2_0

namespace System.Text;

internal static class EncodingExtensions
{
    public static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        // NOTE: this can be made allocation free by introducing unsafe
       
        return encoding.GetString(bytes.ToArray());
    }
}

#endif