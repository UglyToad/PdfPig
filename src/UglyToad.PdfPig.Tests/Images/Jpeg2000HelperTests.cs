namespace UglyToad.PdfPig.Tests.Images
{
    using PdfPig.Images;
    using System;

    public class Jpeg2000HelperTests
    {
        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Images", "Files", "Jpx")));

        public static IEnumerable<object[]> GetAllDocuments
        {
            get
            {
                return Directory.GetFiles(DocumentFolder.Value, "*.jp2").Select(x => new object[] { Path.GetFileName(x) });
            }
        }

        [Fact]
        public void GetJp2BitsPerComponent_ThrowsException_WhenInputIsTooShort()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetJp2BitsPerComponent(new byte[11]));
        }
        
        [Fact]
        public void GetJp2BitsPerComponent_ThrowsException_WhenSignatureBoxIsInvalid()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetJp2BitsPerComponent(new byte[12]));
        }

        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void GetJp2BitsPerComponent_ReturnsCorrectBitsPerComponent_WhenValidInput(string path)
        {
            byte[] image = File.ReadAllBytes(Path.Combine(DocumentFolder.Value, path));
            Assert.Equal(8, Jpeg2000Helper.GetJp2BitsPerComponent(image));
        }
    }
}
