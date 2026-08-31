namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Core;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Core;
    using PdfPig.Tokens;
    using Xunit;

    public class ColorConversionFailureTests
    {
        private static readonly NameToken PatternName = NameToken.Create("P0");

        private sealed class FailingColorSpaceDetails(RenderingIntent? failsUnder = null) : ColorSpaceDetails(ColorSpace.DeviceRGB)
        {
            public override int NumberOfColorComponents => 3;

            public override int BaseNumberOfColorComponents => 3;

            public override bool RenderingIntentAffectsOutput => true;

            public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
            {
                if (values.Length != NumberOfColorComponents)
                {
                    throw new ArgumentException(
                        $"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}",
                        nameof(values));
                }

                if (intent == failsUnder)
                {
                    throw new InvalidOperationException($"No usable transform for {intent}.");
                }

                return new RGBColor(values[0], values[1], values[2]);
            }

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

        private static TilingPatternColor UncolouredPattern()
        {
            var empty = new DictionaryToken(new Dictionary<NameToken, IToken>());

            return new TilingPatternColor(
                TransformationMatrix.Identity,
                empty,
                new StreamToken(empty, []),
                PatternPaintType.Uncoloured,
                PatternTilingType.ConstantSpacing,
                new PdfRectangle(0, 0, 1, 1),
                1,
                1,
                empty,
                ReadOnlyMemory<byte>.Empty);
        }

        /// <summary>
        /// The <c>scn</c> case from issue #1426: one operand for a three component space. The colour cannot
        /// be made, and neither the selection nor a subsequent intent change may throw for it.
        /// </summary>
        [Fact]
        public void OperandsTheSpaceCannotConvertSelectNoColour()
        {
            var state = new CurrentGraphicsState { RenderingIntent = RenderingIntent.RelativeColorimetric };

            state.SetNonStrokingColor(new FailingColorSpaceDetails(), [1.0]);
            state.SetStrokingColor(new FailingColorSpaceDetails(), [1.0]);

            Assert.Null(state.CurrentNonStrokingColor);
            Assert.Null(state.CurrentStrokingColor);

            // The operands are not held on to for a reconversion that would fail exactly as this one did.
            state.RenderingIntent = RenderingIntent.Perceptual;

            Assert.Null(state.CurrentNonStrokingColor);
            Assert.Null(state.CurrentStrokingColor);
        }

        /// <summary>
        /// The same for the colour an uncoloured tiling pattern paints its cell in. The pattern itself came
        /// from a name rather than from the operands, so it is unaffected.
        /// </summary>
        [Fact]
        public void APatternWhoseUnderlyingColourCannotConvertKeepsThePattern()
        {
            var patterns = new Dictionary<NameToken, PatternColor> { { PatternName, UncolouredPattern() } };

            // Fails under the intent it is selected with, so the failure is the underlying conversion's
            // rather than the operand count's - which the Pattern space screens out before converting.
            var space = new PatternColorSpaceDetails(patterns,
                new FailingColorSpaceDetails(RenderingIntent.RelativeColorimetric));

            var state = new CurrentGraphicsState { RenderingIntent = RenderingIntent.RelativeColorimetric };

            state.SetNonStrokingPatternColor(space, PatternName, [1.0, 0.0, 0.0]);

            Assert.IsType<TilingPatternColor>(state.CurrentNonStrokingColor);
            Assert.Null(state.CurrentNonStrokingUnderlyingColor);

            state.RenderingIntent = RenderingIntent.Perceptual;

            Assert.IsType<TilingPatternColor>(state.CurrentNonStrokingColor);
            Assert.Null(state.CurrentNonStrokingUnderlyingColor);
        }

        /// <summary>
        /// A space that converted when the colour was selected but cannot under the intent that follows.
        /// The colour that was actually converted stands, rather than the getter throwing.
        /// </summary>
        [Fact]
        public void AFailureOnlyUnderTheNewIntentKeepsTheColourTheOldOneConverted()
        {
            var state = new CurrentGraphicsState { RenderingIntent = RenderingIntent.RelativeColorimetric };

            state.SetNonStrokingColor(new FailingColorSpaceDetails(RenderingIntent.Perceptual), [1.0, 0.0, 0.0]);

            AssertIsRed(state.CurrentNonStrokingColor);

            state.RenderingIntent = RenderingIntent.Perceptual;

            AssertIsRed(state.CurrentNonStrokingColor);

            // ...and having failed once it is not retried on every subsequent read.
            state.RenderingIntent = RenderingIntent.Saturation;

            AssertIsRed(state.CurrentNonStrokingColor);

            static void AssertIsRed(IColor? color)
            {
                var (r, g, b) = Assert.IsType<RGBColor>(color).ToRGBValues();

                Assert.Equal(1.0, r);
                Assert.Equal(0.0, g);
                Assert.Equal(0.0, b);
            }
        }
    }
}
