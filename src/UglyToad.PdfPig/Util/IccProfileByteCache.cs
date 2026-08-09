namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Filters;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Document-scoped cache of decoded ICC profile streams.
    /// <para>
    /// An <c>/ICCBased</c> colour space is re-parsed every time its resource dictionary is loaded, because
    /// <c>ResourceStore.LoadResourceDictionary</c> clears the colour space caches. Without this cache that
    /// means re-inflating the whole profile stream once per page and once per Form XObject with its own
    /// <c>/Resources</c>. Embedded CMYK profiles are routinely over a megabyte, and a viewer that re-renders
    /// a page on every zoom step pays the cost each time.
    /// </para>
    /// <para>
    /// Two keys are needed because the profile stream reaches this cache in two different shapes. A colour
    /// space named from a resource dictionary, and an output intent's <c>/DestOutputProfile</c>, both keep
    /// the indirect reference the stream was written as, which is unique document-wide and free to compare.
    /// An image XObject does not: <c>XObjectFactory</c> resolves the image dictionary, and resolving rebuilds
    /// every array element as a materialised token, so an <c>/ICCBased</c> colour space on an image arrives
    /// as a freshly allocated <see cref="StreamToken"/> with no reference left to key on - and a different
    /// instance on each resolve, so identity cannot stand in for it either. Those are matched by content.
    /// </para>
    /// <para>
    /// Nothing is evicted: the cache lives and dies with the document, as the decoded profiles it holds are
    /// exactly the ones the document is going to keep asking for.
    /// </para>
    /// </summary>
    internal sealed class IccProfileByteCache
    {
        private readonly Dictionary<IndirectReference, ReadOnlyMemory<byte>> byReference = new();

        private readonly Dictionary<ContentKey, ReadOnlyMemory<byte>> byContent = new();

        /// <summary>
        /// Decode the profile stream, reusing a previously decoded copy of the same profile when there is
        /// one. Returns <see cref="ReadOnlyMemory{T}.Empty"/> when the stream cannot be decoded, in which
        /// case the caller falls back to the colour space's alternate.
        /// </summary>
        /// <param name="profileToken">
        /// The token the stream was resolved from, used as the cache key when it is an indirect reference.
        /// </param>
        /// <param name="profileStream">The resolved profile stream.</param>
        public ReadOnlyMemory<byte> GetOrDecode(IToken profileToken, StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            if (profileToken is IndirectReferenceToken reference)
            {
                if (byReference.TryGetValue(reference.Data, out var cached))
                {
                    return cached;
                }

                var decoded = Decode(profileStream, filterProvider, scanner);
                byReference[reference.Data] = decoded;
                return decoded;
            }

            var key = ContentKey.Create(profileStream.Data.Span);

            if (byContent.TryGetValue(key, out var cachedByContent))
            {
                return cachedByContent;
            }

            var decodedByContent = Decode(profileStream, filterProvider, scanner);
            byContent[key] = decodedByContent;
            return decodedByContent;
        }

        /// <summary>
        /// A failure returns empty rather than throwing, and the caller caches that empty result like any
        /// other: retrying a corrupt multi-megabyte stream on every page is exactly the cost this cache
        /// exists to avoid.
        /// </summary>
        private static ReadOnlyMemory<byte> Decode(StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            try
            {
                return profileStream.Decode(filterProvider, scanner);
            }
            catch
            {
                return ReadOnlyMemory<byte>.Empty;
            }
        }

        /// <summary>
        /// Identifies a profile stream that arrived without an indirect reference to key on - see the class
        /// remarks - by a 128-bit MurmurHash3 of its raw (still encoded) bytes plus their length, rather than
        /// by keeping a copy of those bytes around. Hashing is a single pass where the previous byte-wise
        /// comparison was one pass per cache entry, and nothing of the profile is retained beyond the decoded
        /// result the cache exists to hand back.
        /// <para>
        /// Matching is therefore probabilistic. Two distinct profiles of identical length colliding across
        /// all 128 bits is not a case worth guarding against here: at worst a viewer would render with the
        /// wrong embedded profile, and the odds of that are far below those of the file itself being corrupt.
        /// </para>
        /// </summary>
        private readonly struct ContentKey : IEquatable<ContentKey>
        {
            private readonly ulong hash1;
            private readonly ulong hash2;
            private readonly int length;

            private ContentKey(ulong hash1, ulong hash2, int length)
            {
                this.hash1 = hash1;
                this.hash2 = hash2;
                this.length = length;
            }

            public static ContentKey Create(ReadOnlySpan<byte> raw)
            {
                Span<ulong> hash = stackalloc ulong[2];
                MurmurHash3.Compute_x64_128(raw, hash);
                return new ContentKey(hash[0], hash[1], raw.Length);
            }

            public bool Equals(ContentKey other)
            {
                return hash1 == other.hash1 && hash2 == other.hash2 && length == other.length;
            }

            public override bool Equals(object? obj) => obj is ContentKey other && Equals(other);

            // The hash is already well mixed, so folding half of it is all a bucket index needs.
            public override int GetHashCode() => (int)hash1 ^ (int)(hash1 >> 32);
        }
    }
}
