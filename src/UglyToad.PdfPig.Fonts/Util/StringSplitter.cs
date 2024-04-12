using System;
using System.IO;

namespace UglyToad.PdfPig.Util;

internal ref struct StringSplitter(ReadOnlySpan<char> text, char separator)
{
    private readonly ReadOnlySpan<char> text = text;
    private readonly char separator = separator;
    private int position = 0;

    public bool TryRead(out ReadOnlySpan<char> result)
    {
        if (IsEof)
        {
            result = default;

            return false;
        }

        int separatorIndex = text.Slice(position).IndexOf(separator);

        if (separatorIndex > -1)
        {
            result = text.Slice(position, separatorIndex);

            position += separatorIndex + 1;
        }
        else
        {
            result = text.Slice(position);

            position = text.Length;
        }

        return true;
    }

    public ReadOnlySpan<char> Read()
    {
        if (IsEof)
        {
            ThrowEof();
        }

        int start = position;

        int separatorIndex = text.Slice(position).IndexOf(separator);

        if (separatorIndex > -1)
        {
            position += separatorIndex + 1;

            return text.Slice(start, separatorIndex);
        }
        else
        {
            position = text.Length;

            return text.Slice(start);
        }
    }

    public readonly bool IsEof => position == text.Length;

    private static void ThrowEof()
    {
        throw new EndOfStreamException();
    }
}