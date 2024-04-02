#if NETFRAMEWORK || NETSTANDARD2_0

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

    public static int Read(this Stream stream, Span<byte> buffer)
    {
        var tempBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);

        try
        {
            int read = stream.Read(tempBuffer, 0, buffer.Length);

            tempBuffer.AsSpan(0, read).CopyTo(buffer);

            return read;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }
}

#endif