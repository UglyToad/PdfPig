namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Content;
    using Xunit;

    public class SinglePageNonLatinAcrobatDistillerTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Single Page Non Latin - from acrobat distiller.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.Letter, page.Size);
            }
        }

        [Fact]
        public void GetsCorrectPageTextIgnoringHiddenCharacters()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value));

                // For some reason the C# string reverses these characters but they are extracted correctly.
                // TODO: Need someone who can read these to check them
                Assert.Equal("Hello ﺪﻤﺤﻣ World. ", text);
            }
        }

        [Fact]
        public void LetterPositionsAreCorrectPdfBox()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var pdfBoxData = GetPdfBoxPositionData();

                var index = 0;
                foreach (var pageLetter in page.Letters)
                {
                    if (index >= pdfBoxData.Count)
                    {
                        break;
                    }

                    var myX = pageLetter.Position.X;
                    var theirX = pdfBoxData[index].X;

                    var myLetter = pageLetter.Value;
                    var theirLetter = pdfBoxData[index].Text;

                    if (myLetter == " " && theirLetter != " ")
                    {
                        continue;
                    }

                    var comparer = new DecimalComparer(3m);

                    Assert.Equal(theirLetter, myLetter);

                    Assert.Equal(theirX, myX, comparer);

                    Assert.Equal(pdfBoxData[index].Width, pageLetter.Width, comparer);

                    index++;
                }
            }
        }

        [Fact]
        public void LetterPositionsAreCorrectXfinium()
        {
            var comparer = new DecimalComparer(1);

            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var positions = GetXfiniumPositionData();

                var index = 0;
                foreach (var pageLetter in page.Letters)
                {
                    if (index >= positions.Count)
                    {
                        break;
                    }

                    var myX = pageLetter.Position.X;
                    var theirX = positions[index].X;

                    var myLetter = pageLetter.Value;
                    var theirLetter = positions[index].Text;

                    if (myLetter == " " && theirLetter != " ")
                    {
                        continue;
                    }

                    Assert.Equal(theirLetter, myLetter);
                    Assert.Equal(theirX, myX, comparer);

                    Assert.Equal(positions[index].Width, pageLetter.Width, 1);

                    index++;
                }
            }
        }

        private static IReadOnlyList<AssertablePositionData> GetPdfBoxPositionData()
        {
            const string data = @"90	90.65997	14.42556	H	19	FFJICI+TimesNewRomanPSMT
104.4395	90.65997	8.871117	e	19	FFJICI+TimesNewRomanPSMT
113.3247	90.65997	5.554443	l	19	FFJICI+TimesNewRomanPSMT
118.8931	90.65997	5.554443	l	19	FFJICI+TimesNewRomanPSMT
124.4615	90.65997	9.989998	o	19	FFJICI+TimesNewRomanPSMT
139.4505	90.65997	6.733261	ﺪ	19	FFJIAH+TimesNewRomanPSMT
146.1778	90.65997	7.872116	ﻤ	19	FFJIAH+TimesNewRomanPSMT
154.0439	90.65997	10.5894	ﺤ	19	FFJIAH+TimesNewRomanPSMT
164.6273	90.65997	7.872116	ﻣ	19	FFJIAH+TimesNewRomanPSMT
177.4964	90.65997	18.86111	W	19	FFJICI+TimesNewRomanPSMT
196.3575	90.65997	9.990005	o	19	FFJICI+TimesNewRomanPSMT
206.4275	90.65997	6.653336	r	19	FFJICI+TimesNewRomanPSMT
213.0808	90.65997	5.554443	l	19	FFJICI+TimesNewRomanPSMT
218.6352	90.65997	9.990005	d	19	FFJICI+TimesNewRomanPSMT
228.6252	90.65997	4.994995	.	19	FFJICI+TimesNewRomanPSMT";

            var result = data.Split(new[] {"\r", "\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }

        private static IReadOnlyList<AssertablePositionData> GetXfiniumPositionData()
        {
            const string data = @"90	90.66	14.439546	H	19	FFJICI+TimesNewRomanPSMT	17.802
104.4395	90.66	8.885106	e	19	FFJICI+TimesNewRomanPSMT	17.80218
113.3247	90.66	5.568426	l	19	FFJICI+TimesNewRomanPSMT	17.80218
118.8931	90.66	5.568426	l	19	FFJICI+TimesNewRomanPSMT	17.80218
124.4615	90.66	10.003986	o	19	FFJICI+TimesNewRomanPSMT	17.80218
139.4505	90.66	6.727266	ﺪ	19	FFJIAH+TimesNewRomanPSMT	17.80218
146.1778	90.66	7.866126	ﻤ	19	FFJIAH+TimesNewRomanPSMT	17.80218
154.0439	90.66	10.583406	ﺤ	19	FFJIAH+TimesNewRomanPSMT	17.80218
164.6273	90.66	7.866126	ﻣ	19	FFJIAH+TimesNewRomanPSMT	17.80218
177.4964	90.66	18.86112	W	19	FFJICI+TimesNewRomanPSMT	17.80218";

            var result = data.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }
    }
}
