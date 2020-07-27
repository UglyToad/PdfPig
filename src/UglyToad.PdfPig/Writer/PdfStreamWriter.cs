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
    public class PdfStreamWriter : IDisposable
    {
        private readonly List<int> reservedNumbers = new List<int>();

        private readonly Dictionary<IndirectReferenceToken, IToken> tokenReferences = new Dictionary<IndirectReferenceToken, IToken>();

        private int currentNumber = 1;

        private Stream stream;

        /// <summary>
        /// Flag to set whether or not we want to dispose the stream
        /// </summary>
        public bool DisposeStream { get; set; }

        /// <summary>
        /// Construct a PdfStreamWriter with a memory stream
        /// </summary>
        public PdfStreamWriter() : this(new MemoryStream()) { }

        /// <summary>
        /// Construct a PdfStreamWriter
        /// </summary>
        public PdfStreamWriter(Stream baseStream, bool disposeStream = true)
        {
            stream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            DisposeStream = disposeStream;
        }

        /// <summary>
        /// Flush the document with all the token that we have accumulated
        /// </summary>
        /// <param name="version">Pdf Version that we are targeting</param>
        /// <param name="catalogReference">Catalog's indirect reference token to which the token are related</param>
        public void Flush(decimal version, IndirectReferenceToken catalogReference)
        {
            if (catalogReference == null)
            {
                throw new ArgumentNullException(nameof(catalogReference));
            }

            WriteString($"%PDF-{version.ToString("0.0", CultureInfo.InvariantCulture)}", stream);

            stream.WriteText("%");
            stream.WriteByte(169);
            stream.WriteByte(205);
            stream.WriteByte(196);
            stream.WriteByte(210);
            stream.WriteNewLine();

            var offsets = new Dictionary<IndirectReference, long>();
            ObjectToken catalogToken = null;
            foreach (var pair in tokenReferences)
            {
                var referenceToken = pair.Key;
                var token = pair.Value;
                var offset = stream.Position;
                var obj = new ObjectToken(offset, referenceToken.Data, token);

                TokenWriter.WriteToken(obj, stream);

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
            TokenWriter.WriteCrossReferenceTable(offsets, catalogToken, stream, null);
        }

        /// <summary>
        /// Push a new token to be written
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reservedNumber"></param>
        /// <returns></returns>
        public IndirectReferenceToken WriteToken(IToken token, int? reservedNumber = null)
        {
            // if you can't consider deduplicating the token. 
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
                return AddToken(token, reservedNumber.Value);
            }

            return AddToken(token, currentNumber++);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="referenceToken"></param>
        /// <returns></returns>
        public IToken GetToken(IndirectReferenceToken referenceToken)
        {
            return tokenReferences.TryGetValue(referenceToken, out var token) ? token : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="referenceToken"></param>
        /// <param name="newToken"></param>
        public void ReplaceToken(IndirectReferenceToken referenceToken, IToken newToken)
        { 
            tokenReferences[referenceToken] = newToken;
        }

        /// <summary>
        /// Reserve a number for a token
        /// </summary>
        /// <returns></returns>
        public int ReserveNumber()
        {
            var reserved = currentNumber;
            reservedNumbers.Add(reserved);
            currentNumber++;
            return reserved;
        }

        /// <summary>
        /// Reserve a number and create a token with it
        /// </summary>
        /// <returns></returns>
        public IndirectReferenceToken ReserveNumberToken()
        {
            return new IndirectReferenceToken(new IndirectReference(ReserveNumber(), 0));
        }

        /// <summary>
        /// Return the bytes that have been flushed to the stream
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            var currentPosition = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);

            var bytes = new byte[stream.Length];

            if (stream.Read(bytes, 0, bytes.Length) != bytes.Length)
            {
                throw new Exception("Unable to read all the bytes from stream");
            }

            stream.Seek(currentPosition, SeekOrigin.Begin);

            return bytes;
        }


        /// <summary>
        /// Dispose the stream if the PdfStreamWriter#DisposeStream flag is set
        /// </summary>
        public void Dispose()
        {
            if (!DisposeStream)
            {
                stream = null;
                return;
            }

            stream?.Dispose();
            stream = null;
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
