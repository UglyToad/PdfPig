namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using System.IO;
    using ContentStream;
    using Cos;
    using IO;
    using Logging;
    using Util;

    internal class CosStreamParser
    {
        private static readonly int STREAMCOPYBUFLEN = 8192;
        private static readonly int STRMBUFLEN = 2048;
        private static readonly byte[] ENDOBJ = 
        {
            (byte) 'E', (byte) 'N', (byte) 'D',
            (byte) 'O', (byte) 'B', (byte) 'J'
        };
        private static readonly byte[] ENDSTREAM = 
        {
            (byte) 'E', (byte) 'N', (byte) 'D',
            (byte) 'S', (byte) 'T', (byte) 'R', (byte) 'E', (byte) 'A', (byte) 'M'
        };

        private readonly ILog log;
        private readonly byte[] streamCopyBuf = new byte[STREAMCOPYBUFLEN];
        private readonly byte[] strmBuf = new byte[STRMBUFLEN];
        
        public CosStreamParser(ILog log)
        {
            this.log = log;
        }

        public PdfRawStream Parse(IRandomAccessRead reader, PdfDictionary streamDictionary, bool isLenientParsing, IPdfObjectParser parser)
        {
            PdfRawStream result;

            // read 'stream'; this was already tested in parseObjectsDynamically()
            ReadHelper.ReadExpectedString(reader, "stream");
            
            skipWhiteSpaces(reader);
            
             // This needs to be streamDictionary.getItem because when we are parsing, the underlying object might still be null.
            ICosNumber streamLength = GetLength(reader, streamDictionary.GetItemOrDefault(CosName.LENGTH), streamDictionary.GetName(CosName.TYPE), isLenientParsing, parser);

            ValidateStreamLength(reader, isLenientParsing, streamLength);

            // get output stream to copy data to
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                if (streamLength != null && validateStreamLength(reader, streamLength.AsLong(), reader.Length()))
                {
                    ReadValidStream(reader, writer, streamLength);
                }
                else
                {
                    ReadUntilEndStream(reader, writer);
                }

                result = new PdfRawStream(stream.ToArray(), streamDictionary);
            }

            String endStream = ReadHelper.ReadString(reader);
            if (endStream.Equals("endobj") && isLenientParsing)
            {
                log.Warn($"stream ends with \'endobj\' instead of \'endstream\' at offset {reader.GetPosition()}");

                // avoid follow-up warning about missing endobj
                reader.Rewind("endobj".Length);
            }
            else if (endStream.Length > 9 && isLenientParsing && endStream.Substring(0, 9).Equals("endstream"))
            {
                log.Warn("stream ends with '" + endStream + "' instead of 'endstream' at offset " + reader.GetPosition());
                // unread the "extra" bytes
                reader.Rewind(OtherEncodings.StringAsLatin1Bytes(endStream.Substring(9)).Length);
            }
            else if (!endStream.Equals("endstream"))
            {
                throw new InvalidOperationException("Error reading stream, expected='endstream' actual='"
                        + endStream + "' at offset " + reader.GetPosition());
            }
            
            return result;
        }

        private void ValidateStreamLength(IRandomAccessRead reader, bool isLenientParsing, ICosNumber streamLength)
        {
            if (streamLength != null)
            {
                return;
            }

            if (isLenientParsing)
            {
                log.Warn("The stream doesn't provide any stream length, using fallback readUntilEnd, at offset " +
                         reader.GetPosition());
            }
            else
            {
                throw new InvalidOperationException("Missing length for stream.");
            }
        }

        private ICosNumber GetLength(IRandomAccessRead source, CosBase lengthBaseObj, CosName streamType, bool isLenientParsing, IPdfObjectParser parser)
        {
            if (lengthBaseObj == null)
            {
                return null;
            }
            
            // Length is given directly in the stream dictionary
            if (lengthBaseObj is ICosNumber number)
            {
                return number;
            }

            // length in referenced object
            if (lengthBaseObj is CosObject lengthObj)
            {
                var currentObject = lengthObj.GetObject();

                if (currentObject == null)
                {
                    if (parser == null)
                    {
                        throw new InvalidOperationException("This method required access to the PDF object parser but it was not created yet. Figure out how to fix this.");
                    }

                    var currentOffset = source.GetPosition();

                    var obj = parser.Parse(lengthObj.ToIndirectReference(), source, isLenientParsing);

                    source.Seek(currentOffset);

                    if (obj is ICosNumber referenceNumber)
                    {
                        return referenceNumber;
                    }

                    throw new InvalidOperationException("Length object content was not read.");
                }

                if (currentObject is ICosNumber objectNumber)
                {
                    return objectNumber;
                }


                throw new InvalidOperationException("Wrong type of referenced length object " + lengthObj
                                                    + ": " + lengthObj.GetObject().GetType().Name);
            }

            throw new InvalidOperationException($"Wrong type of length object: {lengthBaseObj.GetType().Name}");
        }

        private void ReadValidStream(IRandomAccessRead reader, BinaryWriter output, ICosNumber streamLengthObj)
        {
            long remainBytes = streamLengthObj.AsLong();
            while (remainBytes > 0)
            {
                int chunk = (remainBytes > STREAMCOPYBUFLEN) ? STREAMCOPYBUFLEN : (int)remainBytes;
                int readBytes = reader.Read(streamCopyBuf, 0, chunk);
                if (readBytes <= 0)
                {
                    // shouldn't happen, the stream length has already been validated
                    throw new InvalidOperationException(
                        $"read error at offset {reader.GetPosition()}: expected {chunk} bytes, but read() returns {readBytes}");
                }
                output.Write(streamCopyBuf, 0, readBytes);
                remainBytes -= readBytes;
            }
        }

        protected void skipWhiteSpaces(IRandomAccessRead reader)
        {
            //PDF Ref 3.2.7 A stream must be followed by either
            //a CRLF or LF but nothing else.

            int whitespace = reader.Read();

            //see brother_scan_cover.pdf, it adds whitespaces
            //after the stream but before the start of the
            //data, so just read those first
            while (whitespace == ' ')
            {
                whitespace = reader.Read();
            }

            if (whitespace == ReadHelper.AsciiCarriageReturn)
            {
                whitespace = reader.Read();
                if (whitespace != ReadHelper.AsciiLineFeed)
                {
                    reader.Unread(whitespace);
                    //The spec says this is invalid but it happens in the real
                    //world so we must support it.
                }
            }
            else if (whitespace != ReadHelper.AsciiLineFeed)
            {
                //we are in an error.
                //but again we will do a lenient parsing and just assume that everything
                //is fine
                reader.Unread(whitespace);
            }
        }

        private bool validateStreamLength(IRandomAccessRead source, long streamLength, long fileLength)
        {
            bool streamLengthIsValid = true;
            long originOffset = source.GetPosition();
            long expectedEndOfStream = originOffset + streamLength;
            if (expectedEndOfStream > fileLength)
            {
                streamLengthIsValid = false;
                //LOG.warn("The end of the stream is out of range, using workaround to read the stream, "
                //        + "stream start position: " + originOffset + ", length: " + streamLength
                //        + ", expected end position: " + expectedEndOfStream);
            }
            else
            {
                source.Seek(expectedEndOfStream);
                ReadHelper.SkipSpaces(source);
                if (!ReadHelper.IsString(source, "endstream"))
                {
                    streamLengthIsValid = false;
                    //LOG.warn("The end of the stream doesn't point to the correct offset, using workaround to read the stream, "
                    //        + "stream start position: " + originOffset + ", length: " + streamLength
                    //        + ", expected end position: " + expectedEndOfStream);
                }
                source.Seek(originOffset);
            }
            return streamLengthIsValid;
        }
        
        private void ReadUntilEndStream(IRandomAccessRead source, BinaryWriter output)
        {
            int bufSize;
            int charMatchCount = 0;
            byte[] keyw = ENDSTREAM;

            // last character position of shortest keyword ('endobj')
            int quickTestOffset = 5;

            // read next chunk into buffer; already matched chars are added to beginning of buffer
            while ((bufSize = source.Read(strmBuf, charMatchCount, STRMBUFLEN - charMatchCount)) > 0)
            {
                bufSize += charMatchCount;

                int bIdx = charMatchCount;
                int quickTestIdx;

                // iterate over buffer, trying to find keyword match
                for (int maxQuicktestIdx = bufSize - quickTestOffset; bIdx < bufSize; bIdx++)
                {
                    // reduce compare operations by first test last character we would have to
                    // match if current one matches; if it is not a character from keywords
                    // we can move behind the test character; this shortcut is inspired by the 
                    // Boyer-Moore string search algorithm and can reduce parsing time by approx. 20%
                    quickTestIdx = bIdx + quickTestOffset;
                    if (charMatchCount == 0 && quickTestIdx < maxQuicktestIdx)
                    {
                        byte ch = strmBuf[quickTestIdx];
                        if ((ch > 't') || (ch < 'a'))
                        {
                            // last character we would have to match if current character would match
                            // is not a character from keywords -> jump behind and start over
                            bIdx = quickTestIdx;
                            continue;
                        }
                    }

                    // could be negative - but we only compare to ASCII
                    byte ch1 = strmBuf[bIdx];

                    if (ch1 == keyw[charMatchCount])
                    {
                        if (++charMatchCount == keyw.Length)
                        {
                            // match found
                            bIdx++;
                            break;
                        }
                    }
                    else
                    {
                        if ((charMatchCount == 3) && (ch1 == ENDOBJ[charMatchCount]))
                        {
                            // maybe ENDSTREAM is missing but we could have ENDOBJ
                            keyw = ENDOBJ;
                            charMatchCount++;
                        }
                        else
                        {
                            // no match; incrementing match start by 1 would be dumb since we already know 
                            // matched chars depending on current char read we may already have beginning 
                            // of a new match: 'e': first char matched; 'n': if we are at match position 
                            // idx 7 we already read 'e' thus 2 chars matched for each other char we have 
                            // to start matching first keyword char beginning with next read position
                            charMatchCount = (ch1 == 'e') ? 1 : ((ch1 == 'n') && (charMatchCount == 7)) ? 2 : 0;
                            // search again for 'endstream'
                            keyw = ENDSTREAM;
                        }
                    }
                }

                int contentBytes = Math.Max(0, bIdx - charMatchCount);

                // write buffer content until first matched char to output stream
                if (contentBytes > 0)
                {
                    output.Write(strmBuf, 0, contentBytes);
                }
                if (charMatchCount == keyw.Length)
                {
                    // keyword matched; unread matched keyword (endstream/endobj) and following buffered content
                    source.Rewind(bufSize - contentBytes);
                    break;
                }
                
                // copy matched chars at start of buffer
                Array.Copy(keyw, 0, strmBuf, 0, charMatchCount);
            }
            // this writes a lonely CR or drops trailing CR LF and LF
            // output.flush();
        }


    }
}
