namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Core;
    using Graphics.Operations;
    using Tokens;

    /// <summary>
    /// This class would lazily flush all token. Allowing us to make changes to references without need to rewrite the whole stream
    /// </summary>
    internal class PdfStreamWriter : IDisposable
    {
        private readonly List<int> reservedNumbers = new List<int>();

        private readonly Dictionary<IndirectReferenceToken, IToken> tokenReferences = new Dictionary<IndirectReferenceToken, IToken>();

        public int CurrentNumber { get; private set; } = 1;

        public Stream Stream { get; private set; }

        public bool DisposeStream { get; set; }

        public PdfStreamWriter(Stream baseStream, bool disposeStream = true)
        {
            Stream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanWrite)
            {
                throw new ArgumentException("Output stream must be writable");
            }

            DisposeStream = disposeStream;
        }

        public void Flush(decimal version, IndirectReferenceToken catalogReference)
        {
            if (catalogReference == null)
            {
                throw new ArgumentNullException(nameof(catalogReference));
            }

            WriteString($"%PDF-{version.ToString("0.0", CultureInfo.InvariantCulture)}", Stream);

            Stream.WriteText("%");
            Stream.WriteByte(169);
            Stream.WriteByte(205);
            Stream.WriteByte(196);
            Stream.WriteByte(210);
            Stream.WriteNewLine();

            var offsets = new Dictionary<IndirectReference, long>();
            ObjectToken catalogToken = null;
            foreach (var pair in tokenReferences)
            {
                var referenceToken = pair.Key;
                var token = pair.Value;
                var offset = Stream.Position;
                var obj = new ObjectToken(offset, referenceToken.Data, token);

                TokenWriter.WriteToken(obj, Stream);

                offsets.Add(referenceToken.Data, offset);

                if (catalogToken == null && referenceToken == catalogReference)
                {
                    catalogToken = obj;
                }
            }

            if (catalogToken == null)
            {
                throw new Exception("Catalog object wasn't found");
            }

            // TODO: Support document information
            TokenWriter.WriteCrossReferenceTable(offsets, catalogToken, Stream, null);
        }

        public IndirectReferenceToken WriteToken(IToken token, int? reservedNumber = null)
        {
            if (!reservedNumber.HasValue)
            {
                return AddToken(token, CurrentNumber++);
            }

            if (!reservedNumbers.Remove(reservedNumber.Value))
            {
                throw new InvalidOperationException("You can't reuse a reserved number");
            }

            // When we end up writing this token, all of his child would already have been added and checked for duplicate
            return AddToken(token, reservedNumber.Value);

        }

        public int ReserveNumber()
        {
            var reserved = CurrentNumber;
            reservedNumbers.Add(reserved);
            CurrentNumber++;
            return reserved;
        }

        public IndirectReferenceToken ReserveNumberToken()
        {
            return new IndirectReferenceToken(new IndirectReference(ReserveNumber(), 0));
        }

        public void Dispose()
        {
            if (!DisposeStream)
            {
                Stream = null;
                return;
            }

            Stream?.Dispose();
            Stream = null;
        }

        private IndirectReferenceToken AddToken(IToken token, int reservedNumber)
        {
            var reference = new IndirectReference(reservedNumber, 0);
            var referenceToken = new IndirectReferenceToken(reference);
            tokenReferences.Add(referenceToken, token);
            return referenceToken;
        }

        private static void WriteString(string text, Stream stream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteNewLine();
        }
    }
}
