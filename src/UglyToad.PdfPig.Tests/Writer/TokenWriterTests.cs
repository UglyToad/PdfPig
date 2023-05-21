namespace UglyToad.PdfPig.Tests.Writer
{
    using Integration;
    using PdfPig.Writer;
    using System.IO;
    using UglyToad.PdfPig.Tokens;
    using Xunit;

    public class TokenWriterTests
    {

        [Fact]
        public void EscapeSpecialCharacter()
        {
            var writer = new TokenWriter();
            using (var memStream = new MemoryStream())
            {
                writer.WriteToken(new StringToken("\\"), memStream);
                writer.WriteToken(new StringToken("(Hello)"), memStream);

                // Read Test
                memStream.Position = 0;
                using (var streamReader = new StreamReader(memStream))
                {
                    var line = streamReader.ReadToEnd();
                    Assert.Equal("(\\\\) (\\(Hello\\)) ", line);
                }
            }
        }
    }
}
