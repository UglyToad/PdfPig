namespace UglyToad.PdfPig.Writer
{
    using System.Buffers;
    using System.Buffers.Text;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Core;
    using Graphics.Operations;
    using Tokens;
    using Util;

    /// <summary>
    /// Writes any type of <see cref="IToken"/> to the corresponding PDF document format output.
    /// </summary>
    public class TokenWriter : ITokenWriter
    {
        private const byte ArrayStart = (byte)'[';
        private const byte ArrayEnd = (byte)']';

        private static ReadOnlySpan<byte> DictionaryStart => "<<"u8;
        private static ReadOnlySpan<byte> DictionaryEnd => ">>"u8;

        private const byte Comment = (byte)'%';

        private static ReadOnlySpan<byte> Eof => "%%EOF"u8;

        private static ReadOnlySpan<byte> FalseBytes => "false"u8;

        private static readonly byte HexStart = (byte)'<';
        private static readonly byte HexEnd = (byte)'>';

        private const byte InUseEntry = (byte)'n';

        private const byte NameStart = (byte)'/';

        private static ReadOnlySpan<byte> Null => "null"u8;

        private static ReadOnlySpan<byte> ObjStart => "obj"u8;
        private static ReadOnlySpan<byte> ObjEnd => "endobj"u8;

        private const byte RByte = (byte)'R';

        private static ReadOnlySpan<byte> StartXref => "startxref"u8;

        /// <summary>
        /// Bytes that indicate start of stream
        /// </summary>
        protected static ReadOnlySpan<byte> StreamStart => "stream"u8;

        /// <summary>
        /// Bytes that indicate end start of stream
        /// </summary>
        protected static ReadOnlySpan<byte> StreamEnd => "endstream"u8;

        private const byte StringStart = (byte)'(';

        private const byte StringEnd = (byte)')';

        private static ReadOnlySpan<byte> Trailer => "trailer"u8;

        private static ReadOnlySpan<byte> TrueBytes => "true"u8;

        private const byte Whitespace = (byte)' ';

        private static ReadOnlySpan<byte> Xref => "xref"u8;

        private static readonly HashSet<char> DelimiterChars = [
            '(',
            ')',
            '<',
            '>',
            '[',
            ']',
            '{',
            '}',
            '/',
            '%'
        ];

        /// <summary>
        /// Single global instance
        /// </summary>
        public static TokenWriter Instance { get; } = new TokenWriter();

        /// <summary>
        /// Writes the given input token to the output stream with the correct PDF format and encoding including whitespace and line breaks as applicable.
        /// </summary>
        /// <param name="token">The token to write to the stream.</param>
        /// <param name="outputStream">The stream to write the token to.</param>
        public void WriteToken(IToken token, Stream outputStream)
        {
            if (token is null)
            {
                WriteNullToken(outputStream);
                return;
            }

            switch (token)
            {
                case ArrayToken array:
                    WriteArray(array, outputStream);
                    break;
                case BooleanToken boolean:
                    WriteBoolean(boolean, outputStream);
                    break;
                case CommentToken comment:
                    WriteComment(comment, outputStream);
                    break;
                case DictionaryToken dictionary:
                    WriteDictionary(dictionary, outputStream);
                    break;
                case HexToken hex:
                    WriteHex(hex, outputStream);
                    break;
                case IndirectReferenceToken reference:
                    WriteIndirectReference(reference, outputStream);
                    break;
                case NameToken name:
                    WriteName(name, outputStream);
                    break;
                case NullToken _:
                    outputStream.Write(Null);
                    WriteWhitespace(outputStream);
                    break;
                case NumericToken number:
                    WriteNumber(number, outputStream);
                    break;
                case ObjectToken objectToken:
                    WriteObject(objectToken, outputStream);
                    break;
                case StreamToken streamToken:
                    WriteStream(streamToken, outputStream);
                    break;
                case StringToken stringToken:
                    WriteString(stringToken, outputStream);
                    break;
                default:
                    throw new PdfDocumentFormatException($"Attempted to write token type of {token.GetType()} but was not known.");
            }
        }

        /// <inheritdoc cref="ITokenWriter.WriteCrossReferenceTable" />
        public void WriteCrossReferenceTable(IReadOnlyDictionary<IndirectReference, long> objectOffsets,
            IndirectReference catalogToken,
            Stream outputStream,
            IndirectReference? documentInformationReference)
        {
            if (objectOffsets.Count == 0)
            {
                throw new InvalidOperationException("Could not write empty cross reference table.");
            }

            WriteLineBreak(outputStream);
            var position = outputStream.Position;
            outputStream.Write(Xref);
            WriteLineBreak(outputStream);

            var sets = new List<XrefSeries>();

            var orderedList = objectOffsets.OrderBy(x => x.Key.ObjectNumber).ToList();

            long firstObjectNumber = 0;
            long currentObjNum = 0;
            var items = new List<XrefSeries.OffsetAndGeneration?>
            {
                // Zero entry
                null
            };

            foreach (var item in orderedList)
            {
                var step = item.Key.ObjectNumber - currentObjNum;
                if (step == 1)
                {
                    currentObjNum = item.Key.ObjectNumber;
                    items.Add(new XrefSeries.OffsetAndGeneration(item.Value, item.Key.Generation));
                }
                else
                {
                    sets.Add(new XrefSeries(firstObjectNumber, items));
                    items = new List<XrefSeries.OffsetAndGeneration?>
                    {
                        new XrefSeries.OffsetAndGeneration(item.Value, item.Key.Generation)
                    };

                    currentObjNum = item.Key.ObjectNumber;
                    firstObjectNumber = item.Key.ObjectNumber;
                }
            }

            if (items.Count > 0)
            {
                sets.Add(new XrefSeries(firstObjectNumber, items));
            }

            foreach (var series in sets)
            {
                WriteLong(series.First, outputStream);
                WriteWhitespace(outputStream);

                WriteLong(series.Offsets.Count, outputStream);

                WriteWhitespace(outputStream);
                WriteLineBreak(outputStream);

                foreach (var offset in series.Offsets)
                {
                    if (offset != null)
                    {
                        /*
                     * nnnnnnnnnn ggggg n eol
                     * where:
                     * nnnnnnnnnn is a 10-digit byte offset
                     * ggggg is a 5-digit generation number
                     * n is a literal keyword identifying this as an in-use entry
                     * eol is a 2-character end-of-line sequence ('\r\n' or ' \n')
                     */
                        var paddedOffset = Encoding.ASCII.GetBytes(offset.Offset.ToString("D10", CultureInfo.InvariantCulture));
                        outputStream.Write(paddedOffset);
                        outputStream.WriteWhiteSpace();

                        var generation = Encoding.ASCII.GetBytes(offset.Generation.ToString("D5", CultureInfo.InvariantCulture));
                        outputStream.Write(generation);

                        WriteWhitespace(outputStream);

                        outputStream.WriteByte(InUseEntry);

                        WriteWhitespace(outputStream);
                        WriteLineBreak(outputStream);

                    }
                    else
                    {
                        WriteFirstXrefEmptyEntry(outputStream);
                    }
                }
            }

            outputStream.Write(Trailer);
            WriteLineBreak(outputStream);

            var identifier = new ArrayToken(new IToken[]
            {
                new HexToken(Guid.NewGuid().ToString("N").AsSpan()),
                new HexToken(Guid.NewGuid().ToString("N").AsSpan())
            });

            var trailerDictionaryData = new Dictionary<NameToken, IToken>
            {
                // 1 for the free entry.
                {NameToken.Size, new NumericToken(objectOffsets.Count + 1)},
                {NameToken.Root, new IndirectReferenceToken(catalogToken)},
                {NameToken.Id, identifier}
            };

            if (documentInformationReference.HasValue)
            {
                trailerDictionaryData[NameToken.Info] = new IndirectReferenceToken(documentInformationReference.Value);
            }

            var trailerDictionary = new DictionaryToken(trailerDictionaryData);

            WriteDictionary(trailerDictionary, outputStream);
            WriteLineBreak(outputStream);

            outputStream.Write(StartXref);
            WriteLineBreak(outputStream);

            WriteLong(position, outputStream);
            WriteLineBreak(outputStream);

            // Complete!
            outputStream.Write(Eof);
        }

        /// <summary>
        /// Indicates that we are writing page contents.
        /// Can be used by a derived class.
        /// </summary>
        public bool WritingPageContents { get; set; }

        /// <inheritdoc cref="ITokenWriter.WriteObject" />
        public void WriteObject(long objectNumber, int generation, byte[] data, Stream outputStream)
        {
            WriteLong(objectNumber, outputStream);
            outputStream.WriteWhiteSpace();

            WriteInt(generation, outputStream);
            outputStream.WriteWhiteSpace();

            outputStream.Write(ObjStart);
            WriteLineBreak(outputStream);

            outputStream.Write(data, 0, data.Length);

            WriteLineBreak(outputStream);
            outputStream.Write(ObjEnd);

            WriteLineBreak(outputStream);
        }

        /// <summary>
        /// Write a hex value to the output stream
        /// </summary>
        protected void WriteHex(HexToken hex, Stream stream)
        {
            stream.WriteByte(HexStart);
            stream.WriteText(hex.GetHexString());
            stream.WriteByte(HexEnd);
        }

        /// <summary>
        /// Write an array to the output stream, with whitespace at the end.
        /// </summary>
        protected void WriteArray(ArrayToken array, Stream outputStream)
        {
            outputStream.WriteByte(ArrayStart);
            outputStream.WriteWhiteSpace();

            for (var i = 0; i < array.Data.Count; i++)
            {
                var value = array.Data[i];
                WriteToken(value, outputStream);
            }

            outputStream.WriteByte(ArrayEnd);
            outputStream.WriteWhiteSpace();
        }

        /// <summary>
        /// Write a boolean "true" or "false" to the output stream, with whitespace at the end.
        /// </summary>
        protected void WriteBoolean(BooleanToken boolean, Stream outputStream)
        {
            var bytes = boolean.Data ? TrueBytes : FalseBytes;
            outputStream.Write(bytes);
            outputStream.WriteWhiteSpace();
        }

        /// <summary>
        /// Write a "%comment" in the output stream, with a line break at the end.
        /// </summary>
        protected void WriteComment(CommentToken comment, Stream outputStream)
        {
            outputStream.WriteByte(Comment);
            outputStream.WriteText(comment.Data);
            WriteLineBreak(outputStream);
        }

        /// <summary>
        /// Write "null" in the output stream with a whitespace at the end.
        /// </summary>
        protected void WriteNullToken(Stream outputStream)
        {
            outputStream.Write("null"u8);
            outputStream.WriteWhiteSpace();
        }

        /// <summary>
        /// Writes dictionary key/value pairs to output stream as Name/Token pairs.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="outputStream"></param>
        protected void WriteDictionary(DictionaryToken dictionary, Stream outputStream)
        {
            outputStream.Write(DictionaryStart);

            foreach (var pair in dictionary.Data)
            {
                WriteName(pair.Key, outputStream);

                // handle scenario where PdfPig has a null value under some circumstances
                if (pair.Value is null)
                {
                    WriteToken(NullToken.Instance, outputStream);
                }
                else
                {
                    WriteToken(pair.Value, outputStream);
                }
            }

            outputStream.Write(DictionaryEnd);
        }

        /// <summary>
        /// Write an indirect reference to the stream, with whitespace at the end. 
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="outputStream"></param>
        protected virtual void WriteIndirectReference(IndirectReferenceToken reference, Stream outputStream)
        {
            WriteLong(reference.Data.ObjectNumber, outputStream);
            outputStream.WriteWhiteSpace();

            WriteInt(reference.Data.Generation, outputStream);
            outputStream.WriteWhiteSpace();

            outputStream.WriteByte(RByte);
            outputStream.WriteWhiteSpace();
        }

        /// <summary>
        /// Write a name to the stream, with whitespace at the end.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="outputStream"></param>
        protected virtual void WriteName(NameToken name, Stream outputStream)
        {
            WriteName(name.Data, outputStream);
        }

        private void WriteName(string name, Stream outputStream)
        {
            /*
             * Beginning with PDF 1.2, any character except null (character code 0) may be
             * included in a name by writing its 2-digit hexadecimal code, preceded by the number sign character (#).
             * This is required for delimiter and whitespace characters.
             * This is recommended for characters whose codes are outside the range 33 (!) to 126 (~).
             */

            using var sb = new ArrayPoolBufferWriter<byte>((name.Length * 2) + 1);

            Span<byte> hexBuffer = stackalloc byte[2];

            foreach (var c in name)
            {
                if (c < 33 || c > 126 || DelimiterChars.Contains(c))
                {
                    Hex.GetUtf8Chars([(byte)c], hexBuffer);

                    sb.Write((byte)'#');
                    sb.Write(hexBuffer);
                }
                else
                {
                    sb.Write((byte)c); // between 33 and 126 (ASCII is 0 - 128)
                }
            }

            outputStream.WriteByte(NameStart);
            outputStream.Write(sb.WrittenSpan);
            outputStream.WriteWhiteSpace();
        }

        /// <summary>
        /// Write a number to the stream, with whitespace at the end. 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="outputStream"></param>
        protected virtual void WriteNumber(NumericToken number, Stream outputStream)
        {
            if (!number.HasDecimalPlaces)
            {
                WriteInt(number.Int, outputStream);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[32]; // matches dotnet Number.CharStackBufferSize

                Utf8Formatter.TryFormat(number.Data, buffer, out int bytesWritten);

                outputStream.Write(buffer.Slice(0, bytesWritten));
            }

            WriteWhitespace(outputStream);
        }

        /// <summary>
        /// Write an object to the stream, with a line break at the end. It writes the following contents:
        /// - "[ObjectNumber] [Generation] obj"
        /// - Object data
        /// - "endobj"
        /// </summary>
        /// <param name="objectToken"></param>
        /// <param name="outputStream"></param>
        protected virtual void WriteObject(ObjectToken objectToken, Stream outputStream)
        {
            WriteLong(objectToken.Number.ObjectNumber, outputStream);
            outputStream.WriteWhiteSpace();

            WriteInt(objectToken.Number.Generation, outputStream);
            outputStream.WriteWhiteSpace();

            outputStream.Write(ObjStart);
            WriteLineBreak(outputStream);

            WriteToken(objectToken.Data, outputStream);

            WriteLineBreak(outputStream);
            outputStream.Write(ObjEnd);

            WriteLineBreak(outputStream);
        }

        /// <summary>
        /// Write a stream token to the output stream, with the following contents:
        /// - Dictionary specifying the length of the stream, any applied compression filters and additional information.
        /// - Stream start indicator
        /// - Bytes in the StreamToken data
        /// - Stream end indicator
        /// </summary>
        /// <param name="streamToken"></param>
        /// <param name="outputStream"></param>
        protected virtual void WriteStream(StreamToken streamToken, Stream outputStream)
        {
            WriteDictionary(streamToken.StreamDictionary, outputStream);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamStart);
            WriteLineBreak(outputStream);
            outputStream.Write(streamToken.Data.Span);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamEnd);
        }

        private static readonly int[] EscapeNeeded =
        [
            '\r', '\n', '\t', '\b', '\f', '\\'
        ];

        private static readonly int[] Escaped =
        [
            'r', 'n', 't', 'b', 'f', '\\'
        ];

        /// <summary>
        /// Write string to the stream, with whitespace at the end
        /// </summary>
        protected virtual void WriteString(StringToken stringToken, Stream outputStream)
        {
            outputStream.WriteByte(StringStart);

            if (stringToken.EncodedWith == StringToken.Encoding.Iso88591 || stringToken.EncodedWith == StringToken.Encoding.PdfDocEncoding)
            {
                // iso 88591 (or really PdfDocEncoding in non-contentstream circumstances shouldn't
                // have these chars but seems like internally this isn't obeyed (see:
                // CanCreateDocumentInformationDictionaryWithNonAsciiCharacters test) and it may
                // happen during parsing as well -> switch to unicode

                var data = stringToken.Data.ToCharArray();
                if (data.Any(x => x > 255))
                {
                    data = new StringToken(stringToken.Data, StringToken.Encoding.Utf16BE)
                        .GetBytes()
                        .Select(b => (char)b)
                        .ToArray();
                }

                int ei;
                for (var i = 0; i < data.Length; i++)
                {
                    var c = (int)data[i];
                    if (c == (int)'(' || c == (int)')') // wastes a little space if escaping not needed but better than forward searching
                    {
                        outputStream.WriteByte((byte)'\\');
                        outputStream.WriteByte((byte)c);
                    }
                    else if ((ei = Array.IndexOf(EscapeNeeded, c)) > -1)
                    {
                        outputStream.WriteByte((byte)'\\');
                        outputStream.WriteByte((byte)Escaped[ei]);
                    }
                    else if (c < 32 || c > 126) // non printable
                    {
                        var b3 = c / 64;
                        var b2 = (c - b3 * 64) / 8;
                        var b1 = c % 8;
                        outputStream.WriteByte((byte)'\\');
                        outputStream.WriteByte((byte)(b3 + '0'));
                        outputStream.WriteByte((byte)(b2 + '0'));
                        outputStream.WriteByte((byte)(b1 + '0'));
                    }
                    else
                    {
                        outputStream.WriteByte((byte)c);
                    }
                }
            }
            else
            {
                var bytes = stringToken.GetBytes();
                outputStream.Write(bytes);
            }

            outputStream.WriteByte(StringEnd);
            outputStream.WriteWhiteSpace();
        }

        /// <summary>
        /// Write an integer to the stream
        /// </summary>
        /// <param name="value"></param>
        /// <param name="outputStream"></param>
        protected virtual void WriteInt(int value, Stream outputStream)
        {
            Span<byte> buffer = stackalloc byte[10]; // max 10
            Utf8Formatter.TryFormat(value, buffer, out int byteWritten);
            outputStream.Write(buffer.Slice(0, byteWritten));
        }

        /// <summary>
        /// Write a line break to the output stream
        /// </summary>
        /// <param name="outputStream"></param>
        protected virtual void WriteLineBreak(Stream outputStream)
        {
            outputStream.WriteNewLine();
        }

        /// <summary>
        /// Write a long to the stream
        /// </summary>
        /// <param name="value"></param>
        /// <param name="outputStream"></param>
        protected virtual void WriteLong(long value, Stream outputStream)
        {
            Span<byte> buffer = stackalloc byte[20]; // max 20
            Utf8Formatter.TryFormat(value, buffer, out int byteWritten);
            outputStream.Write(buffer.Slice(0, byteWritten));
        }

        /// <summary>
        /// Write a space to the output stream
        /// </summary>
        /// <param name="outputStream"></param>
        protected virtual void WriteWhitespace(Stream outputStream)
        {
            outputStream.WriteByte(Whitespace);
        }

        private void WriteFirstXrefEmptyEntry(Stream outputStream)
        {
            /*
             *  The first entry in the table (object number 0) is always free and has a generation number of 65,535;
             * it is the head of the linked list of free objects. 
             */

            outputStream.WriteText(new string('0', 10));
            outputStream.WriteWhiteSpace();
            outputStream.WriteText("65535"u8);
            outputStream.WriteWhiteSpace();
            outputStream.WriteText("f"u8);
            outputStream.WriteWhiteSpace();
            outputStream.WriteNewLine();
        }

        private class XrefSeries
        {
            public long First { get; }

            public IReadOnlyList<OffsetAndGeneration?> Offsets { get; }

            public XrefSeries(long first, IReadOnlyList<OffsetAndGeneration?> offsets)
            {
                First = first;
                Offsets = offsets;
            }

            public class OffsetAndGeneration
            {
                public long Offset { get; }

                public long Generation { get; }

                public OffsetAndGeneration(long offset, long generation)
                {
                    Offset = offset;
                    Generation = generation;
                }
            }
        }
    }
}