namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Linq;
    using Core;
    using Graphics.Operations;
    using Tokens;
    using Util;

    /// <summary>
    /// Writes any type of <see cref="IToken"/> to the corresponding PDF document format output.
    /// </summary>
    public class TokenWriter
    {
        private static readonly byte Backslash = GetByte("\\");

        private static readonly byte ArrayStart = GetByte("[");
        private static readonly byte ArrayEnd = GetByte("]");

        private static readonly byte[] DictionaryStart = OtherEncodings.StringAsLatin1Bytes("<<");
        private static readonly byte[] DictionaryEnd = OtherEncodings.StringAsLatin1Bytes(">>");

        private static readonly byte Comment = GetByte("%");

        private static readonly byte[] Eof = OtherEncodings.StringAsLatin1Bytes("%%EOF");

        private static readonly byte[] FalseBytes = OtherEncodings.StringAsLatin1Bytes("false");

        private static readonly byte HexStart = GetByte("<");
        private static readonly byte HexEnd = GetByte(">");

        private static readonly byte InUseEntry = GetByte("n");

        private static readonly byte NameStart = GetByte("/");

        private static readonly byte[] Null = OtherEncodings.StringAsLatin1Bytes("null");

        private static readonly byte[] ObjStart = OtherEncodings.StringAsLatin1Bytes("obj");
        private static readonly byte[] ObjEnd = OtherEncodings.StringAsLatin1Bytes("endobj");

        private static readonly byte RByte = GetByte("R");

        private static readonly byte[] StartXref = OtherEncodings.StringAsLatin1Bytes("startxref");

        private static readonly byte[] StreamStart = OtherEncodings.StringAsLatin1Bytes("stream");
        private static readonly byte[] StreamEnd = OtherEncodings.StringAsLatin1Bytes("endstream");

        private static readonly byte StringStart = GetByte("(");
        private static readonly byte StringEnd = GetByte(")");

        private static readonly byte[] Trailer = OtherEncodings.StringAsLatin1Bytes("trailer");

        private static readonly byte[] TrueBytes = OtherEncodings.StringAsLatin1Bytes("true");

        private static readonly byte Whitespace = GetByte(" ");

        private static readonly byte[] Xref = OtherEncodings.StringAsLatin1Bytes("xref");

        private static readonly HashSet<char> DelimiterChars = new HashSet<char>
        {
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
        };

        /// <summary>
        /// Writes the given input token to the output stream with the correct PDF format and encoding including whitespace and line breaks as applicable.
        /// </summary>
        /// <param name="token">The token to write to the stream.</param>
        /// <param name="outputStream">The stream to write the token to.</param>
        public static void WriteToken(IToken token, Stream outputStream)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
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
                    outputStream.Write(Null, 0, Null.Length);
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

        /// <summary>
        /// Writes a valid single section cross-reference (xref) table plus trailer dictionary to the output for the set of object offsets.
        /// </summary>
        /// <param name="objectOffsets">The byte offset from the start of the document for each object in the document.</param>
        /// <param name="catalogToken">The object representing the catalog dictionary which is referenced from the trailer dictionary.</param>
        /// <param name="outputStream">The output stream to write to.</param>
        /// <param name="documentInformationReference">The object reference for the document information dictionary if present.</param>
        internal static void WriteCrossReferenceTable(IReadOnlyDictionary<IndirectReference, long> objectOffsets,
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
            outputStream.Write(Xref, 0, Xref.Length);
            WriteLineBreak(outputStream);

            var min = objectOffsets.Min(x => x.Key.ObjectNumber);
            var max = objectOffsets.Max(x => x.Key.ObjectNumber);

            if (max - min != objectOffsets.Count - 1)
            {
                throw new NotSupportedException("Object numbers must form a contiguous range");
            }

            WriteLong(0, outputStream);
            WriteWhitespace(outputStream);
            // 1 extra for the free entry.
            WriteLong(objectOffsets.Count + 1, outputStream);
            WriteWhitespace(outputStream);
            WriteLineBreak(outputStream);

            WriteFirstXrefEmptyEntry(outputStream);

            foreach (var keyValuePair in objectOffsets.OrderBy(x => x.Key.ObjectNumber))
            {
                /*
                 * nnnnnnnnnn ggggg n eol
                 * where:
                 * nnnnnnnnnn is a 10-digit byte offset
                 * ggggg is a 5-digit generation number
                 * n is a literal keyword identifying this as an in-use entry
                 * eol is a 2-character end-of-line sequence ('\r\n' or ' \n')
                 */
                var paddedOffset = OtherEncodings.StringAsLatin1Bytes(keyValuePair.Value.ToString("D10"));
                outputStream.Write(paddedOffset, 0, paddedOffset.Length);

                WriteWhitespace(outputStream);

                var generation = OtherEncodings.StringAsLatin1Bytes(keyValuePair.Key.Generation.ToString("D5"));
                outputStream.Write(generation, 0, generation.Length);

                WriteWhitespace(outputStream);

                outputStream.WriteByte(InUseEntry);

                WriteWhitespace(outputStream);
                WriteLineBreak(outputStream);
            }

            outputStream.Write(Trailer, 0, Trailer.Length);
            WriteLineBreak(outputStream);

            var identifier = new ArrayToken(new IToken[]
            {
                new HexToken(Guid.NewGuid().ToString("N").ToCharArray()),
                new HexToken(Guid.NewGuid().ToString("N").ToCharArray())
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

            outputStream.Write(StartXref, 0, StartXref.Length);
            WriteLineBreak(outputStream);

            WriteLong(position, outputStream);
            WriteLineBreak(outputStream);

            // Complete!
            outputStream.Write(Eof, 0, Eof.Length);
        }

        /// <summary>
        /// Writes pre-serialized token as an object token to the output stream.
        /// </summary>
        /// <param name="objectNumber">Object number of the indirect object.</param>
        /// <param name="generation">Generation of the indirect object.</param>
        /// <param name="data">Pre-serialized object contents.</param>
        /// <param name="outputStream">The stream to write the token to.</param>
        internal static void WriteObject(long objectNumber, int generation, byte[] data, Stream outputStream)
        {
            WriteLong(objectNumber, outputStream);
            WriteWhitespace(outputStream);

            WriteInt(generation, outputStream);
            WriteWhitespace(outputStream);

            outputStream.Write(ObjStart, 0, ObjStart.Length);
            WriteLineBreak(outputStream);

            outputStream.Write(data, 0, data.Length);

            WriteLineBreak(outputStream);
            outputStream.Write(ObjEnd, 0, ObjEnd.Length);

            WriteLineBreak(outputStream);
        }

        private static void WriteHex(HexToken hex, Stream stream)
        {
            stream.WriteByte(HexStart);
            stream.WriteText(hex.GetHexString());
            stream.WriteByte(HexEnd);
        }

        private static void WriteArray(ArrayToken array, Stream outputStream)
        {
            outputStream.WriteByte(ArrayStart);
            WriteWhitespace(outputStream);

            for (var i = 0; i < array.Data.Count; i++)
            {
                var value = array.Data[i];
                WriteToken(value, outputStream);
            }

            outputStream.WriteByte(ArrayEnd);
            WriteWhitespace(outputStream);
        }

        private static void WriteBoolean(BooleanToken boolean, Stream outputStream)
        {
            var bytes = boolean.Data ? TrueBytes : FalseBytes;
            outputStream.Write(bytes, 0, bytes.Length);
            WriteWhitespace(outputStream);
        }

        private static void WriteComment(CommentToken comment, Stream outputStream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(comment.Data);
            outputStream.WriteByte(Comment);
            outputStream.Write(bytes, 0, bytes.Length);
            WriteLineBreak(outputStream);
        }

        private static void WriteDictionary(DictionaryToken dictionary, Stream outputStream)
        {
            outputStream.Write(DictionaryStart, 0, DictionaryStart.Length);

            foreach (var pair in dictionary.Data)
            {
                WriteName(pair.Key, outputStream);

                // handle scenario where PdfPig has a null value under some circumstances
                if (pair.Value == null)
                {
                    WriteToken(NullToken.Instance, outputStream);
                }
                else
                {
                    WriteToken(pair.Value, outputStream);
                }
            }

            outputStream.Write(DictionaryEnd, 0, DictionaryEnd.Length);
        }

        private static void WriteIndirectReference(IndirectReferenceToken reference, Stream outputStream)
        {
            WriteLong(reference.Data.ObjectNumber, outputStream);
            WriteWhitespace(outputStream);

            WriteInt(reference.Data.Generation, outputStream);
            WriteWhitespace(outputStream);

            outputStream.WriteByte(RByte);
            WriteWhitespace(outputStream);
        }

        private static void WriteName(NameToken name, Stream outputStream)
        {
            WriteName(name.Data, outputStream);
        }

        private static void WriteName(string name, Stream outputStream)
        {
            /*
             * Beginning with PDF 1.2, any character except null (character code 0) may be
             * included in a name by writing its 2-digit hexadecimal code, preceded by the number sign character (#).
             * This is required for delimiter and whitespace characters.
             * This is recommended for characters whose codes are outside the range 33 (!) to 126 (~).
             */

            var sb = new StringBuilder();

            foreach (var c in name)
            {
                if (c < 33 || c > 126 || DelimiterChars.Contains(c))
                {
                    var str = Hex.GetString(new[] { (byte)c });
                    sb.Append('#').Append(str);
                }
                else
                {
                    sb.Append(c);
                }
            }

            var bytes = OtherEncodings.StringAsLatin1Bytes(sb.ToString());

            outputStream.WriteByte(NameStart);
            outputStream.Write(bytes, 0, bytes.Length);
            WriteWhitespace(outputStream);
        }

        private static void WriteNumber(NumericToken number, Stream outputStream)
        {
            if (!number.HasDecimalPlaces)
            {
                WriteInt(number.Int, outputStream);
            }
            else
            {
                var bytes = OtherEncodings.StringAsLatin1Bytes(number.Data.ToString("G", CultureInfo.InvariantCulture));
                outputStream.Write(bytes, 0, bytes.Length);
            }

            WriteWhitespace(outputStream);
        }

        private static void WriteObject(ObjectToken objectToken, Stream outputStream)
        {
            WriteLong(objectToken.Number.ObjectNumber, outputStream);
            WriteWhitespace(outputStream);

            WriteInt(objectToken.Number.Generation, outputStream);
            WriteWhitespace(outputStream);

            outputStream.Write(ObjStart, 0, ObjStart.Length);
            WriteLineBreak(outputStream);

            WriteToken(objectToken.Data, outputStream);

            WriteLineBreak(outputStream);
            outputStream.Write(ObjEnd, 0, ObjEnd.Length);

            WriteLineBreak(outputStream);
        }

        private static void WriteStream(StreamToken streamToken, Stream outputStream)
        {
            WriteDictionary(streamToken.StreamDictionary, outputStream);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamStart, 0, StreamStart.Length);
            WriteLineBreak(outputStream);
            outputStream.Write(streamToken.Data.ToArray(), 0, streamToken.Data.Count);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamEnd, 0, StreamEnd.Length);
        }

        private static int[] EscapeNeeded = new int[]
        {
            '\r', '\n', '\t', '\b', '\f', '\\'
        };
        private static int[] Escaped = new int[]
        {
            'r', 'n', 't', 'b', 'f', '\\'
        };
        private static void WriteString(StringToken stringToken, Stream outputStream)
        {
            outputStream.WriteByte(StringStart);

            if (stringToken.EncodedWith == StringToken.Encoding.Iso88591)
            {
                // iso 88591 (or really PdfDocEncoding in non-contentstream circumstances shouldn't
                // have these chars but seems like internally this isn't obeyed (see:
                // CanCreateDocumentInformationDictionaryWithNonAsciiCharacters test) and it may
                // happen during parsing as well -> switch to unicode
                if (stringToken.Data.Any(x => x > 255))
                {
                    var data = new StringToken(stringToken.Data, StringToken.Encoding.Utf16BE).GetBytes();
                    outputStream.Write(data, 0, data.Length);
                }
                else
                {
                    int ei;
                    for (var i = 0; i < stringToken.Data.Length; i++)
                    {
                        var c = (int)stringToken.Data[i];
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
            }
            else
            {
                var bytes = stringToken.GetBytes();
                outputStream.Write(bytes, 0, bytes.Length);
            }

            outputStream.WriteByte(StringEnd);
            WriteWhitespace(outputStream);
        }

        private static void WriteInt(int value, Stream outputStream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(value.ToString("G", CultureInfo.InvariantCulture));
            outputStream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteLineBreak(Stream outputStream)
        {
            outputStream.WriteNewLine();
        }

        private static void WriteLong(long value, Stream outputStream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(value.ToString("G", CultureInfo.InvariantCulture));
            outputStream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteWhitespace(Stream outputStream)
        {
            outputStream.WriteByte(Whitespace);
        }

        private static void WriteFirstXrefEmptyEntry(Stream outputStream)
        {
            /*
             *  The first entry in the table (object number 0) is always free and has a generation number of 65,535;
             * it is the head of the linked list of free objects. 
             */

            outputStream.WriteText(new string('0', 10));
            outputStream.WriteWhiteSpace();
            outputStream.WriteText("65535");
            outputStream.WriteWhiteSpace();
            outputStream.WriteText("f");
            outputStream.WriteWhiteSpace();
            outputStream.WriteNewLine();
        }

        private static byte GetByte(string value)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(value);

            if (bytes.Length > 1)
            {
                throw new InvalidOperationException();
            }

            return bytes[0];
        }
    }
}

