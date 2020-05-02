
namespace UglyToad.PdfPig.Tests.Dla
{
    using System;
    using System.IO;

    internal static class IntegrationHelpers
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
