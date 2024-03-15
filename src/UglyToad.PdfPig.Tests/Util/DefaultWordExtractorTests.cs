namespace UglyToad.PdfPig.Tests.Util
{
    using Integration;

    public class DefaultWordExtractorTests
    {
        [Fact]
        public void ReadWordsFromDataPdfPage3()
        {
            var file = IntegrationHelpers.GetDocumentPath("data.pdf");

            using var pdf = PdfDocument.Open(file);

            var page = pdf.GetPage(3);

            var words = page.GetWords();

            var text = string.Join(" ", words.Select(x => x.Text));

            Assert.Equal(
                "len supp dose 4.2 VC 0.5 11.5 VC 0.5 7.3 VC 0.5 5.8 VC 0.5 6.4 VC 0.5 10.0 VC 0.5 11.2 VC 0.5 11.2 VC 0.5 5.2 VC 0.5 7.0 VC 0.5" +
                " 16.5 VC 1.0 16.5 VC 1.0 15.2 VC 1.0 17.3 VC 1.0 22.5 VC 1.0 3",
                text);
        }

        [Fact]
        public void ReadWordsFromOldGutnishPage1()
        {
            var file = IntegrationHelpers.GetDocumentPath("Old Gutnish Internet Explorer.pdf");

            using var pdf = PdfDocument.Open(file);

            var page = pdf.GetPage(1);

            var words = page.GetWords();

            var text = string.Join(" ", words.Select(x => x.Text));

            Assert.StartsWith(
                "Old Gutnish - Wikipedia Page 1 of 3 Old Gutnish Old Gutnish was the dialect of Old Norse that was spoken on the Baltic island of Gotland." +
                " It shows sufficient differences from the Old West Norse and Old East Norse dialects that it is considered to be a separate branch." +
                " Gutnish is still spoken in some parts of Gotland and on the adjoining island of Fårö.",
                text);
        }

        [Fact]
        public void ReadWordsFromTikka1552Page8()
        {
            var file = IntegrationHelpers.GetDocumentPath("TIKA-1552-0.pdf");

            using var pdf = PdfDocument.Open(file);

            var page = pdf.GetPage(8);

            var words = page.GetWords();

            var text = string.Join(" ", words.Select(x => x.Text));

            Assert.StartsWith(
                "2 THE BUDGET MESSAGE OF THE PRESIDENT Administration’s SelectUSA initiative to help draw businesses and investment from around the world to our shores." +
                " If we want to make the best products, we also have to invest in the best ideas. That is why the Budget maintains a world-class commitment to science and research," +
                " targeting resources to those areas most likely to contribute directly to the creation of transformational technologies that can create the businesses and jobs of the future.",
                text);
        }
    }
}
