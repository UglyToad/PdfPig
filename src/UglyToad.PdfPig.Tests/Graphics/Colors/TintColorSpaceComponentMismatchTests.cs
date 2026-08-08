namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using PdfPig.Functions;
    using PdfPig.Graphics.Colors;
    using PdfPig.Tokens;

    /// <summary>
    /// A Separation or DeviceN tint function is not obliged to produce exactly as many values as its
    /// alternate colour space consumes. <see cref="ColorSpaceDetails.GetColor"/> and
    /// <see cref="ColorSpaceDetails.GetRgb"/> must reconcile the mismatch the same way, so that a colour
    /// space renders identically whether it is reached through an <c>scn</c> operator or through the
    /// unboxed RGB path.
    /// </summary>
    public class TintColorSpaceComponentMismatchTests
    {
        /// <summary>
        /// A type 2 (exponential interpolation) tint function over domain [0 1] with exponent 1,
        /// i.e. f(t) = c0 + t × (c1 − c0). The number of outputs is the length of <paramref name="c0"/>.
        /// </summary>
        private static PdfFunction Tint(double[] c0, double[] c1)
        {
            static ArrayToken Numbers(double[] values)
            {
                var tokens = new IToken[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    tokens[i] = new NumericToken(values[i]);
                }

                return new ArrayToken(tokens);
            }

            var domain = new ArrayToken([new NumericToken(0), new NumericToken(1)]);
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) }
            });

            return new PdfFunctionType2(dictionary, domain, null, Numbers(c0), Numbers(c1), 1);
        }

        private static void AssertGetColorMatchesGetRgb(ColorSpaceDetails colorSpace, double[] values)
        {
            var (r, g, b) = colorSpace.GetColor(values).ToRGBValues();
            colorSpace.GetRgb(values, out double rr, out double rg, out double rb);

            Assert.Equal(rr, r, 12);
            Assert.Equal(rg, g, 12);
            Assert.Equal(rb, b, 12);
        }

        [Fact]
        public void Separation_TintUnderFillsAlternate_GetColorPadsInsteadOfThrowing()
        {
            // Two outputs over a three component alternate. GetColor used to hand DeviceRGB a
            // two-element span and throw, while GetRgb zero-padded and rendered red.
            var separation = new SeparationColorSpaceDetails(
                NameToken.Create("Spot"),
                DeviceRgbColorSpaceDetails.Instance,
                Tint([0, 0], [1, 0]));

            var (r, g, b) = separation.GetColor([1.0]).ToRGBValues();

            Assert.Equal(1.0, r, 12);
            Assert.Equal(0.0, g, 12);
            Assert.Equal(0.0, b, 12);

            AssertGetColorMatchesGetRgb(separation, [1.0]);
        }

        [Fact]
        public void DeviceN_TintUnderFillsAlternate_GetColorPadsInsteadOfThrowing()
        {
            var deviceN = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B")],
                DeviceRgbColorSpaceDetails.Instance,
                Tint([0, 0], [1, 0]));

            var (r, g, b) = deviceN.GetColor([1.0, 1.0]).ToRGBValues();

            Assert.Equal(1.0, r, 12);
            Assert.Equal(0.0, g, 12);
            Assert.Equal(0.0, b, 12);

            AssertGetColorMatchesGetRgb(deviceN, [1.0, 1.0]);
        }

        [Fact]
        public void DeviceN_TintOverFillsAlternate_GetColorTrimsInsteadOfThrowing()
        {
            // Four outputs over a three component alternate: the surplus is dropped.
            var deviceN = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B")],
                DeviceRgbColorSpaceDetails.Instance,
                Tint([0, 0, 0, 0], [1, 0, 0, 1]));

            var (r, g, b) = deviceN.GetColor([1.0, 1.0]).ToRGBValues();

            Assert.Equal(1.0, r, 12);
            Assert.Equal(0.0, g, 12);
            Assert.Equal(0.0, b, 12);

            AssertGetColorMatchesGetRgb(deviceN, [1.0, 1.0]);
        }

        [Fact]
        public void Separation_GetColorIsCachedPerTintValue()
        {
            var separation = new SeparationColorSpaceDetails(
                NameToken.Create("Spot"),
                DeviceRgbColorSpaceDetails.Instance,
                Tint([0, 0, 0], [1, 0, 0]));

            Assert.Same(separation.GetColor([0.25]), separation.GetColor([0.25]));
        }

        [Fact]
        public void DeviceN_GetColorIsCachedPerComponentTuple()
        {
            var deviceN = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B")],
                DeviceRgbColorSpaceDetails.Instance,
                Tint([0, 0, 0], [1, 0, 0]));

            // Distinct arrays with equal contents must hit the same cache entry.
            Assert.Same(deviceN.GetColor([0.25, 0.5]), deviceN.GetColor([0.25, 0.5]));
            Assert.NotSame(deviceN.GetColor([0.25, 0.5]), deviceN.GetColor([0.75, 0.5]));
        }

        [Fact]
        public void ICCBased_GetColor_DoesNotClipIntoTheCallersBuffer()
        {
            // The caller's span may be over an array the caller keeps -- notably the Operands array held by
            // a parsed SetStrokeColorAdvanced operation -- so range clipping must not write through it.
            var icc = new ICCBasedColorSpaceDetails(
                3,
                DeviceRgbColorSpaceDetails.Instance,
                [0.0, 0.5, 0.0, 0.5, 0.0, 0.5],
                null);

            double[] values = [1.0, 1.0, 1.0];

            var (r, g, b) = icc.GetColor(values).ToRGBValues();

            Assert.Equal(0.5, r, 12);
            Assert.Equal(0.5, g, 12);
            Assert.Equal(0.5, b, 12);

            Assert.Equal(new[] { 1.0, 1.0, 1.0 }, values);
        }
    }
}
