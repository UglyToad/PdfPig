namespace UglyToad.PdfPig.Tests.Fonts.TrueType.Tables
{
    using System.IO;
    using System.Linq;
    using PdfPig.Core;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using Xunit;

    public class Os2TableTests
    {
        [Theory]
        [InlineData("Andada-Regular")]
        [InlineData("Roboto-Regular")]
        [InlineData("PMingLiU")]
        public void WritesSameTableAsRead(string fontFile)
        {
            var fontBytes = TrueTypeTestHelper.GetFileBytes(fontFile);

            var parsed = TrueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontBytes)));

            var os2 = parsed.TableRegister.Os2Table;
            var os2Header = parsed.TableHeaders.Single(x => x.Value.Tag == TrueTypeHeaderTable.Os2);

            var os2InputBytes = fontBytes.Skip((int) os2Header.Value.Offset).Take((int) os2Header.Value.Length).ToArray();

            using (var stream = new MemoryStream())
            {
                os2.Write(stream);

                var result = stream.ToArray();

                Assert.Equal(os2InputBytes.Length, result.Length);

                for (var i = 0; i < os2InputBytes.Length; i++)
                {
                    var expected = os2InputBytes[i];
                    var actual = result[i];

                    Assert.Equal(expected, actual);
                }
            }
        }
    }
}
