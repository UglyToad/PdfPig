namespace UglyToad.PdfPig.Tests.Images
{
    using PdfPig.Images;
    using System;
    using UglyToad.PdfPig.Tests.Integration;

    public class Jpeg2000HelperTests
    {
        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Images", "Files", "Jpx")));

        public static IEnumerable<object[]> GetAllJp2Files
        {
            get
            {
                return Directory.GetFiles(DocumentFolder.Value, "*.jp2").Select(x => new object[] { Path.GetFileName(x) });
            }
        }

        [Theory]
        [MemberData(nameof(GetAllJp2Files))]
        public void GetJp2BitsPerComponent_ReturnsCorrectBitsPerComponent_WhenValidInput(string path)
        {
            byte[] image = File.ReadAllBytes(Path.Combine(DocumentFolder.Value, path));
            Assert.Equal(8, Jpeg2000Helper.GetBitsPerComponent(image));
        }

        [Fact]
        public void GetJp2BitsPerComponent_ThrowsException_WhenInputIsTooShort()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetBitsPerComponent(new byte[11]));
        }
        
        [Fact]
        public void GetJp2BitsPerComponent_ThrowsException_WhenSignatureBoxIsInvalid()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetBitsPerComponent(new byte[12]));
        }

        [Fact]
        public void GetJp2BitsPerComponentJ2K()
        {
            string path = IntegrationHelpers.GetSpecificTestDocumentPath("GHOSTSCRIPT-688999-2.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page1 = document.GetPage(1);
                var jpxImage = page1.GetImages().Single();

                var bpc = Jpeg2000Helper.GetBitsPerComponent(jpxImage.RawBytes);

                Assert.Equal(8, bpc);
            }
        }
    }
}
