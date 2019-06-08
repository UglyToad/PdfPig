namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;

    internal static class IntegrationHelpers
    {
        public static string GetDocumentPath(string name, bool isPdf = true)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            if (!name.EndsWith(".pdf") && isPdf)
            {
                name += ".pdf";
            }

            return Path.Combine(documentFolder, name);
        }

        public static string GetSpecificTestDocumentPath(string name, bool isPdf = true)
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "SpecificTestDocuments"));

            if (!name.EndsWith(".pdf") && isPdf)
            {
                name += ".pdf";
            }

            return Path.Combine(documentFolder, name);
        }
    }
}
