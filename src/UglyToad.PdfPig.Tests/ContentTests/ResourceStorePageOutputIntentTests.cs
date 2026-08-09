namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using Tokens;

    /// <summary>
    /// Covers <see cref="IResourceStore.GetPageOutputIntent"/>: page-level <c>/OutputIntents</c> (PDF 2.0,
    /// Table 31) override the catalog's, and neither scope may re-inflate a profile the store has already
    /// decoded - the resolution is done here precisely so both share one ICC byte cache.
    /// </summary>
    public class ResourceStorePageOutputIntentTests
    {
        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        [Fact]
        public void UsesTheCatalogIntentWhenThePageHasNone()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var intent = store.GetPageOutputIntent(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.NotNull(intent);
            Assert.Equal("CATALOG", intent!.OutputConditionIdentifier);
        }

        [Fact]
        public void UsesTheCatalogIntentWhenThereIsNoPageDictionary()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            Assert.Equal("CATALOG", store.GetPageOutputIntent(null)!.OutputConditionIdentifier);
        }

        [Fact]
        public void PageIntentOverridesTheCatalogIntent()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var page = Catalog(scanner, "GTS_PDFX", "PAGE", profileObjectNumber: 8);

            Assert.Equal("PAGE", store.GetPageOutputIntent(page)!.OutputConditionIdentifier);
        }

        [Fact]
        public void FallsBackToTheCatalogWhenThePageArrayYieldsNothing()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var page = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new ArrayToken([]) }
            });

            Assert.Equal("CATALOG", store.GetPageOutputIntent(page)!.OutputConditionIdentifier);
        }

        [Fact]
        public void DecodesAPageProfileOnlyOnceAcrossPagesAndReRenders()
        {
            // Every page of a PDF 2.0 file carrying page-level intents points at the same profile object, and
            // the intent is resolved afresh for each stream processor - so once per page, and again on every
            // re-render of a page. One inflate is the whole point.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters, catalogDictionary: null);

            var outputIntents = new IndirectReference(20, 0);
            scanner.Objects[outputIntents] = new ObjectToken(XrefLocation.File(0), outputIntents,
                new ArrayToken([Intent(scanner, "GTS_PDFX", "PAGE", profileObjectNumber: 7)]));

            var page = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new IndirectReferenceToken(outputIntents) }
            });

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal("PAGE", store.GetPageOutputIntent(page)!.OutputConditionIdentifier);
            }

            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void SharesOneDecodeBetweenTheCatalogAndPageScopes()
        {
            // A PDF/X file repeats the catalog's profile object in its page-level intents; the two scopes are
            // resolved through the same store so that costs one inflate, not two.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            Assert.NotNull(store.OutputIntent!.DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);

            // Same profile object (7), different intent metadata.
            var page = Catalog(scanner, "GTS_PDFX", "PAGE", profileObjectNumber: 7);

            Assert.NotNull(store.GetPageOutputIntent(page)!.DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        private static ResourceStore BuildStore(TestPdfTokenScanner scanner, CountingFilterProvider filters,
            DictionaryToken? catalogDictionary)
        {
            return new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                filters,
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                    IccProfileService = new FakeIccProfileService()
                },
                catalogDictionary);
        }

        private static DictionaryToken Catalog(TestPdfTokenScanner scanner, string subtype,
            string conditionIdentifier, long profileObjectNumber)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([Intent(scanner, subtype, conditionIdentifier, profileObjectNumber)])
                }
            });
        }

        private static DictionaryToken Intent(TestPdfTokenScanner scanner, string subtype,
            string conditionIdentifier, long profileObjectNumber)
        {
            var reference = new IndirectReference(profileObjectNumber, 0);

            if (!scanner.Objects.ContainsKey(reference))
            {
                var streamDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.N, new NumericToken(4) },
                    { NameToken.Length, new NumericToken(4) }
                });

                scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference,
                    new StreamToken(streamDictionary, [1, 2, 3, 4]));
            }

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create(subtype) },
                { NameToken.OutputConditionIdentifier, new StringToken(conditionIdentifier) },
                { NameToken.DestOutputProfile, new IndirectReferenceToken(reference) }
            });
        }

        private sealed class FakeIccProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
            {
                profile = new FakeIccProfile();
                return true;
            }
        }

        private sealed class FakeIccProfile : IIccProfile
        {
            public int NumberOfComponents => 4;

            public bool IsLabInput => false;

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = null;
                return false;
            }
        }
    }
}
