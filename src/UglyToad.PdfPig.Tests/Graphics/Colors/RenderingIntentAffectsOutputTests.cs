namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Functions;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Core;
    using PdfPig.Tokens;
    using Xunit;

    /// <summary>
    /// <see cref="ColorSpaceDetails.RenderingIntentAffectsOutput"/> tells a caller holding a current colour
    /// whether it has to keep the operands around in case the rendering intent moves before the mark is made
    /// (8.6.5.8). Answering <see langword="true"/> spuriously only forgoes an optimisation; answering
    /// <see langword="false"/> wrongly silently pins the colour to the intent it was selected under, so the
    /// interesting cases are the composite spaces, which must forward the answer of whatever they convert
    /// through rather than take the default.
    /// </summary>
    public class RenderingIntentAffectsOutputTests
    {
        /// <summary>
        /// Stands in for an ICCBased space holding a usable profile - the only kind of space that really
        /// reads the intent. Constructing a real one needs profile bytes and an
        /// <c>IIccProfileService</c>; the forwarding under test does not care which space says
        /// <see langword="true"/>, only that its parent repeats it. <see cref="ICCBasedColorSpaceDetails"/>
        /// answering for itself is covered separately, in
        /// <see cref="InitializeColorRenderingIntentTests"/>.
        /// </summary>
        private class IntentSensitiveColorSpaceDetails : ColorSpaceDetails
        {
            public IntentSensitiveColorSpaceDetails() : base(ColorSpace.DeviceRGB)
            {
            }

            public override int NumberOfColorComponents => 3;

            public override int BaseNumberOfColorComponents => 3;

            public override bool RenderingIntentAffectsOutput => true;

            public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
                => intent == RenderingIntent.Perceptual
                    ? new RGBColor(0.10, 0.20, 0.30)
                    : new RGBColor(0.60, 0.70, 0.80);

            public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
                out double r, out double g, out double b)
            {
                (r, g, b) = GetColor(values, intent).ToRGBValues();
            }

            public override IColor? GetInitializeColor(RenderingIntent intent)
                => GetColor([0.0, 0.0, 0.0], intent);

            internal override double[] Process(double[] values, RenderingIntent intent) => values;

            internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent) => decoded;
        }

        /// <summary>
        /// Converts differently per intent while reporting that it does not, which no real colour space
        /// does. It makes the difference between "converted once and kept" and "reconverted on demand"
        /// visible in the output, which is otherwise unobservable precisely because honest spaces agree
        /// across intents.
        /// </summary>
        private sealed class DishonestColorSpaceDetails : IntentSensitiveColorSpaceDetails
        {
            public override bool RenderingIntentAffectsOutput => false;
        }

        /// <summary>
        /// A tint transform onto a three-component alternate. Its own output never depends on the intent -
        /// only the alternate it feeds can - which is the point being pinned.
        /// </summary>
        private static PdfFunction TintTransform()
        {
            return new PdfFunctionType2(
                new DictionaryToken(new Dictionary<NameToken, IToken>()),
                new ArrayToken([new NumericToken(0), new NumericToken(1)]),
                null,
                new ArrayToken([new NumericToken(0), new NumericToken(0), new NumericToken(0)]),
                new ArrayToken([new NumericToken(1), new NumericToken(1), new NumericToken(1)]),
                1.0);
        }

        private static IndexedColorSpaceDetails Indexed(ColorSpaceDetails baseSpace)
            => new IndexedColorSpaceDetails(baseSpace, 1, new byte[6]);

        private static SeparationColorSpaceDetails Separation(ColorSpaceDetails alternate)
            => new SeparationColorSpaceDetails(NameToken.Create("Spot"), alternate, TintTransform());

        private static DeviceNColorSpaceDetails DeviceN(ColorSpaceDetails alternate)
            => new DeviceNColorSpaceDetails([NameToken.Create("Spot")], alternate, TintTransform());

        private static PatternColorSpaceDetails Pattern(ColorSpaceDetails? underlying)
            => new PatternColorSpaceDetails(new Dictionary<NameToken, PatternColor>(), underlying!);

        [Fact]
        public void TheDeviceSpacesDoNotVary()
        {
            Assert.False(DeviceGrayColorSpaceDetails.Instance.RenderingIntentAffectsOutput);
            Assert.False(DeviceRgbColorSpaceDetails.Instance.RenderingIntentAffectsOutput);
            Assert.False(DeviceCmykColorSpaceDetails.Instance.RenderingIntentAffectsOutput);
        }

        [Fact]
        public void TheCieBasedSpacesDoNotVary()
        {
            // These are the ones that look intent-aware and are not: every conversion entry point takes a
            // RenderingIntent and forwards it internally, but nothing ever reads it. Pinning that here means
            // a future change to any of them has to confront this rather than silently make the answer wrong.
            double[] whitePoint = [0.9505, 1.0, 1.089];

            Assert.False(new LabColorSpaceDetails(whitePoint, null, null).RenderingIntentAffectsOutput);
            Assert.False(new CalRGBColorSpaceDetails(whitePoint, null, null, null).RenderingIntentAffectsOutput);
            Assert.False(new CalGrayColorSpaceDetails(whitePoint, null, null).RenderingIntentAffectsOutput);
        }

        [Fact]
        public void AnUnsupportedSpaceDoesNotVary()
        {
            Assert.False(UnsupportedColorSpaceDetails.Instance.RenderingIntentAffectsOutput);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IndexedForwardsItsBaseSpace(bool baseVaries)
        {
            ColorSpaceDetails baseSpace = baseVaries
                ? new IntentSensitiveColorSpaceDetails()
                : DeviceRgbColorSpaceDetails.Instance;

            Assert.Equal(baseVaries, Indexed(baseSpace).RenderingIntentAffectsOutput);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SeparationForwardsItsAlternateSpace(bool alternateVaries)
        {
            ColorSpaceDetails alternate = alternateVaries
                ? new IntentSensitiveColorSpaceDetails()
                : DeviceRgbColorSpaceDetails.Instance;

            Assert.Equal(alternateVaries, Separation(alternate).RenderingIntentAffectsOutput);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DeviceNForwardsItsAlternateSpace(bool alternateVaries)
        {
            ColorSpaceDetails alternate = alternateVaries
                ? new IntentSensitiveColorSpaceDetails()
                : DeviceRgbColorSpaceDetails.Instance;

            Assert.Equal(alternateVaries, DeviceN(alternate).RenderingIntentAffectsOutput);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PatternForwardsItsUnderlyingSpace(bool underlyingVaries)
        {
            ColorSpaceDetails underlying = underlyingVaries
                ? new IntentSensitiveColorSpaceDetails()
                : DeviceRgbColorSpaceDetails.Instance;

            Assert.Equal(underlyingVaries, Pattern(underlying).RenderingIntentAffectsOutput);
        }

        [Fact]
        public void AColouredPatternDoesNotVary()
        {
            // A bare /Pattern declares no underlying space - the pattern's own content stream sets the
            // colours - so there is nothing whose answer could be forwarded.
            Assert.False(Pattern(underlying: null).RenderingIntentAffectsOutput);
        }

        [Fact]
        public void ForwardingIsTransitiveThroughSeveralLevels()
        {
            // Indexed over Separation over an intent-sensitive alternate: the answer has to survive both
            // hops, which a per-class override that consulted only a leaf would get wrong.
            var deep = Indexed(Separation(new IntentSensitiveColorSpaceDetails()));

            Assert.True(deep.RenderingIntentAffectsOutput);
            Assert.False(Indexed(Separation(DeviceCmykColorSpaceDetails.Instance)).RenderingIntentAffectsOutput);
        }

        [Fact]
        public void AVaryingSpaceStillReconvertsWhenTheIntentMoves()
        {
            // The property is only an optimisation hint, so it must not change what a space that does vary
            // actually does: a colour selected under one intent and read under another still moves.
            var state = new PdfPig.Graphics.CurrentGraphicsState
            {
                RenderingIntent = RenderingIntent.RelativeColorimetric
            };

            state.SetNonStrokingColor(new IntentSensitiveColorSpaceDetails(), [0.0, 0.0, 0.0]);

            var (r, _, _) = state.CurrentNonStrokingColor.ToRGBValues();
            Assert.Equal(0.60, r);

            state.RenderingIntent = RenderingIntent.Perceptual;

            (r, _, _) = state.CurrentNonStrokingColor.ToRGBValues();
            Assert.Equal(0.10, r);
        }

        [Fact]
        public void ANonVaryingSpaceIsConvertedOnceAndNotAgain()
        {
            // The other half of the contract, and the point of the property: a space that says its output
            // cannot vary is converted at selection time and nothing is retained to reconvert from, so a
            // later ri cannot move it. Only a space that lies about the property can show this.
            var state = new PdfPig.Graphics.CurrentGraphicsState
            {
                RenderingIntent = RenderingIntent.RelativeColorimetric
            };

            state.SetNonStrokingColor(new DishonestColorSpaceDetails(), [0.0, 0.0, 0.0]);

            var (r, _, _) = state.CurrentNonStrokingColor.ToRGBValues();
            Assert.Equal(0.60, r);

            state.RenderingIntent = RenderingIntent.Perceptual;

            (r, _, _) = state.CurrentNonStrokingColor.ToRGBValues();
            Assert.Equal(0.60, r);
        }

        [Fact]
        public void ANonVaryingSpacesInitialColourIsAlsoConvertedOnce()
        {
            // The cs/CS path, where the colour space supplies its own initial colour and there are no
            // operands at all. It goes through the same conversion, so it has to make the same choice.
            var state = new PdfPig.Graphics.CurrentGraphicsState
            {
                RenderingIntent = RenderingIntent.RelativeColorimetric
            };

            state.SetStrokingColor(new DishonestColorSpaceDetails(), null);

            var (r, _, _) = state.CurrentStrokingColor.ToRGBValues();
            Assert.Equal(0.60, r);

            state.RenderingIntent = RenderingIntent.Perceptual;

            (r, _, _) = state.CurrentStrokingColor.ToRGBValues();
            Assert.Equal(0.60, r);
        }
    }
}
