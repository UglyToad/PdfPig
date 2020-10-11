using System;
using System.Drawing;
using System.IO;
using UglyToad.PdfPig.SystemDrawing;
using Xunit;

namespace UglyToad.PdfPig.Tests
{
    public class ExportToImage
    {
        private const double mult = 3;

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
        private const string TransparentImage = "Random 2 Columns Lists Images";
        private const string APISmap1 = "APISmap1";
        private const string PathExtOddeven = "path_ext_oddeven";
        private const string ComplexRotated = "complex rotated";

        private static string GetFilename(string name)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }

            return Path.Combine(documentFolder, name);
        }

        private static string GetDlaFilename(string name)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Dla", "Documents"));

            if (!name.EndsWith(".pdf"))
            {
                name += ".pdf";
            }

            return Path.Combine(documentFolder, name);
        }

        [Fact]
        public void ComplexRotatedTest()
        {
            RunDla(ComplexRotated, 1);
        }

        [Fact]
        public void ByzantineGeneralsTest()
        {
            Run(ByzantineGenerals, 1);
        }

        [Fact]
        public void NonLatinAcrobatDistillerTest()
        {
            Run(NonLatinAcrobatDistiller, 1);
        }

        [Fact]
        public void SingleGoogleDrivePageTest()
        {
            Run(SingleGoogleDrivePage, 1);
        }

        [Fact]
        public void SinglePageFormattedType0ContentTest()
        {
            Run(SinglePageFormattedType0Content, 1);
        }

        [Fact]
        public void SinglePageType1ContentTest()
        {
            Run(SinglePageType1Content, 1);
        }

        [Fact]
        public void SinglePageType1ContentTest2()
        {
            Run(SinglePageType1Content, 4);
        }

        [Fact]
        public void SingleInkscapePageTest()
        {
            Run(SingleInkscapePage, 1);
        }

        [Fact]
        public void Doc68199001ATest()
        {
            Run("68-1990-01_A", 7);
        }

        [Fact]
        public void MotorInsuranceClaimTest()
        {
            Run(MotorInsuranceClaim, 1);
        }

        [Fact]
        public void PigProductionTest()
        {
            Run(PigProduction, 1);
        }

        [Fact]
        public void SinglePage90ClockwiseRotationTest()
        {
            Run(SinglePage90ClockwiseRotation, 1);
        }

        [Fact]
        public void SinglePage180ClockwiseRotationTest()
        {
            Run(SinglePage180ClockwiseRotation, 1);
        }

        [Fact]
        public void SinglePage270ClockwiseRotationTest()
        {
            Run(SinglePage270ClockwiseRotation, 1);
        }

        [Fact]
        public void TransparentImageTest()
        {
            Run(TransparentImage, 1);
        }

        [Fact]
        public void APISmap1Test()
        {
            Run(APISmap1, 1);
        }

        [Fact]
        public void JournalPone0196757Test()
        {
            Run("journal.pone.0196757", 1);
        }

        [Fact]
        public void OldGutnishInternetExplorerTest()
        {
            Run("Old Gutnish Internet Explorer", 2);
        }

        [Fact]
        public void PathExtOddevenTest()
        {
            Run(PathExtOddeven, 1);
        }

        public static void RunDla(string file, int pageNo)
        {
            if (!Directory.Exists("Images"))
            {
                Directory.CreateDirectory("Images");
            }

            var pdfFileName = GetDlaFilename(file);
            using (var doc = PdfDocument.Open(pdfFileName))
            {
                var page = doc.GetPage(pageNo);
                using (var ms = page.ToImage(mult, new SystemDrawingProcessorV2()))
                {
                    var bitmap = Image.FromStream(ms);
                    var imageName = $"{file}_{pageNo}_system-drawing.jpg";
                    var savePath = Path.Combine("Images", imageName);
                    bitmap.Save(savePath);
                }
            }
        }

        public static void Run(string file, int pageNo)
        {
            if (!Directory.Exists("Images"))
            {
                Directory.CreateDirectory("Images");
            }

            var pdfFileName = GetFilename(file);
            using (var doc = PdfDocument.Open(pdfFileName))
            {
                var page = doc.GetPage(pageNo);
                using (var ms = page.ToImage(mult, new SystemDrawingProcessorV2()))
                {
                    var bitmap = Image.FromStream(ms);
                    var imageName = $"{file}_{pageNo}_system-drawing.jpg";
                    var savePath = Path.Combine("Images", imageName);
                    bitmap.Save(savePath);
                }
            }
        }
    }
}
