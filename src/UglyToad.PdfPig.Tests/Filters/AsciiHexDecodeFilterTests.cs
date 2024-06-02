namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Text;
    using PdfPig.Filters;
    using PdfPig.Tokens;
 
    public class AsciiHexDecodeFilterTests
    {
        private readonly DictionaryToken dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());

        [Fact]
        public void DecodesEncodedTextProperly()
        {
            const string text = "she sells seashells on the sea shore";

            var input = Encoding.ASCII.GetBytes(
                "7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265");

            var decoded = new AsciiHexDecodeFilter().Decode(input, dictionary, 1);

            var decodedText = Encoding.ASCII.GetString(decoded.ToArray());

            Assert.Equal(text, decodedText);
        }

        [Fact]
        public void DecodesEncodedTextWithBracesProperly()
        {
            const string text = "she sells seashells on the sea shore";

            var input = Encoding.ASCII.GetBytes(
                "<7368652073656C6C73207365617368656C6C73206F6E20746865207365612073686F7265>");

            var decoded = new AsciiHexDecodeFilter().Decode(input, dictionary, 1);

            var decodedText = Encoding.ASCII.GetString(decoded.ToArray());

            Assert.Equal(text, decodedText);
        }

        [Fact]
        public void DecodesEncodedTextWithWhitespaceProperly()
        {
            const string text = "once upon a time in a galaxy Far Far Away";

            var input = Encoding.ASCII.GetBytes(
                @"6F6E6365207      5706F6E206120     74696D6520696E
    20612067616C6178792046617220466172204177    6179");

            var decoded = new AsciiHexDecodeFilter().Decode(input, dictionary, 1);

            var decodedText = Encoding.ASCII.GetString(decoded.ToArray());

            Assert.Equal(text, decodedText);
        }

        [Fact]
        public void DecodesEncodedTextLowercaseProperly()
        {
            const string text = "once upon a time in a galaxy Far Far Away";

            var input = Encoding.ASCII.GetBytes("6f6e63652075706f6e20612074696d6520696e20612067616c61787920466172204661722041776179");

            var decoded = new AsciiHexDecodeFilter().Decode(input, dictionary, 1);

            var decodedText = Encoding.ASCII.GetString(decoded.ToArray());

            Assert.Equal(text, decodedText);
        }

        [Theory]
        [InlineData("ZA")]
        [InlineData("AM")]
        public void DecodeWithInvalidCharactersThrows(string inputString)
        {
            var input = Encoding.ASCII.GetBytes(inputString);

            Action action = () => new AsciiHexDecodeFilter().Decode(input, dictionary, 1);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void SubstitutesZeroForLastByte()
        {
            var input = Encoding.ASCII.GetBytes("AE5>");

            var decoded = new AsciiHexDecodeFilter().Decode(input, dictionary, 1);

#pragma warning disable SYSLIB0001
            var decodedText = Encoding.UTF7.GetString(decoded.ToArray());
#pragma warning restore SYSLIB0001

            Assert.Equal("®P", decodedText);
        }

        [Fact]
        public void DecodesEncodedTextStoppingAtLastBrace()
        {
            const string text = "once upon a time in a galaxy Far Far Away";

            var input = Encoding.ASCII.GetBytes("6f6e63652075706f6e20612074696d6520696e20612067616c61787920466172204661722041776179> There is stuff following the EOD.");

            var decoded = new AsciiHexDecodeFilter().Decode(input, dictionary, 1);

            var decodedText = Encoding.ASCII.GetString(decoded.ToArray());

            Assert.Equal(text, decodedText);
        }
    }
}
