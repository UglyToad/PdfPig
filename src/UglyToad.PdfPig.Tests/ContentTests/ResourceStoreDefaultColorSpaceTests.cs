namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Graphics.Colors;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.Tests.Tokens;
    using Xunit;

    public class ResourceStoreDefaultColorSpaceTests
    {
        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        private static ResourceStore BuildStore()
        {
            return new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                });
        }

        [Fact]
        public void DeviceRgbRequest_WithDefaultRgbInResources_UsesDefaultRgb()
        {
            // Resources/ColorSpace/DefaultRGB -> [ /CalRGB << /WhitePoint [0.9505 1 1.089] >> ]
            var calRgbDict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.WhitePoint,
                    new ArrayToken(new IToken[]
                    {
                        new NumericToken(0.9505),
                        new NumericToken(1.0),
                        new NumericToken(1.089),
                    })
                },
            });
            var defaultRgbArray = new ArrayToken(new IToken[] { NameToken.Calrgb, calRgbDict });

            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultRgb, defaultRgbArray },
                    })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);

            var details = store.GetColorSpaceDetails(
                NameToken.Devicergb,
                new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Equal(ColorSpace.CalRGB, details.Type);
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithDefaultRgbInResources_UsesDefaultRgb()
        {
            // The g/rg/k operators select a device colour space directly; per 8.6.5.6 the matching
            // Default* substitution must still apply (it takes precedence over any output intent).
            var calRgbDict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.WhitePoint,
                    new ArrayToken(new IToken[]
                    {
                        new NumericToken(0.9505),
                        new NumericToken(1.0),
                        new NumericToken(1.089),
                    })
                },
            });
            var defaultRgbArray = new ArrayToken(new IToken[] { NameToken.Calrgb, calRgbDict });

            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultRgb, defaultRgbArray },
                    })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);

            var details = store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB);

            Assert.Equal(ColorSpace.CalRGB, details.Type);
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithoutDefault_ReturnsDeviceColorSpace()
        {
            var store = BuildStore();
            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Same(DeviceGrayColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceGray));
            Assert.Same(DeviceRgbColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB));
            Assert.Same(DeviceCmykColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceCMYK));
        }

        [Fact]
        public void IndexedBase_WithDefaultRgbInResources_UsesDefaultRgb()
        {
            // 8.6.5.6: the base colour space of an Indexed space, when it is a device colour space, must
            // be replaced by the corresponding Default* colour space.
            var calRgbDict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.WhitePoint,
                    new ArrayToken(new IToken[]
                    {
                        new NumericToken(0.9505),
                        new NumericToken(1.0),
                        new NumericToken(1.089),
                    })
                },
            });
            var defaultRgbArray = new ArrayToken(new IToken[] { NameToken.Calrgb, calRgbDict });

            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultRgb, defaultRgbArray },
                    })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);

            // [ /Indexed /DeviceRGB 1 <000000FFFFFF> ] : 2 entries of 3 RGB components.
            var indexedArray = new ArrayToken(new IToken[]
            {
                NameToken.Indexed,
                NameToken.Devicergb,
                new NumericToken(1),
                new StringToken("ÿÿÿ"),
            });
            var imageDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ColorSpace, indexedArray },
            });

            var details = store.GetColorSpaceDetails(NameToken.Indexed, imageDictionary);

            var indexed = Assert.IsType<IndexedColorSpaceDetails>(details);
            Assert.Equal(ColorSpace.CalRGB, indexed.BaseColorSpace.Type);
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithIndexedDefaultRgb_ReturnsDeviceColorSpace()
        {
            // 8.6.5.6: any colour space other than a Lab, Indexed, or Pattern colour space may be used as a
            // default colour space. An Indexed DefaultRGB is therefore invalid and must be ignored, leaving
            // the device colour space in place.
            // DefaultRGB -> [ /Indexed /DeviceGray 1 <00FF> ]
            var defaultRgbArray = new ArrayToken(new IToken[]
            {
                NameToken.Indexed,
                NameToken.Devicegray,
                new NumericToken(1),
                new StringToken("ÿ"),
            });

            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultRgb, defaultRgbArray },
                    })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB));
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithSelfReferentialIndexedDefaultRgb_ReturnsDeviceColorSpace()
        {
            // Regression: a self-referential default - /DefaultRGB defined as an Indexed space whose base
            // is /DeviceRGB - must not recurse forever. Resolving DeviceRGB resolves the Indexed default,
            // whose base resolves DeviceRGB again; the re-entrancy guard breaks the loop and the Indexed
            // default is rejected, leaving the device colour space.
            // DefaultRGB -> [ /Indexed /DeviceRGB 1 <000000FFFFFF> ]
            var defaultRgbArray = new ArrayToken(new IToken[]
            {
                NameToken.Indexed,
                NameToken.Devicergb,
                new NumericToken(1),
                new StringToken("ÿÿÿ"),
            });

            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultRgb, defaultRgbArray },
                    })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB));
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithPatternDefaultRgb_ReturnsDeviceColorSpace()
        {
            // 8.6.5.6: a Pattern colour space may not be used as a default colour space, so a Pattern
            // DefaultRGB must be ignored, leaving the device colour space in place.
            // DefaultRGB -> /Pattern
            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultRgb, NameToken.Pattern },
                    })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB));
        }
    }
}
