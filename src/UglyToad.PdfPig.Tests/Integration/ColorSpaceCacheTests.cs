namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using PdfPig.Content;
    using PdfPig.Filters;
    using PdfPig.Graphics.Colors;
    using PdfPig.Tokens;

    public class ColorSpaceCacheTests
    {
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
                null,
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

        [Fact]
        public void ImagesSharingAnIccProfileShareOneColorSpaceInstance()
        {
            // Page 2 of 2108.11480.pdf carries four images with an /ICCBased colour space. The colour
            // space cache is keyed on the definition token, so this only holds while that definition
            // stays in the form it was written in - a name followed by an indirect reference to the
            // profile. Were the profile stream substituted into the array, matching these four would
            // mean hashing and comparing the whole profile on every lookup.
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("2108.11480.pdf"));

            var iccColorSpaces = document.GetPage(2).GetImages()
                .Select(x => x.ColorSpaceDetails)
                .OfType<ICCBasedColorSpaceDetails>()
                .ToList();

            Assert.Equal(4, iccColorSpaces.Count);
            Assert.All(iccColorSpaces, cs => Assert.Same(iccColorSpaces[0], cs));
        }
    }
}
