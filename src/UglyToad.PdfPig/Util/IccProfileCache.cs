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
    /// </summary>
    internal sealed class IccProfileCache
    {
        // NB: A null profile is a real answer, i.e. tried but failed.
        private readonly Dictionary<IndirectReference, IIccProfile?> byReference = new();

        /// <summary>
        /// Resolve the parsed profile for a stream, decoding and parsing it at most once per document.
        /// Returns <c>null</c> when there is no service, when the stream cannot be decoded, or when the
        /// service declines or fails to parse it. In this case, the caller falls back to the colour space's
        /// alternate.
        /// </summary>
        public IIccProfile? GetOrParse(IToken profileToken, StreamToken profileStream,
            ILookupFilterProvider filterProvider, IPdfTokenScanner scanner,
            IIccProfileService? service, ILog? log = null)
        {
            if (service is null)
            {
                // Nothing is going to read the profile, so do not even decode it.
                return null;
            }

            if (profileToken is not IndirectReferenceToken reference)
            {
                return DecodeAndParse(profileStream, filterProvider, scanner, service, log);
            }

            if (byReference.TryGetValue(reference.Data, out var cached))
            {
                return cached;
            }

            return byReference[reference.Data] =
                DecodeAndParse(profileStream, filterProvider, scanner, service, log);
        }

        /// <summary>
        /// Decode the stream and hand it to the service. Every failure returns null rather than throwing,
        /// and the caller caches that null like any other answer.
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
    }
}
