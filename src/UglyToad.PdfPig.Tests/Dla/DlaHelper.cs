namespace UglyToad.PdfPig.Tests.Dla
{
    internal static class DlaHelper
    {
        private static readonly string DlaFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Dla", "Documents"));
        private static readonly string IntegrationFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

        public static string GetDocumentPath(string name, bool isPdf = true)
        {
            if (!name.EndsWith(".pdf") && isPdf)
            {
                name += ".pdf";
            }

            string doc = Path.Combine(DlaFolder, name);
            if (File.Exists(doc))
            {
                return doc;
            }
            
            return Path.Combine(IntegrationFolder, name);
        }
    }
}