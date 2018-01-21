namespace UglyToad.PdfPig.Tests.Parser.Parts.CrossReference
{
    using System;
    using PdfPig.Parser.Parts.CrossReference;
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
            var input = StringBytesTestConverter.Convert("76362", false);

            var result = TableSubsectionDefinition.TryRead(log, input.Bytes, out var _);

            Assert.False(result);
        }

        [Fact]
        public void TryReadIncorrectFormatMultiplePartsFalse()
        {
            var input = StringBytesTestConverter.Convert("76362 100 1000", false);

            var result = TableSubsectionDefinition.TryRead(log, input.Bytes, out var _);

            Assert.False(result);
        }

        [Fact]
        public void FirstPartInvalidFormatFalse()
        {
            var input = StringBytesTestConverter.Convert("00adb85 97", false);

            var result = TableSubsectionDefinition.TryRead(log, input.Bytes, out var _);

            Assert.False(result);
        }

        [Fact]
        public void SecondPartInvalidFormatFalse()
        {
            var input = StringBytesTestConverter.Convert("85 9t", false);
            
            var result = TableSubsectionDefinition.TryRead(log, input.Bytes, out var _);

            Assert.False(result);
        }

        [Fact]
        public void ValidTrue()
        {
            var input = StringBytesTestConverter.Convert("12 32", false);

            var result = TableSubsectionDefinition.TryRead(log, input.Bytes, out var definition);

            Assert.True(result);

            Assert.Equal(12, definition.FirstNumber);
            Assert.Equal(32, definition.Count);
        }

        [Fact]
        public void ValidWithLongTrue()
        {
            var input = StringBytesTestConverter.Convert("214748364700 6", false);

            var result = TableSubsectionDefinition.TryRead(log, input.Bytes, out var definition);

            Assert.True(result);

            Assert.Equal(214748364700L, definition.FirstNumber);
            Assert.Equal(6, definition.Count);
        }
    }
}
