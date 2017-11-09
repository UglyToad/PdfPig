using System;
using System.Text;

namespace UglyToad.Pdf.Tests.Parser.Parts
{
    using IO;
    using Pdf.Parser.Parts;
    using Xunit;

    public class CosNameParserTests
    {
        private readonly CosNameParser parser = new CosNameParser();

        [Fact]
        public void ReaderNull_Throws()
        {
            Action action = () => parser.Parse(null);

            Assert.Throws<ArgumentNullException>(action);
        }
        
        [Fact]
        public void ReadsName()
        {
            var input = "/Type /XRef";

            var bytes = Encoding.UTF8.GetBytes(input);

            var access = new RandomAccessBuffer(bytes);

            var name = parser.Parse(access);

            var text = name.Name;

            Assert.Equal("Type", text);
        }

        [Fact]
        public void ReadsNameNoEndSpace()
        {
            var input = "/Type/XRef";

            var bytes = Encoding.UTF8.GetBytes(input);

            var access = new RandomAccessBuffer(bytes);

            var name = parser.Parse(access);

            var text = name.Name;

            Assert.Equal("Type", text);
        }

        [Fact]
        public void ReadsName_NotAtForwardSlash_Throws()
        {
            var input = " /Type";

            var bytes = Encoding.UTF8.GetBytes(input);

            var access = new RandomAccessBuffer(bytes);

            Action action = () => parser.Parse(access);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ReadsNameAtEndOfStream()
        {
            var input = "/Xref";

            var bytes = Encoding.UTF8.GetBytes(input);

            var access = new RandomAccessBuffer(bytes);

            var name = parser.Parse(access);

            var text = name.Name;

            Assert.Equal("Xref", text);
        }

        [Theory]
        [InlineData("/Name1", "Name1")]
        [InlineData("/ASomewhatLongerName", "ASomewhatLongerName")]
        [InlineData("/A;Name_With−Various***Characters?", "A;Name_With−Various***Characters?")]
        [InlineData("/1.2", "1.2")]
        [InlineData("/$$", "$$")]
        [InlineData("/@pattern", "@pattern")]
        [InlineData("/.notdef", ".notdef")]
        public void ReadsValidPdfNames(string inputString, string expected)
        {
            var input = inputString;

            var bytes = Encoding.UTF8.GetBytes(input);

            var access = new RandomAccessBuffer(bytes);

            var name = parser.Parse(access);

            var text = name.Name;

            Assert.Equal(expected, text);
        }

        [Theory]
        [InlineData("/Adobe#20Green", "Adobe Green")]
        [InlineData("/PANTONE#205757#20CV", "PANTONE 5757 CV")]
        [InlineData("/paired#28#29parentheses", "paired()parentheses")]
        [InlineData("/The_Key_of_F#23_Minor", "The_Key_of_F#_Minor")]
        [InlineData("/A#42", "AB")]
        public void ReadsHexNames(string inputString, string expected)
        {
            var input = inputString;

            var bytes = Encoding.UTF8.GetBytes(input);

            var access = new RandomAccessBuffer(bytes);

            var name = parser.Parse(access);

            var text = name.Name;

            Assert.Equal(expected, text);
        }
    }
}
