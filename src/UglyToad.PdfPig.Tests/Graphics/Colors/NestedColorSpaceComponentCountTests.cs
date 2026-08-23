namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using PdfPig.Functions;
    using PdfPig.Graphics.Colors;
    using PdfPig.Tokens;

    /// <summary>
    /// <see cref="ColorSpaceDetails.BaseNumberOfColorComponents"/> must report the component count of the
    /// space the samples are ultimately expressed in, i.e. the count that <see cref="ColorSpaceDetails.Process"/>
    /// (and therefore <see cref="ColorSpaceDetails.Transform"/>) actually produces per sample, and
    /// <see cref="ColorSpaceDetails.BaseType"/> must name that same space, resolved to the same depth.
    /// </summary>
    public class NestedColorSpaceComponentCountTests
    {
        /// <summary>
        /// A type 2 (exponential interpolation) tint function over domain [0 1] with exponent 1,
        /// i.e. f(t) = c0 + t × (c1 − c0). Only the first input is read, so it doubles as an
        /// N-in / M-out tint function for DeviceN.
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

        /// <summary>
        /// Separation whose tint maps t to (0, 0, 0, t) in DeviceCMYK.
        /// </summary>
        private static SeparationColorSpaceDetails SeparationOverCmyk()
        {
            return new SeparationColorSpaceDetails(
                NameToken.Create("Spot"),
                DeviceCmykColorSpaceDetails.Instance,
                Tint([0, 0, 0, 0], [0, 0, 0, 1]));
        }

        /// <summary>
        /// Separation whose tint maps t to (t, 0, 0) in DeviceRGB.
        /// </summary>
        private static SeparationColorSpaceDetails SeparationOverRgb()
        {
            return new SeparationColorSpaceDetails(
                NameToken.Create("Spot"),
                DeviceRgbColorSpaceDetails.Instance,
                Tint([0, 0, 0], [1, 0, 0]));
        }

        [Fact]
        public void Separation_OverDeviceCmyk_ReportsFourBaseComponents()
        {
            var separation = SeparationOverCmyk();

            Assert.Equal(1, separation.NumberOfColorComponents);
            Assert.Equal(4, separation.BaseNumberOfColorComponents);
        }

        [Fact]
        public void Separation_SetsBaseTypeFromAlternateSpace()
        {
            var separation = SeparationOverCmyk();

            Assert.Equal(ColorSpace.Separation, separation.Type);
            Assert.Equal(ColorSpace.DeviceCMYK, separation.BaseType);
        }

        [Fact]
        public void Separation_OverIccBased_ResolvesToTheDeviceAlternate()
        {
            // The ICCBased space is really converting through DeviceCMYK, which is what
            // BaseNumberOfColorComponents has always said, and what BaseType now says too. Naming the
            // intermediate ICCBased here would describe a step the colours do not actually take.
            var icc = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance, null, null);
            var separation = new SeparationColorSpaceDetails(
                NameToken.Create("Spot"),
                icc,
                Tint([0, 0, 0, 0], [0, 0, 0, 1]));

            Assert.Equal(ColorSpace.DeviceCMYK, separation.BaseType);
            Assert.Equal(4, separation.BaseNumberOfColorComponents);
        }

        [Fact]
        public void IccBased_OverADeviceN_ReportsTheDeviceNsOwnBaseWidth()
        {
            // The alternate is what Transform delegates to, so a 3 component ICCBased over a DeviceN
            // that expands to CMYK still writes 4 bytes per sample. Reporting NumberOfColorComponents
            // here sized every downstream buffer to 3 and ran the write off the end.
            //
            // The alternate takes three components like the space itself, which is what lets it stand in
            // for the profile at all; only the width it converts *into* differs.
            var deviceN = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B"), NameToken.Create("C")],
                DeviceCmykColorSpaceDetails.Instance,
                Tint([0, 0, 0, 0], [0, 0, 0, 1]));

            var icc = new ICCBasedColorSpaceDetails(3, deviceN, null, null);

            Assert.Same(deviceN, icc.AlternateColorSpace);
            Assert.Equal(3, icc.NumberOfColorComponents);
            Assert.Equal(4, icc.BaseNumberOfColorComponents);
            Assert.Equal(ColorSpace.DeviceCMYK, icc.BaseType);
        }

        [Fact]
        public void Separation_OverSeparation_ResolvesBaseComponentsThroughTheChain()
        {
            // Outer tint is the identity, so the outer space resolves to the inner Separation,
            // which itself resolves to DeviceCMYK: 4 components, not the inner space's 1.
            var outer = new SeparationColorSpaceDetails(
                NameToken.Create("Outer"),
                SeparationOverCmyk(),
                Tint([0], [1]));

            Assert.Equal(1, outer.NumberOfColorComponents);
            Assert.Equal(4, outer.BaseNumberOfColorComponents);
        }

        [Fact]
        public void Separation_OverSeparation_TransformProducesOneSamplePerBaseComponent()
        {
            var outer = new SeparationColorSpaceDetails(
                NameToken.Create("Outer"),
                SeparationOverCmyk(),
                Tint([0], [1]));

            // Sizing Transform's buffer from the alternate's NumberOfColorComponents (1) instead of
            // BaseNumberOfColorComponents (4) overflowed the output buffer.
            byte[] transformed = outer.Transform([255, 0]).ToArray();

            Assert.Equal(2 * outer.BaseNumberOfColorComponents, transformed.Length);
            Assert.Equal(new byte[] { 0, 0, 0, 255, 0, 0, 0, 0 }, transformed);
        }

        [Fact]
        public void Separation_OverDeviceCmyk_TransformProducesOneSamplePerBaseComponent()
        {
            var separation = SeparationOverCmyk();

            byte[] transformed = separation.Transform([255, 0]).ToArray();

            Assert.Equal(2 * separation.BaseNumberOfColorComponents, transformed.Length);
            Assert.Equal(new byte[] { 0, 0, 0, 255, 0, 0, 0, 0 }, transformed);
        }

        [Fact]
        public void DeviceN_OverDeviceCmyk_ReportsFourBaseComponents()
        {
            var deviceN = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B")],
                DeviceCmykColorSpaceDetails.Instance,
                Tint([0, 0, 0, 0], [0, 0, 0, 1]));

            Assert.Equal(2, deviceN.NumberOfColorComponents);
            Assert.Equal(4, deviceN.BaseNumberOfColorComponents);
            Assert.Equal(ColorSpace.DeviceCMYK, deviceN.BaseType);
        }

        [Fact]
        public void DeviceN_OverSeparation_ResolvesBaseComponentsThroughTheChain()
        {
            var deviceN = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B")],
                SeparationOverRgb(),
                Tint([0], [1]));

            // The alternate Separation has 1 component but ultimately produces DeviceRGB triples.
            Assert.Equal(2, deviceN.NumberOfColorComponents);
            Assert.Equal(3, deviceN.BaseNumberOfColorComponents);
        }

        [Fact]
        public void DeviceN_OverSeparation_TransformProducesOneSamplePerBaseComponent()
        {
            var deviceN = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B")],
                SeparationOverRgb(),
                Tint([0], [1]));

            // Two samples of two components each.
            byte[] transformed = deviceN.Transform([255, 0, 0, 255]).ToArray();

            Assert.Equal(2 * deviceN.BaseNumberOfColorComponents, transformed.Length);
            Assert.Equal(new byte[] { 255, 0, 0, 0, 0, 0 }, transformed);
        }

        [Fact]
        public void DeviceN_OverDeviceN_ResolvesBaseComponentsThroughTheChain()
        {
            var inner = new DeviceNColorSpaceDetails(
                [NameToken.Create("X"), NameToken.Create("Y")],
                DeviceCmykColorSpaceDetails.Instance,
                Tint([0, 0, 0, 0], [0, 0, 0, 1]));

            var outer = new DeviceNColorSpaceDetails(
                [NameToken.Create("A"), NameToken.Create("B"), NameToken.Create("C")],
                inner,
                Tint([0, 0], [1, 1]));

            Assert.Equal(3, outer.NumberOfColorComponents);
            Assert.Equal(4, outer.BaseNumberOfColorComponents);

            byte[] transformed = outer.Transform([255, 0, 0]).ToArray();

            Assert.Equal(outer.BaseNumberOfColorComponents, transformed.Length);
            Assert.Equal(new byte[] { 0, 0, 0, 255 }, transformed);
        }

        [Fact]
        public void Indexed_OverSeparationOverSeparation_ResolvesBaseComponentsThroughTheChain()
        {
            var separation = new SeparationColorSpaceDetails(
                NameToken.Create("Outer"),
                SeparationOverCmyk(),
                Tint([0], [1]));

            var indexed = new IndexedColorSpaceDetails(separation, 1, [0x00, 0xFF]);

            Assert.Equal(1, indexed.NumberOfColorComponents);
            Assert.Equal(4, indexed.BaseNumberOfColorComponents);

            // Through both Separations to the device space underneath, matching the component count
            // beside it rather than naming the first link in the chain.
            Assert.Equal(ColorSpace.DeviceCMYK, indexed.BaseType);
        }
    }
}
