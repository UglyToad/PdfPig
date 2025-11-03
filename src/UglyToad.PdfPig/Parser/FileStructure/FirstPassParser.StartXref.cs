namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using Tokenization.Scanner;
using Tokens;
using Util;

internal static partial class FirstPassParser
{
    private static ReadOnlySpan<byte> StartXRefBytes => "startxref"u8;

    public const long EndOfFileBufferSize = 1024;

    public static StartXRefLocation GetFirstCrossReferenceOffset(
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        // We used to read backward through the file, but this is quite expensive for streams that directly wrap OS files.
        // Instead we fetch the last 1024 bytes of the file and do a memory search, as cheap first attempt. This is significantly faster
        // in practice, if there is no in-process caching of the file involved
        // 
        // If that fails (in practice it should never) we fall back to the old method of reading backwards.
        var fileLength = bytes.Length;
        {
            var fetchFrom = Math.Max(bytes.Length - EndOfFileBufferSize, 0L);

            bytes.Seek(fetchFrom);

            Span<byte> byteBuffer = new byte[bytes.Length - fetchFrom];   // TODO: Maybe use PoolArray?

            int n = bytes.Read(byteBuffer);

            if (n == byteBuffer.Length)
            {
                int lx = byteBuffer.LastIndexOf("startxref"u8);

                if (lx < 0)
                {
                    // See old code. We also try a mangled version
                    lx = byteBuffer.LastIndexOf("startref"u8);
                }

                if (lx >= 0)
                {
                    scanner.Seek(fetchFrom + lx);

                    if (scanner.TryReadToken(out OperatorToken startXrefOp) && (startXrefOp.Data == "startxref" || startXrefOp.Data == "startref"))
                    {
                        var pos = GetNumericTokenFollowingCurrent(scanner);

                        log.Debug($"Found startxref at {pos}");

                        return new StartXRefLocation(fetchFrom + lx, pos);
                    }
                }

            }
        }

        // Now fall through in the old code
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
