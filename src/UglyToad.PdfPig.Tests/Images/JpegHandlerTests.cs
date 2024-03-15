namespace UglyToad.PdfPig.Tests.Images
{
    using JpegHandler = UglyToad.PdfPig.Images.JpegHandler;

    public class JpegHandlerTests
    {
        [Fact]
        public void CanGetJpegInformation()
        {
            var dataJpg = LoadJpg("218995467-ccb746b0-dc28-4616-bcb1-4ad685f81876.jpg");

            using (var ms = new MemoryStream(dataJpg))
            {
                var jpegInfo = JpegHandler.GetInformation(ms);

                Assert.Equal(8, jpegInfo.BitsPerComponent);
                Assert.Equal(2290, jpegInfo.Height);
                Assert.Equal(1648, jpegInfo.Width);                
            }
        }        

        private static byte[] LoadJpg(string name)
        {
            var baseFolder = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            var JpgFilesFolder = Path.Combine(baseFolder, "..", "..", "..", "Images", "Files", "Jpg"); ;
            var JpgFilePath = Path.Combine(JpgFilesFolder, name);
            return File.ReadAllBytes(JpgFilePath); 
        }
    }
}
