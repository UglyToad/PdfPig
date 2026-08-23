namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Core;
    using PdfPig.Functions;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Tokens;
    using Xunit;

    /// <summary>
    /// <c>SCN</c>/<c>scn</c> selects a pattern by name, and for an uncoloured tiling pattern
    /// (<c>/PaintType 2</c>) it also supplies the colour the pattern's cell is painted in, as operands in the
    /// underlying colour space the Pattern space was declared with (8.7.3.3). The graphics state keeps those
    /// operands rather than dropping them, so the colour they select is available to every consumer.
    /// </summary>
    public class UncolouredTilingPatternColorTests
    {
        private static readonly NameToken PatternName = NameToken.Create("P0");

        private static TilingPatternColor UncolouredPattern()
            => TilingPattern(PatternPaintType.Uncoloured);

        private static TilingPatternColor ColouredPattern()
            => TilingPattern(PatternPaintType.Coloured);

        private static TilingPatternColor TilingPattern(PatternPaintType paintType)
        {
            var empty = new DictionaryToken(new Dictionary<NameToken, IToken>());

            return new TilingPatternColor(
                TransformationMatrix.Identity,
                empty,
                new StreamToken(empty, []),
                paintType,
                PatternTilingType.ConstantSpacing,
                new PdfRectangle(0, 0, 1, 1),
                1,
                1,
                empty,
                ReadOnlyMemory<byte>.Empty);
        }

        private static ShadingPatternColor ShadingPattern()
        {
            var empty = new DictionaryToken(new Dictionary<NameToken, IToken>());

            var function = new PdfFunctionType2(
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.FunctionType, new NumericToken(2) }
                }),
                new ArrayToken([new NumericToken(0), new NumericToken(1)]),
                null,
                new ArrayToken([new NumericToken(0)]),
                new ArrayToken([new NumericToken(1)]),
                1);

            var shading = new AxialShading(
                antiAlias: false,
                shadingDictionary: empty,
                colorSpace: DeviceGrayColorSpaceDetails.Instance,
                bbox: null,
                background: null,
                coords: [0.0, 0.0, 1.0, 0.0],
                domain: [0.0, 1.0],
                functions: [function],
                extend: [false, false]);

            return new ShadingPatternColor(TransformationMatrix.Identity, empty, empty, shading);
        }

        /// <summary>
        /// A Pattern colour space over <paramref name="underlying"/>, and a graphics state to select into.
        /// A <see langword="null"/> underlying space is how a bare <c>/Pattern</c> - a coloured tiling
        /// pattern or a shading pattern, which supply their own colours - is parsed.
        /// </summary>
        private static (CurrentGraphicsState State, PatternColorSpaceDetails Space) Build(
            ColorSpaceDetails? underlying, PatternColor? pattern = null)
        {
            var patterns = new Dictionary<NameToken, PatternColor> { { PatternName, pattern ?? UncolouredPattern() } };

            return (new CurrentGraphicsState(),
                new PatternColorSpaceDetails(patterns, underlying!));
        }

        [Fact]
        public void TheOperandsSelectTheColourTheCellIsPaintedIn()
        {
            var (state, space) = Build(DeviceRgbColorSpaceDetails.Instance);

            state.SetNonStrokingPatternColor(space, PatternName, [1.0, 0.0, 0.0]);

            // The current colour is still the pattern - that part is unchanged.
            Assert.IsType<TilingPatternColor>(state.CurrentNonStrokingColor);

            var (r, g, b) = state.CurrentNonStrokingUnderlyingColor!.ToRGBValues();
            Assert.Equal(1.0, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.0, b);
        }

        [Fact]
        public void TheStrokingOperatorKeepsItsOwnOperands()
        {
            var (state, space) = Build(DeviceRgbColorSpaceDetails.Instance);

            state.SetStrokingPatternColor(space, PatternName, [0.0, 1.0, 0.0]);

            var (r, g, b) = state.CurrentStrokingUnderlyingColor!.ToRGBValues();
            Assert.Equal(0.0, r);
            Assert.Equal(1.0, g);
            Assert.Equal(0.0, b);

            // Selecting a stroking pattern says nothing about the non-stroking colour.
            Assert.Null(state.CurrentNonStrokingUnderlyingColor);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NoOperandsMeansTheUnderlyingSpacesInitialColour(bool nullRatherThanEmpty)
        {
            var (state, space) = Build(DeviceRgbColorSpaceDetails.Instance);

            state.SetNonStrokingPatternColor(space, PatternName, nullRatherThanEmpty ? null : []);

            // Not the same as "no underlying colour": the space still has an initial colour, and DeviceRGB's
            // is black. Empty and null must not be told apart - scn writes an empty operand list.
            var (r, g, b) = state.CurrentNonStrokingUnderlyingColor!.ToRGBValues();
            Assert.Equal(0.0, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.0, b);
        }

        [Fact]
        public void AColouredPatternHasNoUnderlyingColour()
        {
            // A bare /Pattern colour space declares no underlying space, because the pattern's own content
            // stream sets the colours.
            var (state, space) = Build(underlying: null);

            state.SetNonStrokingPatternColor(space, PatternName, []);

            Assert.IsType<TilingPatternColor>(state.CurrentNonStrokingColor);
            Assert.Null(state.CurrentNonStrokingUnderlyingColor);
        }

        [Fact]
        public void AShadingPatternHasNoUnderlyingColourEvenOverADeclaredUnderlyingSpace()
        {
            // A shading pattern paints the colours its shading defines, so there is no colour to tint its
            // content with. It is normally selected from a bare /Pattern space, which would settle this -
            // but the space is declared in the resource dictionary while the pattern is chosen per scn, so
            // a [/Pattern /DeviceRGB] space can be left in force over one. The pattern is what knows.
            var (state, space) = Build(DeviceRgbColorSpaceDetails.Instance, pattern: ShadingPattern());

            state.SetNonStrokingPatternColor(space, PatternName, []);

            Assert.IsType<ShadingPatternColor>(state.CurrentNonStrokingColor);
            Assert.Null(state.CurrentNonStrokingUnderlyingColor);
        }

        [Fact]
        public void AColouredTilingPatternHasNoUnderlyingColourEvenOverADeclaredUnderlyingSpace()
        {
            // /PaintType 1: the pattern's own content stream sets its colours, so as with a shading pattern
            // there is nothing for an underlying colour to mean.
            var (state, space) = Build(DeviceRgbColorSpaceDetails.Instance, pattern: ColouredPattern());

            state.SetNonStrokingPatternColor(space, PatternName, []);

            Assert.Null(state.CurrentNonStrokingUnderlyingColor);
        }

        [Fact]
        public void AnUnsupportedUnderlyingSpaceHasNoUnderlyingColour()
        {
            var (state, space) = Build(UnsupportedColorSpaceDetails.Instance);

            state.SetNonStrokingPatternColor(space, PatternName, [0.5]);

            Assert.Null(state.CurrentNonStrokingUnderlyingColor);
        }

        [Fact]
        public void OperandsThatDoNotFitTheUnderlyingSpaceYieldNoColourRatherThanThrowing()
        {
            // Every ColorSpaceDetails.GetColor throws on a component count it did not expect. This
            // conversion happens while the content stream is processed, so a /Pattern array that disagrees
            // with the operator must not take down every consumer of the page.
            var (state, space) = Build(DeviceRgbColorSpaceDetails.Instance);

            var exception = Record.Exception(
                () => state.SetNonStrokingPatternColor(space, PatternName, [0.5]));

            Assert.Null(exception);
            Assert.Null(state.CurrentNonStrokingUnderlyingColor);

            // The pattern itself still selected, so painting it is still possible.
            Assert.IsType<TilingPatternColor>(state.CurrentNonStrokingColor);
        }

        [Fact]
        public void AnOrdinaryColourHasNoUnderlyingColour()
        {
            // The property must answer only for patterns, not hand back the current colour under another
            // name.
            var state = new CurrentGraphicsState();

            state.SetNonStrokingColor(DeviceRgbColorSpaceDetails.Instance, [1.0, 0.0, 0.0]);

            Assert.NotNull(state.CurrentNonStrokingColor);
            Assert.Null(state.CurrentNonStrokingUnderlyingColor);
        }

        [Fact]
        public void DeepCloneCarriesBothThePatternAndItsUnderlyingColour()
        {
            var (state, space) = Build(DeviceRgbColorSpaceDetails.Instance);

            state.SetNonStrokingPatternColor(space, PatternName, [1.0, 0.0, 0.0]);

            var clone = state.DeepClone();

            Assert.IsType<TilingPatternColor>(clone.CurrentNonStrokingColor);

            var (r, g, b) = clone.CurrentNonStrokingUnderlyingColor!.ToRGBValues();
            Assert.Equal(1.0, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.0, b);
        }
    }
}
