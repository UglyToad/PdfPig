namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    public class ResourceStoreDefaultColorSpaceTests
    {
        private static ResourceStore BuildStore()
        {
            return new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
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
        public void DeviceRgbRequest_WithIndexedDefaultRgb_ReturnsDeviceColorSpace()
        {
            // 8.6.5.6: any colour space other than a Lab, Indexed, or Pattern colour space may be used as a
            // default. Selecting /DeviceRGB through the cs/CS operator must reject an invalid Indexed
            // DefaultRGB and fall back to the device space, exactly like the g/rg/k path.
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

            var details = store.GetColorSpaceDetails(
                NameToken.Devicergb,
                new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details);
        }

        [Fact]
        public void DeviceRgbRequest_WithPatternDefaultRgb_ReturnsDeviceColorSpace()
        {
            // 8.6.5.6: a Pattern colour space may not be used as a default colour space, so selecting
            // /DeviceRGB through the cs/CS operator must ignore a Pattern DefaultRGB and use the device space.
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

            var details = store.GetColorSpaceDetails(
                NameToken.Devicergb,
                new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details);
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

        /// <summary>
        /// A default of a permitted family that nonetheless fails to parse. The family check in
        /// <c>TryGetDefaultSubstitute</c> looks only at the name, so these get through it and only the parse
        /// discovers they are unusable.
        /// </summary>
        public static IEnumerable<object[]> UnparseableDefaults()
        {
            // /N is required on an ICCBased stream; without it the parse yields UnsupportedColorSpaceDetails.
            yield return
            [
                new ArrayToken(new IToken[]
                {
                    NameToken.Iccbased,
                    new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>()), new byte[] { 0x01 })
                })
            ];

            // 8.6.5.5 needs at least two elements, the name and the stream.
            yield return [new ArrayToken(new IToken[] { NameToken.Iccbased })];

            // A Separation array is four elements; three cannot be interpreted.
            yield return
            [
                new ArrayToken(new IToken[] { NameToken.Separation, NameToken.Create("Spot"), NameToken.Devicegray })
            ];
        }

        private static ResourceStore StoreWithDefault(NameToken defaultName, IToken definition)
        {
            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken> { { defaultName, definition } })
                },
            });

            var store = BuildStore();
            store.LoadResourceDictionary(resources);
            return store;
        }

        [Theory]
        [MemberData(nameof(UnparseableDefaults))]
        public void GetDeviceColorSpaceDetails_WithAnUnparseableDefaultRgb_ReturnsDeviceColorSpace(IToken definition)
        {
            // Handing back UnsupportedColorSpaceDetails would make the next rg throw
            // InvalidOperationException from GetColor, and nothing catches a colour operator, so the whole
            // page is lost. Losing only the substitution is the proportionate answer, and matches what a
            // default of a forbidden family already does.
            var store = StoreWithDefault(NameToken.DefaultRgb, definition);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB));
        }

        [Theory]
        [MemberData(nameof(UnparseableDefaults))]
        public void GetColorSpaceDetails_WithAnUnparseableDefaultRgb_ReturnsDeviceColorSpace(IToken definition)
        {
            // The cs/CS operator resolves substitutes through a second site (GetColorSpaceDetailsInternal),
            // which has to answer the same way, otherwise selecting /DeviceRGB by name would leave the
            // colour space Unsupported and silently turn every following sc/scn into a no-op.
            var store = StoreWithDefault(NameToken.DefaultRgb, definition);

            var details = store.GetColorSpaceDetails(
                NameToken.Devicergb,
                new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details);
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithAnUnparseableDefaultGray_ReturnsDeviceColorSpace()
        {
            var store = StoreWithDefault(NameToken.DefaultGray, new ArrayToken(new IToken[] { NameToken.Iccbased }));

            Assert.Same(DeviceGrayColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceGray));
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithAnUnparseableDefaultCmyk_ReturnsDeviceColorSpace()
        {
            var store = StoreWithDefault(NameToken.DefaultCmyk, new ArrayToken(new IToken[] { NameToken.Iccbased }));

            Assert.Same(DeviceCmykColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceCMYK));
        }

        [Fact]
        public void AParseableDefaultIsStillSubstituted()
        {
            // The no-regression guard: dropping unusable defaults must not drop usable ones.
            var calRgb = new ArrayToken(new IToken[]
            {
                NameToken.Calrgb,
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    {
                        NameToken.WhitePoint,
                        new ArrayToken(new IToken[]
                        {
                            new NumericToken(0.9505), new NumericToken(1.0), new NumericToken(1.089)
                        })
                    }
                })
            });

            var store = StoreWithDefault(NameToken.DefaultRgb, calRgb);

            Assert.Equal(ColorSpace.CalRGB, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB).Type);
        }

        /// <summary>
        /// Defaults that parse perfectly well but take a number of components the device operator does not
        /// supply. Nothing is malformed here - <c>/DefaultRGB /DeviceGray</c> is one valid name, and an
        /// <c>/ICCBased</c> with <c>/N 1</c> is a valid grey profile - so the family check upstream, which
        /// sees only the name, cannot tell.
        /// </summary>
        public static IEnumerable<object[]> MismatchedWidthDefaultsForRgb()
        {
            yield return [NameToken.Devicegray];                                        // 1 component
            yield return [NameToken.Devicecmyk];                                        // 4 components
            yield return [IccBased(1)];
            yield return [IccBased(4)];
        }

        private static ArrayToken IccBased(int numberOfComponents)
        {
            var stream = new StreamToken(
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.N, new NumericToken(numberOfComponents) }
                }),
                new byte[] { 0x01 });

            return new ArrayToken(new IToken[] { NameToken.Iccbased, stream });
        }

        [Theory]
        [MemberData(nameof(MismatchedWidthDefaultsForRgb))]
        public void GetDeviceColorSpaceDetails_WithAMismatchedWidthDefaultRgb_ReturnsDeviceColorSpace(IToken definition)
        {
            // The substitute receives the device operator's operands verbatim, so a three-operand rg against
            // a one- or four-component default throws ArgumentException from GetColor. Same uncaught path,
            // same lost page as an unparseable default, and from a file that is not even malformed.
            var store = StoreWithDefault(NameToken.DefaultRgb, definition);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB));
        }

        [Theory]
        [MemberData(nameof(MismatchedWidthDefaultsForRgb))]
        public void GetColorSpaceDetails_WithAMismatchedWidthDefaultRgb_ReturnsDeviceColorSpace(IToken definition)
        {
            var store = StoreWithDefault(NameToken.DefaultRgb, definition);

            var details = store.GetColorSpaceDetails(
                NameToken.Devicergb,
                new DictionaryToken(new Dictionary<NameToken, IToken>()));

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details);
        }

        [Fact]
        public void GetDeviceColorSpaceDetails_WithAThreeComponentDefaultGray_ReturnsDeviceColorSpace()
        {
            // The mismatch is judged against the space being substituted for, not against RGB.
            var store = StoreWithDefault(NameToken.DefaultGray, IccBased(3));

            Assert.Same(DeviceGrayColorSpaceDetails.Instance, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceGray));
        }

        [Fact]
        public void ADefaultOfTheRightWidthIsStillSubstituted()
        {
            // The control for the width check, in the same family as the mismatched cases: only the count
            // differs between this and the /N 1 and /N 4 cases above.
            var store = StoreWithDefault(NameToken.DefaultRgb, IccBased(3));

            Assert.Equal(ColorSpace.ICCBased, store.GetDeviceColorSpaceDetails(ColorSpace.DeviceRGB).Type);
        }

        [Theory]
        [MemberData(nameof(MismatchedWidthDefaultsForRgb))]
        public void TheRgOperatorSurvivesAMismatchedWidthDefaultRgb(IToken definition)
        {
            var store = StoreWithDefault(NameToken.DefaultRgb, definition);

            var state = new CurrentGraphicsState();
            var context = new ColorSpaceContext(() => state, store);
            state.ColorSpaceContext = context;

            var exception = Record.Exception(() => context.SetNonStrokingColorRgb(0.25, 0.5, 0.75));

            Assert.Null(exception);
            Assert.Same(DeviceRgbColorSpaceDetails.Instance, context.CurrentNonStrokingColorSpace);
        }

        [Theory]
        [MemberData(nameof(UnparseableDefaults))]
        public void TheRgOperatorSurvivesAnUnparseableDefaultRgb(IToken definition)
        {
            // What the guard is actually for. Nothing catches an exception from a colour operator
            // (BaseStreamProcessor.ProcessOperations runs them straight) so an unusable default used to cost
            // the whole page, in text extraction as much as in rendering. The operator now behaves as though
            // no default had been declared.
            var store = StoreWithDefault(NameToken.DefaultRgb, definition);

            var state = new CurrentGraphicsState();
            var context = new ColorSpaceContext(() => state, store);
            state.ColorSpaceContext = context;

            var exception = Record.Exception(() => context.SetNonStrokingColorRgb(0.25, 0.5, 0.75));

            Assert.Null(exception);
            Assert.Same(DeviceRgbColorSpaceDetails.Instance, context.CurrentNonStrokingColorSpace);

            var (r, g, b) = state.CurrentNonStrokingColor.ToRGBValues();
            Assert.Equal(0.25, r);
            Assert.Equal(0.5, g);
            Assert.Equal(0.75, b);
        }
    }
}
