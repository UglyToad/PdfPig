namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using Tokenization.Scanner;
using Tokens;
using Util;

internal static partial class FirstPassParser
{
    private static ReadOnlySpan<byte> StartXRefBytes => "startxref"u8;

    public static StartXRefLocation GetFirstCrossReferenceOffset(
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        var fileLength = bytes.Length;

        var buffer = new CircularByteBuffer(StartXRefBytes.Length);

        // Start from the end of the file
        bytes.Seek(fileLength);

        long? capturedOffset = null;
        var i = 0;
        do
        {
            buffer.AddReverse(bytes.CurrentByte);
            i++;

            if (i >= StartXRefBytes.Length)
            {
                if (buffer.IsCurrentlyEqual("startxref"))
                {
                    capturedOffset = bytes.CurrentOffset - 1;
                    break;
                }
                
                // This can be a mangled version of the startxref operator.
                if (buffer.EndsWith("startref"))
                {
                    capturedOffset = bytes.CurrentOffset;
                    break;
                }
            }

            bytes.Seek(bytes.CurrentOffset - 1);
        } while (bytes.CurrentOffset > 0);

        long? specifiedXrefOffset = null;
        if (capturedOffset.HasValue)
        {
            scanner.Seek(capturedOffset.Value);

            if (scanner.TryReadToken(out OperatorToken startXrefOp)
                && (startXrefOp.Data == "startxref" || startXrefOp.Data == "startref"))
            {
                specifiedXrefOffset = GetNumericTokenFollowingCurrent(scanner);

                log.Debug($"Found startxref at {specifiedXrefOffset}");
            }
        }
        else
        {
            log.Warn("No startxref token found in the document");
        }

        return new StartXRefLocation(capturedOffset, specifiedXrefOffset);
    }

    private static long? GetNumericTokenFollowingCurrent(ISeekableTokenScanner scanner)
    {
        while (scanner.MoveNext())
        {
            if (scanner.CurrentToken is NumericToken token)
            {
                return token.Long;
            }

            if (scanner.CurrentToken is not CommentToken)
            {
                break;
            }
        }

        return null;
    }

    public record StartXRefLocation(long? StartXRefOperatorToken, long? StartXRefDeclaredOffset)
    {
        /// <summary>
        /// The offset in the file the "startxref" we located (if any) declares the xref should be located.
        /// </summary>
        public long? StartXRefDeclaredOffset { get; } = StartXRefDeclaredOffset;

        /// <summary>
        /// The offset in the file the "startxref" token we located (if any) starts at.
        /// </summary>
        public long? StartXRefOperatorToken { get; } = StartXRefOperatorToken;

        public bool IsValidOffset(IInputBytes bytes)
        {
            if (!StartXRefDeclaredOffset.HasValue
                || StartXRefDeclaredOffset < 0
                || StartXRefDeclaredOffset > bytes.Length)
            {
                return false;
            }

            return true;
        }
    }
}
