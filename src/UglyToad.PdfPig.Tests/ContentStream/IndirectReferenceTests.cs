namespace UglyToad.PdfPig.Tests.ContentStream
{
    using PdfPig.Core;

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
        public void IndirectReferenceHashTest()
        {
            var reference0 = new IndirectReference(1574, 690);
            Assert.Equal(1574, reference0.ObjectNumber);
            Assert.Equal(690, reference0.Generation);

            var reference1 = new IndirectReference(-1574, 690);
            Assert.Equal(-1574, reference1.ObjectNumber);
            Assert.Equal(690, reference1.Generation);

            var reference2 = new IndirectReference(58949797283757, 16);
            Assert.Equal(58949797283757, reference2.ObjectNumber);
            Assert.Equal(16, reference2.Generation);

            var reference3 = new IndirectReference(-58949797283757, ushort.MaxValue);
            Assert.Equal(-58949797283757, reference3.ObjectNumber);
            Assert.Equal(ushort.MaxValue, reference3.Generation);

            var reference4 = new IndirectReference(140737488355327, ushort.MaxValue);
            Assert.Equal(140737488355327, reference4.ObjectNumber);
            Assert.Equal(ushort.MaxValue, reference4.Generation);

            var reference5 = new IndirectReference(-140737488355327, ushort.MaxValue);
            Assert.Equal(-140737488355327, reference5.ObjectNumber);
            Assert.Equal(ushort.MaxValue, reference5.Generation);

            var ex0 = Assert.Throws<ArgumentOutOfRangeException>(() => new IndirectReference(140737488355328, 0));
            Assert.StartsWith("Object number must be between -140,737,488,355,327 and 140,737,488,355,327.", ex0.Message);
            var ex1 = Assert.Throws<ArgumentOutOfRangeException>(() => new IndirectReference(-140737488355328, 0));
            Assert.StartsWith("Object number must be between -140,737,488,355,327 and 140,737,488,355,327.", ex1.Message);
            
            var ex2 = Assert.Throws<ArgumentOutOfRangeException>(() => new IndirectReference(1574, -1));
            Assert.StartsWith("Generation number must not be a negative value.", ex2.Message);
            
            // We make sure object number is still correct even if generation is not
            var reference6 = new IndirectReference(1574, int.MaxValue);
            Assert.Equal(1574, reference6.ObjectNumber);
            
            var reference7 = new IndirectReference(-1574, ushort.MaxValue + 10);
            Assert.Equal(-1574, reference7.ObjectNumber);

            var reference9 = new IndirectReference(-140737488355327, ushort.MaxValue + 10);
            Assert.Equal(-140737488355327, reference9.ObjectNumber);

            var reference10 = new IndirectReference(140737488355327, ushort.MaxValue * 10);
            Assert.Equal(140737488355327, reference10.ObjectNumber);
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
