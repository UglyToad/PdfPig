namespace UglyToad.PdfPig.Tests
{
    using System.Collections.Generic;
    using Xunit;

    public class PublicApiScannerTests
    {
        [Fact]
        public void OnlyExposedApiIsPublic()
        {
            var assembly = typeof(PdfDocument).Assembly;

            var types = assembly.GetTypes();

            var publicTypeNames = new List<string>();

            foreach (var type in types)
            {
                if (type.IsPublic)
                {
                    publicTypeNames.Add(type.FullName);
                }
            }

            var expected = new List<string>
            {
                "UglyToad.PdfPig.IndirectReference",
                "UglyToad.PdfPig.PdfDocument",
                "UglyToad.PdfPig.ParsingOptions",
                "UglyToad.PdfPig.Structure",
                "UglyToad.PdfPig.Logging.ILog",
                "UglyToad.PdfPig.Geometry.PdfPoint",
                "UglyToad.PdfPig.Geometry.PdfRectangle",
                "UglyToad.PdfPig.Fonts.Exceptions.InvalidFontFormatException",
                "UglyToad.PdfPig.Exceptions.PdfDocumentFormatException",
                "UglyToad.PdfPig.Content.Catalog",
                "UglyToad.PdfPig.Content.DocumentInformation",
                "UglyToad.PdfPig.Content.Letter",
                "UglyToad.PdfPig.Content.Page",
                "UglyToad.PdfPig.Content.PageSize",
                "UglyToad.PdfPig.CrossReference.CrossReferenceTable",
                "UglyToad.PdfPig.CrossReference.CrossReferenceType",
                "UglyToad.PdfPig.CrossReference.TrailerDictionary",
                "UglyToad.PdfPig.Tokens.ArrayToken",
                "UglyToad.PdfPig.Tokens.BooleanToken",
                "UglyToad.PdfPig.Tokens.CommentToken",
                "UglyToad.PdfPig.Tokens.DictionaryToken",
                "UglyToad.PdfPig.Tokens.HexToken",
                "UglyToad.PdfPig.Tokens.IDataToken`1",
                "UglyToad.PdfPig.Tokens.IndirectReferenceToken",
                "UglyToad.PdfPig.Tokens.IToken",
                "UglyToad.PdfPig.Tokens.NameToken",
                "UglyToad.PdfPig.Tokens.NullToken",
                "UglyToad.PdfPig.Tokens.NumericToken",
                "UglyToad.PdfPig.Tokens.ObjectToken",
                "UglyToad.PdfPig.Tokens.StreamToken",
                "UglyToad.PdfPig.Tokens.StringToken"
            };

            foreach (var publicTypeName in publicTypeNames)
            {
                Assert.True(expected.Contains(publicTypeName), $"Type should not be public: {publicTypeName}.");
            }
        }
    }
}
