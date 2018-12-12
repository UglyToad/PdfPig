namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Tokens;
    using Util;

    internal class TokenWriter
    {
        private static readonly byte ArrayStart = GetByte("[");
        private static readonly byte ArrayEnd = GetByte("]");

        private static readonly byte[] DictionaryStart = OtherEncodings.StringAsLatin1Bytes("<<");
        private static readonly byte[] DictionaryEnd = OtherEncodings.StringAsLatin1Bytes(">>");

        private static readonly byte Comment = GetByte("%");

        private static readonly byte EndOfLine = OtherEncodings.StringAsLatin1Bytes("\n")[0];

        private static readonly byte[] Eof = OtherEncodings.StringAsLatin1Bytes("%%EOF");

        private static readonly byte[] FalseBytes = OtherEncodings.StringAsLatin1Bytes("false");

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

        public static void WriteToken(IToken token, Stream outputStream)
        {
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
                case HexToken _:
                    throw new NotImplementedException();
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
            }
        }

        public static void WriteCrossReferenceTable(IReadOnlyDictionary<IndirectReference, long> objectOffsets, 
            ObjectToken catalogToken,
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

            WriteLong(min, outputStream);
            WriteWhitespace(outputStream);
            WriteLong(max, outputStream);
            WriteWhitespace(outputStream);
            WriteLineBreak(outputStream);

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

            var trailerDictionaryData = new Dictionary<NameToken, IToken>
            {
                {NameToken.Size, new NumericToken(objectOffsets.Count)},
                {NameToken.Root, new IndirectReferenceToken(catalogToken.Number)}
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
                WriteToken(pair.Value, outputStream);
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
            var bytes = OtherEncodings.StringAsLatin1Bytes(name);

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
                var bytes = OtherEncodings.StringAsLatin1Bytes(number.Data.ToString("G"));
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

        private static void WriteString(StringToken stringToken, Stream outputStream)
        {
            outputStream.WriteByte(StringStart);
            var bytes = OtherEncodings.StringAsLatin1Bytes(stringToken.Data);
            outputStream.Write(bytes, 0, bytes.Length);
            outputStream.WriteByte(StringEnd);

            WriteWhitespace(outputStream);
        }

        private static void WriteInt(int value, Stream outputStream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(value.ToString("G"));
            outputStream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteLineBreak(Stream outputStream)
        {
            outputStream.WriteByte(EndOfLine);
        }

        private static void WriteLong(long value, Stream outputStream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(value.ToString("G"));
            outputStream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteWhitespace(Stream outputStream)
        {
            outputStream.WriteByte(Whitespace);
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

