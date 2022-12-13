namespace UglyToad.PdfPig.Tests.Writer.Fonts
{
    using System;
     
    using System.Linq;
    using PdfPig.Fonts;
    using PdfPig.Content;
    using UglyToad.PdfPig.Core;    
    using UglyToad.PdfPig.Fonts.Standard14Fonts;
    using UglyToad.PdfPig.Writer;
     
    using Xunit;
    using System.Reflection;
    using System.Collections.Generic;
    using UglyToad.PdfPig.Fonts.AdobeFontMetrics;
    using System.IO;
    using System.Drawing;
    using System.Diagnostics;

    public class Standard14WritingFontTests
    {
        [Fact]
        public void ZapfDingbatsFontAddText()
        {
            PdfDocumentBuilder builder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont F1 = builder.AddStandard14Font(Standard14Font.ZapfDingbats);
            PdfPageBuilder page = builder.AddPage(PageSize.A4);

            double topPageY = page.PageSize.Top - 50;
            double inch = (page.PageSize.Width / 8.5);
            double cm = inch / 2.54;
            double leftX = 1 * cm;
            var point = new PdfPoint(leftX, topPageY);

            // Get existing (but private) EncodingTable from encoding class using reflection so we can obtain all codes
            var ZapfDingbatsEncodingType = typeof(UglyToad.PdfPig.Fonts.Encodings.ZapfDingbatsEncoding);
            var EncodingTableFieldInfo = ZapfDingbatsEncodingType.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                                            .FirstOrDefault(v=>v.Name=="EncodingTable");            
            (int, string)[] EncodingTable = ((int, string)[])EncodingTableFieldInfo.GetValue(Activator.CreateInstance(ZapfDingbatsEncodingType, true));


            foreach ((var code, var name) in EncodingTable)
            {             
                var ch = (char)code; // Note code is already base 10 no need to use OctalHelpers.FromOctalInt or System.Convert.ToInt32($"{code}", 8);
                point = AddLetterWithFont(page, point, $"{ch}", F1, nameof(F1));
            }

            // Save one page PDF to file system for manual review.
            var pdfBytes = builder.Build();
            WritePdfFile(nameof(ZapfDingbatsFontAddText), pdfBytes);
        }

        [Fact]
        public void SymbolFontAddText()
        {
            PdfDocumentBuilder builder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont F1 = builder.AddStandard14Font(Standard14Font.Symbol);
            PdfPageBuilder page = builder.AddPage(PageSize.A4);

            double topPageY = page.PageSize.Top - 50;
            double inch = (page.PageSize.Width / 8.5);
            double cm = inch / 2.54;
            double leftX = 1 * cm;
            var point = new PdfPoint(leftX, topPageY);

            // Get existing (but private) EncodingTable from encoding class using reflection so we can obtain all codes
            var SymbolEncodingType = typeof(UglyToad.PdfPig.Fonts.Encodings.SymbolEncoding);
            var EncodingTableFieldInfo = SymbolEncodingType.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                                            .FirstOrDefault(v => v.Name == "EncodingTable");
            (int, string)[] EncodingTable = ((int, string)[])EncodingTableFieldInfo.GetValue(Activator.CreateInstance(SymbolEncodingType, true));
             

            foreach ((var code, var name) in EncodingTable)
            {
                var ch = (char)code; // Note code is already base 10 no need to use  OctalHelpers.FromOctalInt or System.Convert.ToInt32($"{code}", 8);
                point = AddLetterWithFont(page, point, $"{ch}", F1, nameof(F1));
            }

            // Save one page PDF to file system for manual review.
            var pdfBytes = builder.Build();
            WritePdfFile(nameof(SymbolFontAddText), pdfBytes);
        }
    

        [Fact]
        public void StandardFontsAddText()
        {
            PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont F1 = pdfBuilder.AddStandard14Font(Standard14Font.TimesRoman);
            PdfDocumentBuilder.AddedFont F2 = pdfBuilder.AddStandard14Font(Standard14Font.TimesBold);
            PdfDocumentBuilder.AddedFont F3 = pdfBuilder.AddStandard14Font(Standard14Font.TimesItalic);
            PdfDocumentBuilder.AddedFont F4 = pdfBuilder.AddStandard14Font(Standard14Font.TimesBoldItalic);
            PdfDocumentBuilder.AddedFont F5 = pdfBuilder.AddStandard14Font(Standard14Font.Helvetica);
            PdfDocumentBuilder.AddedFont F6 = pdfBuilder.AddStandard14Font(Standard14Font.HelveticaBold);
            PdfDocumentBuilder.AddedFont F7 = pdfBuilder.AddStandard14Font(Standard14Font.HelveticaOblique);
            PdfDocumentBuilder.AddedFont F8 = pdfBuilder.AddStandard14Font(Standard14Font.HelveticaBoldOblique);
            PdfDocumentBuilder.AddedFont F9 = pdfBuilder.AddStandard14Font(Standard14Font.Courier);
            PdfDocumentBuilder.AddedFont F10 = pdfBuilder.AddStandard14Font(Standard14Font.CourierBold);
            PdfDocumentBuilder.AddedFont F11 = pdfBuilder.AddStandard14Font(Standard14Font.CourierOblique);
            PdfDocumentBuilder.AddedFont F12 = pdfBuilder.AddStandard14Font(Standard14Font.CourierBoldOblique);
           

            var standardFontWithStandardEncoding = new PdfDocumentBuilder.AddedFont[]
            {
                 F1,
                 F2,
                 F3,
                 F4,
                 F5,
                 F6,
                 F7,
                 F8,
                 F9,
                 F10,
                 F11,
                 F12
            };
             
            //AddLetterWithFont(page, point, "v", F1, nameof(F1));
            //AddLetterWithFont(page, point, "v", F2, nameof(F2));
            //AddLetterWithFont(page, point, "v", F3, nameof(F3));
            //AddLetterWithFont(page, point, "v", F4, nameof(F4));
            //AddLetterWithFont(page, point, "v", F5, nameof(F5));
            //AddLetterWithFont(page, point, "v", F6, nameof(F6));
            //AddLetterWithFont(page, point, "v", F7, nameof(F7));
            //AddLetterWithFont(page, point, "v", F8, nameof(F8));
            //AddLetterWithFont(page, point, "v", F9, nameof(F9));
            //AddLetterWithFont(page, point, "v", F10, nameof(F10));
            //AddLetterWithFont(page, point, "v", F11, nameof(F11));
            //AddLetterWithFont(page, point, "v", F12, nameof(F12));


            // Get all characters/codes in font using existing (but private) class using reflection
            var Standard14Type = typeof(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14);
            var Standard14CacheFieldInfos = Standard14Type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            var Standard14Cache = (Dictionary<string, AdobeFontMetrics>)Standard14CacheFieldInfos.FirstOrDefault(v => v.Name == "Standard14Cache").GetValue(null);


            // Alternatively all 12 fonts should conform to 'StanardEncoding'
            var SymbolEncodingType = typeof(UglyToad.PdfPig.Fonts.Encodings.StandardEncoding);
            var EncodingTableFieldInfo = SymbolEncodingType.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                                            .FirstOrDefault(v => v.Name == "EncodingTable");
            (int, string)[] EncodingTable = ((int, string)[])EncodingTableFieldInfo.GetValue(Activator.CreateInstance(SymbolEncodingType, true));


            int fontNumber = 0;
            foreach (var font in standardFontWithStandardEncoding)
            {
                PdfPageBuilder page = pdfBuilder.AddPage(PageSize.A4);

                double topPageY = page.PageSize.Top - 50;
                double inch = (page.PageSize.Width / 8.5);
                double cm = inch / 2.54;
                double leftX = 1 * cm;

                fontNumber++;
                var storedFont = pdfBuilder.Fonts[font.Id];
                var fontProgram = storedFont.FontProgram;
                var fontName = fontProgram.Name;

                var pointHeading = new PdfPoint(leftX, topPageY);
                var letters = page.AddText("Font: " + fontName, 21, pointHeading, font);
                var newY = topPageY - letters.Select(v => v.GlyphRectangle.Height).Max() * 2;
                var point = new PdfPoint(leftX, newY);

                var metrics = Standard14Cache[fontName];

                var codesFromMetrics = new HashSet<int>();
                foreach (var metric in metrics.CharacterMetrics)
                {
                    var code = metric.Value.CharacterCode;
                    if (code == -1) continue;
                    codesFromMetrics.Add(code);
                    char ch = (char)code;

                    point = AddLetterWithFont(page, point, $"{ch}", font, $"F{fontNumber}");
                }

                // Detect if all codes in Standard encoding table are in metrics for font.
                bool isMissing = false;
                foreach ((var codeNotBase8Converted, var name) in EncodingTable)
                {
                    var codeBase10 = System.Convert.ToInt32($"{codeNotBase8Converted}", 8);
                    if (codesFromMetrics.Contains(codeBase10) == false)
                    {                        
                        var ch = (char)codeBase10; 
                        isMissing = true;
                        Debug.WriteLine($"In Adobe Standard Font '{fontName}' code {codeBase10} is in Standard encoding table but not in font metrics.");
                    }
                }

                Assert.False(isMissing, $"Adobe Standard Font '{fontName}' contains code(s) in Standard encoding table but not in font metrics. See Debug output for details.");                
            }

            // Save one page per standard font to file system for manual review.
            var pdfBytes = pdfBuilder.Build();
            WritePdfFile($"{nameof(StandardFontsAddText)}", pdfBytes);
        }

        static double maxY = 0;
        internal PdfPoint AddLetterWithFont(PdfPageBuilder page, PdfPoint point, string stringToAdd, PdfDocumentBuilder.AddedFont font, string fontName)
        {
            if (stringToAdd is null) { throw new ArgumentException("Text to add must be a single letter.", nameof(stringToAdd)); }
            if (stringToAdd.Length>1) { throw new ArgumentException("Text to add must be a single letter.", nameof(stringToAdd)); }
            if (fontName.ToUpper() != fontName) { throw new ArgumentException(@"FontName must be in uppercase eg. ""F1"".", nameof(fontName)); }
            var letter = page.AddText(stringToAdd, 12, point, font);
            Assert.NotNull(letter);                     // We should get back something.
            Assert.Equal(1, letter.Count);              // There should be only one letter returned after the add operation.
            Assert.Equal(stringToAdd, letter[0].Value);           // Check we got back the name letter (eg. "v")
            //Assert.Equal(fontName, letter[0].FontName); // eg. "F1" for first font added, "F2" for second etc.

            double inch = (page.PageSize.Width / 8.5);
            double cm = inch / 2.54;
         

            var letterWidth = letter[0].GlyphRectangle.Width*2;
            var letterHeight = letter[0].GlyphRectangle.Height * 2;
             
            var newX = point.X + letterWidth;
            var newY = point.Y;
            if (letterHeight > maxY) maxY = letterHeight;
            if (newX > page.PageSize.Width - 2 * letterWidth)
            {
                newX = 1 * cm;
                newY -= maxY *2;
                maxY=0;
            }
            return new PdfPoint(newX, newY);
        }

        private static void WritePdfFile(string name, byte[] bytes, string extension = "pdf")
        { 
            const string subFolder = nameof(Standard14WritingFontTests);
            var folderPath = subFolder;

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, $"{name}.{extension}");
            File.WriteAllBytes(filePath, bytes);
            Debug.WriteLine($@"{Path.Combine(Directory.GetCurrentDirectory(), filePath)}");
        }

    }
}
