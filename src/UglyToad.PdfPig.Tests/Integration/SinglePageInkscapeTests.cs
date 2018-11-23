namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class SinglePageInkscapeTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Single Page Simple - from inkscape.pdf");
        }

        [Fact]
        public void LettersHaveCorrectPositionsPdfBox()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var letters = page.Letters;
                var positions = GetPdfBoxData();

                for (int i = 0; i < letters.Count; i++)
                {
                    var letter = letters[i];
                    var position = positions[i];
                    position.AssertWithinTolerance(letter, page, false);
                }
            }
        }

        private static IReadOnlyList<AssertablePositionData> GetPdfBoxData()
        {
            const string data = @"100.57143	687.4286	31.616001	W	32.0	KTICVV+DejaVuSans	47.776
130.74742	687.4286	13.152	r	32.0	KTICVV+DejaVuSans	36.704002
143.89941	687.4286	8.864	i	32.0	KTICVV+DejaVuSans	49.792004
152.76341	687.4286	12.544001	t	32.0	KTICVV+DejaVuSans	46.016003
165.30742	687.4286	19.68	e	32.0	KTICVV+DejaVuSans	37.632
184.98743	687.4286	10.144	 	32.0	KTICVV+DejaVuSans	0.0
195.13142	687.4286	16.640001	s	32.0	KTICVV+DejaVuSans	37.632
211.77142	687.4286	19.552	o	32.0	KTICVV+DejaVuSans	37.632
231.32343	687.4286	31.168001	m	32.0	KTICVV+DejaVuSans	36.704002
262.49142	687.4286	19.68	e	32.0	KTICVV+DejaVuSans	37.632
282.17142	687.4286	12.544001	t	32.0	KTICVV+DejaVuSans	46.016003
294.71542	687.4286	20.256	h	32.0	KTICVV+DejaVuSans	49.792004
315.06744	687.4286	8.864	i	32.0	KTICVV+DejaVuSans	49.792004
323.93146	687.4286	20.256	n	32.0	KTICVV+DejaVuSans	36.704002
344.18747	687.4286	20.288	g	32.0	KTICVV+DejaVuSans	50.336002
364.47546	687.4286	10.144	 	32.0	KTICVV+DejaVuSans	0.0
374.61948	687.4286	8.864	i	32.0	KTICVV+DejaVuSans	49.792004
383.4835	687.4286	20.256	n	32.0	KTICVV+DejaVuSans	36.704002
100.57143	647.4286	9.408	I	32.0	KTICVV+DejaVuSans	47.776
109.97942	647.4286	20.256	n	32.0	KTICVV+DejaVuSans	36.704002
130.23543	647.4286	18.528002	k	32.0	KTICVV+DejaVuSans	49.792004
148.76343	647.4286	16.640001	s	32.0	KTICVV+DejaVuSans	37.632
165.40343	647.4286	17.568	c	32.0	KTICVV+DejaVuSans	37.632
183.06743	647.4286	19.584002	a	32.0	KTICVV+DejaVuSans	37.632
202.65143	647.4286	20.288	p	32.0	KTICVV+DejaVuSans	50.336002
222.93942	647.4286	19.68	e	32.0	KTICVV+DejaVuSans	37.632
";

            var result = data.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }
    }
}
