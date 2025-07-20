namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using Tokenization.Scanner;
using Util;

internal static class XrefBruteForcer
{
    private static ReadOnlySpan<byte> XRefBytes => "xref"u8;
    private static ReadOnlySpan<byte> SpaceObjBytes => " obj"u8;

    public static IReadOnlyList<(long offset, object streamOrTable)> FindAllXrefsInFileOrder(
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        var tables = BruteForceSearchForTables(bytes, scanner, log);

        return [];
    }

    private static IReadOnlyList<(long offset, XrefTable table)> BruteForceSearchForTables(
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        var results = new List<(long, XrefTable)>();

        var startOffset = bytes.CurrentOffset;

        bytes.Seek(0);

        var buffer = new CircularByteBuffer(XRefBytes.Length + 1);

        // search for xref tables
        while (bytes.MoveNext() && !bytes.IsAtEnd())
        {
            if (ReadHelper.IsWhitespace(bytes.CurrentByte))
            {
                // Normalize whitespace
                buffer.Add((byte)' ');
            }
            else
            {
                buffer.Add(bytes.CurrentByte);
            }

            if (buffer.IsCurrentlyEqual(" xref"))
            {
                var potentialTableOffset = bytes.CurrentOffset - 4;
                var table = XrefTableParser.TryReadTableAtOffset(
                    potentialTableOffset,
                    bytes,
                    scanner,
                    log);

                if (table != null)
                {
                    results.Add((potentialTableOffset, table));
                }
                else
                {
                    log.Warn(
                        $"Found a table at {potentialTableOffset} but couldn't parse it.");
                }
            }
        }

        return results;
    }
}
