namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using System.IO;
    using Tokens;

    internal class PdfDedupStreamWriter : PdfStreamWriter
    {
        private readonly Dictionary<byte[], IndirectReferenceToken> hashes = new Dictionary<byte[], IndirectReferenceToken>(new FNVByteComparison());

        public PdfDedupStreamWriter(Stream stream, bool dispose) : base(stream, dispose)
        {
        }

        private readonly MemoryStream ms = new MemoryStream();
        public override IndirectReferenceToken WriteToken(IToken token)
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

        public override IndirectReferenceToken WriteToken(IToken token, IndirectReferenceToken indirectReference)
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

        public new void Dispose()
        {
            hashes.Clear();
            base.Dispose();
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
