namespace UglyToad.PdfPig.Writer
{
using Core;
    using Graphics.Operations;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Tokens;

    internal class PdfDedupStreamWriter : IPdfStreamWriter
    {

        public Stream Stream { get; }
        private int CurrentNumber { get; set; } = 1;
        private bool DisposeStream { get; set; }
        private const decimal DefaultVersion = 1.2m;
        private bool Initialized { get; set; }
        private readonly Dictionary<IndirectReference, long> offsets = new Dictionary<IndirectReference, long>();
        private readonly Dictionary<byte[], IndirectReferenceToken> hashes = new Dictionary<byte[], IndirectReferenceToken>(new FNVByteComparison());

        public PdfDedupStreamWriter(Stream stream, bool dispose)
        {
            Stream = stream;
            DisposeStream = dispose;
        }

        private MemoryStream ms = new MemoryStream();
        public IndirectReferenceToken WriteToken(IToken token)
        {
            if (!Initialized)
            {
                InitializePdf(DefaultVersion);
            }

            ms.SetLength(0);
            TokenWriter.WriteToken(token, ms);
            var contents = ms.ToArray();
            if (hashes.TryGetValue(contents, out var value))
            {
                return value;
            }

            var ir = ReserveObjectNumber();
            hashes.Add(contents, ir);

            offsets.Add(ir.Data, Stream.Position);
            TokenWriter.WriteObject(ir.Data.ObjectNumber, ir.Data.Generation, contents, Stream);

            return ir;
        }

        public IndirectReferenceToken WriteToken(IToken token, IndirectReferenceToken indirectReference)
        {
            if (!Initialized)
            {
                InitializePdf(DefaultVersion);
            }

            ms.SetLength(0);
            TokenWriter.WriteToken(token, ms);
            var contents = ms.ToArray();

            hashes.Add(contents, indirectReference);
            offsets.Add(indirectReference.Data, Stream.Position);
            TokenWriter.WriteObject(indirectReference.Data.ObjectNumber, indirectReference.Data.Generation, contents, Stream);
            return indirectReference;
        }

        public IndirectReferenceToken ReserveObjectNumber()
        {
            return new IndirectReferenceToken(new IndirectReference(CurrentNumber++, 0));
        }

        public void InitializePdf(decimal version)
        {
            WriteString($"%PDF-{version.ToString("0.0", CultureInfo.InvariantCulture)}", Stream);

            Stream.WriteText("%");
            Stream.WriteByte(169);
            Stream.WriteByte(205);
            Stream.WriteByte(196);
            Stream.WriteByte(210);
            Stream.WriteNewLine();

            Initialized = true;
        }

        public void CompletePdf(IndirectReferenceToken catalogReference, IndirectReferenceToken documentInformationReference=null)
        {
            TokenWriter.WriteCrossReferenceTable(offsets, catalogReference.Data, Stream, documentInformationReference?.Data);
        }

        private static void WriteString(string text, Stream stream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteNewLine();
        }

        public void Dispose()
        {
            if (DisposeStream)
            {
                Stream.Dispose();
            }

            hashes.Clear();
        }

        class FNVByteComparison : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(byte[] obj)
            {
                var hash = FnvHash.Create();
                foreach (var t in obj)
                {
                    hash.Combine(t);
                }

                return hash.HashCode;
            }
        }

        /// <summary>
        /// A hash combiner that is implemented with the Fowler/Noll/Vo algorithm (FNV-1a). This is a mutable struct for performance reasons.
        /// </summary>
        struct FnvHash
        {
            /// <summary>
            /// The starting point of the FNV hash.
            /// </summary>
            public const int Offset = unchecked((int)2166136261);

            /// <summary>
            /// The prime number used to compute the FNV hash.
            /// </summary>
            private const int Prime = 16777619;

            /// <summary>
            /// Gets the current result of the hash function.
            /// </summary>
            public int HashCode { get; private set; }

            /// <summary>
            /// Creates a new FNV hash initialized to <see cref="Offset"/>.
            /// </summary>
            public static FnvHash Create()
            {
                var result = new FnvHash();
                result.HashCode = Offset;
                return result;
            }

            /// <summary>
            /// Adds the specified byte to the hash.
            /// </summary>
            /// <param name="data">The byte to hash.</param>
            public void Combine(byte data)
            {
                unchecked
                {
                    HashCode ^= data;
                    HashCode *= Prime;
                }
            }

            /// <summary>
            /// Adds the specified integer to this hash, in little-endian order.
            /// </summary>
            /// <param name="data">The integer to hash.</param>
            public void Combine(int data)
            {
                Combine(unchecked((byte)data));
                Combine(unchecked((byte)(data >> 8)));
                Combine(unchecked((byte)(data >> 16)));
                Combine(unchecked((byte)(data >> 24)));
            }
        }
    }
}
