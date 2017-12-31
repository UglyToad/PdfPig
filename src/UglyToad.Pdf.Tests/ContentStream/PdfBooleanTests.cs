namespace UglyToad.Pdf.Tests.ContentStream
{
    using System;
    using System.IO;
    using Pdf.ContentStream;
    using Pdf.Util;
    using Xunit;

    public class PdfBooleanTests
    {
        [Fact]
        public void TrueIsTrue()
        {
            Assert.True(PdfBoolean.True.Value);
        }

        [Fact]
        public void FalseIsFalse()
        {
            Assert.False(PdfBoolean.False.Value);
        }

        [Fact]
        public void ImplicitTrueIsTrue()
        {
            Assert.True(PdfBoolean.True);
        }

        [Fact]
        public void ImplicitFalseIsFalse()
        {
            Assert.False(PdfBoolean.False);
        }

        [Fact]
        public void CastNullThrows()
        {
            PdfBoolean boolean = null;

            // ReSharper disable once ExpressionIsAlwaysNull
            Action action = () => Test(boolean);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void StringRepresentationTrueCorrect()
        {
            Assert.Equal("true", PdfBoolean.True.ToString());
        }

        [Fact]
        public void StringRepresentationFalseCorrect()
        {
            Assert.Equal("false", PdfBoolean.False.ToString());
        }

        [Fact]
        public void OperatorEqualityWorksCorrectly()
        {
            // ReSharper disable once EqualExpressionComparison
            Assert.True(PdfBoolean.True == PdfBoolean.True);
            // ReSharper disable once EqualExpressionComparison
            Assert.True(PdfBoolean.False == PdfBoolean.False);

            Assert.False(PdfBoolean.True == PdfBoolean.False);
            Assert.False(PdfBoolean.False == PdfBoolean.True);
        }

        [Fact]
        public void EqualityWorksCorrectly()
        {
            var result = PdfBoolean.True.Equals(PdfBoolean.False);

            Assert.False(result);

            result = PdfBoolean.False.Equals(PdfBoolean.True);

            Assert.False(result);

            result = PdfBoolean.True.Equals(PdfBoolean.True);

            Assert.True(result);

            result = PdfBoolean.False.Equals(PdfBoolean.False);

            Assert.True(result);
        }

        [Fact]
        public void ObjectEqualityWorksCorrectly()
        {
            var result = PdfBoolean.True.Equals(null);

            Assert.False(result);

            // ReSharper disable once SuspiciousTypeConversion.Global
            result = PdfBoolean.False.Equals("happy");

            Assert.False(result);

            result = PdfBoolean.False.Equals((object) PdfBoolean.False);

            Assert.True(result);
        }

        [Fact]
        public void WritesToStreamCorrectly()
        {
            using (var memoryStream = new MemoryStream())
            using (var write = new BinaryWriter(memoryStream))
            {
                PdfBoolean.True.WriteToPdfStream(write);
                PdfBoolean.False.WriteToPdfStream(write);

                write.Flush();

                var result = OtherEncodings.BytesAsLatin1String(memoryStream.ToArray());

                Assert.Equal("truefalse", result);
            }
        }

        private static bool Test(bool input)
        {
            return input;
        }
    }
}
