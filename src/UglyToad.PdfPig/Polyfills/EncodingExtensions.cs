#if !NET

namespace System.Text;

internal static class EncodingExtensions
{
    internal static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        // NOTE: this can be made allocation free by introducing unsafe

        return encoding.GetString(bytes.ToArray());
    }
}

#endif