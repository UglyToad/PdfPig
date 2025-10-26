namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Text;

    public class CMapLocalCachingTests
    {
        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents")));
        private static readonly Lazy<string> DlaFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Dla", "Documents")));

        public static object[][] DocumentsData = new object[][]
        {
            ["68-1990-01_A.pdf"],
            ["Type0 Font.pdf"],
            ["11194059_2017-11_de_s.pdf"],
            ["2108.11480.pdf"],
            ["reference-2-numeric-error.pdf"],
            ["MOZILLA-3136-0.pdf"],
            ["FICTIF_TABLE_INDEX.pdf"],
            ["Approved_Document_B__fire_safety__volume_2_-_Buildings_other_than_dwellings__2019_edition_incorporating_2020_and_2022_amendments.pdf"],
            ["dotnet-ai.pdf"],
            ["Old Gutnish Internet Explorer.pdf"],
            ["Random 2 Columns Lists Hyph - Justified.pdf"]
        };
        
        [Theory]
        [MemberData(nameof(DocumentsData))]
        public void CheckText(string documentName)
        {
            string fullPath = Path.Combine(DocumentFolder.Value, documentName);
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(DlaFolder.Value, documentName);
            }
            
            Assert.True(File.Exists(fullPath));

            var sb = new StringBuilder();
            
            using (var document = PdfDocument.Open(fullPath, new ParsingOptions { UseLenientParsing = true }))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);
                    sb.Append(page.Text);
                }
            }

            //File.WriteAllText(Path.ChangeExtension(fullPath, "txt"), sb.ToString());

            string expected = File.ReadAllText(Path.ChangeExtension(fullPath, "txt"));
            Assert.Equal(expected, sb.ToString());
        }
    }
}
