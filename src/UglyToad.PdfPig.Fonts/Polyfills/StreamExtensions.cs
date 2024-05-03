#if !NET

namespace System.IO;

using System.Buffers;

internal static class StreamExtensions
{
    public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
    {
        var tempBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);

        buffer.CopyTo(tempBuffer);

        try
        {
            stream.Write(tempBuffer, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }
}

#endif