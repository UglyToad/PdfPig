using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Writer
{
    /// <summary>
    /// This class would lazily flush all token. Allowing us to make changes to references without need to rewrite the whole stream
    /// </summary>
    internal class PdfStreamWriter : IDisposable
    {
        private readonly SortedSet<int> reservedNumbers = new SortedSet<int>();

        private readonly Dictionary<IndirectReferenceToken, IToken> tokenReferences = new Dictionary<IndirectReferenceToken, IToken>();

        public int CurrentNumber { get; private set; } = 1;

        public Stream Stream { get; }

        public PdfStreamWriter() : this(new MemoryStream()) { }

        public PdfStreamWriter(Stream baseStream)
        {
            Stream = baseStream;
        }
        
        public void Flush(decimal version, IndirectReferenceToken catalogReference)
        {
            if (catalogReference == null)
                throw new ArgumentNullException(nameof(catalogReference));

            WriteString($"%PDF-{version:0.0}", Stream);

            Stream.WriteText("%");
            Stream.WriteByte(169);
            Stream.WriteByte(205);
            Stream.WriteByte(196);
            Stream.WriteByte(210);
            Stream.WriteNewLine();

            var offsets = new Dictionary<IndirectReference, long>();
            ObjectToken catalogToken = null;
            foreach(var pair in tokenReferences)
            {
                var referenceToken = pair.Key;
                var token = pair.Value;
                var offset = Stream.Position;
                var obj = new ObjectToken(offset, referenceToken.Data, token);

                TokenWriter.WriteToken(obj, Stream);

                offsets.Add(referenceToken.Data, offset);

                if (catalogToken == null && referenceToken == catalogReference)
                {
                    catalogToken = new ObjectToken(offset, referenceToken.Data, token);
                }
            }

            if (catalogToken == null)
            {
                throw new Exception("Catalog object wasn't found");
            }

            // TODO: Support document information
            TokenWriter.WriteCrossReferenceTable(offsets, catalogToken, Stream, null);
        }

        public IndirectReferenceToken WriteObject(IToken token, int? reservedNumber = null)
        {
            // if you can't consider deduplicating a token. 
            // It must be because it's referenced by his child element, so you must have reserved a number before hand
            // Example /Pages Obj
            var canBeDuplicated = !reservedNumber.HasValue;
            if (!canBeDuplicated)
            {
                if (!reservedNumbers.Remove(reservedNumber.Value))
                {
                    throw new InvalidOperationException("You can't reuse a reserved number");
                }

                // When we end up writing this token, all of his child would already have been added and checked for duplicate
                return AddObject(token, reservedNumber.Value);
            }

            var reference = FindToken(token);
            if (reference == null)
            {
                // TODO: Check his children
                return AddObject(token, CurrentNumber++);
            }

            return reference;
        }

        private IndirectReferenceToken AddObject(IToken token, int reservedNumber)
        {
            var reference = new IndirectReference(reservedNumber, 0);
            var referenceToken = new IndirectReferenceToken(reference);
            tokenReferences.Add(referenceToken, token);
            return referenceToken;
        }

        private IndirectReferenceToken FindToken(IToken token)
        {
            foreach(var pair in tokenReferences)
            {
                var reference = pair.Key;
                var storedToken = pair.Value;
                if (storedToken.Equals(token))
                {
                    return reference;
                }
            }

            return null;
        }

        public int ReserveNumber()
        {
            var reserved = CurrentNumber;
            reservedNumbers.Add(reserved);
            CurrentNumber++;
            return reserved;
        }

        public IndirectReferenceToken ReserveNumberToken() => new IndirectReferenceToken(new IndirectReference(ReserveNumber(), 0));

        private static void WriteString(string text, Stream stream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteNewLine();
        }

        public byte[] ToArray()
        {
            if (!Stream.CanSeek)
                throw new NotSupportedException("Stream can't seek");
            
            var currentPosition = Stream.Position;
            Stream.Seek(0, SeekOrigin.Begin);

            var bytes = new byte[Stream.Length];

            // Should we slice the reading into smaller chunks?
            if (Stream.Read(bytes, 0, bytes.Length) != bytes.Length)
                throw new Exception("Unable to read all the bytes from stream");

            Stream.Seek(currentPosition, SeekOrigin.Begin);

            return bytes;
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
