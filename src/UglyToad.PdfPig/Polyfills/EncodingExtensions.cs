#if NETFRAMEWORK || NETSTANDARD2_0

namespace System.Text;

internal static class EncodingExtensions
{
    public unsafe static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* pBytes = bytes)
        {
            return encoding.GetString(pBytes, bytes.Length);
        }
    }
}

#endif