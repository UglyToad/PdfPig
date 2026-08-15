namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Content;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
    using PdfPig.PdfFonts;
    using PdfPig.Tests.Tokens;
    using PdfPig.Tokens;
    using Xunit;

    /// <summary>
    /// The <c>cs</c> and <c>CS</c> operators install a colour space's initial colour, and for an ICCBased
    /// space that colour is converted through the profile there and then. The rendering intent in force is a
    /// graphics state parameter maintained by the <c>ri</c> operator and the ExtGState <c>/RI</c> entry, and
    /// it has to reach that conversion the same way it reaches the one behind <c>sc</c>/<c>scn</c>.
    /// </summary>
    public class InitializeColorRenderingIntentTests
    {
        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        /// <summary>
        /// Answers a different colour per intent, so which transform was chosen is visible in the output.
        /// </summary>
        private sealed class PerIntentTransform : IIccTransform
        {
            private readonly (double r, double g, double b) fixedOut;

            public PerIntentTransform(int components, (double r, double g, double b) fixedOut)
            {
                NumberOfComponents = components;
                this.fixedOut = fixedOut;
            }

            public int NumberOfComponents { get; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values) => fixedOut;

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb) => dstRgb.Clear();
        }

        private sealed class PerIntentProfile : IIccProfile
        {
            public int NumberOfComponents => 3;

            public IReadOnlyList<double> ComponentRanges => [0.0, 1.0, 0.0, 1.0, 0.0, 1.0];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = intent == RenderingIntent.Perceptual
                    ? new PerIntentTransform(3, (0.10, 0.20, 0.30))
                    : new PerIntentTransform(3, (0.60, 0.70, 0.80));

                return true;
            }
        }

        private sealed class PerIntentProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes, [NotNullWhen(true)] out IIccProfile? profile)
            {
                profile = new PerIntentProfile();
                return true;
            }
        }

        /// <summary>
        /// A resource dictionary holding one ICCBased colour space, under <c>/CS0</c> and - when
        /// <paramref name="asDefaultRgb"/> - also as the <c>/DefaultRGB</c> substitute that 8.6.5.6 makes
        /// <c>rg</c> and <c>RG</c> resolve to.
        /// </summary>
        private static DictionaryToken IccResources(bool asDefaultRgb = false)
        {
            var profileStream = new StreamToken(
                new DictionaryToken(new Dictionary<NameToken, IToken> { { NameToken.N, new NumericToken(3) } }),
                [0x01]);

            var entries = new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("CS0"), new ArrayToken([NameToken.Iccbased, profileStream]) }
            };

            if (asDefaultRgb)
            {
                entries[NameToken.Create("DefaultRGB")] = new ArrayToken([NameToken.Iccbased, profileStream]);
            }

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.ColorSpace, new DictionaryToken(entries) }
            });
        }

        private static (ColorSpaceContext Context, CurrentGraphicsState State) Build(RenderingIntent intent,
            bool defaultRgb = false)
        {
            var store = new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                    IccProfileService = new PerIntentProfileService()
                },
                null);

            store.LoadResourceDictionary(IccResources(defaultRgb));

            var state = new CurrentGraphicsState { RenderingIntent = intent };
            var context = new ColorSpaceContext(() => state, store);
            state.ColorSpaceContext = context;

            return (context, state);
        }

        [Theory]
        [InlineData(RenderingIntent.Perceptual, 0.10, 0.20, 0.30)]
        [InlineData(RenderingIntent.RelativeColorimetric, 0.60, 0.70, 0.80)]
        public void SetNonStrokingColorspace_InitialColourUsesTheGraphicsStateIntent(
            RenderingIntent intent, double expectedR, double expectedG, double expectedB)
        {
            var (context, state) = Build(intent);

            context.SetNonStrokingColorspace(NameToken.Create("CS0"));

            var (r, g, b) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(expectedR, r);
            Assert.Equal(expectedG, g);
            Assert.Equal(expectedB, b);
        }

        [Theory]
        [InlineData(RenderingIntent.Perceptual, 0.10, 0.20, 0.30)]
        [InlineData(RenderingIntent.RelativeColorimetric, 0.60, 0.70, 0.80)]
        public void SetStrokingColorspace_InitialColourUsesTheGraphicsStateIntent(
            RenderingIntent intent, double expectedR, double expectedG, double expectedB)
        {
            var (context, state) = Build(intent);

            context.SetStrokingColorspace(NameToken.Create("CS0"));

            var (r, g, b) = state.CurrentStrokingColor!.ToRGBValues();
            Assert.Equal(expectedR, r);
            Assert.Equal(expectedG, g);
            Assert.Equal(expectedB, b);
        }

        private static void AssertPerceptual(IColor? color)
        {
            var (r, g, b) = color!.ToRGBValues();
            Assert.Equal(0.10, r);
            Assert.Equal(0.20, g);
            Assert.Equal(0.30, b);
        }

        [Fact]
        public void IntentSetAfterTheColorspaceOperator_StillApplies()
        {
            // cs installs the initial colour, then ri changes the intent, then the mark is made. Rendering
            // intent is a graphics state parameter consumed when the mark is made (8.6.5.8), so the second
            // intent is the one that decides the colour.
            var (context, state) = Build(RenderingIntent.RelativeColorimetric);

            context.SetNonStrokingColorspace(NameToken.Create("CS0"));
            state.RenderingIntent = RenderingIntent.Perceptual;

            AssertPerceptual(state.CurrentNonStrokingColor);
        }

        [Fact]
        public void IntentSetAfterTheColourOperator_StillApplies()
        {
            // The same for scn, which is the far more common ordering: a colour is chosen, an ExtGState or
            // ri then changes the intent, and only afterwards is anything painted.
            var (context, state) = Build(RenderingIntent.RelativeColorimetric);

            context.SetNonStrokingColorspace(NameToken.Create("CS0"));
            context.SetNonStrokingColor([0.1, 0.2, 0.3], null);
            state.RenderingIntent = RenderingIntent.Perceptual;

            AssertPerceptual(state.CurrentNonStrokingColor);
        }

        [Fact]
        public void IntentSetAfterTheStrokingColourOperator_StillApplies()
        {
            var (context, state) = Build(RenderingIntent.RelativeColorimetric);

            context.SetStrokingColorspace(NameToken.Create("CS0"));
            context.SetStrokingColor([0.1, 0.2, 0.3], null);
            state.RenderingIntent = RenderingIntent.Perceptual;

            AssertPerceptual(state.CurrentStrokingColor);
        }

        [Fact]
        public void IntentSetAfterADeviceColourOperator_StillApplies_WhenDefaultRgbIsIccBased()
        {
            // rg is a device operator, and DeviceRGB itself cannot vary by intent - but 8.6.5.6 remaps it to
            // /DefaultRGB when the resources define one, and that substitute may be an ICCBased space. This
            // is the only route by which a device colour operator becomes intent-dependent.
            var (context, state) = Build(RenderingIntent.RelativeColorimetric, defaultRgb: true);

            context.SetNonStrokingColorRgb(0.1, 0.2, 0.3);
            state.RenderingIntent = RenderingIntent.Perceptual;

            AssertPerceptual(state.CurrentNonStrokingColor);
        }

        [Fact]
        public void ADeviceColourIsFixed_AndDoesNotMoveWithTheIntent()
        {
            // With no /DefaultRGB substitute, rg resolves to the DeviceRGB singleton, which converts the
            // same way under every intent. Nothing is retained for it and the colour stands as converted -
            // this is the fixed path the public setters used to reach, and the hot path this fix must not
            // burden. Contrast IntentSetAfterADeviceColourOperator_StillApplies, which supplies a
            // substitute and does move.
            var (context, state) = Build(RenderingIntent.RelativeColorimetric);

            context.SetNonStrokingColorRgb(0.1, 0.2, 0.3);
            state.RenderingIntent = RenderingIntent.Perceptual;

            var (r, g, b) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(0.1, r);
            Assert.Equal(0.2, g);
            Assert.Equal(0.3, b);
        }

        [Fact]
        public void DeepClone_CarriesTheOperandsSoTheCloneStillFollowsItsOwnIntent()
        {
            // q/Q clone the graphics state, and a ri inside the q applies only until the Q. The clone has to
            // carry enough to reconvert, and must not write back into the state it was cloned from.
            var (context, state) = Build(RenderingIntent.RelativeColorimetric);

            context.SetNonStrokingColorspace(NameToken.Create("CS0"));
            context.SetNonStrokingColor([0.1, 0.2, 0.3], null);

            var clone = state.DeepClone();
            clone.RenderingIntent = RenderingIntent.Perceptual;

            AssertPerceptual(clone.CurrentNonStrokingColor);

            // The original is still on the intent it was set under.
            var (r, g, b) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(0.60, r);
            Assert.Equal(0.70, g);
            Assert.Equal(0.80, b);
        }
    }
}
