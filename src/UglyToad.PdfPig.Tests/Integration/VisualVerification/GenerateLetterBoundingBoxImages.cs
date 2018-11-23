namespace UglyToad.PdfPig.Tests.Integration.VisualVerification
{
    using System;
    using System.Drawing;
    using System.IO;
    using Xunit;

    public class GenerateLetterBoundingBoxImages
    {
        private const string NonLatinAcrobatDistiller = "Single Page Non Latin - from acrobat distiller";
        private const string SingleGoogleDrivePage = "Single Page Simple - from google drive";
        private const string SinglePageFormattedType0Content = "Type0 Font";
        private const string SinglePageType1Content = "ICML03-081";
        private const string SingleInkscapePage = "Single Page Simple - from inkscape";
        private const string PigProduction = "Pig Production Handbook";

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
            Run(@"Rotated Text Libre Office", 841);
        }

        [Fact]
        public void PigProductionCompactFontFormat()
        {
            Run(PigProduction, 680);
        }

        private static void Run(string file, int imageHeight = 792)
        {
            var pdfFileName = GetFilename(file);

            using (var document = PdfDocument.Open(pdfFileName))
            using (var image = GetCorrespondingImage(pdfFileName))
            {
                var page = document.GetPage(1);

                var redPen = new Pen(Color.BlueViolet, 1);

                using (var bitmap = new Bitmap(image))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    foreach (var word in page.Letters)
                    {
                        graphics.DrawRectangle(redPen, new Rectangle((int)word.GlyphRectangle.Left,
                            imageHeight - (int)(word.GlyphRectangle.Bottom + word.GlyphRectangle.Height), (int)Math.Max(1, word.GlyphRectangle.Width), (int)word.GlyphRectangle.Height));
                    }

                    var imageName = $"{file}.jpg";

                    if (!Directory.Exists("Images"))
                    {
                        Directory.CreateDirectory("Images");
                    }

                    var savePath = Path.Combine("Images", imageName);

                    bitmap.Save(savePath);
                }
            }
        }

        private static Image GetCorrespondingImage(string filename)
        {
            var pdf = GetFilename(filename);

            pdf = pdf.Replace(".pdf", ".jpg");

            return Image.FromFile(pdf);
        }
    }
}
