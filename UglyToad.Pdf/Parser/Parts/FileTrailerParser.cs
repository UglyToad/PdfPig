namespace UglyToad.Pdf.Parser.Parts
{
    using System;
    using System.Linq;
    using IO;

    /*
     * The trailer of a PDF file allows us to quickly find the cross-reference table and other special objects. 
     * Readers should read a PDF file from its end. 
     * The last line of the file should contain the end-of-file marker, %%EOF. 
     * The two preceding lines should be the keyword startxref and the byte offset of the cross-reference section from the start of the document.
     * The startxref line might be preceded by the trailer dictionary of the form:
     * trailer
     * <</key1 value1/key2 value2/key3 value3/key4 value4>>
     * startxref
     * byte-offset
     * %%EOF
     */

    internal class FileTrailerParser
    {
        private const int DefaultTrailerByteLength = 2048;

        private readonly byte[] endOfFileBytes;
        private readonly byte[] startXRefBytes;

        public FileTrailerParser()
        {
            endOfFileBytes = "%%EOF".Select(x => (byte)x).ToArray();
            startXRefBytes = "startxref".Select(x => (byte)x).ToArray();
        }

        public long GetXrefOffset(IRandomAccessRead reader, bool isLenientParsing)
        {
            var startXrefOffset = GetByteOffsetForStartXref(reader, (int)reader.Length(), isLenientParsing);

            reader.Seek(startXrefOffset);

            long actualXrefOffset = Math.Max(0, ParseXrefStartPosition(reader));

            return actualXrefOffset;
        }

        private long ParseXrefStartPosition(IRandomAccessRead reader)
        {
            long startXref = -1;

            if (ReadHelper.IsString(reader, startXRefBytes))
            {
                ReadHelper.ReadString(reader);

                ReadHelper.SkipSpaces(reader);

                // This integer is the byte offset of the first object referenced by the xref or xref stream
                startXref = ReadHelper.ReadLong(reader);
            }
            return startXref;
        }

        private long GetByteOffsetForStartXref(IRandomAccessRead reader, int fileLength, bool isLenientParsing)
        {
            byte[] buf;
            long skipBytes;
            // read trailing bytes into buffer
            try
            {
                var trailByteCount = fileLength < DefaultTrailerByteLength ? fileLength : DefaultTrailerByteLength;
                buf = new byte[trailByteCount];

                skipBytes = fileLength - trailByteCount;

                reader.Seek(skipBytes);
                int off = 0;
                while (off < trailByteCount)
                {
                    var readBytes = reader.Read(buf, off, trailByteCount - off);

                    // in order to not get stuck in a loop we check readBytes (this should never happen)
                    if (readBytes < 1)
                    {
                        throw new InvalidOperationException(
                                "No more bytes to read for trailing buffer, but expected: "
                                        + (trailByteCount - off));
                    }

                    off += readBytes;
                }
            }
            finally
            {
                reader.ReturnToBeginning();
            }

            // find last '%%EOF'
            int bufOff = LastIndexOf(endOfFileBytes, buf, buf.Length);
            if (bufOff < 0)
            {
                if (isLenientParsing)
                {
                    // in lenient mode the '%%EOF' isn't needed
                    bufOff = buf.Length;
                    //LOG.debug("Missing end of file marker '" + new String(EOF_MARKER) + "'");
                }
                else
                {
                    throw new InvalidOperationException("Missing end of file marker '%%EOF'");
                }
            }
            // find last startxref preceding EOF marker
            bufOff = LastIndexOf(startXRefBytes, buf, bufOff);
            long startXRefOffset = skipBytes + bufOff;

            if (bufOff < 0)
            {
                throw new NotImplementedException();
                //if (isLenientParsing)
                //{
                //    //LOG.debug("Performing brute force search for last startxref entry");
                //    long bfOffset = bfSearchForLastStartxrefEntry();
                //    bool offsetIsValid = false;
                //    if (bfOffset > -1)
                //    {
                //        reader.Seek(bfOffset);
                //        long bfXref = ParseXrefStartPosition();
                //        if (bfXref > -1)
                //        {
                //            offsetIsValid = checkXRefOffset(bfXref) == bfXref;
                //        }
                //    }

                //    reader.ReturnToBeginning();

                //    // use the new offset only if it is a valid pointer to a xref table
                //    return offsetIsValid ? bfOffset : -1;
                //}

                throw new InvalidOperationException("Missing 'startxref' marker.");
            }

            return startXRefOffset;
        }

        private int LastIndexOf(byte[] pattern, byte[] bytes, int endOff)
        {
            int lastPatternByte = pattern.Length - 1;

            int bufferOffset = endOff;
            int patternByte = lastPatternByte;
            byte targetByte = pattern[patternByte];

            while (--bufferOffset >= 0)
            {
                if (bytes[bufferOffset] == targetByte)
                {
                    if (--patternByte < 0)
                    {
                        // whole pattern matched
                        return bufferOffset;
                    }
                    // matched current byte, advance to preceding one
                    targetByte = pattern[patternByte];
                }
                else if (patternByte < lastPatternByte)
                {
                    // no byte match but already matched some chars; reset
                    patternByte = lastPatternByte;
                    targetByte = pattern[patternByte];
                }
            }

            return -1;
        }
    }
}
