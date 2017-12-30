// ReSharper disable AccessToDisposedClosure
namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Content;
    using Xunit;

    public class SinglePageSimpleTests
    {
        private static readonly HashSet<string> IgnoredHiddenCharacters = new HashSet<string>
        {
            "\u200B"
        };

        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Single Page Simple - from google drive.pdf");
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
        public void CanAccessPage()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                var page = document.GetPage(1);

                Assert.NotNull(page);

                Assert.Equal(1, page.Number);
            }
        }

        [Fact]
        public void AccessPageLowerThanOneThrows()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Action action = () => document.GetPage(0);

                Assert.Throws<ArgumentOutOfRangeException>(action);
            }
        }

        [Fact]
        public void PageHasCorrectDimensions()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                var page = document.GetPage(1);

                Assert.Equal(612, page.Width);
                Assert.Equal(792, page.Height);
            }
        }

        [Fact]
        public void PageHasCorrectTextIgnoringHiddenCharacters()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value).Where(x => !IgnoredHiddenCharacters.Contains(x)));

                const string expected =
                    "This is the document title  There is some lede text here  And then another line of text. ";

                Assert.Equal(expected, text);
            }
        }

        [Fact]
        public void GetsCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.Letter, page.Size);
            }
        }

        [Fact]
        public void LettersHavePdfBoxPositions()
        {
            var file = GetFilename();

            var pdfBoxData = GetPdfBoxPositionData();
            var index = 0;

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                var page = document.GetPage(1);

                foreach (var letter in page.Letters)
                {
                    // Something a bit weird with how we or PdfBox handle hidden characters and spaces.
                    if (IgnoredHiddenCharacters.Contains(letter.Value) || string.IsNullOrWhiteSpace(letter.Value))
                    {
                        continue;
                    }

                    var datum = pdfBoxData[index];

                    while (IgnoredHiddenCharacters.Contains(datum.Text))
                    {
                        index++;
                        datum = pdfBoxData[index];
                    }

                    Assert.Equal(datum.Text, letter.Value);
                    Assert.Equal(datum.X, letter.Location.X, 2);

                    var transformed = page.Height - letter.Location.Y;
                    Assert.Equal(datum.Y, transformed, 2);

                    Assert.Equal(datum.Width, letter.Width, 2);

                    Assert.Equal(datum.FontName, letter.FontName);

                    // I think we have font size wrong for now, or right, but differently correct...

                    index++;
                }
            }
        }

        [Fact]
        public void LettersHaveOtherProviderPositions()
        {
            var file = GetFilename();

            var pdfBoxData = GetOtherPositionData1();
            var index = 0;

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                var page = document.GetPage(1);

                foreach (var letter in page.Letters)
                {
                    // Something a bit weird with how we or this provider handle hidden characters and spaces.
                    if (IgnoredHiddenCharacters.Contains(letter.Value) || string.IsNullOrWhiteSpace(letter.Value))
                    {
                        continue;
                    }

                    var datum = pdfBoxData[index];

                    while (IgnoredHiddenCharacters.Contains(datum.Text) || datum.Text == " ")
                    {
                        index++;
                        datum = pdfBoxData[index];
                    }

                    Assert.Equal(datum.Text, letter.Value);
                    Assert.Equal(datum.X, letter.Location.X, 2);

                    var transformed = page.Height - letter.Location.Y;
                    Assert.Equal(datum.Y, transformed, 2);

                    // Until we get width from glyphs we're a bit out.
                    Assert.True(Math.Abs(datum.Width - letter.Width) < 0.03m);

                    index++;
                }
            }
        }
        
        private static IReadOnlyList<AssertablePositionData> GetPdfBoxPositionData()
        {
            // X    Y   Width   Letter  FontSize    Font
            const string fromPdfBox = @"72	105	9.771912	T	21	ArialMT
81.77106	105	8.897049	h	21	ArialMT
90.66733	105	3.554138	i	21	ArialMT
94.22115	105	7.998741	s	21	ArialMT
102.2192	105	0	​	21	Gautami
106.6634	105	0	​	21	Gautami
106.6634	105	3.554131	i	21	ArialMT
110.2173	105	7.998749	s	21	ArialMT
118.2153	105	0	​	21	Gautami
122.6595	105	0	​	21	Gautami
122.6595	105	4.444618	t	21	ArialMT
127.1038	105	8.897049	h	21	ArialMT
136	105	8.897049	e	21	ArialMT
144.8963	105	0	​	21	Gautami
149.3405	105	0	​	21	Gautami
149.3405	105	8.897049	d	21	ArialMT
158.2368	105	8.897049	o	21	ArialMT
167.1331	105	7.998749	c	21	ArialMT
175.1311	105	8.897049	u	21	ArialMT
184.0274	105	13.32605	m	21	ArialMT
197.3523	105	8.897049	e	21	ArialMT
206.2485	105	8.897049	n	21	ArialMT
215.1448	105	4.444611	t	21	ArialMT
219.5891	105	0	​	21	Gautami
224.0333	105	0	​	21	Gautami
224.0333	105	4.444611	t	21	ArialMT
228.4775	105	3.554138	i	21	ArialMT
232.0313	105	4.444611	t	21	ArialMT
236.4756	105	3.554123	l	21	ArialMT
240.0294	105	8.897049	e	21	ArialMT
72	143.25	6.716187	T	14	ArialMT
78.71446	143.25	6.114899	h	14	ArialMT
84.8278	143.25	6.114891	e	14	ArialMT
90.94113	143.25	3.661423	r	14	ArialMT
94.60161	143.25	6.114899	e	14	ArialMT
100.7149	143.25	0	​	14	Gautami
103.7689	143.25	0	​	14	Gautami
103.7689	143.25	2.442749	i	14	ArialMT
106.211	143.25	5.497505	s	14	ArialMT
111.7071	143.25	0	​	14	Gautami
114.7611	143.25	0	​	14	Gautami
114.7611	143.25	5.497505	s	14	ArialMT
120.2572	143.25	6.114899	o	14	ArialMT
126.3705	143.25	9.158928	m	14	ArialMT
135.5271	143.25	6.114899	e	14	ArialMT
141.6404	143.25	0	​	14	Gautami
144.6944	143.25	0	​	14	Gautami
144.6944	143.25	2.442749	l	14	ArialMT
147.1365	143.25	6.114899	e	14	ArialMT
153.2499	143.25	6.114899	d	14	ArialMT
159.3632	143.25	6.114899	e	14	ArialMT
165.4765	143.25	0	​	14	Gautami
168.5305	143.25	0	​	14	Gautami
168.5305	143.25	3.054749	t	14	ArialMT
171.5845	143.25	6.114899	e	14	ArialMT
177.6978	143.25	5.497498	x	14	ArialMT
183.1939	143.25	3.054764	t	14	ArialMT
186.2479	143.25	0	​	14	Gautami
189.3019	143.25	0	​	14	Gautami
189.3019	143.25	6.114899	h	14	ArialMT
195.4152	143.25	6.114899	e	14	ArialMT
201.5285	143.25	3.661423	r	14	ArialMT
205.189	143.25	6.114899	e	14	ArialMT
72	173.25	7.33358	A	14	ArialMT
79.3317	173.25	6.114891	n	14	ArialMT
85.44504	173.25	6.114891	d	14	ArialMT
91.55836	173.25	0	​	14	Gautami
94.61235	173.25	0	​	14	Gautami
94.61235	173.25	3.054756	t	14	ArialMT
97.66633	173.25	6.114899	h	14	ArialMT
103.7797	173.25	6.114899	e	14	ArialMT
109.893	173.25	6.114899	n	14	ArialMT
116.0063	173.25	0	​	14	Gautami
119.0603	173.25	0	​	14	Gautami
119.0603	173.25	6.114899	a	14	ArialMT
125.1736	173.25	6.114899	n	14	ArialMT
131.287	173.25	6.114899	o	14	ArialMT
137.4003	173.25	3.054749	t	14	ArialMT
140.4543	173.25	6.114899	h	14	ArialMT
146.5676	173.25	6.114899	e	14	ArialMT
152.6809	173.25	3.661423	r	14	ArialMT
156.3414	173.25	0	​	14	Gautami
159.3954	173.25	0	​	14	Gautami
159.3954	173.25	2.442749	l	14	ArialMT
161.8375	173.25	2.442734	i	14	ArialMT
164.2796	173.25	6.114899	n	14	ArialMT
170.393	173.25	6.114899	e	14	ArialMT
176.5063	173.25	0	​	14	Gautami
179.5603	173.25	0	​	14	Gautami
179.5603	173.25	6.114899	o	14	ArialMT
185.6736	173.25	3.054764	f	14	ArialMT
188.7276	173.25	0	​	14	Gautami
191.7816	173.25	0	​	14	Gautami
191.7816	173.25	3.054764	t	14	ArialMT
194.8355	173.25	6.114899	e	14	ArialMT
200.9489	173.25	5.497482	x	14	ArialMT
206.445	173.25	3.054764	t	14	ArialMT
209.499	173.25	3.054764	.	14	ArialMT";

            return fromPdfBox.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();
        }

        private static IReadOnlyList<AssertablePositionData> GetOtherPositionData1()
        {
            // These do not include the font information
            const string fromOther = @"72	105	9.758476	T	0	ArialMT
81.77106	105	8.894608	h	0	ArialMT
90.66733	105	3.551445	i	0	ArialMT
94.22115	105	7.998749	s	0	ArialMT
102.2192	105	4.431305	 	0	ArialMT
102.2192	105	0	​	0	ArialMT
106.6634	105	3.551445	i	0	ArialMT
106.6634	105	0	​	0	ArialMT
110.2173	105	7.998749	s	0	ArialMT
118.2153	105	0	​	0	ArialMT
118.2153	105	4.431305	 	0	ArialMT
122.6595	105	4.431305	t	0	ArialMT
122.6595	105	0	​	0	ArialMT
127.1038	105	8.894608	h	0	ArialMT
136	105	8.894608	e	0	ArialMT
144.8963	105	4.431305	 	0	ArialMT
144.8963	105	0	​	0	ArialMT
149.3405	105	8.894608	d	0	ArialMT
149.3405	105	0	​	0	ArialMT
158.2368	105	8.894608	o	0	ArialMT
167.1331	105	7.998749	c	0	ArialMT
175.1311	105	8.894608	u	0	ArialMT
184.0274	105	13.32591	m	0	ArialMT
197.3523	105	8.894608	e	0	ArialMT
206.2485	105	8.894608	n	0	ArialMT
215.1448	105	4.431305	t	0	ArialMT
219.5891	105	4.431305	 	0	ArialMT
219.5891	105	0	​	0	ArialMT
224.0333	105	4.431305	t	0	ArialMT
224.0333	105	0	​	0	ArialMT
228.4775	105	3.551453	i	0	ArialMT
232.0313	105	4.431305	t	0	ArialMT
236.4756	105	3.551453	l	0	ArialMT
240.0294	105	8.894608	e	0	ArialMT
248.918	105	4.431305	 	0	ArialMT
72	128.25	3.045616	 	0	ArialMT
72	143.25	6.706947	T	0	ArialMT
78.71446	143.25	6.11322	h	0	ArialMT
84.8278	143.25	6.11322	e	0	ArialMT
90.94113	143.25	3.661331	r	0	ArialMT
94.60161	143.25	6.11322	e	0	ArialMT
100.7149	143.25	3.045616	 	0	ArialMT
100.7149	143.25	0	​	0	ArialMT
103.7689	143.25	2.440887	i	0	ArialMT
103.7689	143.25	0	​	0	ArialMT
106.211	143.25	5.497498	s	0	ArialMT
111.7071	143.25	3.045616	 	0	ArialMT
111.7071	143.25	0	​	0	ArialMT
114.7611	143.25	0	​	0	ArialMT
114.7611	143.25	5.497498	s	0	ArialMT
120.2572	143.25	6.11322	o	0	ArialMT
126.3705	143.25	9.158836	m	0	ArialMT
135.5271	143.25	6.11322	e	0	ArialMT
141.6404	143.25	0	​	0	ArialMT
141.6404	143.25	3.045609	 	0	ArialMT
144.6944	143.25	2.440887	l	0	ArialMT
144.6944	143.25	0	​	0	ArialMT
147.1365	143.25	6.11322	e	0	ArialMT
153.2499	143.25	6.11322	d	0	ArialMT
159.3632	143.25	6.11322	e	0	ArialMT
165.4765	143.25	0	​	0	ArialMT
165.4765	143.25	3.045609	 	0	ArialMT
168.5305	143.25	3.045609	t	0	ArialMT
168.5305	143.25	0	​	0	ArialMT
171.5845	143.25	6.11322	e	0	ArialMT
177.6978	143.25	5.497498	x	0	ArialMT
183.1939	143.25	3.045609	t	0	ArialMT
186.2479	143.25	0	​	0	ArialMT
186.2479	143.25	3.045609	 	0	ArialMT
189.3019	143.25	6.11322	h	0	ArialMT
189.3019	143.25	0	​	0	ArialMT
195.4152	143.25	6.11322	e	0	ArialMT
201.5285	143.25	3.661331	r	0	ArialMT
205.189	143.25	6.11322	e	0	ArialMT
211.3008	143.25	3.045609	 	0	ArialMT
72	158.25	3.045616	 	0	ArialMT
72	173.25	7.32267	A	0	ArialMT
79.3317	173.25	6.11322	n	0	ArialMT
85.44504	173.25	6.11322	d	0	ArialMT
91.55836	173.25	3.045616	 	0	ArialMT
91.55836	173.25	0	​	0	ArialMT
94.61235	173.25	0	​	0	ArialMT
94.61235	173.25	3.045616	t	0	ArialMT
97.66633	173.25	6.11322	h	0	ArialMT
103.7797	173.25	6.11322	e	0	ArialMT
109.893	173.25	6.11322	n	0	ArialMT
116.0063	173.25	0	​	0	ArialMT
116.0063	173.25	3.045616	 	0	ArialMT
119.0603	173.25	6.11322	a	0	ArialMT
119.0603	173.25	0	​	0	ArialMT
125.1736	173.25	6.11322	n	0	ArialMT
131.287	173.25	6.11322	o	0	ArialMT
137.4003	173.25	3.045609	t	0	ArialMT
140.4543	173.25	6.11322	h	0	ArialMT
146.5676	173.25	6.11322	e	0	ArialMT
152.6809	173.25	3.661331	r	0	ArialMT
156.3414	173.25	3.045609	 	0	ArialMT
156.3414	173.25	0	​	0	ArialMT
159.3954	173.25	2.440887	l	0	ArialMT
159.3954	173.25	0	​	0	ArialMT
161.8375	173.25	2.440887	i	0	ArialMT
164.2796	173.25	6.11322	n	0	ArialMT
170.393	173.25	6.11322	e	0	ArialMT
176.5063	173.25	3.045609	 	0	ArialMT
176.5063	173.25	0	​	0	ArialMT
179.5603	173.25	6.11322	o	0	ArialMT
179.5603	173.25	0	​	0	ArialMT
185.6736	173.25	3.045609	f	0	ArialMT
188.7276	173.25	0	​	0	ArialMT
188.7276	173.25	3.045609	 	0	ArialMT
191.7816	173.25	3.045609	t	0	ArialMT
191.7816	173.25	0	​	0	ArialMT
194.8355	173.25	6.11322	e	0	ArialMT
200.9489	173.25	5.497498	x	0	ArialMT
206.445	173.25	3.045609	t	0	ArialMT
209.499	173.25	3.045609	.	0	ArialMT
212.543	173.25	3.045609	 	0	ArialMT";

            return fromOther.Split(new[]{"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();
        }
    }
}
