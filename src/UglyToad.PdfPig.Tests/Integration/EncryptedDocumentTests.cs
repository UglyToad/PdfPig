namespace UglyToad.PdfPig.Tests.Integration
{
    using Exceptions;

    public class EncryptedDocumentTests
    {
        private const string FileName = "encrypted-password-is-password.pdf";
        private const string Password = "password";

        [Fact]
        public void NoPasswordThrows()
        {
            Action action = () => PdfDocument.Open(GetPath());

            Assert.Throws<PdfDocumentEncryptedException>(action);
        }

        [Fact]
        public void CanOpenDocumentAndGetPage()
        {
            using (var document = PdfDocument.Open(GetPath(), new ParsingOptions
            {
                Password = Password
            }))
            {
                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page.Text);
                }
            }
        }

        [Fact]
        public void CanProvideMultiplePasswords()
        {
            using (var document = PdfDocument.Open(GetPath(), new ParsingOptions
            {
                Passwords = new List<string> { "pangolin", "harpsichord", Password }
            }))
            {
                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page.Text);
                }
            }
        }

        [Fact]
        public void CanReadDocumentWithUEAsString()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("string_encryption_key.pdf")))
            {
                Assert.NotNull(document.Information.Producer);
            }
        }
        
        [Fact]
        public void CanReadDocumentWithEmptyStringEncryptedWithAESEncryptionAndOnlyIV()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("r4_aes_empty_string.pdf")))
            {
                Assert.Empty(document.Information.Producer);
            }
        }

        [Fact]
        public void CanReadDocumentWithNoKeyLengthAndRevision4()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("r4_aesv2_no_length")))
            {
                Assert.Empty(document.Information.Producer);
            }
        }

        private static string GetPath() => IntegrationHelpers.GetSpecificTestDocumentPath(FileName);
    }
}