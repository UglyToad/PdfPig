namespace UglyToad.PdfPig.Util;

using System;
using System.Text;

internal sealed class CircularByteBuffer(int size)
{
    private readonly byte[] buffer = new byte[size];

    private int start;
    private int count;

    public void Add(byte b)
    {
        var insertionPosition = (start + count) % buffer.Length;

        buffer[insertionPosition] = b;
        if (count < buffer.Length)
        {
            count++;
        }
        else
        {
            start = (start + 1) % buffer.Length;
        }
    }
    
    /// <summary>
    /// Adds a byte to the start of the buffer. If the buffer is full,
    /// the byte at the end is overwritten.
    /// </summary>
    /// <param name="b">The byte to add.</param>
    public void AddReverse(byte b)
    {
        // Move the start pointer back by one, wrapping around if necessary.
        // This is the new position for the prepended byte.
        start = (start - 1 + buffer.Length) % buffer.Length;

        // Place the new byte at the new start position.
        buffer[start] = b;

        // If the buffer isn't full, increment the count.
        // If it is full, the new byte effectively overwrites what was
        // previously the last logical byte, and the count remains the same.
        if (count < buffer.Length)
        {
            count++;
        }
    }

    public bool EndsWith(string s)
    {
        if (s.Length > count)
        {
            return false;
        }

        for (var i = 0; i < s.Length; i++)
        {
            var str = s[i];

            var inBuffer = count - (s.Length - i);

            var buff = buffer[IndexToBufferIndex(inBuffer)];

            if (buff != str)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsCurrentlyEqual(string s)
    {
        if (s.Length > buffer.Length)
        {
            return false;
        }

        for (var i = 0; i < s.Length; i++)
        {
            var b = (byte)s[i];
            var buff = buffer[IndexToBufferIndex(i)];

            if (b != buff)
            {
                return false;
            }
        }

        return true;
    }

    public ReadOnlySpan<byte> AsSpan()
    {
        Span<byte> tmp = new byte[count];
        for (int i = 0; i < count; i++)
        {
            tmp[i] = buffer[IndexToBufferIndex(i)];
        }

        return tmp;
    }

    public override string ToString()
    {
        return Encoding.ASCII.GetString(AsSpan());
    }

    private int IndexToBufferIndex(int i) => (start + i) % buffer.Length;
}