namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// Issue #1390: a page with thousands of form XObjects sharing one resource dictionary re-expanded that
    /// dictionary on every invocation, which is quadratic in the number of resource entries.
    /// </summary>
    public class ResourceStoreCachingTests
    {
        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        private static ResourceStore BuildStore(TestPdfTokenScanner scanner)
        {
            return new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                });
        }

        /// <summary>
        /// Builds `&lt;&lt; /ExtGState 20 0 R &gt;&gt;` where object 20 is `&lt;&lt; /G0 21 0 R /G1 22 0 R &gt;&gt;`,
        /// matching the shape of the document in issue #1390.
        /// </summary>
        private static DictionaryToken RegisterResourcesWithIndirectExtGState(TestPdfTokenScanner scanner)
        {
            var extGStateReference = new IndirectReference(20, 0);
            var g0Reference = new IndirectReference(21, 0);
            var g1Reference = new IndirectReference(22, 0);

            void Register(IndirectReference reference, IToken token)
                => scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, token);

            Register(g0Reference, new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Lw, new NumericToken(1) }
            }));

            Register(g1Reference, new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Lw, new NumericToken(2) }
            }));

            Register(extGStateReference, new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("G0"), new IndirectReferenceToken(g0Reference) },
                { NameToken.Create("G1"), new IndirectReferenceToken(g1Reference) }
            }));

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ExtGState, new IndirectReferenceToken(extGStateReference) }
            });
        }

        [Fact]
        public void ReloadingTheSameResourceDictionaryResolvesNoFurtherObjects()
        {
            var scanner = new TestPdfTokenScanner();
            var resources = RegisterResourcesWithIndirectExtGState(scanner);
            var store = BuildStore(scanner);

            store.LoadResourceDictionary(resources);
            store.UnloadResourceDictionary();

            var afterFirstLoad = scanner.GetCallCount;

            store.LoadResourceDictionary(resources);

            Assert.Equal(afterFirstLoad, scanner.GetCallCount);
        }

        [Fact]
        public void ReloadingTheSameResourceDictionaryStillResolvesItsEntries()
        {
            var scanner = new TestPdfTokenScanner();
            var resources = RegisterResourcesWithIndirectExtGState(scanner);
            var store = BuildStore(scanner);

            store.LoadResourceDictionary(resources);
            var firstLoad = store.GetExtendedGraphicsStateDictionary(NameToken.Create("G1"));
            store.UnloadResourceDictionary();

            store.LoadResourceDictionary(resources);
            var secondLoad = store.GetExtendedGraphicsStateDictionary(NameToken.Create("G1"));

            Assert.Same(firstLoad, secondLoad);
        }
    }
}
