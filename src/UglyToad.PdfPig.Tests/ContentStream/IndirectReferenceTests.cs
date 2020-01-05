namespace UglyToad.PdfPig.Tests.ContentStream
{
    using PdfPig.Core;
    using Xunit;

    public class IndirectReferenceTests
    {
        [Fact]
        public void SetsProperties()
        {
            var reference = new IndirectReference(129, 45);

            Assert.Equal(129, reference.ObjectNumber);
            Assert.Equal(45, reference.Generation);
        }

        [Fact]
        public void ToStringCorrect()
        {
            var reference = new IndirectReference(130, 70);

            Assert.Equal("130 70", reference.ToString());
        }

        [Fact]
        public void TwoIndirectReferenceEqual()
        {
            var reference1 = new IndirectReference(1574, 690);
            var reference2 = new IndirectReference(1574, 690);

            Assert.True(reference1.Equals(reference2));
        }

        [Fact]
        public void TwoIndirectReferenceNotEqual()
        {
            var reference1 = new IndirectReference(1574, 690);
            var reference2 = new IndirectReference(12, 0);

            Assert.False(reference1.Equals(reference2));
        }

        [Fact]
        public void TwoIndirectHashCodeEqual()
        {
            var reference1 = new IndirectReference(1267775544, 690);
            var reference2 = new IndirectReference(1267775544, 690);

            Assert.Equal(reference1.GetHashCode(), reference2.GetHashCode());
        }

        [Fact]
        public void TwoIndirectHashCodeNotEqual()
        {
            var reference1 = new IndirectReference(1267775544, 690);
            var reference2 = new IndirectReference(1267775544, 12);

            Assert.NotEqual(reference1.GetHashCode(), reference2.GetHashCode());
        }

        [Fact]
        public void TwoIndirectHashCodeSimilarValuesNotEqual()
        {
            var reference1 = new IndirectReference(12, 1);
            var reference2 = new IndirectReference(1, 12);

            Assert.NotEqual(reference1.GetHashCode(), reference2.GetHashCode());
        }

        [Fact]
        public void OtherObjectNotEqual()
        {
            var reference = new IndirectReference(1267775544, 690);
            var obj = "test";

            // ReSharper disable once SuspiciousTypeConversion.Global
            Assert.False(reference.Equals(obj));
        }

        [Fact]
        public void NullNotEqual()
        {
            var reference = new IndirectReference(1267775544, 690);

            // ReSharper disable once SuspiciousTypeConversion.Global
            Assert.False(reference.Equals(null));
        }
    }
}
