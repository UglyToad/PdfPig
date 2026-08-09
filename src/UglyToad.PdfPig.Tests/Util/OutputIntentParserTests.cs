namespace UglyToad.PdfPig.Tests.Util
{
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Core;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
    using PdfPig.Tokens;
    using PdfPig.Util;
    using Tokens;

    public class OutputIntentParserTests
    {
        private static readonly TestPdfTokenScanner Scanner = new TestPdfTokenScanner();

        [Fact]
        public void PrefersPdfXOverPdfAWhenBothCarryAProfile()
        {
            // Both usable, PDF/A written first: the subtype must decide, not array order.
            var catalog = Catalog(
                Intent("GTS_PDFA1", "FOGRA39", withProfile: true),
                Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var result = Create(catalog);

            Assert.NotNull(result);
            Assert.Equal("GTS_PDFX", result!.Name);
            Assert.Equal("FOGRA51", result.OutputConditionIdentifier);
            Assert.NotNull(result.DestOutputProfile);
        }

        [Fact]
        public void PrefersPdfXWhenWrittenFirstToo()
        {
            // The ordering rule must not merely reverse the array.
            var catalog = Catalog(
                Intent("GTS_PDFX", "FOGRA51", withProfile: true),
                Intent("GTS_PDFA1", "FOGRA39", withProfile: true));

            Assert.Equal("GTS_PDFX", Create(catalog)!.Name);
        }

        [Fact]
        public void PrefersPdfAOverUnknownSubtype()
        {
            var catalog = Catalog(
                Intent("ISO_PDFE1", "PDFE", withProfile: true),
                Intent("GTS_PDFA1", "FOGRA39", withProfile: true));

            Assert.Equal("GTS_PDFA1", Create(catalog)!.Name);
        }

        [Fact]
        public void AUsableProfileBeatsABetterSubtypeWithoutOne()
        {
            // Only an embedded profile can drive colour management, so profile availability
            // outranks the subtype: the PDF/X entry here has nothing to transform with.
            var catalog = Catalog(
                Intent("GTS_PDFX", "FOGRA51", withProfile: false),
                Intent("GTS_PDFA1", "FOGRA39", withProfile: true));

            var result = Create(catalog);

            Assert.Equal("GTS_PDFA1", result!.Name);
            Assert.NotNull(result.DestOutputProfile);
        }

        [Fact]
        public void FallsBackToBestRankedEntryWhenNoneCarriesAProfile()
        {
            // Nothing is usable for colour management, but the metadata must still be surfaced,
            // and the entry chosen is still the best-ranked one rather than the first.
            var catalog = Catalog(
                Intent("ISO_PDFE1", "PDFE", withProfile: false),
                Intent("GTS_PDFX", "FOGRA51", withProfile: false));

            var result = Create(catalog);

            Assert.Equal("GTS_PDFX", result!.Name);
            Assert.Null(result.DestOutputProfile);
        }

        [Fact]
        public void KeepsArrayOrderAmongEntriesOfTheSameRank()
        {
            var catalog = Catalog(
                Intent("GTS_PDFX", "FIRST", withProfile: true),
                Intent("GTS_PDFX", "SECOND", withProfile: true));

            Assert.Equal("FIRST", Create(catalog)!.OutputConditionIdentifier);
        }

        [Fact]
        public void TreatsAMissingSubtypeAsLowestRank()
        {
            var withoutSubtype = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputConditionIdentifier, new StringToken("NO_SUBTYPE") },
                { NameToken.DestOutputProfile, ProfileStream() }
            });

            var catalog = Catalog(withoutSubtype, Intent("GTS_PDFA1", "FOGRA39", withProfile: true));

            Assert.Equal("GTS_PDFA1", Create(catalog)!.Name);
        }

        [Fact]
        public void ReturnsNullWhenThereIsNoOutputIntentsArray()
        {
            Assert.Null(Create(new DictionaryToken(new Dictionary<NameToken, IToken>())));
        }

        [Fact]
        public void ReturnsNullWithoutAProfileService()
        {
            var catalog = Catalog(Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            Assert.Null(OutputIntentParser.Create(catalog, Scanner, TestFilterProvider.Instance, null, new IccProfileByteCache()));
        }

        [Fact]
        public void DecodesTheSameProfileObjectOnlyOnceAcrossCalls()
        {
            // A page-level output intent is resolved once per page and again on every re-render, and a
            // PDF/X file points every page at the same profile object. The shared byte cache is what stops
            // that inflating an embedded CMYK profile over and over.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var cache = new IccProfileByteCache();
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var first = OutputIntentParser.Create(catalog, scanner, filters, new FakeIccProfileService(), cache);
            var second = OutputIntentParser.Create(catalog, scanner, filters, new FakeIccProfileService(), cache);

            Assert.NotNull(first!.DestOutputProfile);
            Assert.NotNull(second!.DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void SharesTheCacheWithProfilesDecodedElsewhere()
        {
            // The point of taking the cache rather than owning one: an /ICCBased colour space pointing at
            // the same object as /DestOutputProfile must not pay for a second inflate.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var cache = new IccProfileByteCache();
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var reference = new IndirectReferenceToken(new IndirectReference(7, 0));
            cache.GetOrDecode(reference, (StreamToken)scanner.Get(reference.Data).Data, filters, scanner);

            Assert.Equal(1, filters.DecodeCount);

            Assert.NotNull(OutputIntentParser.Create(catalog, scanner, filters, new FakeIccProfileService(), cache)!.DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void FallsBackWhenTheProfileStreamCannotBeDecoded()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var result = OutputIntentParser.Create(catalog, scanner, filters, new FakeIccProfileService(),
                new IccProfileByteCache());

            Assert.NotNull(result);
            Assert.Null(result!.DestOutputProfile);
        }

        /// <summary>
        /// An intent whose <c>/DestOutputProfile</c> is written as an indirect reference, as it is in a real
        /// file - which is also what lets the byte cache recognise the profile without touching the stream.
        /// </summary>
        private static DictionaryToken IntentReferencingProfile(TestPdfTokenScanner scanner, long objectNumber)
        {
            var reference = new IndirectReference(objectNumber, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, ProfileStream());

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.OutputConditionIdentifier, new StringToken("FOGRA51") },
                { NameToken.DestOutputProfile, new IndirectReferenceToken(reference) }
            });
        }

        private static OutputIntent? Create(DictionaryToken catalog)
        {
            return OutputIntentParser.Create(catalog, Scanner, TestFilterProvider.Instance, new FakeIccProfileService(),
                new IccProfileByteCache());
        }

        private static DictionaryToken Catalog(params IToken[] intents)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new ArrayToken(intents) }
            });
        }

        private static DictionaryToken Intent(string subtype, string conditionIdentifier, bool withProfile)
        {
            var entries = new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create(subtype) },
                { NameToken.OutputConditionIdentifier, new StringToken(conditionIdentifier) }
            };

            if (withProfile)
            {
                entries[NameToken.DestOutputProfile] = ProfileStream();
            }

            return new DictionaryToken(entries);
        }

        /// <summary>
        /// A stand-in for an embedded ICC profile. The bytes are never interpreted here — the parser hands
        /// them straight to <see cref="FakeIccProfileService"/>, which accepts anything. <c>/N</c> is present
        /// only because a real <c>DestOutputProfile</c> stream usually carries it; nothing reads it.
        /// </summary>
        private static StreamToken ProfileStream()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.N, new NumericToken(4) },
                { NameToken.Length, new NumericToken(4) }
            });

            return new StreamToken(dictionary, new byte[] { 1, 2, 3, 4 });
        }

        private sealed class FakeIccProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
            {
                profile = new FakeIccProfile(4);
                return true;
            }
        }

        private sealed class FakeIccProfile(int numberOfComponents) : IIccProfile
        {
            public int NumberOfComponents { get; } = numberOfComponents;

            public bool IsLabInput { get; } = false;
            
            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = null;
                return false;
            }
        }
    }
}
