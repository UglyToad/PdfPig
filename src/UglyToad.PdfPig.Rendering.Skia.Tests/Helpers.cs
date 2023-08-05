namespace UglyToad.PdfPig.Rendering.Skia.Tests
{
    internal static class Helpers
    {
        private static readonly string basePath = Path.GetFullPath("..\\..\\..\\..\\UglyToad.PdfPig.Tests\\Integration\\Documents");

        public static string GetDocumentPath(string fileName)
        {
            if (!fileName.EndsWith(".pdf"))
            {
                fileName += ".pdf";
            }

            return Path.Combine(basePath, fileName);
        }

        public static string[] GetAllDocuments()
        {
            return Directory.GetFiles(basePath, "*.pdf");
        }
    }
}
