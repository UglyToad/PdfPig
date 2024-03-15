namespace UglyToad.PdfPig.Tests.Writer.Fonts
{
    using PdfPig.Fonts;
    using PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Fonts.Standard14Fonts;
    using UglyToad.PdfPig.Writer;

    using System.Reflection;
    using UglyToad.PdfPig.Fonts.AdobeFontMetrics;
    using System.Diagnostics;

    public class Standard14WritingFontTests
    {
        [Fact]
        public void ZapfDingbatsFontAddText()
        {
            PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont F1 = pdfBuilder.AddStandard14Font(Standard14Font.ZapfDingbats);
            var encodingTable = GetEncodingTable(typeof(UglyToad.PdfPig.Fonts.Encodings.ZapfDingbatsEncoding));
            var unicodesCharacters = GetUnicodeCharacters(encodingTable, GlyphList.ZapfDingbats);
            {
                PdfDocumentBuilder.AddedFont F2 = pdfBuilder.AddStandard14Font(Standard14Font.TimesRoman);
                PdfPageBuilder page = pdfBuilder.AddPage(PageSize.A4);

                double topPageY = page.PageSize.Top - 50;
                double inch = (page.PageSize.Width / 8.5);
                double cm = inch / 2.54;
                double leftX = 1 * cm;

                var point = new PdfPoint(leftX, topPageY);
                DateTimeStampPage(pdfBuilder, page, point, cm);
                var letters = page.AddText("Adobe Standard Font ZapfDingbats", 21, point, F2);
                var newY = topPageY - letters.Select(v => v.GlyphRectangle.Height).Max() * 1.2;
                point = new PdfPoint(leftX, newY);
                letters = page.AddText("Font Specific encoding in Black (octal) and Unicode in Blue (hex)", 10, point, F2);
                newY = newY - letters.Select(v => v.GlyphRectangle.Height).Max() * 3;
                point = new PdfPoint(leftX, newY);
                var eachRowY = new List<double>();
                eachRowY.Add(newY); // First row

                (var maxCharacterHeight, var maxCharacterWidth) = GetCharacterDetails(page, F1, 12d, unicodesCharacters);
                var context = GetContext(F1, page, nameof(F1), F2, maxCharacterHeight, maxCharacterWidth);

                // Font specific character codes (in black)
                page.SetTextAndFillColor(0, 0, 0); //Black
                foreach ((var code, var name) in encodingTable)
                {
                    var ch = (char)code; // Note code is already base 10 no need to use OctalHelpers.FromOctalInt or System.Convert.ToInt32($"{code}", 8);
                    point = AddLetterWithContext(point, $"{ch}", context, true);

                    if (eachRowY.Last() != point.Y) { eachRowY.Add(point.Y); }
                }

                // Second set of rows for (unicode) characters : Test mapping from (C#) unicode chars to PDF encoding
                newY = newY - maxCharacterHeight * 1.2;
                point = new PdfPoint(leftX, newY);

                // Unicode character codes (in blue)
                page.SetTextAndFillColor(0, 0, 200); //Blue
                foreach (var unicodeCh in unicodesCharacters)
                {
                    point = AddLetterWithContext(point, $"{unicodeCh}", context, isHexLabel: true);
                }
            }

            // Save one page PDF to file system for manual review.
            var pdfBytes = pdfBuilder.Build();
            WritePdfFile(nameof(ZapfDingbatsFontAddText), pdfBytes);


            // Check extracted letters
            using (var document = PdfDocument.Open(pdfBytes))
            {
                var page1 = document.GetPage(1);
                var letters = page1.Letters;

                {
                    var lettersFontSpecificCodes = letters.Where(l => l.FontName == "ZapfDingbats"
                                                                && l.Color.ToRGBValues().b == 0)
                                                    .ToList();


                    Assert.Equal(188, lettersFontSpecificCodes.Count);
                    for (int i = 0; i < lettersFontSpecificCodes.Count; i++)
                    {
                        var letter = lettersFontSpecificCodes[i];

                        (var code, var name) = encodingTable[i];
                        var unicodeString = GlyphList.ZapfDingbats.NameToUnicode(name);

                        var letterCharacter = letter.Value[0];
                        var unicodeCharacter = unicodeString[0];
                        Assert.Equal(letterCharacter, unicodeCharacter);
                        //Debug.WriteLine($"{letterCharacter} , {unicodeCharacter}");
                    }
                }

                {
                    var lettersUnicode = letters.Where(l => l.FontName == "ZapfDingbats"
                                                           && l.Color.ToRGBValues().b > 0.78)
                                               .ToList();
                    Assert.Equal(188, lettersUnicode.Count);
                    for (int i = 0; i < lettersUnicode.Count; i++)
                    {
                        var letter = lettersUnicode[i];

                        var letterCharacter = letter.Value[0];
                        var unicodeCharacter = unicodesCharacters[i];
                        Assert.Equal(letterCharacter, unicodeCharacter);
                        //Debug.WriteLine($"{letterCharacter} , {unicodeCharacter}");
                    }
                }
            }
        }

        [Fact]
        public void ZapfDingbatsFontErrorResponseAddingInvalidText()
        {
            PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont F1 = pdfBuilder.AddStandard14Font(Standard14Font.ZapfDingbats);
            var EncodingTable = GetEncodingTable(typeof(UglyToad.PdfPig.Fonts.Encodings.ZapfDingbatsEncoding));

            {
                PdfPageBuilder page = pdfBuilder.AddPage(PageSize.A4);
                var cm = (page.PageSize.Width / 8.5 / 2.54);
                var point = new PdfPoint(cm, page.PageSize.Top - cm);

                {
                    // Get the codes that have no character associated in the font specific coding. 
                    var codesUnder255 = Enumerable.Range(0, 255).Select(v => (char)v).ToArray();
                    var codesFromEncodingTable = EncodingTable.Select(v => (char)v.code).ToArray();
                    var invalidCharactersUnder255 = codesUnder255.Except(codesFromEncodingTable);
                    //Debug.WriteLine($"Number of invalid under 255 characters: {invalidCharactersUnder255.Count()}");
                    Assert.Equal(67, invalidCharactersUnder255.Count());
                    foreach (var ch in invalidCharactersUnder255)
                    {
                        try
                        {
                            var letter = page.AddText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                        try
                        {
                            var letter = page.MeasureText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");

                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                    }
                }

                {
                    // UnicodeRanges.Dingbats - 0x2700 - 0x27BF
                    var codesFromUnicodeDingbatBlock = Enumerable.Range(0x2700, 0xBF).Select(v => (char)v).ToArray();
                    var unicodesCharacters = GetUnicodeCharacters(EncodingTable, GlyphList.ZapfDingbats);
                    var invalidCharactersInUnicodeDingbaBlock = codesFromUnicodeDingbatBlock.Except(unicodesCharacters);
                    //Debug.WriteLine($"Number of invalid unicode characters: {invalidCharactersInUnicodeDingbaBlock.Count()}");
                    Assert.Equal(31, invalidCharactersInUnicodeDingbaBlock.Count());
                    foreach (var ch in invalidCharactersInUnicodeDingbaBlock)
                    {
                        try
                        {
                            var letter = page.AddText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                        try
                        {
                            var letter = page.MeasureText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                    }
                }
            }
        }

        [Fact]
        public void SymbolFontAddText()
        {
            PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont F1 = pdfBuilder.AddStandard14Font(Standard14Font.Symbol);
            var EncodingTable = GetEncodingTable(typeof(UglyToad.PdfPig.Fonts.Encodings.SymbolEncoding));
            var unicodesCharacters = GetUnicodeCharacters(EncodingTable, GlyphList.AdobeGlyphList);
            {
                PdfDocumentBuilder.AddedFont F2 = pdfBuilder.AddStandard14Font(Standard14Font.TimesRoman);
                PdfPageBuilder page = pdfBuilder.AddPage(PageSize.A4);

                double topPageY = page.PageSize.Top - 50;
                double inch = (page.PageSize.Width / 8.5);
                double cm = inch / 2.54;
                double leftX = 1 * cm;

                var point = new PdfPoint(leftX, topPageY);
                DateTimeStampPage(pdfBuilder, page, point, cm);
                var letters = page.AddText("Adobe Standard Font Symbol ", 21, point, F2);
                var newY = topPageY - letters.Select(v => v.GlyphRectangle.Height).Max() * 1.2;
                point = new PdfPoint(leftX, newY);
                letters = page.AddText("Font Specific encoding in Black (octal), Unicode in Blue (hex), Red only available using Unicode", 10, point, F2);
                newY = newY - letters.Select(v => v.GlyphRectangle.Height).Max() * 3;



                (var maxCharacterHeight, var maxCharacterWidth) = GetCharacterDetails(page, F1, 12d, unicodesCharacters);
                var context = GetContext(F1, page, nameof(F1), F2, maxCharacterHeight, maxCharacterWidth);

                // First set of rows for direct PDF font specific character codes
                newY = newY - maxCharacterHeight;
                point = new PdfPoint(leftX, newY);
                var eachRowY = new List<double>(new[] { newY });
                page.SetTextAndFillColor(0, 0, 0); //Black
                bool isTextColorBlack = true;
                foreach ((var codeFontSpecific, var name) in EncodingTable)
                {
                    var code = codeFontSpecific; // Code is already converted [neither OctalHelpers.FromOctalInt or System.Convert.ToInt32($"{code}", 8); is required]
                    // For a clash library uses unicode interpretation. 
                    // Substitue if code is any of the 4 codes that clash (in Unicode and font specific encodes for Symbol)
                    if (code == 0xac) code = '\u2190';    // 0xac in unicode is logicalnot    ('¬')                       use Unicode alternative for arrowleft ('←') 0x2190
                    if (code == 0xf7) code = '\uf8f7';    // 0xf7 in unicode is divide        ('÷') (different form '/')  use Unicode alternative for parenrightex Unicode 0xF8F7
                    if (code == 0xb5) code = '\u221D';    // 0xb5 in unicode is lowercase mu  ('µ')                       use Unicode alternative for proportiona('∝')  0x221D
                    if (code == 0xd7) code = '\u22c5';    // 0xd7 in unicode is muliply       ('×') (different from '*')  use Unicode alternative for dotmath ('⋅')  0x22C5
                    if (code != codeFontSpecific && isTextColorBlack) { page.SetTextAndFillColor(200, 0, 0); isTextColorBlack = false; }
                    if (code == codeFontSpecific && isTextColorBlack == false) { page.SetTextAndFillColor(0, 0, 0); isTextColorBlack = true; }

                    char ch = (char)code;
                    point = AddLetterWithContext(point, $"{ch}", context, isTextColorBlack);
                    if (eachRowY.Last() != point.Y) { eachRowY.Add(point.Y); }
                }

                // Second set of rows for (unicode) characters : Test mapping from (C#) unicode chars to font specific encoding
                newY = newY - maxCharacterHeight * 1.2;
                point = new PdfPoint(leftX, newY);

                page.SetTextAndFillColor(0, 0, 200); //Blue
                foreach (var unicodeCh in unicodesCharacters)
                {
                    point = AddLetterWithContext(point, $"{unicodeCh}", context, isHexLabel: true);
                }
            }

            // Save two page PDF to file system for manual review.
            var pdfBytes = pdfBuilder.Build();
            WritePdfFile(nameof(SymbolFontAddText), pdfBytes);


            // Check extracted letters
            using (var document = PdfDocument.Open(pdfBytes))
            {
                var page1 = document.GetPage(1);
                var letters = page1.Letters;

                {
                    var lettersFontSpecificCodes = letters.Where(l => l.FontName == "Symbol"
                                                                && l.Color.ToRGBValues().b == 0
                                                                 && (l.Color.ToRGBValues().b == 0
                                                                        || l.Color.ToRGBValues().r == 200)
                                                                        )
                                                    .ToList();


                    Assert.Equal(189, lettersFontSpecificCodes.Count);
                    Assert.Equal(EncodingTable.Length, lettersFontSpecificCodes.Count);
                    for (int i = 0; i < lettersFontSpecificCodes.Count; i++)
                    {
                        var letter = lettersFontSpecificCodes[i];

                        (var code, var name) = EncodingTable[i];
                        var unicodeString = GlyphList.AdobeGlyphList.NameToUnicode(name);

                        var letterCharacter = letter.Value[0];
                        var unicodeCharacter = unicodeString[0];
                        //Debug.WriteLine($"{letterCharacter} , {unicodeCharacter}");
                        Assert.Equal(letterCharacter, unicodeCharacter);
                    }
                }

                {
                    var lettersUnicode = letters.Where(l => l.FontName == "Symbol"
                                                           && l.Color.ToRGBValues().b > 0.78)
                                               .ToList();
                    Assert.Equal(189, lettersUnicode.Count);
                    for (int i = 0; i < lettersUnicode.Count; i++)
                    {
                        var letter = lettersUnicode[i];

                        var letterCharacter = letter.Value[0];
                        var unicodeCharacter = unicodesCharacters[i];
                        //Debug.WriteLine($"{letterCharacter} , {unicodeCharacter}");
                        Assert.Equal(letterCharacter, unicodeCharacter);
                    }
                }
            }
        }

        [Fact]
        public void SymbolFontErrorResponseAddingInvalidText()
        {
            PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont F1 = pdfBuilder.AddStandard14Font(Standard14Font.Symbol);
            var EncodingTable = GetEncodingTable(typeof(UglyToad.PdfPig.Fonts.Encodings.SymbolEncoding));

            {
                PdfPageBuilder page = pdfBuilder.AddPage(PageSize.A4);
                var cm = (page.PageSize.Width / 8.5 / 2.54);
                var point = new PdfPoint(cm, page.PageSize.Top - cm);

                {
                    // Get the codes that have no character associated in the font specific coding. 
                    var codesUnder255 = Enumerable.Range(0, 255).Select(v => (char)v).ToArray();
                    var codesFromEncodingTable = EncodingTable.Select(v => (char)v.code).ToArray();
                    var invalidCharactersUnder255 = codesUnder255.Except(codesFromEncodingTable);
                    Debug.WriteLine($"Number of invalid under 255 characters: {invalidCharactersUnder255.Count()}");
                    foreach (var ch in invalidCharactersUnder255)
                    {
                        try
                        {
                            var letter = page.AddText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                        try
                        {
                            var letter = page.MeasureText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");

                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                    }
                }

                {
                    var unicodesCharacters = GetUnicodeCharacters(EncodingTable, GlyphList.AdobeGlyphList);

                    var randomCharacters = new char[10];
                    {
                        var listUnicodeCharacters = unicodesCharacters.Select(v => (int)v).ToList();
                        var rnd = new Random();
                        int nextIndex = 0;
                        while (nextIndex < randomCharacters.Length)
                        {
                            var value = rnd.Next(0x10ffff);

                            if (listUnicodeCharacters.Contains(value)) { continue; }
                            char ch = (char)value;
                            int i = (int)ch;
                            if (i >= 0xd800 && i <= 0xdfff) { continue; }
                            randomCharacters[nextIndex++] = ch;
                            Debug.WriteLine($"{value:X}");
                        }
                    }
                    foreach (var ch in randomCharacters)
                    {
                        int i = (int)ch;
                        if (i > 0x10ffff)
                        {
                            Debug.WriteLine("Unexpected unicode point. Too large to be unicode. Expected: <0x10ffff. Got: 0x{i:X}");
                            continue;
                        }
                        if (i >= 0xd800 && i <= 0xdfff)
                        {
                            Debug.WriteLine("Unexpected unicode point that is not a surrogate  Expected: <0xd800 && >0xdfff. Got: 0x{i:X}");
                            continue;
                        }
                        try
                        {
                            var letter = page.AddText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                        try
                        {
                            var letter = page.MeasureText($"{ch}", 12, point, F1);
                            Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Expected
                            // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                            Assert.Contains("The font does not contain a character", ex.Message);
                        }
                    }
                }
            }
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

            var standardFontsWithStandardEncoding = new PdfDocumentBuilder.AddedFont[]
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


            // Get all characters codes in font using existing metrics in (private) Standard14Cache class (using reflection).
            var Standard14Cache = GetStandard14Cache();

            // All 12 fonts should conform to 'StanardEncoding'          
            var EncodingTable = ((int code, string name)[])GetEncodingTable(typeof(UglyToad.PdfPig.Fonts.Encodings.StandardEncoding));
            var unicodesCharacters = GetUnicodeCharacters(EncodingTable, GlyphList.AdobeGlyphList);

            int fontNumber = 0;
            foreach (var font in standardFontsWithStandardEncoding)
            {
                fontNumber++;
                var storedFont = pdfBuilder.Fonts[font.Id];
                var fontProgram = storedFont.FontProgram;
                var fontName = fontProgram.Name;

                {
                    PdfPageBuilder page = pdfBuilder.AddPage(PageSize.A4);

                    double topPageY = page.PageSize.Top - 50;
                    double inch = (page.PageSize.Width / 8.5);
                    double cm = inch / 2.54;
                    double leftX = 1 * cm;

                    var point = new PdfPoint(leftX, topPageY);
                    DateTimeStampPage(pdfBuilder, page, point, cm);
                    var letters = page.AddText("Adobe Standard Font " + fontName, 21, point, F2);
                    var newY = topPageY - letters.Select(v => v.GlyphRectangle.Height).Max() * 1.2;
                    point = new PdfPoint(leftX, newY);
                    letters = page.AddText("Font Specific encoding in Black, Unicode in Blue, Red only available using Unicode", 10, point, F2);
                    newY = newY - letters.Select(v => v.GlyphRectangle.Height).Max() * 3;
                    point = new PdfPoint(leftX, newY);


                    var eachRowY = new List<double>(new[] { newY });

                    var metrics = Standard14Cache[fontName];

                    var codesFromMetrics = new HashSet<int>();
                    page.SetTextAndFillColor(0, 0, 0); //Black

                    (var maxCharacterHeight, var maxCharacterWidth) = GetCharacterDetails(page, F1, 12d, unicodesCharacters);
                    var context = GetContext(font, page, $"F{fontNumber}", F2, maxCharacterHeight, maxCharacterWidth);

                    // Detect if all codes in Standard encoding table are in metrics for font.
                    bool isMissing = false;
                    bool isTextColorBlack = true;
                    foreach ((var codeNotBase8Converted, var name) in EncodingTable)
                    {
                        var codeFontSpecific = Convert.ToInt32($"{codeNotBase8Converted}", 8);
                        var code = codeFontSpecific;
                        if (codeFontSpecific == 0xc6) { code = 0x02D8; }
                        else if (codeFontSpecific == 0xb4) { code = 0x00b7; }
                        else if (codeFontSpecific == 0xb7) { code = 0x2022; }
                        else if (codeFontSpecific == 0xb8) { code = 0x201A; }
                        else if (codeFontSpecific == 0xa4) { code = 0x2044; }
                        else if (codeFontSpecific == 0xa8) { code = 0x00a4; }
                        else if (codeFontSpecific == 0x60) { code = 0x2018; }
                        else if (codeFontSpecific == 0xaf) { code = 0xFB02; }
                        else if (codeFontSpecific == 0xaa) { code = 0x201C; }
                        else if (codeFontSpecific == 0xba) { code = 0x201D; }
                        else if (codeFontSpecific == 0xf8) { code = 0x0142; }
                        else if (codeFontSpecific == 0x27) { code = 0x2019; }
                        if (code != codeFontSpecific && isTextColorBlack) { page.SetTextAndFillColor(200, 0, 0); isTextColorBlack = false; }
                        if (code == codeFontSpecific && isTextColorBlack == false) { page.SetTextAndFillColor(0, 0, 0); isTextColorBlack = true; }

                        char ch = (char)code;
                        point = AddLetterWithContext(point, $"{ch}", context, isTextColorBlack);

                        if (eachRowY.Last() != point.Y) { eachRowY.Add(point.Y); }
                    }

                    foreach (var metric in metrics.CharacterMetrics)
                    {
                        var code = metric.Value.CharacterCode;
                        if (code == -1) continue;
                        codesFromMetrics.Add(code);
                    }

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

                    // Second set of rows for (unicode) characters : Test mapping from (C#) unicode chars to PDF encoding
                    newY = newY - maxCharacterHeight * 1.2;
                    point = new PdfPoint(leftX, newY);
                    page.SetTextAndFillColor(0, 0, 200); //Blue
                    foreach (var unicodeCh in unicodesCharacters)
                    {
                        point = AddLetterWithContext(point, $"{unicodeCh}", context, isHexLabel: true);
                    }
                }
            }

            // Save one page per standard font to file system for manual review.
            var pdfBytes = pdfBuilder.Build();
            WritePdfFile($"{nameof(StandardFontsAddText)}", pdfBytes);

            // Check extracted letters
            using (var document = PdfDocument.Open(pdfBytes))
            {
                foreach (var page in document.GetPages())
                {
                    var letters = page.Letters;
                    var expectedFontName = letters.FirstOrDefault(l => l.FontSize == 12d).FontName;


                    {
                        var lettersFontSpecificCodes = letters.Where(l => l.FontName == expectedFontName
                                                                    && l.FontSize == 12d
                                                                    && (l.Color.ToRGBValues().b == 0
                                                                        || l.Color.ToRGBValues().r == 200)
                                                                        )
                                                        .ToList();


                        Assert.Equal(149, lettersFontSpecificCodes.Count);
                        Assert.Equal(lettersFontSpecificCodes.Count, EncodingTable.Length);
                        for (int i = 0; i < lettersFontSpecificCodes.Count; i++)
                        {
                            var letter = lettersFontSpecificCodes[i];

                            (var code, var name) = EncodingTable[i];
                            var unicodeString = GlyphList.AdobeGlyphList.NameToUnicode(name);

                            var letterCharacter = letter.Value[0];
                            var unicodeCharacter = unicodeString[0];
                            if (letterCharacter != unicodeCharacter) Debug.WriteLine($"{letterCharacter} , {unicodeCharacter}");
                            Assert.Equal(unicodeCharacter, letterCharacter);
                        }
                    }

                    {
                        var lettersUnicode = letters.Where(l => l.FontName == expectedFontName
                                                                && l.FontSize == 12d
                                                                && l.Color.ToRGBValues().b > 0.78)
                                                   .ToList();
                        Assert.Equal(149, lettersUnicode.Count);
                        for (int i = 0; i < lettersUnicode.Count; i++)
                        {
                            var letter = lettersUnicode[i];

                            var letterCharacter = letter.Value[0];
                            var unicodeCharacter = unicodesCharacters[i];
                            //Debug.WriteLine($"{letterCharacter} , {unicodeCharacter}");
                            Assert.Equal(unicodeCharacter, letterCharacter);
                        }
                    }

                }
            }
        }


        [Fact]
        public void StandardFontErrorResponseAddingInvalidText()
        {
            PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
            PdfPageBuilder page = pdfBuilder.AddPage(PageSize.A4);
            var cm = (page.PageSize.Width / 8.5 / 2.54);
            var point = new PdfPoint(cm, page.PageSize.Top - cm);

            PdfDocumentBuilder.AddedFont[] standardFontsWithStandardEncoding;
            {
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

                standardFontsWithStandardEncoding = new PdfDocumentBuilder.AddedFont[]
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
            }
            var EncodingTable = GetEncodingTable(typeof(UglyToad.PdfPig.Fonts.Encodings.StandardEncoding));

            // Get the codes that have no character associated in the font specific coding. 
            char[] invalidCharactersUnder255;
            {
                var codesUnder255 = Enumerable.Range(0, 255).Select(v => (char)v).ToArray();
                var codesFromEncodingTable = EncodingTable.Select(v => (char)v.code).ToArray();
                invalidCharactersUnder255 = codesUnder255.Except(codesFromEncodingTable).ToArray();
                Debug.WriteLine($"Number of invalid under 255 characters: {invalidCharactersUnder255.Count()}");
            }

            // Get random unicodes not valid for any font with Standard encoding.
            var randomUnicodeCharacters = new char[10];
            {
                var unicodesCharacters = GetUnicodeCharacters(EncodingTable, GlyphList.AdobeGlyphList);
                {
                    var listUnicodeCharacters = unicodesCharacters.Select(v => (int)v).ToList();
                    var rnd = new Random();
                    int nextIndex = 0;
                    while (nextIndex < randomUnicodeCharacters.Length)
                    {
                        var value = rnd.Next(0x10ffff);

                        if (listUnicodeCharacters.Contains(value)) { continue; }
                        char ch = (char)value;
                        int i = (int)ch;
                        if (i >= 0xd800 && i <= 0xdfff) { continue; }
                        randomUnicodeCharacters[nextIndex++] = ch;
                        Debug.WriteLine($"{value:X}");
                    }
                }
            }

            int fontNumber = 0;
            foreach (var font in standardFontsWithStandardEncoding)
            {
                fontNumber++;
                var storedFont = pdfBuilder.Fonts[font.Id];
                var fontProgram = storedFont.FontProgram;
                var fontName = fontProgram.Name;

                foreach (var ch in invalidCharactersUnder255)
                {
                    try
                    {
                        var letter = page.AddText($"{ch}", 12, point, font);
                        Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported. Font: '{fontName}'");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Expected
                        // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                        Assert.Contains("The font does not contain a character", ex.Message);
                    }
                    try
                    {
                        var letter = page.MeasureText($"{ch}", 12, point, font);
                        Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported. Font: '{fontName}'");

                    }
                    catch (InvalidOperationException ex)
                    {
                        // Expected
                        // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                        Assert.Contains("The font does not contain a character", ex.Message);
                    }
                }


                foreach (var ch in randomUnicodeCharacters)
                {
                    int i = (int)ch;
                    if (i > 0x10ffff)
                    {
                        Debug.WriteLine("Unexpected unicode point. Too large to be unicode. Expected: <0x10ffff. Got: 0x{i:X}");
                        continue;
                    }
                    if (i >= 0xd800 && i <= 0xdfff)
                    {
                        Debug.WriteLine("Unexpected unicode point that is not a surrogate  Expected: <0xd800 && >0xdfff. Got: 0x{i:X}");
                        continue;
                    }
                    try
                    {
                        var letter = page.AddText($"{ch}", 12, point, font);
                        Assert.True(true, $"Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Expected
                        // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                        Assert.Contains("The font does not contain a character", ex.Message);
                    }
                    try
                    {
                        var letter = page.MeasureText($"{ch}", 12, point, font);
                        Assert.True(true, "Unexpected. Character: '{ch}' (0x{(int)ch:X}) should throw. Not supported.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Expected
                        // "The font does not contain a character: '?' (0xnn)." where ? is a character and nn is hex number.
                        Assert.Contains("The font does not contain a character", ex.Message);
                    }
                }

            }
        }

        internal PdfPoint AddLetterWithContext(PdfPoint point, string stringToAdd, (PdfDocumentBuilder.AddedFont font, PdfPageBuilder page, string fontName, PdfDocumentBuilder.AddedFont fontLabel, double maxCharacterHeight, double maxCharacterWidth) context, bool isOctalLabel = false, bool isHexLabel = false)
        {
            var font = context.font;
            var page = context.page;
            var fontName = context.fontName;
            var fontLabel = context.fontLabel;
            var maxCharacterHeight = context.maxCharacterHeight;
            var maxCharacterWidth = context.maxCharacterWidth;

            return AddLetter(page, point, stringToAdd, font, fontName, fontLabel, maxCharacterHeight, maxCharacterWidth, isOctalLabel, isHexLabel);
        }
        internal PdfPoint AddLetter(PdfPageBuilder page, PdfPoint point, string stringToAdd, PdfDocumentBuilder.AddedFont font, string fontName, PdfDocumentBuilder.AddedFont fontLabel, double maxCharacterHeight, double maxCharacterWidth, bool isOctalLabel = false, bool isHexLabel = false)
        {
            if (stringToAdd is null) { throw new ArgumentException("Text to add must be a single letter.", nameof(stringToAdd)); }
            if (stringToAdd.Length > 1) { throw new ArgumentException("Text to add must be a single letter.", nameof(stringToAdd)); }
            if (fontName.ToUpper() != fontName) { throw new ArgumentException(@"FontName must be in uppercase eg. ""F1"".", nameof(fontName)); }

            var letter = page.AddText(stringToAdd, 12, point, font);
            if (isOctalLabel)
            {
                var labelPointSize = 5;
                var octalString = System.Convert.ToString((int)stringToAdd[0], 8).PadLeft(3, '0');
                var label = octalString;
                var codeMidPoint = point.X + letter[0].GlyphRectangle.Width / 2;
                var ml = page.MeasureText(label, labelPointSize, point, fontLabel);
                var labelY = point.Y + ml.Max(v => v.GlyphRectangle.Height) * 0.1 + maxCharacterHeight;
                var xLabel = codeMidPoint - (ml.Sum(v => v.GlyphRectangle.Width) / 2);
                var labelPoint = new PdfPoint(xLabel, labelY);
                page.AddText(label, labelPointSize, labelPoint, fontLabel);
            }

            if (isHexLabel)
            {
                var labelPointSize = 3;
                var hexString = $"{(int)stringToAdd[0]:X}".PadLeft(4, '0');
                var label = "0x" + hexString;
                var codeMidPoint = point.X + letter[0].GlyphRectangle.Width / 2;
                var ml = page.MeasureText(label, labelPointSize, point, fontLabel);
                var labelY = point.Y - ml.Max(v => v.GlyphRectangle.Height) * 2.5;
                var xLabel = codeMidPoint - (ml.Sum(v => v.GlyphRectangle.Width) / 2);
                var labelPoint = new PdfPoint(xLabel, labelY);
                page.AddText(label, labelPointSize, labelPoint, fontLabel);
            }


            Assert.NotNull(letter);                         // We should get back something.
            Assert.Single(letter);                          // There should be only one letter returned after the add operation.
            Assert.Equal(stringToAdd, letter[0].Value);     // Check we got back the name letter (eg. "v")
                                                            //Debug.WriteLine($"{letter[0]}");

            double inch = (page.PageSize.Width / 8.5);
            double cm = inch / 2.54;

            var letterWidth = letter[0].GlyphRectangle.Width * 2;
            var letterHeight = letter[0].GlyphRectangle.Height * 2;

            var newX = point.X + maxCharacterWidth * 1.1;
            var newY = point.Y;

            if (newX > page.PageSize.Width - cm)
            {
                return newLine(cm, point.Y, maxCharacterHeight);
            }
            return new PdfPoint(newX, newY);
        }

        PdfPoint newLine(double cm, double y, double maxCharacterHeight)
        {
            var newX = 1 * cm;
            var newY = y - maxCharacterHeight * 5;
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

        private static (int code, string name)[] GetEncodingTable(Type t)
        {
            // Get existing (but private) EncodingTable from encoding class using reflection so we can obtain all codes            
            var EncodingTableFieldInfo = t.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                                            .FirstOrDefault(v => v.Name == "EncodingTable");
            (int, string)[] EncodingTable = ((int, string)[])EncodingTableFieldInfo.GetValue(Activator.CreateInstance(t, true));
            return EncodingTable;
        }


        private (PdfDocumentBuilder.AddedFont font, PdfPageBuilder page, string fontName, PdfDocumentBuilder.AddedFont fontLabel, double maxCharacterHeight, double maxCharacterWidth) GetContext(PdfDocumentBuilder.AddedFont font, PdfPageBuilder page, string fontName, PdfDocumentBuilder.AddedFont fontLabel, double maxCharacterHeight, double maxCharacterWidth)
        {
            return (font, page, fontName, fontLabel, maxCharacterHeight, maxCharacterWidth);

        }

        private static char[] GetUnicodeCharacters((int code, string name)[] EncodingTable, GlyphList glyphList)
        {
            var gylphNamesFromEncodingTable = EncodingTable.Select(v => v.name).ToArray();
            char[] unicodesCharacters = gylphNamesFromEncodingTable.Select(v => (char)glyphList.NameToUnicode(v)[0]).ToArray();
            return unicodesCharacters;
        }
        (double maxCharacterHeight, double maxCharacterWidth) GetCharacterDetails(PdfPageBuilder page, PdfDocumentBuilder.AddedFont font, double fontSize, char[] unicodesCharacters)
        {
            double maxCharacterHeight;
            double maxCharacterWidth;
            {
                var point = new PdfPoint(10, 10);
                var characterRectangles = unicodesCharacters.Select(v => page.MeasureText($"{v}", 12, point, font)[0].GlyphRectangle);
                maxCharacterHeight = characterRectangles.Max(v => v.Height);
                maxCharacterWidth = characterRectangles.Max(v => v.Height);
            }
            return (maxCharacterHeight, maxCharacterWidth);
        }


        private static Dictionary<string, AdobeFontMetrics> GetStandard14Cache()
        {
            var Standard14Type = typeof(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14);
            var Standard14CacheFieldInfos = Standard14Type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            var Standard14Cache = (Dictionary<string, AdobeFontMetrics>)Standard14CacheFieldInfos.FirstOrDefault(v => v.Name == "Standard14Cache").GetValue(null);
            return Standard14Cache;
        }

        private static void DateTimeStampPage(PdfDocumentBuilder pdfBuilder, PdfPageBuilder page, PdfPoint point, double cm)
        {
            var courierFont = pdfBuilder.AddStandard14Font(Standard14Font.Courier);

            var stampTextUTC = "  UTC: " + DateTime.UtcNow.ToString("yyyy-MMM-dd HH:mm");
            var stampTextLocal = "Local: " + DateTimeOffset.Now.ToString("yyyy-MMM-dd HH:mm zzz");

            const double fontSize = 7;

            var indentFromLeft = page.PageSize.Width - cm;
            {
                var mtUTC = page.MeasureText(stampTextUTC, fontSize, point, courierFont);
                var mtlocal = page.MeasureText(stampTextLocal, fontSize, point, courierFont);
                var widthUTC = mtUTC.Sum(v => v.GlyphRectangle.Width);
                var widthLocal = mtlocal.Sum(v => v.GlyphRectangle.Width);

                indentFromLeft -= Math.Max(widthUTC, widthLocal);
            }

            {
                point = new PdfPoint(indentFromLeft, point.Y);
                var letters = page.AddText(stampTextUTC, 7, point, courierFont);
                var maxHeight = letters.Max(v => v.GlyphRectangle.Height);
                point = new PdfPoint(indentFromLeft, point.Y - maxHeight * 1.2);
            }

            {
                var letters = page.AddText(stampTextLocal, 7, point, courierFont);
            }
        }
    }
}
