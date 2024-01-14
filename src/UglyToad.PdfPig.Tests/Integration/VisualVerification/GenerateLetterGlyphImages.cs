namespace UglyToad.PdfPig.Tests.Integration.VisualVerification
{
    using SkiaSharp;
    using System;
    using System.IO;
    using UglyToad.PdfPig.Tests.Integration.VisualVerification.SkiaHelpers;
    using Xunit;

    public class GenerateLetterGlyphImages
    {
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

        private const float Scale = 2f;

        private static readonly SKMatrix ScaleMatrix = SKMatrix.CreateScale(Scale, Scale);

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

                using (var picture = document.GetPage<SKPicture>(pageNo))
                {
                    Assert.NotNull(picture);

                    var imageName = $"{file}_{pageNo}.png";
                    var savePath = Path.Combine(OutputPath, imageName);

                    using (var fs = new FileStream(savePath, FileMode.Create))
                    using (var image = SKImage.FromPicture(picture, new SKSizeI((int)(page.Width * Scale), (int)(page.Height * Scale)), ScaleMatrix))
                    using (SKData d = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        d.SaveTo(fs);
                    }
                }
            }
        }

        [Fact]
        public void TIKA_1552_0_4()
        {
            Run("TIKA-1552-0", 4);
        }

        [Fact]
        public void TIKA_1552_0_3()
        {
            Run("TIKA-1552-0",3);
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
