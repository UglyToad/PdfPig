using System;
using System.Buffers;

namespace UglyToad.PdfPig.Core;

/// <summary>
/// Pooled Buffer Writer
/// </summary>
public sealed class ArrayPoolBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private const int DefaultBufferSize = 256;

    private T[] buffer;
    private int position;

    /// <summary>
    /// PooledBufferWriter constructor
    /// </summary>
    public ArrayPoolBufferWriter()
    {
        buffer = ArrayPool<T>.Shared.Rent(DefaultBufferSize);
        position = 0;
    }

    /// <summary>
    /// Constructs a PooledBufferWriter
    /// </summary>
    /// <param name="size">The size of the initial buffer</param>
    public ArrayPoolBufferWriter(int size)
    {
        buffer = ArrayPool<T>.Shared.Rent(size);
        position = 0;
    }

    /// <summary>
    /// Advanced the current position
    /// </summary>
    /// <param name="count"></param>
    public void Advance(int count)
    {
        position += count;
    }

    /// <summary>
    /// Writes the provided value
    /// </summary>
    public void Write(T value)
    {
        GetSpan(1)[0] = value;

        position += 1;
    }

    /// <summary>
    /// Writes the provided values
    /// </summary>
    /// <param name="values"></param>
    public void Write(ReadOnlySpan<T> values)
    {
        values.CopyTo(GetSpan(values.Length));

        position += values.Length;
    }

    /// <summary>
    /// Returns a writeable block of memory that can be written to
    /// </summary>
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);

        return buffer.AsMemory(position);
    }

    /// <summary>
    /// Returns a span that can be written to
    /// </summary>
    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);

        return buffer.AsSpan(position);
    }

    /// <summary>
    /// Returns the number of bytes written to the buffer
    /// </summary>
    public int WrittenCount => position;

    /// <summary>
    /// Returns the committed data as Memory
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory => buffer.AsMemory(0, position);

    /// <summary>
    /// Returns the committed data as a Span
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan => buffer.AsSpan(0, position);

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint is 0)
        {
            sizeHint = 1;
        }

        if (sizeHint > RemainingBytes)
        {
            var newBuffer = ArrayPool<T>.Shared.Rent(Math.Max(position + sizeHint, 512));

            if (buffer.Length != 0)
            {
                Array.Copy(buffer, 0, newBuffer, 0, position);
                ArrayPool<T>.Shared.Return(buffer);
            }

            buffer = newBuffer;
        }
    }

    private int RemainingBytes => buffer.Length - position;

    /// <summary>
    /// Resets the internal state so the instance can be reused before disposal
    /// </summary>
    /// <param name="clearArray"></param>
    public void Reset(bool clearArray = false)
    {
        position = 0;

        if (clearArray)
        {
            buffer.AsSpan().Clear();
        }
    }

    /// <summary>
    /// Disposes the buffer and returns any rented memory to the pool
    /// </summary>
    public void Dispose()
    {
        if (buffer.Length != 0)
        {
            ArrayPool<T>.Shared.Return(buffer);
            buffer = [];
        }
    }
}