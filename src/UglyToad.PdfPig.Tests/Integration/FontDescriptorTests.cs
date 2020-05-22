namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Content;
    using Xunit;

    public class FontDescriptorTests
    {
        public static object[][] DataBoldItalic => new[]
        {
            new object[] { "L", false, false },
            new object[] { "o", false, false },
            new object[] { "r", false, false },
            new object[] { "e", false, false },
            new object[] { "m", false, false },
            new object[] { " ", false, false },
            new object[] { "i", true, false },
            new object[] { "p", true, false },
            new object[] { "s", true, false },
            new object[] { "u", true, false },
            new object[] { "m", true, false },
            new object[] { " ", false, false },
            new object[] { "d", false, false },
            new object[] { "o", false, false },
            new object[] { "l", false, false },
            new object[] { "o", false, false },
            new object[] { "r", false, false },
            new object[] { " ", false, false },
            new object[] { "s", false, true },
            new object[] { "i", false, true },
            new object[] { "t", false, true },
            new object[] { " ", false, false },
            new object[] { "a", true, true },
            new object[] { "m", true, true },
            new object[] { "e", true, true },
            new object[] { "t", true, true },
            new object[] { ",", false, false },
            new object[] { " ", false, false },
            new object[] { "c", true, true },
            new object[] { "o", true, true },
            new object[] { "n", true, true },
            new object[] { "s", true, true },
            new object[] { "e", true, true },
            new object[] { "c", true, true },
            new object[] { "t", true, true },
            new object[] { "e", true, true },
            new object[] { "t", true, true },
            new object[] { "u", true, true },
            new object[] { "r", true, true },
            new object[] { " ", false, false },
            new object[] { "a", false, false },
            new object[] { "d", false, false },
            new object[] { "i", false, false },
            new object[] { "p", false, false },
            new object[] { "i", false, false },
            new object[] { "s", false, false },
            new object[] { "c", false, false },
            new object[] { "i", false, false },
            new object[] { "n", false, false },
            new object[] { "g", false, false },
            new object[] { " ", false, false },
            new object[] { "e", false, false },
            new object[] { "l", false, false },
            new object[] { "i", false, false },
            new object[] { "t", false, false },
            new object[] { ".", false, false },
            new object[] { " ", false, false }
        };

        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "bold-italic.pdf");
        }

        [Fact]
        public void GetsCorrectBoldItalic()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);
                Assert.Equal(DataBoldItalic.Length, page.Letters.Count);

                for (int l = 0; l < page.Letters.Count; l++)
                {
                    var letter = page.Letters[l];
                    var expected = DataBoldItalic[l];
                    Assert.Equal((string)expected[0], letter.Value);
                    Assert.Equal((bool)expected[1], letter.Font.IsBold);
                    Assert.Equal((bool)expected[2], letter.Font.IsItalic);
                }
            }
        }
    }
}
