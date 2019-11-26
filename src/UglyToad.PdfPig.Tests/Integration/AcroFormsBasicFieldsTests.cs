namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Linq;
    using Xunit;

    public class AcroFormsBasicFieldsTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields");
        }

        [Fact]
        public void GetFormNotNull()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var form = document.GetForm();
                Assert.NotNull(form);
            }
        }

        [Fact]
        public void GetFormDisposedThrows()
        {
            var document = PdfDocument.Open(GetFilename());

            document.Dispose();

            Action action = () => document.GetForm();

            Assert.Throws<ObjectDisposedException>(action);
        }

        [Fact]
        public void GetsAllFormFields()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var form = document.GetForm();
                Assert.Equal(18, form.Fields.Count);
            }
        }

        [Fact]
        public void GetFormFieldsByPage()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var form = document.GetForm();
                var fields = form.GetFieldsForPage(1).ToList();
                Assert.Equal(18, fields.Count);
            }
        }
    }
}
