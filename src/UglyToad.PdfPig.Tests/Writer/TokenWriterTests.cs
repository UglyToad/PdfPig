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
            using (var memStream = new MemoryStream())
            {
                TokenWriter.WriteToken(new StringToken("\\"), memStream);
                TokenWriter.WriteToken(new StringToken("(Hello)"), memStream);

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
