namespace UglyToad.PdfPig.Tests.Integration.VisualVerification
{
    using PdfPig.Core;
    using SkiaSharp;
    using System.IO;
    using UglyToad.PdfPig.Tests.Integration.VisualVerification.SkiaHelpers;

    public class GenerateLetterGlyphImages
    {
        private const bool RenderGlyphRectangle = true;

        private const string NonLatinAcrobatDistiller = "Single Page Non Latin - from acrobat distiller";
        private const string SingleGoogleDrivePage = "Single Page Simple - from google drive";
        private const string SinglePageFormattedType0Content = "Type0 Font";
        private const string SinglePageType1Content = "ICML03-081";
        private const string SingleInkscapePage = "Single Page Simple - from inkscape";
        private const string MotorInsuranceClaim = "Motor Insurance claim form";
        private const string PigProduction = "Pig Production Handbook";
        private const string SinglePage90ClockwiseRotation = "SinglePage90ClockwiseRotation - from PdfPig";
        private const string SinglePage180ClockwiseRotation = "SinglePage180ClockwiseRotation - from PdfPig";
        private const string SinglePage270ClockwiseRotation = "SinglePage270ClockwiseRotation - from PdfPig";
        private const string CroppedAndRotatedFile = "cropped-and-rotated";
        private const string MOZILLA_3136_0 = "MOZILLA-3136-0";

        private const string OutputPath = "ImagesGlyphs";

        private const float Scale = 10f;

        private static readonly SKMatrix ScaleMatrix = SKMatrix.CreateScale(Scale, Scale);

        private static readonly SKPaint redPaint = new SKPaint() { Color = SKColors.Crimson, StrokeWidth = 1 };

        public GenerateLetterGlyphImages()
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
        }

        private static string GetFilename(string name)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }

            return Path.Combine(documentFolder, name);
        }

        private static void Run(string file, int pageNo = 1)
        {
            var pdfFileName = GetFilename(file);

            using (var document = PdfDocument.Open(pdfFileName))
            {
                document.AddPageFactory<SKPicture, SkiaGlyphPageFactory>();

                var page = document.GetPage(pageNo);
                var size = new SKSizeI((int)(page.Width * Scale), (int)(page.Height * Scale));

                using (var picture = document.GetPage<SKPicture>(pageNo))
                using (var image = SKImage.FromPicture(picture, size, ScaleMatrix))
                using (var bmp = SKBitmap.FromImage(image))
                using (var canvas = new SKCanvas(bmp))
                {
                    Assert.NotNull(picture);

                    if (RenderGlyphRectangle)
                    {
                        foreach (var letter in page.Letters)
                        {
                            DrawRectangle(letter.GlyphRectangle, canvas, redPaint, size.Height, Scale);
                        }
                    }

                    var imageName = $"{file}_{pageNo}.png";
                    var savePath = Path.Combine(OutputPath, imageName);

                    using (var fs = new FileStream(savePath, FileMode.Create))
                    using (var d = bmp.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        d.SaveTo(fs);
                    }
                }
            }
        }

        private static void DrawRectangle(PdfRectangle rectangle, SKCanvas graphics, SKPaint pen, int imageHeight, double scale)
        {
            int GetY(PdfPoint p)
            {
                return imageHeight - (int)(p.Y * scale);
            }

            SKPoint GetPoint(PdfPoint p)
            {
                return new SKPoint((int)(p.X * scale), GetY(p));
            }

            graphics.DrawLine(GetPoint(rectangle.BottomLeft), GetPoint(rectangle.BottomRight), pen);
            graphics.DrawLine(GetPoint(rectangle.BottomRight), GetPoint(rectangle.TopRight), pen);
            graphics.DrawLine(GetPoint(rectangle.TopRight), GetPoint(rectangle.TopLeft), pen);
            graphics.DrawLine(GetPoint(rectangle.TopLeft), GetPoint(rectangle.BottomLeft), pen);
        }

        // veraPDF_Issue1010_x -> https://github.com/veraPDF/veraPDF-library/issues/1010
        [Fact(Skip = "Text is in annotations")]
        public void veraPDF_Issue1010_1()
        {
            Run("FontMatrix-1");
        }

        [Fact(Skip = "Skipping for the moment")]
        public void veraPDF_Issue1010_2()
        {
            Run("FontMatrix-otf");
        }

        [Fact(Skip = "Skipping for the moment")]
        public void veraPDF_Issue1010_3()
        {
            Run("FontMatrix-raw");
        }

        [Fact]
        public void JudgementDocument()
        {
            Run("Judgement Document");
        }

        [Fact]
        public void veraPDF_Issue1010_4()
        {
            Run("felltypes-test");
        }

        [Fact(Skip = "Skipping for the moment")]
        public void veraPDF_Issue1010_5()
        {
            Run("FontMatrix-otf-bad-hmtx");
        }

        [Fact(Skip = "Text is in annotations")]
        public void veraPDF_Issue1010_6()
        {
            Run("FontMatrix-concat");
        }

        [Fact]
        public void EmbeddedCidFont_1()
        {
            Run("GHOSTSCRIPT-696547-0.zip-7");
        }

        [Fact]
        public void EmbeddedCidFont_2()
        {
            Run("GHOSTSCRIPT-696547-0.zip-9");
        }

        [Fact]
        public void EmbeddedCidFont_Music_1()
        {
            Run("GHOSTSCRIPT-696171-0");
        }

        [Fact]
        public void pdf995_1()
        {
            Run("GHOSTSCRIPT-699035-0");
        }

        [Fact]
        public void pdf995_3()
        {
            Run("GHOSTSCRIPT-699035-0", 3);
        }

        [Fact]
        public void EmbeddedType1Cid_1()
        {
            Run("GHOSTSCRIPT-697507-0");
        }

        [Fact(Skip = "Glyphs cannot be rendered")]
        public void EmbeddedType1Cid_MatrixIssue()
        {
            // It seems it's correct that the glyphs cannot be rendered
            // Leaving it there just in case
            Run("GHOSTSCRIPT-698168-0");
        }

        [Fact]
        public void EmbeddedType1Cid_2()
        {
            Run("GHOSTSCRIPT-698721-0.zip-6");
        }

        [Fact]
        public void EmbeddedType1Cid_3()
        {
            Run("GHOSTSCRIPT-699554-0.zip-4");
        }

        [Fact]
        public void EmbeddedType1Cid_4()
        {
            Run("GHOSTSCRIPT-700931-0.7z-5");
        }

        [Fact]
        public void EmbeddedType1_PatternColor_1()
        {
            Run("GHOSTSCRIPT-698721-1");
        }

        [Fact]
        public void RepresentativeWindEnergyDeals()
        {
            // Colors look wrong but they are correct as we do not support ICC profile color space.
            // The colors are the same as when the document is opened in pdf.js (Firefox).
            Run("GHOSTSCRIPT-702013-1");
        }

        [Fact]
        public void Psion()
        {
            Run("GHOSTSCRIPT-700236-1");
        }

        [Fact]
        public void RabobankWestland()
        {
            Run("GHOSTSCRIPT-700125-1");
        }

        [Fact]
        public void MammaMia()
        {
            Run("GHOSTSCRIPT-699375-5");
        }

        [Fact]
        public void InstallingMuAndPygameZero()
        {
            // Still an issue with some of the glyphs
            Run("GHOSTSCRIPT-700139-0");
        }

        [Fact]
        public void FolhaDeLondrina()
        {
            Run("GHOSTSCRIPT-699488-0");
        }

        [Fact]
        public void LithgowHighSchool()
        {
            Run("GHOSTSCRIPT-700370-2");
        }

        [Fact]
        public void TIKA_1552_0_4()
        {
            Run("TIKA-1552-0", 4);
        }

        [Fact]
        public void TIKA_1552_0_3()
        {
            Run("TIKA-1552-0", 3);
        }

        [Fact]
        public void issue_671()
        {
            Run("issue_671");
        }

        [Fact]
        public void bold_italic()
        {
            Run("bold-italic");
        }

        [Fact]
        public void cat_genetics()
        {
            Run("cat-genetics");
        }

        [Fact]
        public void _68_1990_01_A()
        {
            Run("68-1990-01_A", 2);
        }

        [Fact]
        public void SinglePageWithType1Content()
        {
            Run(SinglePageType1Content);
        }

        [Fact]
        public void SinglePageSimpleFromInkscape()
        {
            // Issue with Glyph related to Bézier curves in PdfPig
            Run(SingleInkscapePage);
        }

        [Fact]
        public void SinglePageNonLatinFromAcrobatDistiller()
        {
            Run(NonLatinAcrobatDistiller);
        }

        [Fact]
        public void SinglePageSimpleFromGoogleDrive()
        {
            Run(SingleGoogleDrivePage);
        }

        [Fact]
        public void SinglePageType0Font()
        {
            Run(SinglePageFormattedType0Content);
        }

        [Fact]
        public void RotatedTextLibreOffice()
        {
            Run("Rotated Text Libre Office");
        }

        [Fact]
        public void MOZILLA_3136_0Test()
        {
            Run(MOZILLA_3136_0, 3);
        }

        [Fact]
        public void PigProductionCompactFontFormat()
        {
            Run(PigProduction);
        }

        [Fact]
        public void PopBugzilla37292()
        {
            Run("pop-bugzilla37292");
        }

        [Fact]
        public void MultiPageMortalityStatistics()
        {
            Run("Multiple Page - from Mortality Statistics");
        }

        [Fact]
        public void MotorInsuranceClaimForm()
        {
            Run(MotorInsuranceClaim);
        }

        [Fact]
        public void SinglePage90ClockwiseRotationFromPdfPig()
        {
            Run(SinglePage90ClockwiseRotation);
        }

        [Fact]
        public void SinglePage180ClockwiseRotationFromPdfPig()
        {
            Run(SinglePage180ClockwiseRotation);
        }

        [Fact]
        public void SinglePage270ClockwiseRotationFromPdfPig()
        {
            Run(SinglePage270ClockwiseRotation);
        }

        [Fact]
        public void CroppedAndRotatedTest()
        {
            Run(CroppedAndRotatedFile);
        }
    }
}
