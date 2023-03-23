namespace UglyToad.PdfPig.Images.Jpg.Parts
{
 
    using System.IO;
  
    internal class App
    { 
        private void ParseApp(JpgBinaryStreamReader reader, int app)
        {
            int length = reader.ReadInt16BE();
            reader.Seek(length - 2, SeekOrigin.Current);
        } 
    }
}
