namespace UglyToad.PdfPig.Tests.Parser.Parts.CrossReference
{
    using System;
    using IO;
    using PdfPig.Parser.Parts.CrossReference;
    using PdfPig.Util;
    using Xunit;

    public class TableSubsectionDefinitionTests
    {
        private readonly TestingLog log = new TestingLog();

        [Fact]
        public void SetsPropertiesCorrectly()
        {
            var definition = new TableSubsectionDefinition(5, 12);

            Assert.Equal(5, definition.FirstNumber);
            Assert.Equal(12, definition.Count);
        }

        [Fact]
        public void CountCannotBeNegative()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action action = () => new TableSubsectionDefinition(1, -12);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void ToStringRepresentsPdfForm()
        {
            var definition = new TableSubsectionDefinition(420, 69);

            Assert.Equal("420 69", definition.ToString());
        }

        [Fact]
        public void TryReadIncorrectFormatSinglePartFalse()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(@"76362");

            var input = new RandomAccessBuffer(bytes);

            var result = TableSubsectionDefinition.TryRead(log, input, out var _);

            Assert.False(result);
        }

        [Fact]
        public void TryReadIncorrectFormatMultiplePartsFalse()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(@"76362 100 1000");

            var input = new RandomAccessBuffer(bytes);

            var result = TableSubsectionDefinition.TryRead(log, input, out var _);

            Assert.False(result);
        }

        [Fact]
        public void FirstPartInvalidFormatFalse()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes("00adb85 97");

            var input = new RandomAccessBuffer(bytes);

            var result = TableSubsectionDefinition.TryRead(log, input, out var _);

            Assert.False(result);
        }

        [Fact]
        public void SecondPartInvalidFormatFalse()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes("85 9t");

            var input = new RandomAccessBuffer(bytes);

            var result = TableSubsectionDefinition.TryRead(log, input, out var _);

            Assert.False(result);
        }

        [Fact]
        public void ValidTrue()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes("12 32");

            var input = new RandomAccessBuffer(bytes);

            var result = TableSubsectionDefinition.TryRead(log, input, out var definition);

            Assert.True(result);

            Assert.Equal(12, definition.FirstNumber);
            Assert.Equal(32, definition.Count);
        }

        [Fact]
        public void ValidWithLongTrue()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes("214748364700 6");

            var input = new RandomAccessBuffer(bytes);

            var result = TableSubsectionDefinition.TryRead(log, input, out var definition);

            Assert.True(result);

            Assert.Equal(214748364700L, definition.FirstNumber);
            Assert.Equal(6, definition.Count);
        }
    }
}
