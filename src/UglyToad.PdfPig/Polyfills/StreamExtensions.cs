#if !NET

namespace System.IO;

using System.Buffers;

internal static class StreamExtensions
{
    public static void Write(this Stream stream, ReadOnlySpan<byte> data)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(data.Length);

        data.CopyTo(buffer);

        try
        {
            stream.Write(buffer, 0, data.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

#endif