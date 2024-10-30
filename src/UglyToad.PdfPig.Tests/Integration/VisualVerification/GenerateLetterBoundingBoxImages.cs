namespace UglyToad.PdfPig.Tests.Integration.VisualVerification
{
    using PdfPig.Core;
    using SkiaSharp;

    public class GenerateLetterBoundingBoxImages
    {
        private const string ByzantineGenerals = "byz";
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
        private const string SPARCv9ArchitectureManual = "SPARC - v9 Architecture Manual";
        private const string CroppedAndRotatedFile = "cropped-and-rotated";
        private const string MOZILLA_10372_2File = "MOZILLA-10372-2";
        private const string Type3FontZeroHeight = "type3-font-zero-height";

        private const string OutputPath = "Images";

        private static readonly SKPaint violetPen = new SKPaint() { Color = SKColors.BlueViolet, StrokeWidth = 1 };
        private static readonly SKPaint redPen = new SKPaint() { Color = SKColors.Crimson, StrokeWidth = 1 };
        private static readonly SKPaint bluePen = new SKPaint() { Color = SKColors.GreenYellow, StrokeWidth = 1 };
        
        private static string GetFilename(string name)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }

            return Path.Combine(documentFolder, name);
        }

        [Fact]
        public void SinglePageWithType1Content()
        {
            Run(SinglePageType1Content);
        }

        [Fact]
        public void SinglePageSimpleFromInkscape()
        {
            Run(SingleInkscapePage, 841);
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
            Run("Rotated Text Libre Office", 841);
        }

        [Fact]
        public void PigProductionCompactFontFormat()
        {
            Run(PigProduction, 680);
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
            Run(MotorInsuranceClaim, 841);
        }

        [Fact]
        public void ByzantineGeneralsTrueTypeStandard14()
        {
            Run(ByzantineGenerals, 702);
        }

        [Fact]
        public void SinglePage90ClockwiseRotationFromPdfPig()
        {
            Run(SinglePage90ClockwiseRotation, 595);
        }

        [Fact]
        public void SinglePage180ClockwiseRotationFromPdfPig()
        {
            Run(SinglePage180ClockwiseRotation, 842);
        }

        [Fact]
        public void SinglePage270ClockwiseRotationFromPdfPig()
        {
            Run(SinglePage270ClockwiseRotation, 595);
        }

        [Fact]
        public void SPARCv9ArchitectureManualTest()
        {
            Run(SPARCv9ArchitectureManual);
        }

        [Fact]
        public void CroppedAndRotatedTest()
        {
            Run(CroppedAndRotatedFile, 205);
        }

        [Fact]
        public void MOZILLA_10372_2Test()
        {
            Run(MOZILLA_10372_2File, 1584, 7);
        }

        [Fact]
        public void Type3FontZeroHeightTest()
        {
            Run(Type3FontZeroHeight, 1255);
        }

        private static void Run(string file, int imageHeight = 792, int pageNo = 1)
        {
            var pdfFileName = GetFilename(file);

            using (var document = PdfDocument.Open(pdfFileName))
            using (var image = GetCorrespondingImage(pdfFileName))
            {
                var page = document.GetPage(pageNo);

                double scale = imageHeight / page.Height;

                using (var bitmap = SKBitmap.FromImage(image))
                using (var graphics = new SKCanvas(bitmap))
                {
                    foreach (var word in page.GetWords())
                    {
                        DrawRectangle(word.BoundingBox, graphics, redPen, imageHeight, scale);
                    }

                    foreach (var letter in page.Letters)
                    {
                        DrawRectangle(letter.GlyphRectangle, graphics, violetPen, imageHeight, scale);
                    }

                    foreach (var annotation in page.GetAnnotations())
                    {
                        DrawRectangle(annotation.Rectangle, graphics, bluePen, imageHeight, scale);
                    }

                    graphics.Flush();

                    var imageName = $"{file}.jpg";

                    if (!Directory.Exists(OutputPath))
                    {
                        Directory.CreateDirectory(OutputPath);
                    }

                    var savePath = Path.Combine(OutputPath, imageName);

                    using (var fs = new FileStream(savePath, FileMode.Create))
                    using (SKData d = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100))
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

        private static SKImage GetCorrespondingImage(string filename)
        {
            var pdf = GetFilename(filename);

            pdf = pdf.Replace(".pdf", ".jpg");

            return SKImage.FromEncodedData(pdf);
        }
    }
}
