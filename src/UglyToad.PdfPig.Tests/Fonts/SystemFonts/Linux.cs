using System.Collections.Generic;
using UglyToad.PdfPig.Fonts.SystemFonts;
using UglyToad.PdfPig.Tests.Dla;
using Xunit;

namespace UglyToad.PdfPig.Tests.Fonts.SystemFonts
{
    using PdfPig.Core;

    public class Linux
    {
        public static IEnumerable<object[]> DataExtract => new[]
         {
            new object[]
            {
                "90 180 270 rotated.pdf",
                new ExpectedLetterData[]
                {
                    new ExpectedLetterData
                    {
                        TopLeft = new PdfPoint(53.88, 759.48),
                        Width = 2.495859375,
                        Height = 0,
                        Rotation = 0
                    },
                    new ExpectedLetterData
                    {
                        TopLeft = new PdfPoint(514.925312502883, 744.099765720344),
                        Width = 6.83203125,
                        Height = 7.94531249999983,
                        Rotation = -90
                    },
                    new ExpectedLetterData
                    {
                        TopLeft = new PdfPoint(512.505390717836, 736.603703191305),
                        Width = 5.1796875,
                        Height = 5.68945312499983,
                        Rotation = -90
                    },
                    new ExpectedLetterData
                    {
                        TopLeft = new PdfPoint(512.505390785898, 730.931828191305),
                        Width = 3.99609375, 
                        Height = 5.52539062499994,
                        Rotation = -90
                    },
                }
            },
        };

        [SkippableTheory]
        [MemberData(nameof(DataExtract))]
        public void GetCorrectBBoxLinux(string name, ExpectedLetterData[] expected)
        {
            // success on Windows but LinuxSystemFontLister cannot find the 'TimesNewRomanPSMT' font
            var font = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPSMT");

            Skip.If(font == null, "Skipped because the font TimesNewRomanPSMT could not be found in the execution environment.");
            
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath(name)))
            {
                var page = document.GetPage(1);
                for (int i = 0; i < expected.Length; i++)
                {
                    var expectedData = expected[i];

                    var current = page.Letters[i];

                    Assert.Equal(expectedData.TopLeft.X, current.GlyphRectangle.TopLeft.X, 7);
                    Assert.Equal(expectedData.TopLeft.Y, current.GlyphRectangle.TopLeft.Y, 7);
                    Assert.Equal(expectedData.Width, current.GlyphRectangle.Width, 7);
                    Assert.Equal(expectedData.Height, current.GlyphRectangle.Height, 7);
                    Assert.Equal(expectedData.Rotation, current.GlyphRectangle.Rotation, 3);
                }
            }
        }

        public class ExpectedLetterData
        {
            public PdfPoint TopLeft { get; set; }

            public double Width { get; set; }

            public double Height { get; set; }

            public double Rotation { get; set; }
        }
    }
}
