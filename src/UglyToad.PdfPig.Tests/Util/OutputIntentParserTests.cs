namespace UglyToad.PdfPig.Tests.Util
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Core;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Tokens;
    using PdfPig.Util;
    using Tokens;

    /// <summary>
    /// Covers <see cref="OutputIntentParser"/>: every entry of an <c>/OutputIntents</c> array is parsed, in the
    /// order the array wrote them, and the embedded <c>/DestOutputProfile</c> is resolved through the shared
    /// <see cref="IccProfileCache"/> or left null, without inflating anything, when nothing can read it.
    /// </summary>
    public class OutputIntentParserTests
    {
        private static readonly TestPdfTokenScanner Scanner = new();

        [Fact]
        public void EveryDeclaredIntentIsReturned_InArrayOrder()
        {
            // The list is the primitive: a conformance check needs every entry the file
            // declared, and it needs them in the order the file wrote them.
            var catalog = Catalog(
                Intent("GTS_PDFA1", "FOGRA39", withProfile: false),
                Intent("GTS_PDFX", "FOGRA51", withProfile: true),
                Intent("ISO_PDFE1", "PDFE", withProfile: false));

            var all = CreateAll(catalog);

            Assert.Equal(3, all.Count);
            Assert.Equal(["GTS_PDFA1", "GTS_PDFX", "ISO_PDFE1"], all.Select(x => x.Name));
            Assert.Equal(["FOGRA39", "FOGRA51", "PDFE"], all.Select(x => x.OutputConditionIdentifier));

            // No entry is preferred over another, and no entry is dropped for lacking a profile.
            Assert.Null(all[0].DestOutputProfile);
            Assert.NotNull(all[1].DestOutputProfile);
            Assert.Null(all[2].DestOutputProfile);
        }

        [Fact]
        public void EntriesOfTheSameSubtypeAreAllKept()
        {
            // Nothing de-duplicates by subtype: two PDF/X entries are two entries.
            var catalog = Catalog(
                Intent("GTS_PDFX", "FIRST", withProfile: true),
                Intent("GTS_PDFX", "SECOND", withProfile: true));

            Assert.Equal(["FIRST", "SECOND"], CreateAll(catalog).Select(x => x.OutputConditionIdentifier));
        }

        [Fact]
        public void ReturnsAnEmptyListWhenThereIsNoOutputIntentsArray()
        {
            Assert.Empty(CreateAll(new DictionaryToken(new Dictionary<NameToken, IToken>())));
        }

        [Fact]
        public void ReturnsAnEmptyListWhenTheOutputIntentsArrayIsEmpty()
        {
            Assert.Empty(CreateAll(Catalog()));
        }

        [Fact]
        public void ReturnsAnEmptyListWhenOutputIntentsIsNotAnArray()
        {
            var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new NumericToken(42) }
            });

            Assert.Empty(CreateAll(catalog));
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
        public void IntentDictionariesWrittenAsIndirectReferencesAreResolved()
        {
            // Real files write each intent as its own object.
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(11, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference,
                Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var catalog = Catalog(new IndirectReferenceToken(reference));

            var intent = Assert.Single(OutputIntentParser.CreateAll(catalog, scanner, TestFilterProvider.Instance,
                new TestIccProfileService(4), new IccProfileCache()));

            Assert.Equal("FOGRA51", intent.OutputConditionIdentifier);
            Assert.NotNull(intent.DestOutputProfile);
        }

        [Fact]
        public void AnOutputIntentsArrayWrittenAsAnIndirectReferenceIsResolved()
        {
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(12, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference,
                new ArrayToken([Intent("GTS_PDFX", "FOGRA51", withProfile: false)]));

            var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new IndirectReferenceToken(reference) }
            });

            var intent = Assert.Single(OutputIntentParser.CreateAll(catalog, scanner, TestFilterProvider.Instance,
                new TestIccProfileService(4), new IccProfileCache()));

            Assert.Equal("GTS_PDFX", intent.Name);
        }

        [Fact]
        public void AbsentEntriesAreNullRatherThanEmptyStrings()
        {
            // /S and /OutputConditionIdentifier are required, so a conformance check has to be able to tell
            // "absent" from "present but empty".
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

            // The optional PDF 2.0 entries are absent too, and an absent one is null, not empty.
            Assert.Null(intent.DestOutputProfileRef);
            Assert.Null(intent.MixingHints);
            Assert.Null(intent.SpectralData);
        }

        [Fact]
        public void AnEntryWithoutASubtypeIsStillReturned()
        {
            // /S is required, but a file that omits it is not a reason to hide the entry, reporting the
            // absence is the whole point of keeping Name nullable.
            var withoutSubtype = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputConditionIdentifier, new StringToken("NO_SUBTYPE") },
                { NameToken.DestOutputProfile, ProfileStream() }
            });

            var intent = Assert.Single(CreateAll(Catalog(withoutSubtype)));

            Assert.Null(intent.Name);
            Assert.Equal("NO_SUBTYPE", intent.OutputConditionIdentifier);
            Assert.NotNull(intent.DestOutputProfile);
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
        public void MixingHintsAndSpectralDataArePassedThrough()
        {
            // PDF 2.0 entries PdfPig does not interpret: they are handed back as written for a caller that
            // does, so what matters is that they survive the parse untouched.
            var mixingHints = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Solidities"), new DictionaryToken(new Dictionary<NameToken, IToken>()) }
            });

            var spectralData = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("PANTONE 123 C"), new NumericToken(1) }
            });

            var withHints = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.MixingHints, mixingHints },
                { NameToken.SpectralData, spectralData }
            });

            var intent = Assert.Single(CreateAll(Catalog(withHints)));

            Assert.Same(mixingHints, intent.MixingHints);
            Assert.Same(spectralData, intent.SpectralData);
        }

        [Fact]
        public void DestOutputProfileRefIsParsed()
        {
            // ISO 32000-2 Table 402. The profile is not embedded, so it is described rather than resolved.
            var colorantTable = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Cyan"), new NumericToken(0) }
            });

            var urls = new ArrayToken([new StringToken("http://www.color.org/profile.icc")]);

            // /ICCVersion and /CheckSum are byte strings, so they come back as the bytes the file wrote
            // rather than as text.
            var iccVersion = new byte[] { 0x02, 0x10, 0x00, 0x00 };
            var checkSum = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            var profileRef = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ProfileCS, new StringToken("CMYK") },
                { NameToken.ProfileName, new StringToken("Coated FOGRA39") },
                { NameToken.IccVersion, new StringToken("ignored", StringToken.Encoding.Iso88591, iccVersion) },
                { NameToken.CheckSum, new StringToken("ignored", StringToken.Encoding.Iso88591, checkSum) },
                { NameToken.ColorantTable, colorantTable },
                { NameToken.Urls, urls }
            });

            var withRef = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.DestOutputProfileRef, profileRef }
            });

            var intent = Assert.Single(CreateAll(Catalog(withRef)));

            Assert.NotNull(intent.DestOutputProfileRef);
            Assert.Equal("CMYK", intent.DestOutputProfileRef!.ProfileCS);
            Assert.Equal("Coated FOGRA39", intent.DestOutputProfileRef.ProfileName);
            Assert.Equal(iccVersion, intent.DestOutputProfileRef.ICCVersion.ToArray());
            Assert.Equal(checkSum, intent.DestOutputProfileRef.CheckSum.ToArray());
            Assert.Same(colorantTable, intent.DestOutputProfileRef.ColorantTable);
            Assert.Same(urls, intent.DestOutputProfileRef.Urls);

            // A referenced profile is not an embedded one: nothing is resolved from it.
            Assert.Null(intent.DestOutputProfile);
        }

        [Fact]
        public void DestOutputProfileRefAcceptsAProfileCsWrittenAsAName()
        {
            // Table 402 says ProfileCS is a string, but the ICC signature reads like a name and files write
            // it as one; the string form above and this one must both be understood.
            var profileRef = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ProfileCS, NameToken.Create("CMYK") }
            });

            var withRef = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.DestOutputProfileRef, profileRef }
            });

            var intent = Assert.Single(CreateAll(Catalog(withRef)));

            Assert.Equal("CMYK", intent.DestOutputProfileRef!.ProfileCS);
        }

        [Fact]
        public void AnEmptyDestOutputProfileRefLeavesEveryEntryNull()
        {
            var withRef = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.DestOutputProfileRef, new DictionaryToken(new Dictionary<NameToken, IToken>()) }
            });

            var reference = Assert.Single(CreateAll(Catalog(withRef))).DestOutputProfileRef;

            Assert.NotNull(reference);
            Assert.Null(reference!.ProfileCS);
            Assert.Null(reference.ProfileName);
            Assert.Equal(0, reference.ICCVersion.Length);
            Assert.Equal(0, reference.CheckSum.Length);
            Assert.Null(reference.ColorantTable);
            Assert.Null(reference.Urls);
        }

        [Fact]
        public void TheProfileTheServiceReturnedIsTheOneHandedBack()
        {
            var catalog = Catalog(Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var profile = Assert.Single(CreateAll(catalog)).DestOutputProfile;

            Assert.NotNull(profile);
            Assert.Equal(4, profile!.NumberOfComponents);
        }

        [Fact]
        public void ADestOutputProfileThatIsNotAStreamIsIgnored()
        {
            var broken = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create("GTS_PDFX") },
                { NameToken.OutputConditionIdentifier, new StringToken("FOGRA51") },
                { NameToken.DestOutputProfile, new NumericToken(0) }
            });

            var intent = Assert.Single(CreateAll(Catalog(broken)));

            Assert.Equal("FOGRA51", intent.OutputConditionIdentifier);
            Assert.Null(intent.DestOutputProfile);
        }

        [Fact]
        public void WithoutAProfileService_StillReportsTheOutputCondition()
        {
            // The descriptive entries are what PDF/A and PDF/X conformance checking reads and have nothing
            // to do with colour management, so an absent IIccProfileService must not hide them, it is only
            // /DestOutputProfile that goes unresolved.
            var catalog = Catalog(Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var intent = Assert.Single(OutputIntentParser.CreateAll(catalog, Scanner,
                TestFilterProvider.Instance, null, new IccProfileCache()));

            Assert.Equal("GTS_PDFX", intent.Name);
            Assert.Equal("FOGRA51", intent.OutputConditionIdentifier);
            Assert.Null(intent.DestOutputProfile);
        }

        [Fact]
        public void WithoutAProfileService_TheProfileStreamIsNeverDecoded()
        {
            // Not resolving the profile has to mean not paying for it either: an embedded CMYK profile is
            // routinely megabytes, and nothing is going to look at it.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider();
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var intent = Assert.Single(
                OutputIntentParser.CreateAll(catalog, scanner, filters, null, new IccProfileCache()));

            Assert.Null(intent.DestOutputProfile);
            Assert.Equal(0, filters.DecodeCount);
        }

        [Fact]
        public void WhenTheServiceDeclinesTheProfileTheIntentIsStillReturned()
        {
            // An implementation that does not recognise the profile costs the intent its profile and nothing
            // else. The caller falls back to the colour space's alternate.
            var catalog = Catalog(Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var intent = Assert.Single(OutputIntentParser.CreateAll(catalog, Scanner,
                TestFilterProvider.Instance, new DecliningIccProfileService(), new IccProfileCache()));

            Assert.Equal("FOGRA51", intent.OutputConditionIdentifier);
            Assert.Null(intent.DestOutputProfile);
        }

        [Fact]
        public void WhenTheServiceThrowsTheIntentIsStillReturned()
        {
            // TryGetProfile is third-party code; a profile that makes it throw is no worse than one it
            // declines, and must not take the document's output intents down with it.
            var catalog = Catalog(Intent("GTS_PDFX", "FOGRA51", withProfile: true));

            var intent = Assert.Single(OutputIntentParser.CreateAll(catalog, Scanner,
                TestFilterProvider.Instance, new ThrowingIccProfileService(), new IccProfileCache()));

            Assert.Equal("FOGRA51", intent.OutputConditionIdentifier);
            Assert.Null(intent.DestOutputProfile);
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

            var first = Assert.Single(
                OutputIntentParser.CreateAll(catalog, scanner, filters, new TestIccProfileService(4), cache));
            var second = Assert.Single(
                OutputIntentParser.CreateAll(catalog, scanner, filters, new TestIccProfileService(4), cache));

            Assert.NotNull(first.DestOutputProfile);
            Assert.NotNull(second.DestOutputProfile);
            Assert.Same(first.DestOutputProfile, second.DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void AProfileWrittenDirectlyIntoTheIntentIsStillResolved()
        {
            // A stream shall be an indirect object (7.3.8), so this shape only comes from a malformed file.
            // The profile is still resolved, it just does not go through the cache (there is no reference to
            // key it by), so each one costs its own inflate.
            var filters = new CountingFilterProvider();
            var cache = new IccProfileCache();
            var catalog = Catalog(
                Intent("GTS_PDFX", "FOGRA51", withProfile: true),
                Intent("GTS_PDFA1", "FOGRA39", withProfile: true));

            var all = OutputIntentParser.CreateAll(catalog, Scanner, filters, new TestIccProfileService(4), cache);

            Assert.Equal(2, all.Count);
            Assert.NotNull(all[0].DestOutputProfile);
            Assert.NotNull(all[1].DestOutputProfile);
            Assert.Equal(2, filters.DecodeCount);
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
                new TestIccProfileService(4));

            Assert.Equal(1, filters.DecodeCount);

            var intent = Assert.Single(
                OutputIntentParser.CreateAll(catalog, scanner, filters, new TestIccProfileService(4), cache));

            Assert.NotNull(intent.DestOutputProfile);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void FallsBackWhenTheProfileStreamCannotBeDecoded()
        {
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            var intent = Assert.Single(OutputIntentParser.CreateAll(catalog, scanner, filters,
                new TestIccProfileService(4), new IccProfileCache()));

            Assert.Equal("FOGRA51", intent.OutputConditionIdentifier);
            Assert.Null(intent.DestOutputProfile);
        }

        [Fact]
        public void AProfileStreamThatCannotBeDecodedIsNotRetried()
        {
            // The null is cached like any other answer: retrying a corrupt multi-megabyte stream on every
            // page is exactly the cost the cache exists to avoid.
            var scanner = new TestPdfTokenScanner();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var cache = new IccProfileCache();
            var catalog = Catalog(IntentReferencingProfile(scanner, objectNumber: 7));

            for (int i = 0; i < 3; i++)
            {
                Assert.Null(Assert.Single(OutputIntentParser.CreateAll(catalog, scanner, filters,
                    new TestIccProfileService(4), cache)).DestOutputProfile);
            }

            Assert.Equal(1, filters.DecodeCount);
        }

        /// <summary>
        /// An intent whose <c>/DestOutputProfile</c> is written as an indirect reference, as it is in a real
        /// file, which is also what lets the byte cache recognise the profile without touching the stream.
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

        private static IReadOnlyList<OutputIntent> CreateAll(DictionaryToken catalog)
        {
            return OutputIntentParser.CreateAll(catalog, Scanner, TestFilterProvider.Instance,
                new TestIccProfileService(4), new IccProfileCache());
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
        /// A stand-in for an embedded ICC profile. The bytes are never interpreted here: the parser hands
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

        /// <summary>
        /// An implementation that does not recognise the profile it was handed.
        /// </summary>
        private sealed class DecliningIccProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
            {
                profile = null;
                return false;
            }
            public bool UseOutputIntent => false;

            public string? PreferredOutputIntentSubtype => null;
        }

        /// <summary>
        /// An implementation that fails outright rather than returning false.
        /// </summary>
        private sealed class ThrowingIccProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
            {
                throw new InvalidOperationException("Malformed profile.");
            }
            public bool UseOutputIntent => false;

            public string? PreferredOutputIntentSubtype => null;
        }
    }
}
