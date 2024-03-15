namespace UglyToad.PdfPig.Tests.Encryption
{
    using PdfPig.Encryption;
    using PdfPig.Tokens;

    public class RC4Tests
    {
        [Theory]
        [InlineData("Plaintext", "Key", "BBF316E8D940AF0AD3")]
        [InlineData("pedia", "Wiki", "1021BF0420")]
        [InlineData("Attack at dawn", "Secret", "45A01F645FC35B383552544B9BF5")]
        public void Encrypt(string message, string keyText, string cipherTextHex)
        {
            var data = TextToBytes(message);
            var key = TextToBytes(keyText);

            var result = RC4.Encrypt(key, data);

            var expectedBytes = HexToBytes(cipherTextHex);

            Assert.Equal(expectedBytes, result);

            var reversed = RC4.Encrypt(key, result);

            Assert.Equal(data, reversed);
        }

        private static byte[] TextToBytes(string text)
        {
            return text.Select(x => (byte) x).ToArray();
        }

        private static byte[] HexToBytes(string hex)
        {
            var result = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                result[i / 2] = HexToken.Convert(hex[i], hex[i + 1]);
            }

            return result;
        }
    }
}
