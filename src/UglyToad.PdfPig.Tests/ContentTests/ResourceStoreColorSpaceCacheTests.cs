namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Graphics.Colors;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.Tests.Tokens;
    using Xunit;

    public class ResourceStoreColorSpaceCacheTests
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
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                });
        }

        private static ArrayToken CreateSeparationArray()
        {
            // [ /Separation /MySpot /DeviceRGB << Type2 tint function >> ]
            var tintFunction = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, new ArrayToken(new IToken[] { new NumericToken(0), new NumericToken(1) }) },
                { NameToken.C0, new ArrayToken(new IToken[] { new NumericToken(0), new NumericToken(0), new NumericToken(0) }) },
                { NameToken.C1, new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(0), new NumericToken(0) }) },
                { NameToken.N, new NumericToken(1) }
            });

            return new ArrayToken(new IToken[]
            {
                NameToken.Separation,
                NameToken.Create("MySpot"),
                NameToken.Devicergb,
                tintFunction
            });
        }

        private static DictionaryToken CreateShadingLikeDictionary(IndirectReference colorSpaceReference)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ShadingType, new NumericToken(2) },
                { NameToken.ColorSpace, new IndirectReferenceToken(colorSpaceReference) }
            });
        }

        [Fact]
        public void SharedIndirectColorSpaceIsParsedOnce()
        {
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(12, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, CreateSeparationArray());

            var store = BuildStore(scanner);
            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            // Two different consumers (e.g. two shadings) each referencing '/ColorSpace 12 0 R'.
            var details1 = store.GetColorSpaceDetails(NameToken.Separation, CreateShadingLikeDictionary(reference));
            var details2 = store.GetColorSpaceDetails(NameToken.Separation, CreateShadingLikeDictionary(reference));

            Assert.IsType<SeparationColorSpaceDetails>(details1);
            Assert.Same(details1, details2);
        }

        [Fact]
        public void DifferentIndirectColorSpacesAreParsedSeparately()
        {
            var scanner = new TestPdfTokenScanner();
            var reference1 = new IndirectReference(12, 0);
            var reference2 = new IndirectReference(13, 0);
            scanner.Objects[reference1] = new ObjectToken(XrefLocation.File(0), reference1, CreateSeparationArray());
            scanner.Objects[reference2] = new ObjectToken(XrefLocation.File(0), reference2, CreateSeparationArray());

            var store = BuildStore(scanner);
            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            var details1 = store.GetColorSpaceDetails(NameToken.Separation, CreateShadingLikeDictionary(reference1));
            var details2 = store.GetColorSpaceDetails(NameToken.Separation, CreateShadingLikeDictionary(reference2));

            Assert.NotSame(details1, details2);
        }

        [Fact]
        public void CacheIsClearedWhenResourceDictionaryChanges()
        {
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(12, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, CreateSeparationArray());

            var store = BuildStore(scanner);

            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));
            var details1 = store.GetColorSpaceDetails(NameToken.Separation, CreateShadingLikeDictionary(reference));
            store.UnloadResourceDictionary();

            // Default* substitutes in another resource scope can change the parse result, so the cache
            // must not survive the scope change.
            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));
            var details2 = store.GetColorSpaceDetails(NameToken.Separation, CreateShadingLikeDictionary(reference));

            Assert.NotSame(details1, details2);
        }

        [Fact]
        public void FilteredShadingStreamDictionaryIsCached()
        {
            // Shading types 4 to 7 are streams whose dictionaries carry a /Filter entry (e.g. FlateDecode).
            // Only CCITTFaxDecode influences colour space parsing, so these must still hit the cache.
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(12, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, CreateSeparationArray());

            var store = BuildStore(scanner);
            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            DictionaryToken CreateStreamShadingDictionary() => new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ShadingType, new NumericToken(7) },
                { NameToken.Filter, NameToken.FlateDecode },
                { NameToken.Length, new NumericToken(256) },
                { NameToken.ColorSpace, new IndirectReferenceToken(reference) }
            });

            var details1 = store.GetColorSpaceDetails(NameToken.Separation, CreateStreamShadingDictionary());
            var details2 = store.GetColorSpaceDetails(NameToken.Separation, CreateStreamShadingDictionary());

            Assert.IsType<SeparationColorSpaceDetails>(details1);
            Assert.Same(details1, details2);
        }

        [Fact]
        public void EqualInlineColorSpaceArraysShareOneInstance()
        {
            // The colour space definition is repeated inline (no indirect reference): value-based token
            // equality still allows the definitions to share a single parsed instance.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner);
            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            DictionaryToken CreateInlineDictionary() => new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ShadingType, new NumericToken(2) },
                { NameToken.ColorSpace, CreateSeparationArray() }
            });

            var details1 = store.GetColorSpaceDetails(NameToken.Separation, CreateInlineDictionary());
            var details2 = store.GetColorSpaceDetails(NameToken.Separation, CreateInlineDictionary());

            Assert.IsType<SeparationColorSpaceDetails>(details1);
            Assert.Same(details1, details2);
        }

        [Fact]
        public void ImageMaskDictionaryBypassesCache()
        {
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(12, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, CreateSeparationArray());

            var store = BuildStore(scanner);
            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            // A stencil mask referencing the same colour space object parses to a stencil, which must not
            // be cached under the reference and returned to non-mask consumers.
            var maskDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ImageMask, BooleanToken.True },
                { NameToken.ColorSpace, new IndirectReferenceToken(reference) }
            });

            var maskDetails = store.GetColorSpaceDetails(NameToken.Separation, maskDictionary);
            var plainDetails = store.GetColorSpaceDetails(NameToken.Separation, CreateShadingLikeDictionary(reference));

            Assert.IsType<IndexedColorSpaceDetails>(maskDetails);
            Assert.IsType<SeparationColorSpaceDetails>(plainDetails);
        }
    }
}
