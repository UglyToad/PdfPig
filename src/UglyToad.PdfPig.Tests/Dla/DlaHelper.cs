namespace UglyToad.PdfPig.Tests.Dla
{
    internal static class DlaHelper
    {
        public static string GetDocumentPath(string name, bool isPdf = true)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Dla", "Documents"));

            if (!name.EndsWith(".pdf") && isPdf)
            {
                name += ".pdf";
            }

            return Path.Combine(documentFolder, name);
        }
    }
}