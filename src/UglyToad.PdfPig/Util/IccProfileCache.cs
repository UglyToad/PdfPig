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
    /// Document-scoped cache of ICC profiles parsed from profile streams.
    /// <para>
    /// An <c>/ICCBased</c> colour space is re-parsed every time its resource dictionary is loaded, because
    /// <c>ResourceStore.LoadResourceDictionary</c> clears the colour space caches. Without this cache that
    /// means re-inflating the whole profile stream, and handing it to
    /// <see cref="IIccProfileService.TryGetProfile"/> again, once per page and once per Form XObject with its
    /// own <c>/Resources</c>. Embedded CMYK profiles are routinely over a megabyte, and a viewer that
    /// re-renders a page on every zoom step pays the cost each time. PDFBox has no such problem: it caches
    /// the constructed <c>PDICCBased</c> against the profile stream's <c>COSObject</c> in the document
    /// resource cache, so the parsed profile and its transforms are shared for the document's lifetime.
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
    /// Only the parsed profile is kept. The decoded bytes exist just long enough to be handed to the service
    /// and are then collectable, which is the difference between holding a handful of profile objects for the
    /// life of the document and holding several megabytes of inflated profile data nothing will read again.
    /// </para>
    /// <para>
    /// Nothing is evicted: the cache lives and dies with the document, as the profiles it holds are exactly
    /// the ones the document is going to keep asking for. It assumes one
    /// <see cref="IIccProfileService"/> per document, which is what <see cref="ParsingOptions"/> provides.
    /// </para>
    /// </summary>
    internal sealed class IccProfileCache
    {
        // A null value is a real answer - "this profile was tried and yielded nothing" - so lookups go
        // through TryGetValue rather than testing the value for null.
        private readonly Dictionary<IndirectReference, IIccProfile?> byReference = new();

        private readonly Dictionary<ContentKey, IIccProfile?> byContent = new();

        /// <summary>
        /// Resolve the parsed profile for a stream, decoding and parsing it at most once per document.
        /// Returns <c>null</c> when there is no service, when the stream cannot be decoded, or when the
        /// service declines or fails to parse it - in every case the caller falls back to the colour space's
        /// alternate.
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
                // Nothing is going to read the profile, so do not even inflate it.
                return null;
            }

            if (profileToken is IndirectReferenceToken reference)
            {
                if (byReference.TryGetValue(reference.Data, out var cached))
                {
                    return cached;
                }

                var parsed = DecodeAndParse(profileStream, filterProvider, scanner, service, log);
                byReference[reference.Data] = parsed;
                return parsed;
            }

            var key = ContentKey.Create(profileStream.Data.Span);

            if (byContent.TryGetValue(key, out var cachedByContent))
            {
                return cachedByContent;
            }

            var parsedByContent = DecodeAndParse(profileStream, filterProvider, scanner, service, log);
            byContent[key] = parsedByContent;
            return parsedByContent;
        }

        /// <summary>
        /// Inflate the stream and hand it to the service. Every failure returns null rather than throwing,
        /// and the caller caches that null like any other answer: retrying a corrupt multi-megabyte stream
        /// on every page is exactly the cost this cache exists to avoid.
        /// <para>
        /// The decoded bytes are local to this call, so once the service has built its profile they are no
        /// longer reachable from the cache.
        /// </para>
        /// </summary>
        private static IIccProfile? DecodeAndParse(StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner,
            IIccProfileService service, ILog? log)
        {
            ReadOnlyMemory<byte> bytes;

            try
            {
                bytes = profileStream.Decode(filterProvider, scanner);
            }
            catch (Exception ex)
            {
                log?.Warn($"An ICC profile stream could not be decoded ({ex.Message}); " +
                          "the colour space will use its alternate.");
                return null;
            }

            if (bytes.IsEmpty)
            {
                return null;
            }

            try
            {
                if (service.TryGetProfile(bytes, out var profile))
                {
                    return profile;
                }

                log?.Warn("An ICC profile could not be parsed by the configured IIccProfileService; " +
                          "the colour space will use its alternate.");
            }
            catch (Exception ex)
            {
                // TryGetProfile is third-party code. A profile that makes it throw is no worse than one it
                // declines, so it costs the colour space its profile and nothing more.
                log?.Error("The configured IIccProfileService threw while parsing an ICC profile; " +
                           "the colour space will use its alternate.", ex);
            }

            return null;
        }

        /// <summary>
        /// Identifies a profile stream that arrived without an indirect reference to key on - see the class
        /// remarks - by a 128-bit MurmurHash3 of its raw (still encoded) bytes plus their length, rather than
        /// by keeping a copy of those bytes around. Hashing is a single pass where the previous byte-wise
        /// comparison was one pass per cache entry, and nothing of the profile is retained beyond the parsed
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
