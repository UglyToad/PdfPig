namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;
    using DocumentLayoutAnalysis.PageSegmenter;
    using DocumentLayoutAnalysis.WordExtractor;
    using PdfPig.Core;
    using PdfPig.Tokens;
    using SkiaSharp;
    using UglyToad.PdfPig.AcroForms;
    using UglyToad.PdfPig.AcroForms.Fields;

    public class GithubIssuesTests
    {
        [Fact]
        public void Issues1248()
        {
            var path = IntegrationHelpers.GetDocumentPath("jtehm-melillo-2679746.pdf");
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);
                foreach (var letter in page.Letters)
                {
                    var font = letter.GetFont();

                    if (font?.Name?.Data.Contains("TimesLT") == true)
                    {
                        Assert.True(font.TryGetPath(100, out _));
                    }
                }
            }
        }

        [Fact]
        public void Issues1238()
        {
            var path = IntegrationHelpers.GetDocumentPath("6.Secrets.to.Startup.Success.PDFDrive.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(159);
                Assert.NotNull(page);
                Assert.StartsWith("uct. At the longer-cycle, broader end of the spectrum are identity-level", page.Text);
                Assert.Equal(0, page.Rotation.Value);
            }
        }

        [Fact]
        public void Issue1217()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("stackoverflow_error.pdf");

            var options = new ParsingOptions()
            {
                UseLenientParsing = true,
                MaxStackDepth = 100
            };
            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, options));
            Assert.Equal($"Exceeded maximum nesting depth of {options.MaxStackDepth}.", ex.Message);
        }

        [Fact]
        public void Issue1223()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("23056.PMC2132516.pdf");
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                Assert.NotNull(document);
                var firstPage = document.GetPage(1);
                Assert.NotNull(firstPage);
                Assert.Contains("The Rockefeller University Press", firstPage.Text);
            }
        }

        [Fact]
        public void Issue1213()
        {
            var path = IntegrationHelpers.GetDocumentPath("GlyphDataTableReadCompositeGlyphError.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    Assert.NotNull(page);
                }
            }
        }

        [Fact]
        public void Issue1208()
        {
            string[] files = ["Input.visible.pdf", "Input.invisible.pdf"];

            foreach (var file in files)
            {
                var path = IntegrationHelpers.GetSpecificTestDocumentPath(file);

                using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
                {
                    Assert.True(document.TryGetForm(out AcroForm form));
                    Assert.Single(form.Fields);
                    Assert.Equal(AcroFieldType.Signature, form.Fields[0].FieldType);
                }
            }
        }

        [Fact]
        public void Issue1209()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-9176-2.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    Assert.NotNull(page);

                    foreach (var image in page.GetImages())
                    {
                        Assert.True(image.ImageDictionary.ContainsKey(NameToken.Height)); // Was missing
                        Assert.True(image.ImageDictionary.ContainsKey(NameToken.Width));

                        if (image.ImageDictionary.TryGet<DictionaryToken>(NameToken.DecodeParms, out var decodeParms))
                        {
                            Assert.True(decodeParms.ContainsKey(NameToken.Columns)); // Was missing
                            Assert.True(decodeParms.ContainsKey(NameToken.Rows));
                        }
                    }
                }
            }
        }

        [Fact]
        public void Revert_e11dc6b()
        {
            var path = IntegrationHelpers.GetDocumentPath("GHOSTSCRIPT-699488-0.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);
                var images = page.GetImages().ToArray();

                Assert.Equal(9, images.Length);

                foreach (var image in images)
                {
                    if (image.ImageDictionary.TryGet(NameToken.Filter, out var token) && token is NameToken nt)
                    {
                        if (nt.Data.Contains("DCT"))
                        {
                            continue;
                        }
                    }

                    Assert.True(image.TryGetPng(out _));
                }

                var paths = page.Paths;
                Assert.Equal(66, paths.Count);
                var letters = page.Letters;
                Assert.Equal(2685, letters.Count);
            }
        }

        [Fact]
        public void Issue1199()
        {
            var path = IntegrationHelpers.GetDocumentPath("TrueTypeTablesGlyphDataTableReadGlyphsError.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    Assert.NotNull(page);
                }
            }
        }

        [Fact]
        public void Issue1183()
        {
            var path = IntegrationHelpers.GetDocumentPath("test_a.pdf");

            byte[] expected =
            [
                82, 85, 134, 255, 87, 90, 139, 255, 81, 84, 133, 255, 87, 89, 139, 255, 89, 91, 141, 255, 81, 83, 133,
                255, 84, 86, 136, 255, 84, 86, 136, 255, 70, 59, 113, 255, 69, 62, 116, 255, 75, 73, 126, 255, 45, 48,
                100, 255, 42, 48, 99, 255, 50, 55, 107, 255, 56, 59, 111, 255, 64, 66, 118, 255, 68, 63, 118, 255, 61,
                56, 111, 255, 70, 64, 120, 255, 67, 62, 117, 255, 61, 56, 111, 255, 68, 63, 118, 255, 68, 62, 118, 255,
                59, 54, 109, 255, 61, 60, 117, 255, 69, 65, 122, 255, 67, 59, 116, 255, 71, 62, 118, 255, 66, 60, 115,
                255, 47, 49, 102, 255, 40, 51, 102, 255, 35, 51, 100, 255, 70, 58, 114, 255, 68, 56, 112, 255, 76, 65,
                121, 255, 68, 58, 114, 255, 66, 58, 114, 255, 71, 64, 119, 255, 62, 56, 111, 255, 67, 62, 117, 255, 77,
                61, 118, 255, 71, 56, 113, 255, 76, 63, 119, 255, 74, 63, 118, 255, 63, 55, 108, 255, 71, 64, 116, 255,
                73, 68, 119, 255, 52, 49, 99, 255, 38, 51, 99, 255, 49, 62, 110, 255, 39, 51, 100, 255, 46, 55, 106,
                255, 50, 55, 107, 255, 63, 62, 116, 255, 67, 60, 116, 255, 71, 60, 116, 255, 67, 58, 112, 255, 68, 61,
                114, 255, 70, 67, 119, 255, 50, 50, 101, 255, 42, 47, 96, 255, 49, 59, 106, 255, 40, 54, 100, 255, 42,
                57, 103, 255, 51, 51, 102, 255, 67, 60, 112, 255, 73, 62, 114, 255, 71, 65, 117, 255, 48, 53, 103, 255,
                45, 55, 104, 255, 49, 55, 105, 255, 63, 63, 114, 255, 68, 59, 115, 255, 71, 59, 115, 255, 73, 59, 115,
                255, 74, 61, 118, 255, 66, 58, 114, 255, 50, 51, 105, 255, 39, 51, 104, 255, 34, 52, 103, 255, 64, 60,
                116, 255, 67, 64, 119, 255, 66, 66, 120, 255, 46, 49, 102, 255, 45, 51, 102, 255, 52, 61, 111, 255, 39,
                51, 99, 255, 41, 54, 102, 255, 42, 54, 100, 255, 43, 53, 99, 255, 47, 55, 103, 255, 51, 56, 104, 255,
                56, 57, 108, 255, 67, 65, 117, 255, 67, 63, 116, 255, 52, 47, 100, 255, 44, 55, 106, 255, 44, 56, 106,
                255, 42, 54, 103, 255, 42, 54, 102, 255, 40, 52, 100, 255, 41, 52, 99, 255, 45, 57, 103, 255, 42, 53,
                99, 255, 38, 54, 95, 255, 39, 55, 97, 255, 47, 64, 105, 255, 37, 53, 95, 255, 37, 53, 95, 255, 46, 63,
                104, 255, 39, 55, 96, 255, 42, 58, 99, 255, 41, 55, 105, 255, 45, 55, 106, 255, 46, 51, 103, 255, 51,
                51, 103, 255, 63, 61, 114, 255, 70, 68, 121, 255, 60, 60, 113, 255, 46, 48, 100, 255, 49, 51, 101, 255,
                51, 52, 103, 255, 58, 58, 109, 255, 69, 66, 119, 255, 64, 60, 113, 255, 61, 55, 109, 255, 70, 62, 118,
                255, 67, 58, 114, 255, 72, 59, 115, 255, 70, 58, 115, 255, 72, 62, 118, 255, 61, 55, 110, 255, 64, 62,
                116, 255, 65, 65, 119, 255, 47, 50, 104, 255, 52, 56, 109, 255, 39, 53, 106, 255, 41, 54, 107, 255, 40,
                50, 102, 255, 45, 51, 103, 255, 64, 66, 117, 255, 62, 61, 112, 255, 67, 63, 114, 255, 53, 47, 98, 255,
                49, 54, 101, 255, 51, 56, 104, 255, 43, 48, 95, 255, 50, 55, 102, 255, 49, 54, 102, 255, 42, 47, 94,
                255, 51, 56, 103, 255, 47, 52, 100, 255, 72, 62, 114, 255, 71, 62, 114, 255, 72, 67, 119, 255, 52, 52,
                103, 255, 44, 48, 99, 255, 48, 57, 106, 255, 39, 52, 100, 255, 43, 58, 106, 255, 43, 51, 98, 255, 44,
                52, 99, 255, 48, 57, 104, 255, 46, 55, 102, 255, 41, 50, 97, 255, 45, 55, 101, 255, 49, 59, 105, 255,
                43, 53, 100, 255, 51, 57, 106, 255, 41, 49, 98, 255, 40, 52, 100, 255, 45, 60, 107, 255, 38, 53, 101,
                255, 36, 48, 96, 255, 46, 54, 102, 255, 49, 55, 104, 255, 44, 55, 104, 255, 46, 56, 105, 255, 48, 58,
                107, 255, 41, 49, 99, 255, 43, 50, 100, 255, 52, 59, 108, 255, 50, 55, 105, 255, 50, 55, 105, 255, 43,
                54, 105, 255, 42, 51, 102, 255, 45, 53, 104, 255, 45, 49, 101, 255, 63, 63, 116, 255, 66, 63, 116, 255,
                68, 63, 117, 255, 62, 55, 109, 255, 74, 60, 120, 255, 73, 59, 119, 255, 72, 58, 119, 255, 76, 62, 122,
                255, 74, 60, 120, 255, 71, 57, 118, 255, 75, 61, 121, 255, 76, 62, 123, 255
            ];

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(16);
                var images = page.GetImages().ToArray();
                
                Assert.Single(images);

                var image = images[0];
                
                Assert.True(image.TryGetPng(out var bytes));

                File.WriteAllBytes("test_a_16.png", bytes);

                using (SKBitmap actual = SKBitmap.Decode(bytes, new SKImageInfo(431, 690, SKColorType.Bgra8888)))
                {
                    var pixels = actual.GetPixelSpan();
                    Assert.Equal(1189560, pixels.Length);
                    Assert.Equal(expected, pixels.Slice(0, 4 * 200).ToArray());
                }
            }
        }

        [Fact]
        public void Issue1156()
        {
            var path = IntegrationHelpers.GetDocumentPath("felltypes-test.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);

                var letters = page.Letters;

                var words = NearestNeighbourWordExtractor.Instance.GetWords(letters).ToArray();

                var wordThe = words[0];
                Assert.Equal("THE", wordThe.Text);
                Assert.Equal(wordThe.BoundingBox.BottomLeft, new PdfPoint(x: 242.9877, y: 684.7435));
                Assert.Equal(wordThe.BoundingBox.BottomRight, new PdfPoint(x: 323.93999999999994, y: 684.7435));

                var wordBook = words[2];
                Assert.Equal("BOOK:", wordBook.Text);
                Assert.Equal(wordBook.BoundingBox.BottomLeft, new PdfPoint(x: 280.4371, y: 652.0399));
                Assert.Equal(wordBook.BoundingBox.BottomRight, new PdfPoint(x: 405.65439999999995, y: 652.0399));

                var wordPremeffa = words[35];
                Assert.Equal("preme\ue009a.", wordPremeffa.Text); // The 'ff' glyph is not properly parsed
                Assert.Equal(wordPremeffa.BoundingBox.BottomLeft, new PdfPoint(x: 331.16020000000003, y: 515.2256999999998));
                Assert.Equal(wordPremeffa.BoundingBox.BottomRight, new PdfPoint(x: 374.2954000000001, y: 515.2256999999998));
            }
        }

        [Fact]
        public void Issue1148()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("P2P-33713919.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(2);

                var letters = page.Letters;

                var words = NearestNeighbourWordExtractor.Instance.GetWords(letters).ToArray();

                var firstTableLine = words[42];

                Assert.EndsWith("C<--,:", firstTableLine.Text); // Just to make sure we are looking at the correct line. Text might change as this is not actually correct

                Assert.Equal(firstTableLine.BoundingBox.BottomLeft, new PdfPoint(x: 31.890118, y: 693.035685));
                Assert.Equal(firstTableLine.BoundingBox.BottomRight, new PdfPoint(x: 563.3851179999991, y: 693.035685));
            }
        }

        [Fact]
        public void Issue1122()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("StackOverflow_Issue_1122.pdf");
            
            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }));
            Assert.Equal("The root object in the trailer did not resolve to a readable dictionary.", ex.Message);
        }

        [Fact]
        public void Issue1096()
        {
            // Ensure no StackOverflowException
            // (already fixed by https://github.com/UglyToad/PdfPig/pull/1097)
            
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("issue_1096.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    foreach (var image in page.GetImages())
                    {
                        Assert.NotNull(image);
                    }
                }
            }
        }

        [Fact]
        public void Issue1067()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("GHOSTSCRIPT-691770-0.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var ex = Assert.Throws<PdfDocumentFormatException>(() => document.GetPage(1));
                Assert.StartsWith("Decoded stream size exceeds the estimated maximum size.", ex.Message);
            }
        }

        [Fact]
        public void Issue1054()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("MOZILLA-11518-0.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    foreach (var image in page.GetImages())
                    {
                        Assert.NotNull(image);
                    }
                }
            }
        }

        [Fact]
        public void Issue1050()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("SpookyPass.pdf");
            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }));
            Assert.Equal("The root object in the trailer did not resolve to a readable dictionary.", ex.Message);
        }

        [Fact]
        public void Issue1047()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("Hang.pdf");

            using var doc = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = true });

            var ex = Assert.Throws<PdfDocumentFormatException>(() => doc.GetPage(1));
            Assert.StartsWith("Could not find", ex.Message);
        }

        [Fact]
        public void Issue1048()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("InvalidCast.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);
                Assert.NotNull(page.Letters);

                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

                Assert.Single(blocks);
                Assert.Equal("hey, i'm a bug.", blocks[0].Text);
            }
        }

        [Fact]
        public void Issue554()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("2022.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    Assert.NotNull(page.Letters);

                    if (p < document.NumberOfPages)
                    {
                        Assert.NotEmpty(page.Letters);
                    }
                }
            }
        }

        [Fact]
        public void Issue822()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("FileData_7.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    Assert.NotNull(page.Letters);
                }
            }
        }
        
        [Fact]
        public void Issue1040()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("pdfpig-issue-1040.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true}))
            {
                var page1 = document.GetPage(1);
                Assert.NotEmpty(page1.Letters);

                var page2 = document.GetPage(2);
                Assert.NotEmpty(page2.Letters);
            }
        }
        
        [Fact]
        public void Issue1013()
        {
            // NB: We actually do not fix issue 953 here, but another bug found with the same document.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("document_with_failed_fonts.pdf");

            // Lenient parsing ON + Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page2 = document.GetPage(2);
                Assert.NotEmpty(page2.Letters);

                var words2 = NearestNeighbourWordExtractor.Instance.GetWords(page2.Letters).ToArray();
                Assert.Equal("Doplňující", words2[0].Text);

                var page3 = document.GetPage(3);
                Assert.NotEmpty(page3.Letters);

                var words3 = NearestNeighbourWordExtractor.Instance.GetWords(page3.Letters).ToArray();
                Assert.Equal("Vinohradská", words3[8].Text);
            }
        }

        [Fact]
        public void Issue1016()
        {
            // Doc has letters with Shading pattern color

            var path = IntegrationHelpers.GetSpecificTestDocumentPath("colorcomparecrash.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page = document.GetPage(1);

                var letters = page.Letters;

                var firstLetter = letters[0];
                Assert.NotNull(firstLetter.Color);

                var secondLetter = letters[1];
                Assert.NotNull(secondLetter.Color);

                Assert.True(firstLetter.Color.Equals(secondLetter.Color));
            }
        }

        [Fact]
        public void Issue953()
        {
            // NB: We actually do not fix issue 953 here, but another bug found with the same document.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("FailedToParseContentForPage32.pdf");

            // Lenient parsing ON + Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true}))
            {
                var page = document.GetPage(33);
                Assert.Equal(33, page.Number);
                Assert.Equal(792, page.Height);
                Assert.Equal(612, page.Width);
            }

            // Lenient parsing ON + Do not Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = false }))
            {
                var pageException = Assert.Throws<InvalidOperationException>(() =>  document.GetPage(33));
                Assert.Equal("Could not find the font with name /TT4 in the resource store. It has not been loaded yet.", pageException.Message);
            }

            var docException = Assert.Throws<PdfDocumentFormatException>(() =>
                PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = false, SkipMissingFonts = false }));
            Assert.Equal("Could not find dictionary associated with reference in pages kids array: 102 0.", docException.Message);
        }

        [Fact]
        public void Issue953_IntOverflow()
        {
            // There is an integer overflow in Docstrum. We might want to fix that later on.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("FailedToParseContentForPage32.pdf");

            // Lenient parsing ON + Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page = document.GetPage(13);
                // This used to fail with an overflow exception when we failed to validate the zlib encoded data
                Assert.NotNull(DocstrumBoundingBoxes.Instance.GetBlocks(page.GetWords()));
            }
        }

        [Fact]
        public void Issue987()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("zeroheightdemo.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToArray();
                foreach (var word in words)
                {
                    Assert.True(word.BoundingBox.Width > 0);
                    Assert.True(word.BoundingBox.Height > 0);
                }
            }
        }
        
        [Fact]
        public void Issue982()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("PDFBOX-659-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                for (int p = 1; p <= document.NumberOfPages; ++p)
                {
                    var page = document.GetPage(p);
                    foreach (var pdfImage in page.GetImages())
                    {
                        Assert.True(pdfImage.TryGetPng(out _));
                    }
                }
            }
        }

        [Fact]
        public void Issue973()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("JD5008.pdf");

            // Lenient parsing ON
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(2);
                Assert.NotNull(page);
                Assert.Equal(2, page.Number);
                Assert.NotEmpty(page.Letters);
            }

            // Lenient parsing OFF
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = false }))
            {
                var exception = Assert.Throws<InvalidOperationException>(() => document.GetPage(2));
                Assert.Equal("Cannot execute a pop of the graphics state stack, it would leave the stack empty.",
                    exception.Message);
            }
        }

        [Fact]
        public void Issue959()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("algo.pdf");

            // Lenient parsing ON
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int i = 1; i <= document.NumberOfPages; ++i)
                {
                    var page = document.GetPage(i);
                    Assert.NotNull(page);
                    Assert.Equal(i, page.Number);
                }
            }
        }

        [Fact]
        public void Issue945()
        {
            // Odd ligatures names
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-3136-0.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(2);
                Assert.Contains("ff", page.Letters.Select(l => l.Value));
            }

            path = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(7);
                Assert.Contains("fi", page.Letters.Select(l => l.Value));
            }

            path = IntegrationHelpers.GetDocumentPath("TIKA-2054-0.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(3);
                Assert.Contains("fi", page.Letters.Select(l => l.Value));

                page = document.GetPage(4);
                Assert.Contains("ff", page.Letters.Select(l => l.Value));

                page = document.GetPage(6);
                Assert.Contains("fl", page.Letters.Select(l => l.Value));

                page = document.GetPage(16);
                Assert.Contains("ffi", page.Letters.Select(l => l.Value));
            }
        }

        [Fact]
        public void Issue943()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-10225-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                Assert.NotNull(page);

                var letters = page.Letters;
                Assert.NotNull(letters);

                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

                Assert.Equal("Rocket and Spacecraft Propulsion", blocks[0].TextLines[0].Text);
                Assert.Equal("Principles, Practice and New Developments (Second Edition)", blocks[0].TextLines[1].Text);
            }
        }

        [Fact]
        public void Issue736()
        {
            var doc = IntegrationHelpers.GetDocumentPath("Approved_Document_B__fire_safety__volume_2_-_Buildings_other_than_dwellings__2019_edition_incorporating_2020_and_2022_amendments.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                Assert.True(document.TryGetBookmarks(out var bookmarks));
                Assert.Single(bookmarks.Roots);
                Assert.Equal(36, bookmarks.Roots[0].Children.Count);
            }
        }

        [Fact]
        public void Issue693()
        {
            var doc = IntegrationHelpers.GetDocumentPath("reference-2-numeric-error.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page1 = document.GetPage(1);
                Assert.Equal(1269, page1.Letters.Count);
            }
        }

        [Fact]
        public void Issue692()
        {
            var doc = IntegrationHelpers.GetDocumentPath("cmap-parsing-exception.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page1 = document.GetPage(1);
                Assert.Equal(796, page1.Letters.Count);
            }

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = false, SkipMissingFonts = false }))
            {
                var ex = Assert.Throws<InvalidOperationException>(() => document.GetPage(1));
                Assert.StartsWith("Read byte called on input bytes which was at end of byte set.", ex.Message);
            }
        }

        [Fact]
        public void Issue874()
        {
            var doc = IntegrationHelpers.GetDocumentPath("ErcotFacts.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = false }))
            {
                var page1 = document.GetPage(1);
                Assert.Equal(1939, page1.Letters.Count);

                var page2 = document.GetPage(2);
                Assert.Equal(2434, page2.Letters.Count);
            }
        }

        [Fact]
        public void Issue913()
        {
            var doc = IntegrationHelpers.GetSpecificTestDocumentPath("Rotation 45.pdf");

            using (var document = PdfDocument.Open(doc))
            {
                var page1 = document.GetPage(1);

                for (int l = 131; l <= 137; ++l)
                {
                    var letter = page1.Letters[l];
                    Assert.Equal(TextOrientation.Other, letter.TextOrientation);
                    Assert.Equal(45.0, letter.GlyphRectangle.Rotation, 5);
                }

                var page2 = document.GetPage(2);
                Assert.Equal(157, page2.Letters.Count);

                var page3 = document.GetPage(3);
                Assert.Equal(283, page3.Letters.Count);

                var page4 = document.GetPage(4);
                Assert.Equal(304, page4.Letters.Count);
            }
        }
    }
}
