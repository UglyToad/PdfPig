namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Filters;
    using PdfPig.Graphics.Colors;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;

    public class ColorSpaceCacheTests
    {
        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        [Fact]
        public void ShadingsSharingColorSpaceObjectShareOneInstance()
        {
            // ColorIssue.pdf contains nine shadings; eight are ShadingType 7 streams (so their
            // dictionaries carry /Filter /FlateDecode) which all reference '/ColorSpace 8 0 R',
            // a six-colorant DeviceN colour space. They must share a single parsed instance.
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("ColorIssue.pdf"));

            var page = document.GetPage(1);
            var scanner = document.Structure.TokenScanner;

            Assert.True(page.Dictionary.TryGet(NameToken.Resources, scanner, out DictionaryToken resources));

            var store = new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                new FilterProviderWithLookup(DefaultFilterProvider.Instance),
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                });

            store.LoadResourceDictionary(resources);

            var deviceNColorSpaces = new List<ColorSpaceDetails>();
            for (var i = 0; i <= 8; i++)
            {
                var shading = store.GetShading(NameToken.Create($"Sh{i}"));
                if (shading.ColorSpace is DeviceNColorSpaceDetails)
                {
                    deviceNColorSpaces.Add(shading.ColorSpace);
                }
            }

            Assert.Equal(8, deviceNColorSpaces.Count);
            Assert.All(deviceNColorSpaces, cs => Assert.Same(deviceNColorSpaces[0], cs));
        }
    }
}
