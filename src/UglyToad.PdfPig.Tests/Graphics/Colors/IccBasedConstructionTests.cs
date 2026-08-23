namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Graphics.Colors;
    using Xunit;

    /// <summary>
    /// A colour space is built while a page is being processed, and nothing catches an exception from
    /// there, so a malformed entry that only affects one colour must not be allowed to cost the whole page.
    /// The ICCBased dictionary has two such entries: <c>/Range</c>, which is only ever used to clip, and
    /// <c>/Alternate</c>, which has a device colour space standing ready behind it.
    /// </summary>
    public class IccBasedConstructionTests
    {
        private static ICCBasedColorSpaceDetails Create(int n, ColorSpaceDetails? alternate = null,
            IReadOnlyList<double>? range = null)
            => new ICCBasedColorSpaceDetails(n, alternate, range, null);

        [Fact]
        public void ARangeOfTheWrongLength_IsIgnoredRatherThanThrown()
        {
            // /Range only clips; nothing about the colour space is unusable without it. Throwing here lost
            // the page over an entry the colour space could simply have done without.
            var details = Create(3, DeviceRgbColorSpaceDetails.Instance, [0.0, 1.0]);

            Assert.Equal(new double[] { 0, 1, 0, 1, 0, 1 }, details.Range);
        }

        [Fact]
        public void ARangeThatIsTooLong_IsIgnoredRatherThanThrown()
        {
            var details = Create(1, DeviceGrayColorSpaceDetails.Instance, [0.0, 1.0, 0.0, 1.0]);

            Assert.Equal(new double[] { 0, 1 }, details.Range);
        }

        [Fact]
        public void ARangeOfTheRightLengthIsStillHonoured()
        {
            // The control: dropping malformed ranges must not drop well-formed ones.
            var details = Create(3, DeviceRgbColorSpaceDetails.Instance, [0.0, 0.5, 0.0, 0.5, 0.0, 0.5]);

            Assert.Equal(new double[] { 0, 0.5, 0, 0.5, 0, 0.5 }, details.Range);
        }

        [Fact]
        public void AnAlternateOfTheWrongWidth_IsReplacedByTheSpaceImpliedByN()
        {
            // The alternate is handed the colour space's own operands, so a 4 component space over a
            // 3 component alternate cannot convert at all - GetColor on the alternate throws on the count.
            var details = Create(4, DeviceRgbColorSpaceDetails.Instance);

            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void AnAlternateOfTheWrongWidth_DoesNotThrowOnConversion()
        {
            var details = Create(4, DeviceRgbColorSpaceDetails.Instance);

            var exception = Record.Exception(() => details.GetColor([0.1, 0.2, 0.3, 0.4]));

            Assert.Null(exception);
        }

        [Fact]
        public void AnAlternateOfTheRightWidthIsStillUsed()
        {
            var lab = new LabColorSpaceDetails([0.9505, 1.0, 1.089], null, [-100, 100, -100, 100]);

            var details = Create(3, lab);

            Assert.Same(lab, details.AlternateColorSpace);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        public void AComponentCountNoIccBasedSpaceMayHave_StillThrows(int n)
        {
            // Unlike /Range and /Alternate there is nothing to fall back to: /N decides how many operands
            // every colour in this space carries, so a value outside 1, 3 or 4 leaves nothing to build.
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(n));
        }
    }
}
