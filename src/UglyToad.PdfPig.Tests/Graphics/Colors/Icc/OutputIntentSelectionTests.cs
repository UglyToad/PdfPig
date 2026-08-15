namespace UglyToad.PdfPig.Tests.Graphics.Colors.Icc
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
    using Xunit;

    /// <summary>
    /// <c>/OutputIntents</c> is an array and 14.11.5 does not say which entry wins, so a consumer that wants
    /// to colour-manage device colours has to choose one. <see cref="OutputIntentParser"/> deliberately parses
    /// every entry unranked and in array order - a conformance check needs exactly what the file declared -
    /// which leaves the choosing to <see cref="OutputIntent.SelectForColorManagement"/>, covered here.
    /// </summary>
    public class OutputIntentSelectionTests
    {
        private sealed class StubProfile : IIccProfile
        {
            public int NumberOfComponents => 4;

            public IReadOnlyList<double> ComponentRanges => [0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = null;
                return false;
            }
        }

        /// <summary>
        /// An output intent identified by its <c>/OutputConditionIdentifier</c>, so that which entry was
        /// picked is visible in the assertion.
        /// </summary>
        private static OutputIntent Intent(string? subtype, string identifier, bool withProfile)
            => new OutputIntent(subtype, null, identifier, null, null,
                withProfile ? new StubProfile() : null, null, null, null);

        private static string? Select(params OutputIntent[] intents)
            => OutputIntent.SelectForColorManagement(intents)?.OutputConditionIdentifier;

        [Fact]
        public void PrefersPdfXOverPdfAWhenBothCarryAProfile()
        {
            // PDF/X exists to pin down device colour, which is the question being asked; PDF/A is about
            // archiving and merely happens to carry a profile too.
            Assert.Equal("X", Select(
                Intent("GTS_PDFA1", "A", withProfile: true),
                Intent("GTS_PDFX", "X", withProfile: true)));
        }

        [Fact]
        public void PrefersPdfXWhenWrittenFirstToo()
        {
            // Guards against the rule accidentally being "prefer the last entry" rather than "prefer PDF/X".
            Assert.Equal("X", Select(
                Intent("GTS_PDFX", "X", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void PrefersPdfAOverAnUnknownSubtype()
        {
            Assert.Equal("A", Select(
                Intent("ISO_PDFE1", "E", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void TreatsAMissingSubtypeAsLowestRank()
        {
            // /S is required, so an entry without one cannot claim to be the PDF/X intent.
            Assert.Equal("A", Select(
                Intent(null, "NONE", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void AnUnknownSubtypeIsStillUsedWhenItIsTheOnlyCandidate()
        {
            // Ranked last, not excluded: an extension subtype carrying a profile still characterizes a device.
            Assert.Equal("E", Select(Intent("ISO_PDFE1", "E", withProfile: true)));
        }

        [Fact]
        public void AUsableProfileBeatsABetterSubtypeWithoutOne()
        {
            // Only an embedded /DestOutputProfile can drive a conversion, so profile availability is the
            // primary key and the subtype only breaks ties among entries that have one.
            Assert.Equal("A", Select(
                Intent("GTS_PDFX", "X", withProfile: false),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void KeepsArrayOrderAmongEntriesOfTheSameRank()
        {
            Assert.Equal("FIRST", Select(
                Intent("GTS_PDFX", "FIRST", withProfile: true),
                Intent("GTS_PDFX", "SECOND", withProfile: true)));
        }

        [Fact]
        public void ReturnsNullWhenNoEntryCarriesAProfile()
        {
            // Not a fallback to the best-ranked entry: the result exists to be converted through, and one
            // that cannot be would only move the null check to the caller.
            Assert.Null(OutputIntent.SelectForColorManagement([
                Intent("GTS_PDFX", "X", withProfile: false),
                Intent("GTS_PDFA1", "A", withProfile: false)
            ]));
        }

        [Fact]
        public void ReturnsNullForAnEmptyList()
        {
            // What a document declaring no output intents parses to.
            Assert.Null(OutputIntent.SelectForColorManagement([]));
        }

        [Fact]
        public void ReturnsNullForNull()
        {
            // How a consumer suppresses an intent that does exist - see CurrentGraphicsState.OutputIntents.
            Assert.Null(OutputIntent.SelectForColorManagement(null));
        }

        [Fact]
        public void SubtypeMatchingIsExact()
        {
            // /S is a name object with spec-defined values; a near miss is an unknown subtype, not PDF/X.
            Assert.Equal("A", Select(
                Intent("gts_pdfx", "LOWER", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void TheSelectedEntryIsTheOneCarryingTheProfileTheCallerWillUse()
        {
            // The whole point of the return value: DestOutputProfile is non-null on it.
            var selected = OutputIntent.SelectForColorManagement([
                Intent("GTS_PDFA1", "A", withProfile: false),
                Intent("GTS_PDFX", "X", withProfile: true)
            ]);

            Assert.NotNull(selected);
            Assert.Equal("GTS_PDFX", selected!.Name);
            Assert.NotNull(selected.DestOutputProfile);
        }

        [Fact]
        public void SingleDeclaredIntentIsSimplyReturned()
        {
            // All but every real file: one intent, and it is the answer whatever its subtype.
            Assert.Equal("FOGRA51", Select(Intent("GTS_PDFX", "FOGRA51", withProfile: true)));
        }

        private static string? Select(string? preferredSubtype, params OutputIntent[] intents)
            => OutputIntent.SelectForColorManagement(intents, preferredSubtype)?.OutputConditionIdentifier;

        [Fact]
        public void APreferredSubtypeOutranksTheBuiltInOrder()
        {
            // The built-in order answers "which entry characterizes the output device", but a caller may be
            // asking something narrower - proofing a PDF/A archive, say - and knows better than the default.
            Assert.Equal("A", Select("GTS_PDFA1",
                Intent("GTS_PDFX", "X", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void APreferredSubtypeMayBeOneTheBuiltInOrderRanksLast()
        {
            // The point of the parameter: an extension subtype is only ranked last by default, and a caller
            // that wants it can say so.
            Assert.Equal("E", Select("ISO_PDFE1",
                Intent("GTS_PDFX", "X", withProfile: true),
                Intent("ISO_PDFE1", "E", withProfile: true)));
        }

        [Fact]
        public void APreferredSubtypeNoEntryDeclares_LeavesTheBuiltInOrder()
        {
            // A preference is a preference, not a filter: asking for something the file does not contain
            // gets the entry that would have been chosen anyway, not null.
            Assert.Equal("X", Select("ISO_PDFE1",
                Intent("GTS_PDFX", "X", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void APreferredSubtypeStillRequiresAnEmbeddedProfile()
        {
            // Profile availability stays the primary key. A preference cannot promote an entry that cannot
            // drive a conversion, or the return value stops meaning what every caller relies on.
            Assert.Equal("X", Select("GTS_PDFA1",
                Intent("GTS_PDFX", "X", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: false)));
        }

        [Fact]
        public void KeepsArrayOrderAmongEntriesMatchingThePreferredSubtype()
        {
            Assert.Equal("FIRST", Select("GTS_PDFA1",
                Intent("GTS_PDFA1", "FIRST", withProfile: true),
                Intent("GTS_PDFA1", "SECOND", withProfile: true)));
        }

        [Fact]
        public void PreferredSubtypeMatchingIsExact()
        {
            // As for the built-in subtypes: /S is a name object, and a near miss is a different subtype.
            Assert.Equal("X", Select("gts_pdfa1",
                Intent("GTS_PDFX", "X", withProfile: true),
                Intent("GTS_PDFA1", "A", withProfile: true)));
        }

        [Fact]
        public void NoPreferredSubtypeIsTheBuiltInOrder()
        {
            // The parameter is optional, so passing nothing and passing null have to agree.
            Assert.Equal("X", Select((string?)null,
                Intent("GTS_PDFA1", "A", withProfile: true),
                Intent("GTS_PDFX", "X", withProfile: true)));
        }
    }
}
