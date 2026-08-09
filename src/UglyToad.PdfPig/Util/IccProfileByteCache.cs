namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Filters;
    using Graphics.Colors.Icc;
    using Logging;
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
        /// <summary>
        /// What the cache holds against a key: the decoded bytes, and the profile parsed from them once
        /// anything has asked for it. Keeping both in one entry means the content hash that identifies a
        /// directly written profile is computed once per lookup rather than once per thing looked up.
        /// </summary>
        private sealed class Entry
        {
            public ReadOnlyMemory<byte> Bytes;

            public IIccProfile? Profile;

            /// <summary>
            /// Distinguishes "not parsed yet" from "parsed, and the answer was no profile" - the second is
            /// as worth remembering as the first is worth avoiding.
            /// </summary>
            public bool ProfileResolved;
        }

        private readonly Dictionary<IndirectReference, Entry> byReference = new();

        private readonly Dictionary<ContentKey, Entry> byContent = new();

        /// <summary>
        /// Decode the profile stream, reusing a previously decoded copy of the same profile when there is
        /// one. Returns <see cref="ReadOnlyMemory{T}.Empty"/> when the stream cannot be decoded, in which
        /// case the caller falls back to the colour space's alternate.
        /// </summary>
        /// <param name="profileToken">
        /// The token the stream was resolved from, used as the cache key when it is an indirect reference.
        /// </param>
        /// <param name="profileStream">The resolved profile stream.</param>
        /// <param name="filterProvider">Used to decode the stream.</param>
        /// <param name="scanner">Used to resolve the stream's own indirect entries.</param>
        public ReadOnlyMemory<byte> GetOrDecode(IToken profileToken, StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            return GetOrCreateEntry(profileToken, profileStream, filterProvider, scanner).Bytes;
        }

        /// <summary>
        /// Resolve the parsed profile for a stream, parsing it at most once per document.
        /// <para>
        /// Caching the bytes alone still left <see cref="IIccProfileService.TryGetProfile"/> called once per
        /// resource dictionary - so once per page, and again per Form XObject with its own resources -
        /// because <c>ResourceStore</c> clears its colour space caches each time one is loaded. PDFBox has
        /// no such problem: it caches the constructed <c>PDICCBased</c> against the profile stream's
        /// <c>COSObject</c> in the document resource cache, so the parsed profile and its transforms are
        /// shared for the document's lifetime. This is that guarantee, keyed the same two ways the bytes
        /// already are.
        /// </para>
        /// </summary>
        /// <param name="profileToken">
        /// The token the stream was resolved from, used as the cache key when it is an indirect reference.
        /// </param>
        /// <param name="profileStream">The resolved profile stream.</param>
        /// <param name="filterProvider">Used to decode the stream.</param>
        /// <param name="scanner">Used to resolve the stream's own indirect entries.</param>
        /// <param name="service">The service that parses profile bytes, or <c>null</c> for no colour management.</param>
        /// <param name="log">Receives a warning when a profile is present but unusable.</param>
        public IIccProfile? GetOrParse(IToken profileToken, StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner,
            IIccProfileService? service, ILog? log = null)
        {
            if (service is null)
            {
                return null;
            }

            var entry = GetOrCreateEntry(profileToken, profileStream, filterProvider, scanner);

            if (entry.ProfileResolved)
            {
                return entry.Profile;
            }

            entry.ProfileResolved = true;

            if (entry.Bytes.IsEmpty)
            {
                return null;
            }

            try
            {
                if (service.TryGetProfile(entry.Bytes, out var profile))
                {
                    entry.Profile = profile;
                }
                else
                {
                    log?.Warn("ICC profile could not be parsed by the configured IIccProfileService; " +
                              "the colour space will use its alternate.");
                }
            }
            catch (Exception ex)
            {
                // TryGetProfile is third-party code. A profile that makes it throw is no worse than one it
                // declines, so it costs the colour space its profile and nothing more.
                log?.Error("The configured IIccProfileService threw while parsing an ICC profile; " +
                           "the colour space will use its alternate.", ex);
            }

            return entry.Profile;
        }

        private Entry GetOrCreateEntry(IToken profileToken, StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner)
        {
            if (profileToken is IndirectReferenceToken reference)
            {
                if (byReference.TryGetValue(reference.Data, out var cached))
                {
                    return cached;
                }

                var entry = new Entry { Bytes = Decode(profileStream, filterProvider, scanner) };
                byReference[reference.Data] = entry;
                return entry;
            }

            var key = ContentKey.Create(profileStream.Data.Span);

            if (byContent.TryGetValue(key, out var cachedByContent))
            {
                return cachedByContent;
            }

            var entryByContent = new Entry { Bytes = Decode(profileStream, filterProvider, scanner) };
            byContent[key] = entryByContent;
            return entryByContent;
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
