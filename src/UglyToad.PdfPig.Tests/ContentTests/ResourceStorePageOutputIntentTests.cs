namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using System.Linq;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Tokens;
    using Tokens;

    /// <summary>
    /// Covers <see cref="IResourceStore.GetPageOutputIntents"/>: page-level <c>/OutputIntents</c> (PDF 2.0,
    /// Table 31) override the catalog's, and neither scope may re-inflate a profile the store has already
    /// decoded - the resolution is done here precisely so both share one ICC profile cache.
    /// </summary>
    public class ResourceStorePageOutputIntentTests
    {
        [Fact]
        public void ExposesTheCatalogIntents()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var intent = Assert.Single(store.DocumentOutputIntents);

            Assert.Equal("GTS_PDFX", intent.Name);
            Assert.Equal("CATALOG", intent.OutputConditionIdentifier);
            Assert.NotNull(intent.DestOutputProfile);
        }

        [Fact]
        public void EveryCatalogIntentIsExposedInArrayOrder()
        {
            // No entry is preferred over another: the store hands back what the file declared.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([
                        Intent(scanner, "GTS_PDFA1", "FIRST", profileObjectNumber: 7),
                        Intent(scanner, "GTS_PDFX", "SECOND", profileObjectNumber: 8)
                    ])
                }
            });

            var store = BuildStore(scanner, filters, catalog);

            Assert.Equal(["FIRST", "SECOND"], store.DocumentOutputIntents.Select(x => x.OutputConditionIdentifier));
        }

        [Fact]
        public void ADocumentWithoutACatalogDictionaryHasNoIntents()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters, catalogDictionary: null);

            Assert.Empty(store.DocumentOutputIntents);
            Assert.Empty(store.GetPageOutputIntents(null));
            Assert.Empty(store.GetPageOutputIntents(new DictionaryToken(new Dictionary<NameToken, IToken>())));
        }

        [Fact]
        public void ACatalogWithoutOutputIntentsHasNoIntents()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters, new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Empty(store.DocumentOutputIntents);
        }

        [Fact]
        public void TheCatalogIntentsAreNotParsedUntilTheyAreAsked()
        {
            // Most documents never look at their output intents, and the profile behind one is routinely
            // megabytes: building the store must not pay for it.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            Assert.Equal(0, filters.DecodeCount);

            Assert.NotNull(store.DocumentOutputIntents[0].DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void TheCatalogIntentsAreParsedOnlyOnce()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            Assert.Same(store.DocumentOutputIntents, store.DocumentOutputIntents);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void PageIntentsFallBackToTheCatalogList()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var intents = store.GetPageOutputIntents(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Same(store.DocumentOutputIntents, intents);
            Assert.Equal("CATALOG", Assert.Single(intents).OutputConditionIdentifier);
        }

        [Fact]
        public void UsesTheCatalogIntentsWhenThereIsNoPageDictionary()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            Assert.Equal("CATALOG", Assert.Single(store.GetPageOutputIntents(null)).OutputConditionIdentifier);
        }

        [Fact]
        public void PageIntentsOverrideTheCatalogIntents()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var page = Catalog(scanner, "GTS_PDFX", "PAGE", profileObjectNumber: 8);

            Assert.Equal("PAGE", Assert.Single(store.GetPageOutputIntents(page)).OutputConditionIdentifier);

            // Overriding is per page: the document scope is untouched.
            Assert.Equal("CATALOG", Assert.Single(store.DocumentOutputIntents).OutputConditionIdentifier);
        }

        [Fact]
        public void EveryPageIntentIsExposedInArrayOrder()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters, catalogDictionary: null);

            var page = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([
                        Intent(scanner, "GTS_PDFX", "PAGE_FIRST", profileObjectNumber: 7),
                        Intent(scanner, "GTS_PDFA1", "PAGE_SECOND", profileObjectNumber: 8)
                    ])
                }
            });

            Assert.Equal(["PAGE_FIRST", "PAGE_SECOND"],
                store.GetPageOutputIntents(page).Select(x => x.OutputConditionIdentifier));
        }

        [Fact]
        public void FallsBackToTheCatalogWhenThePageArrayYieldsNothing()
        {
            // An empty (or wholly unparseable) page array is not a page that declares "no output intent":
            // the document scope still applies.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var page = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new ArrayToken([]) }
            });

            Assert.Equal("CATALOG", Assert.Single(store.GetPageOutputIntents(page)).OutputConditionIdentifier);
        }

        [Fact]
        public void FallsBackToTheCatalogWhenAnIndirectPageArrayYieldsNothing()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7));

            var outputIntents = new IndirectReference(20, 0);
            scanner.Objects[outputIntents] = new ObjectToken(XrefLocation.File(0), outputIntents,
                new ArrayToken([]));

            var page = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new IndirectReferenceToken(outputIntents) }
            });

            Assert.Equal("CATALOG", Assert.Single(store.GetPageOutputIntents(page)).OutputConditionIdentifier);
        }

        [Fact]
        public void DecodesAPageProfileOnlyOnceAcrossPagesAndReRenders()
        {
            // Every page of a PDF 2.0 file carrying page-level intents points at the same profile object, and
            // the intents are resolved afresh for each stream processor - so once per page, and again on every
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
                var intent = Assert.Single(store.GetPageOutputIntents(page));

                Assert.Equal("PAGE", intent.OutputConditionIdentifier);
                Assert.NotNull(intent.DestOutputProfile);
            }

            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void AnIndirectPageArrayIsParsedOnlyOnce()
        {
            // The parsed list itself is cached against the array's reference, so re-rendering a page does not
            // even walk the intent dictionaries again.
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

            Assert.Same(store.GetPageOutputIntents(page), store.GetPageOutputIntents(page));
        }

        [Fact]
        public void DecodesADirectlyWrittenPageProfileOnlyOnce()
        {
            // A page array written directly has no reference to cache the parsed list against, so it is
            // walked again on each call - but the profile behind it is still decoded once.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = BuildStore(scanner, filters, catalogDictionary: null);

            var page = Catalog(scanner, "GTS_PDFX", "PAGE", profileObjectNumber: 7);

            for (int i = 0; i < 5; i++)
            {
                Assert.NotNull(Assert.Single(store.GetPageOutputIntents(page)).DestOutputProfile);
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

            Assert.NotNull(Assert.Single(store.DocumentOutputIntents).DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);

            // Same profile object (7), different intent metadata.
            var page = Catalog(scanner, "GTS_PDFX", "PAGE", profileObjectNumber: 7);

            Assert.NotNull(Assert.Single(store.GetPageOutputIntents(page)).DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void WithoutAProfileServiceThePageIntentsAreStillReported()
        {
            // The output condition is what conformance checking reads; it does not depend on colour
            // management being configured, and asking for it must not inflate the profile.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var store = new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                filters,
                Catalog(scanner, "GTS_PDFX", "CATALOG", profileObjectNumber: 7),
                new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true });

            var intent = Assert.Single(store.GetPageOutputIntents(null));

            Assert.Equal("CATALOG", intent.OutputConditionIdentifier);
            Assert.Null(intent.DestOutputProfile);
            Assert.Equal(0, filters.DecodeCount);
        }

        private static ResourceStore BuildStore(TestPdfTokenScanner scanner, CountingFilterProvider filters,
            DictionaryToken? catalogDictionary)
        {
            return new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                filters,
                catalogDictionary,
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                    IccProfileService = new TestIccProfileService(4)
                });
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
    }
}
