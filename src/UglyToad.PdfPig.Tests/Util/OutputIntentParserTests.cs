namespace UglyToad.PdfPig.Tests.Util
{
    using System.Linq;
    using System.Collections.Generic;
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
        public void EveryDeclaredIntentIsReturned_InArrayOrder()
        {
            // The list is the primitive, as it is in PDFBox: a conformance check needs the entries the
            // selection policy would discard, and it needs them in the order the file wrote them.
            var catalog = Catalog(
                Intent("GTS_PDFA1", "FOGRA39", withProfile: false),
                Intent("GTS_PDFX", "FOGRA51", withProfile: true),
                Intent("ISO_PDFE1", "PDFE", withProfile: false));

            var all = CreateAll(catalog);

            Assert.Equal(3, all.Count);
            Assert.Equal(["GTS_PDFA1", "GTS_PDFX", "ISO_PDFE1"], all.Select(x => x.Name));
            Assert.Equal(["FOGRA39", "FOGRA51", "PDFE"], all.Select(x => x.OutputConditionIdentifier));
        }

        [Fact]
        public void SelectingTheEffectiveIntentDoesNotDisturbTheList()
        {
            var catalog = Catalog(
                Intent("GTS_PDFA1", "FOGRA39", withProfile: true),
                Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var all = CreateAll(catalog);
            var effective = OutputIntent.SelectEffective(all);

            Assert.Equal("GTS_PDFX", effective!.Name);
            Assert.Equal("GTS_PDFA1", all[0].Name); // array order preserved
            Assert.Contains(all, x => ReferenceEquals(x, effective));
        }

        [Fact]
        public void EmptyOrNullListSelectsNothing()
        {
            Assert.Null(OutputIntent.SelectEffective(null));
            Assert.Null(OutputIntent.SelectEffective([]));
        }

        [Fact]
        public void EntriesThatAreNotDictionariesAreSkippedWithoutLosingTheRest()
        {
            var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([
                        new NumericToken(42),
                        Intent("GTS_PDFX", "FOGRA51", withProfile: true)
                    ])
                }
            });

            var all = CreateAll(catalog);

            Assert.Single(all);
            Assert.Equal("GTS_PDFX", all[0].Name);
        }

        [Fact]
        public void AbsentEntriesAreNullRatherThanEmptyStrings()
        {
            // /S and /OutputConditionIdentifier are required, so a conformance check has to be able to tell
            // "absent" from "present but empty". PDFBox's getString accessors return null the same way.
            var bare = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.DestOutputProfile, ProfileStream() }
            });

            var intent = Assert.Single(CreateAll(Catalog(bare)));

            Assert.Null(intent.Name);
            Assert.Null(intent.OutputConditionIdentifier);
            Assert.Null(intent.RegistryName);
            Assert.Null(intent.OutputCondition);
            Assert.Null(intent.Info);
        }

        [Fact]
        public void PresentButEmptyEntriesStayEmptyRatherThanBecomingNull()
        {
            // The other half of the distinction: an entry the file actually wrote, empty, is not absent.
            var empty = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.OutputConditionIdentifier, new StringToken(string.Empty) },
                { NameToken.RegistryName, new StringToken(string.Empty) }
            });

            var intent = Assert.Single(CreateAll(Catalog(empty)));

            Assert.Equal(string.Empty, intent.OutputConditionIdentifier);
            Assert.Equal(string.Empty, intent.RegistryName);
        }

        [Fact]
        public void RegistryNameIsReadWhenPresent()
        {
            var withRegistry = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.OutputConditionIdentifier, new StringToken("FOGRA51") },
                { NameToken.RegistryName, new StringToken("http://www.color.org") },
                { NameToken.OutputCondition, new StringToken("Coated FOGRA51") },
                { NameToken.Info, new StringToken("Some info") }
            });

            var intent = Assert.Single(CreateAll(Catalog(withRegistry)));

            Assert.Equal("http://www.color.org", intent.RegistryName);
            Assert.Equal("Coated FOGRA51", intent.OutputCondition);
            Assert.Equal("Some info", intent.Info);
        }

        [Fact]
        public void WithoutAProfileService_StillReportsTheOutputCondition()
        {
            // The descriptive entries are what PDF/A and PDF/X conformance checking reads and have nothing
            // to do with colour management, so an absent IIccProfileService must not hide them - it is only
            // /DestOutputProfile that goes unresolved. PDFBox exposes getOutputIntents() unconditionally.
            var catalog = Catalog(Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var result = OutputIntent.SelectEffective(OutputIntentParser.CreateAll(catalog, Scanner,
                TestFilterProvider.Instance, null, new IccProfileCache()));

            Assert.NotNull(result);
            Assert.Equal("GTS_PDFX", result!.Name);
            Assert.Equal("FOGRA51", result.OutputConditionIdentifier);
            Assert.Null(result.DestOutputProfile);
        }

        [Fact]
        public void WithoutAProfileService_TheProfileStreamIsNeverDecoded()
        {
            // Not resolving the profile has to mean not paying for it either: an embedded CMYK profile is
            // routinely megabytes, and nothing is going to look at it.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var result = OutputIntent.SelectEffective(
                OutputIntentParser.CreateAll(catalog, scanner, filters, null, new IccProfileCache()));

            Assert.NotNull(result);
            Assert.Null(result!.DestOutputProfile);
            Assert.Equal(0, filters.DecodeCount);
        }

        [Fact]
        public void DecodesTheSameProfileObjectOnlyOnceAcrossCalls()
        {
            // A page-level output intent is resolved once per page and again on every re-render, and a
            // PDF/X file points every page at the same profile object. The shared byte cache is what stops
            // that inflating an embedded CMYK profile over and over.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var cache = new IccProfileCache();
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var first = OutputIntent.SelectEffective(
                OutputIntentParser.CreateAll(catalog, scanner, filters, new FakeIccProfileService(), cache));
            var second = OutputIntent.SelectEffective(
                OutputIntentParser.CreateAll(catalog, scanner, filters, new FakeIccProfileService(), cache));

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
            var cache = new IccProfileCache();
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var reference = new IndirectReferenceToken(new IndirectReference(7, 0));
            cache.GetOrParse(reference, (StreamToken)scanner.Get(reference.Data).Data, filters, scanner,
                new FakeIccProfileService());

            Assert.Equal(1, filters.DecodeCount);

            Assert.NotNull(OutputIntent.SelectEffective(OutputIntentParser.CreateAll(catalog, scanner, filters,
                new FakeIccProfileService(), cache))!.DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void FallsBackWhenTheProfileStreamCannotBeDecoded()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var result = OutputIntent.SelectEffective(OutputIntentParser.CreateAll(catalog, scanner, filters,
                new FakeIccProfileService(), new IccProfileCache()));

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

        /// <summary>
        /// The effective intent, resolved the way production does: parse every entry, then apply the
        /// documented selection policy.
        /// </summary>
        private static OutputIntent? Create(DictionaryToken catalog)
        {
            return OutputIntent.SelectEffective(CreateAll(catalog));
        }

        private static IReadOnlyList<OutputIntent> CreateAll(DictionaryToken catalog)
        {
            return OutputIntentParser.CreateAll(catalog, Scanner, TestFilterProvider.Instance,
                new FakeIccProfileService(), new IccProfileCache());
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

            public IReadOnlyList<double> ComponentRanges { get; } =
                Enumerable.Repeat(new[] { 0.0, 1.0 }, numberOfComponents).SelectMany(x => x).ToArray();
            
            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = null;
                return false;
            }
        }
    }
}
