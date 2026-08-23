namespace UglyToad.PdfPig.Tests.ContentTests
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Core;
    using PdfPig.Tokens;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// Covers the <c>/Alternate</c> entry of an <c>/ICCBased</c> colour space (8.6.5.5, Table 66).
    /// <para>
    /// Without an <see cref="PdfPig.Graphics.Colors.Icc.IIccProfileService"/> configured (the default) the
    /// alternate is the only thing that decides the rendered colour, so what is accepted here is not a
    /// fallback detail.
    /// </para>
    /// </summary>
    public class IccBasedAlternateColorSpaceTests
    {
        private static readonly DictionaryToken Empty = new DictionaryToken(new Dictionary<NameToken, IToken>());

        private static ArrayToken CalRgbArray()
        {
            var calRgb = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.WhitePoint,
                    new ArrayToken(new IToken[]
                    {
                        new NumericToken(0.9505), new NumericToken(1.0), new NumericToken(1.089)
                    })
                }
            });

            return new ArrayToken(new IToken[] { NameToken.Calrgb, calRgb });
        }

        /// <summary>
        /// <c>[/Separation /Spot /DeviceCMYK &lt;tint&gt;]</c> - one input component, four out of its base.
        /// </summary>
        private static ArrayToken SeparationOverCmykArray()
        {
            var tint = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, new ArrayToken(new IToken[] { new NumericToken(0), new NumericToken(1) }) },
                {
                    NameToken.C0,
                    new ArrayToken(new IToken[]
                    {
                        new NumericToken(0), new NumericToken(0), new NumericToken(0), new NumericToken(0)
                    })
                },
                {
                    NameToken.C1,
                    new ArrayToken(new IToken[]
                    {
                        new NumericToken(0), new NumericToken(0), new NumericToken(0), new NumericToken(1)
                    })
                },
                { NameToken.N, new NumericToken(1) }
            });

            return new ArrayToken(new IToken[]
            {
                NameToken.Separation, NameToken.Create("Spot"), NameToken.Devicecmyk, tint
            });
        }

        /// <summary>
        /// Parse <c>[/ICCBased &lt;stream&gt;]</c> where the profile stream declares <paramref name="n"/>
        /// components and, when given, the supplied <c>/Alternate</c>.
        /// </summary>
        private static ICCBasedColorSpaceDetails Parse(IToken? alternate, int n,
            TestPdfTokenScanner? scanner = null, DictionaryToken? resources = null,
            IToken[]? trailingArrayElements = null)
        {
            var streamDictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.N, new NumericToken(n) }
            };

            if (alternate is not null)
            {
                streamDictionary[NameToken.Alternate] = alternate;
            }

            // No IIccProfileService is configured, so the bytes are never decoded - only /N, /Alternate
            // and /Range are read off the stream dictionary.
            var profileStream = new StreamToken(new DictionaryToken(streamDictionary), new byte[] { 0x01 });

            var store = new ResourceStore(
                scanner ?? new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true });

            store.LoadResourceDictionary(resources ?? Empty);

            var colorSpaceArray = new List<IToken> { NameToken.Iccbased, profileStream };
            if (trailingArrayElements is not null)
            {
                colorSpaceArray.AddRange(trailingArrayElements);
            }

            var details = store.GetColorSpaceDetails(NameToken.Iccbased,
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.ColorSpace, new ArrayToken(colorSpaceArray) }
                }));

            return Assert.IsType<ICCBasedColorSpaceDetails>(details);
        }

        [Fact]
        public void ArrayWithTrailingJunkElements_IsStillParsed()
        {
            // Only the first two elements are read (PDFBox requires size() >= 2 and ignores the rest)
            var details = Parse(NameToken.Devicecmyk, n: 4,
                trailingArrayElements: [new NumericToken(0), NameToken.Create("Junk")]);

            Assert.Equal(4, details.NumberOfColorComponents);
            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void SeparationAlternate_ReportsItsOwnBaseComponentCount()
        {
            // /N 1 with [/Separation /Spot /DeviceCMYK <tint>]: one operand in, four components out of the
            // alternate's base. BaseNumberOfColorComponents describes what Transform produces, so it has to
            // be the alternate's four rather than /N's one.
            var details = Parse(SeparationOverCmykArray(), n: 1);

            Assert.Equal(ColorSpace.Separation, details.AlternateColorSpace.Type);
            Assert.Equal(1, details.NumberOfColorComponents);
            Assert.Equal(4, details.BaseNumberOfColorComponents);

            Span<byte> samples = stackalloc byte[2] { 0, 255 };
            var transformed = details.Transform(samples, RenderingIntent.RelativeColorimetric);
            Assert.Equal(2 * details.BaseNumberOfColorComponents, transformed.Length);
        }

        [Fact]
        public void AlternateAsName_IsUsed()
        {
            // The shape that already worked, kept as a regression guard.
            var details = Parse(NameToken.Devicecmyk, n: 4);

            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void AlternateAsArray_IsUsed()
        {
            // /Alternate [ /CalRGB << /WhitePoint [...] >> ].
            var details = Parse(CalRgbArray(), n: 3);

            Assert.Equal(ColorSpace.CalRGB, details.AlternateColorSpace.Type);
        }

        [Fact]
        public void AlternateAsIndirectName_IsResolved()
        {
            // /Alternate 5 0 R -> /DeviceCMYK.
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(5, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, NameToken.Devicecmyk);

            var details = Parse(new IndirectReferenceToken(reference), n: 4, scanner);

            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void AlternateAsIndirectArray_IsResolved()
        {
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(6, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, CalRgbArray());

            var details = Parse(new IndirectReferenceToken(reference), n: 3, scanner);

            Assert.Equal(ColorSpace.CalRGB, details.AlternateColorSpace.Type);
        }

        [Fact]
        public void AlternateOfTheWrongWidth_IsIgnored()
        {
            // /N 4 with a 3-component alternate. The alternate stands in for the profile and is handed the
            // very same operands, so a mismatch is not a colour that renders badly but one that cannot be
            // evaluated at all - GetColor on the alternate would throw on the operand count. Falling back
            // to the device space implied by /N keeps the two widths in step.
            var details = Parse(CalRgbArray(), n: 4);

            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);

            // The point of the guard: this must not throw.
            var (r, g, b) = details.GetColor([0.1, 0.2, 0.3, 0.4]).ToRGBValues();
            Assert.InRange(r, 0.0, 1.0);
            Assert.InRange(g, 0.0, 1.0);
            Assert.InRange(b, 0.0, 1.0);
        }

        [Fact]
        public void AlternateOfPattern_IsIgnored()
        {
            // Table 66: the alternate "shall not be a Pattern colour space".
            var details = Parse(new ArrayToken(new IToken[] { NameToken.Pattern }), n: 3);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void AlternateNamingIccBased_IsRejectedRatherThanRecursed()
        {
            // Table 66: the alternate "shall not be an ICCBased colour space". It is not merely invalid.
            // A name alternate is resolved against the OUTER image dictionary, so /Alternate /ICCBased
            // re-enters this very colour space's /ColorSpace array and used to recurse until the stack
            // was exhausted - on a single token, with no indirect references involved.
            //
            // NB: a regression here does not fail this assertion, it kills the test host, because
            // StackOverflowException cannot be caught.
            var details = Parse(NameToken.Iccbased, n: 3);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void AlternateCyclingBackToItsOwnIccBasedArray_IsRejected()
        {
            // The array form of the same prohibition, wired into a genuine cycle: /Alternate 7 0 R, where
            // object 7 is [/ICCBased 8 0 R] and the /Alternate of stream 8 points back at 7 0 R.
            var scanner = new TestPdfTokenScanner();

            var arrayReference = new IndirectReference(7, 0);
            var streamReference = new IndirectReference(8, 0);

            var innerProfile = new StreamToken(
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.N, new NumericToken(3) },
                    { NameToken.Alternate, new IndirectReferenceToken(arrayReference) }
                }),
                new byte[] { 0x01 });

            scanner.Objects[streamReference] =
                new ObjectToken(XrefLocation.File(0), streamReference, innerProfile);

            scanner.Objects[arrayReference] = new ObjectToken(XrefLocation.File(0), arrayReference,
                new ArrayToken(new IToken[]
                {
                    NameToken.Iccbased, new IndirectReferenceToken(streamReference)
                }));

            var details = Parse(new IndirectReferenceToken(arrayReference), n: 3, scanner);

            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void UnparseableAlternate_FallsBackToTheSpaceImpliedByN()
        {
            // A /CalRGB naming no dictionary cannot be built; /N decides instead of the space being lost.
            var details = Parse(new ArrayToken(new IToken[] { NameToken.Calrgb }), n: 1);

            Assert.Same(DeviceGrayColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        public void NoAlternate_UsesTheSpaceImpliedByN(int n)
        {
            var expected = n switch
            {
                1 => (ColorSpaceDetails)DeviceGrayColorSpaceDetails.Instance,
                3 => DeviceRgbColorSpaceDetails.Instance,
                _ => DeviceCmykColorSpaceDetails.Instance
            };

            Assert.Same(expected, Parse(alternate: null, n).AlternateColorSpace);
        }

        [Fact]
        public void AlternateIsNotSubjectToTheDefaultColorSpaceSubstitution()
        {
            // 8.6.5.6 scopes DefaultGray/DefaultRGB/DefaultCMYK to device colour spaces selected from a
            // resource dictionary. An /Alternate lives in the profile stream's own dictionary and is not
            // selected that way, so /DefaultCMYK must not capture it.
            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultCmyk, CalRgbArray() }
                    })
                }
            });

            var details = Parse(NameToken.Devicecmyk, n: 4, scanner: null, resources);

            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void DefaultColorSpaceSubstitutionStillAppliesToASeparationAlternate()
        {
            // The counterpart to the test above: the same device name reached as a Separation's alternate
            // IS one of the cases 8.6.5.6 names, so the substitution must still happen there. Guards
            // against the opt-out being applied too broadly.
            var resources = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.ColorSpace,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.DefaultRgb, CalRgbArray() }
                    })
                }
            });

            var store = new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true });

            store.LoadResourceDictionary(resources);

            // [ /Separation /Spot /DeviceRGB << /FunctionType 2 /C0 [0 0 0] /C1 [1 1 1] /Domain [0 1] /N 1 >> ]
            var tint = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, new ArrayToken(new IToken[] { new NumericToken(0), new NumericToken(1) }) },
                { NameToken.C0, new ArrayToken(new IToken[] { new NumericToken(0), new NumericToken(0), new NumericToken(0) }) },
                { NameToken.C1, new ArrayToken(new IToken[] { new NumericToken(1), new NumericToken(1), new NumericToken(1) }) },
                { NameToken.N, new NumericToken(1) }
            });

            var separation = new ArrayToken(new IToken[]
            {
                NameToken.Separation, NameToken.Create("Spot"), NameToken.Devicergb, tint
            });

            var details = store.GetColorSpaceDetails(NameToken.Separation,
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.ColorSpace, separation }
                }));

            var asSeparation = Assert.IsType<SeparationColorSpaceDetails>(details);
            Assert.Equal(ColorSpace.CalRGB, asSeparation.AlternateColorSpace.Type);
        }
    }
}
