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
    using PdfPig.Tests.Tokens;
    using PdfPig.Tokens;
    using Xunit;

    /// <summary>
    /// A document's output intent characterises the device its device colours were authored for, so a
    /// consumer previewing or proofing it converts DeviceGray/DeviceRGB/DeviceCMYK through that profile
    /// rather than through the built-in approximations (14.11.5 and 8.6.5.7).
    /// <para>
    /// Whether to do so is the <see cref="IIccProfileService"/>'s call, because it cannot be done without one
    /// to parse the intent's profile. The conversion itself belongs here, alongside every other colour
    /// conversion, so that every consumer gets the same answer.
    /// </para>
    /// </summary>
    public class OutputIntentDeviceColorTests
    {
        /// <summary>
        /// Maps every colour to one recognisable RGB, so that "went through the output intent" is visible in
        /// the output whatever the input was.
        /// </summary>
        private sealed class ManagedTransform(int components) : IIccTransform
        {
            public int NumberOfComponents { get; } = components;

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values) => (0.25, 0.5, 0.75);

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
            {
                for (int p = 0; p < dstRgb.Length / 3; p++)
                {
                    dstRgb[p * 3] = 64;
                    dstRgb[p * 3 + 1] = 128;
                    dstRgb[p * 3 + 2] = 191;
                }
            }
        }

        private sealed class ManagedProfile(int components) : IIccProfile
        {
            public int NumberOfComponents { get; } = components;

            public IReadOnlyList<double> ComponentRanges { get; } = new double[components * 2];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = new ManagedTransform(NumberOfComponents);
                return true;
            }
        }

        private sealed class ProofingIccProfileService(bool useOutputIntent) : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes, [NotNullWhen(true)] out IIccProfile? profile)
            {
                profile = new ManagedProfile(4);
                return true;
            }

            public bool UseOutputIntent { get; } = useOutputIntent;

            public string? PreferredOutputIntentSubtype => null;
        }

        private static (ColorSpaceContext Context, CurrentGraphicsState State) Build(bool useOutputIntent,
            int profileComponents = 4)
        {
            var store = new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                    IccProfileService = new ProofingIccProfileService(useOutputIntent)
                });

            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            var intent = new OutputIntent(OutputIntent.PdfXSubtype, null, "FOGRA", null, null,
                new ManagedProfile(profileComponents), null, null, null);

            var state = new CurrentGraphicsState { OutputIntents = [intent] };
            var context = new ColorSpaceContext(() => state, store);
            state.ColorSpaceContext = context;

            return (context, state);
        }

        [Fact]
        public void ADeviceCmykColourIsManagedThroughTheOutputIntent()
        {
            var (context, state) = Build(useOutputIntent: true);

            context.SetNonStrokingColorCmyk(0.85, 0.03, 1.0, 0.15);

            var (r, g, b) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(0.25, r, 6);
            Assert.Equal(0.5, g, 6);
            Assert.Equal(0.75, b, 6);
        }

        [Fact]
        public void TheStrokingOperatorIsManagedToo()
        {
            var (context, state) = Build(useOutputIntent: true);

            context.SetStrokingColorCmyk(0.85, 0.03, 1.0, 0.15);

            var (r, _, _) = state.CurrentStrokingColor!.ToRGBValues();
            Assert.Equal(0.25, r, 6);
        }

        [Fact]
        public void AServiceThatDoesNotOptInLeavesDeviceColoursAlone()
        {
            // 14.11.5: an output intent "shall be for informational purposes only, and PDF processors are
            // free to disregard it". Managing device colours is opt-in, and the service is what opts in.
            var (context, state) = Build(useOutputIntent: false);

            context.SetNonStrokingColorCmyk(0.0, 1.0, 1.0, 0.0);

            var (r, g, b) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(1.0, r, 6);
            Assert.Equal(0.0, g, 6);
            Assert.Equal(0.0, b, 6);
        }

        [Fact]
        public void ADeviceSpaceTheProfileCannotExpressKeepsItsBuiltInConversion()
        {
            // DeviceRGB against a CMYK output intent has no well-defined neutral mapping, so the colour is
            // left as the built-in conversion produced it rather than forced through the profile.
            var (context, state) = Build(useOutputIntent: true);

            context.SetNonStrokingColorRgb(1.0, 0.0, 0.0);

            var (r, g, b) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(1.0, r, 6);
            Assert.Equal(0.0, g, 6);
            Assert.Equal(0.0, b, 6);
        }

        /// <summary>
        /// Answers a different colour per intent, which is what a real profile does - a transform is
        /// resolved per <see cref="RenderingIntent"/>.
        /// </summary>
        private sealed class PerIntentTransform(RenderingIntent intent) : IIccTransform
        {
            public int NumberOfComponents => 4;

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
                => intent == RenderingIntent.Perceptual ? (0.1, 0.1, 0.1) : (0.9, 0.9, 0.9);

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb) => dstRgb.Clear();
        }

        private sealed class PerIntentProfile : IIccProfile
        {
            public int NumberOfComponents => 4;

            public IReadOnlyList<double> ComponentRanges { get; } = new double[8];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = new PerIntentTransform(intent);
                return true;
            }
        }

        [Fact]
        public void AManagedDeviceColourFollowsALaterIntentChange()
        {
            // A device colour space cannot vary by intent on its own, which is why it reports
            // RenderingIntentAffectsOutput false. Managed through an output intent it can: the profile
            // resolves a different transform per intent. So a ri arriving between the colour operator and
            // the paint has to re-manage the colour, exactly as it would for an ICCBased space.
            var store = new ResourceStore(
                new TestPdfTokenScanner(),
                new NoOpFontFactory(),
                new TestFilterProvider(),
                null,
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                    IccProfileService = new ProofingIccProfileService(useOutputIntent: true)
                });

            store.LoadResourceDictionary(new DictionaryToken(new Dictionary<NameToken, IToken>()));

            var outputIntent = new OutputIntent(OutputIntent.PdfXSubtype, null, "FOGRA", null, null,
                new PerIntentProfile(), null, null, null);

            var state = new CurrentGraphicsState
            {
                OutputIntents = [outputIntent],
                RenderingIntent = RenderingIntent.RelativeColorimetric
            };

            var context = new ColorSpaceContext(() => state, store);
            state.ColorSpaceContext = context;

            context.SetNonStrokingColorCmyk(0.0, 0.0, 0.0, 1.0);

            var (r, _, _) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(0.9, r, 6);

            state.RenderingIntent = RenderingIntent.Perceptual;

            (r, _, _) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(0.1, r, 6);
        }

        [Fact]
        public void SuppressingTheOutputIntentRestoresTheBuiltInConversion()
        {
            // A renderer clears OutputIntents for the duration of a soft-mask group, where device values are
            // an alpha computation rather than output-device colour.
            var (context, state) = Build(useOutputIntent: true);

            state.OutputIntents = null;
            context.SetNonStrokingColorCmyk(0.0, 1.0, 1.0, 0.0);

            var (r, _, _) = state.CurrentNonStrokingColor!.ToRGBValues();
            Assert.Equal(1.0, r, 6);
        }
    }
}
