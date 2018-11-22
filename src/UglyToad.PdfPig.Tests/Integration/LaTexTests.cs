namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class LaTexTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "ICML03-081.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("TacklingthePoorAssumptionsofNaiveBayesTextClassiﬁers", page.Text);

                var page2 = document.GetPage(2);

                Assert.Contains("isθc={θc1,θc2,...,θcn},", page2.Text);
            }
        }

        [Fact]
        public void LettersHaveHeight()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.NotEqual(0, page.Letters[0].GlyphRectangle.Height);
            }
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(8, document.NumberOfPages);
            }
        }

        [Fact]
        public void LettersHaveCorrectPositionsXfinium()
        {
            var positions = GetXfiniumPositionData();
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                for (var i = 0; i < page.Letters.Count; i++)
                {
                    if (i >= positions.Count)
                    {
                        break;
                    }
                    var letter = page.Letters[i];
                    var expected = positions[i];

                    expected.AssertWithinTolerance(letter, page, false);
                }
            }
        }

        [Fact]
        public void LettersHaveCorrectPositionsPdfBox()
        {
            var positions = GetPdfBoxPositionData();
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                for (var i = 0; i < page.Letters.Count; i++)
                {
                    if (i >= positions.Count)
                    {
                        break;
                    }
                    var letter = page.Letters[i];
                    var expected = positions[i];

                    expected.AssertWithinTolerance(letter, page);
                }
            }
        }

        private static IReadOnlyList<AssertablePositionData> GetPdfBoxPositionData()
        {
            const string data = @"75.731	698.917	11.218573	T	14.346	WDKAAR+CMBX12	9.712242
85.615395	698.917	7.8472624	a	14.346	WDKAAR+CMBX12	6.584814
93.46266	698.917	7.173	c	14.346	WDKAAR+CMBX12	6.584814
100.17659	698.917	8.521524	k	14.346	WDKAAR+CMBX12	9.956124
108.698105	698.917	4.4902983	l	14.346	WDKAAR+CMBX12	9.956124
113.18841	698.917	4.4902983	i	14.346	WDKAAR+CMBX12	9.97047
117.67871	698.917	8.966249	n	14.346	WDKAAR+CMBX12	6.4557
126.64496	698.917	8.076798	g	14.346	WDKAAR+CMBX12	9.425322
140.08716	698.917	6.2835484	t	14.346	WDKAAR+CMBX12	9.195786
146.3707	698.917	8.966249	h	14.346	WDKAAR+CMBX12	9.956124
155.33694	698.917	7.359498	e	14.346	WDKAAR+CMBX12	6.584814
168.06186	698.917	11.032075	P	14.346	WDKAAR+CMBX12	9.841356
178.6492	698.917	8.076798	o	14.346	WDKAAR+CMBX12	6.584814
187.15637	698.917	8.076798	o	14.346	WDKAAR+CMBX12	6.584814
195.23318	698.917	6.584814	r	14.346	WDKAAR+CMBX12	6.4557
207.19774	698.917	12.1941	A	14.346	WDKAAR+CMBX12	10.0422
219.39185	698.917	6.3696246	s	14.346	WDKAAR+CMBX12	6.584814
225.76147	698.917	6.3696246	s	14.346	WDKAAR+CMBX12	6.584814
232.1311	698.917	8.966249	u	14.346	WDKAAR+CMBX12	6.541776
241.09735	698.917	13.456548	m	14.346	WDKAAR+CMBX12	6.4557
254.5539	698.917	8.966249	p	14.346	WDKAAR+CMBX12	9.238825
263.52014	698.917	6.2835484	t	14.346	WDKAAR+CMBX12	9.195786
269.8037	698.917	4.4902983	i	14.346	WDKAAR+CMBX12	9.97047
274.294	698.917	8.076798	o	14.346	WDKAAR+CMBX12	6.584814
282.3708	698.917	8.966249	n	14.346	WDKAAR+CMBX12	6.4557
291.33704	698.917	6.3696246	s	14.346	WDKAAR+CMBX12	6.584814
303.0434	698.917	8.076798	o	14.346	WDKAAR+CMBX12	6.584814
311.12018	698.917	4.9350243	f	14.346	WDKAAR+CMBX12	10.0422
321.43494	698.917	12.62448	N	14.346	WDKAAR+CMBX12	9.841356
334.05945	698.917	7.8472624	a	14.346	WDKAAR+CMBX12	6.584814
341.90668	698.917	4.4902983	i	14.346	WDKAAR+CMBX12	9.97047
346.39697	698.917	8.521524	v	14.346	WDKAAR+CMBX12	6.4413543
354.44507	698.917	7.359498	e	14.346	WDKAAR+CMBX12	6.584814
367.18433	698.917	11.4768	B	14.346	WDKAAR+CMBX12	9.841356
378.66113	698.917	7.8472624	a	14.346	WDKAAR+CMBX12	6.584814
386.06366	698.917	8.521524	y	14.346	WDKAAR+CMBX12	9.238825
394.1261	698.917	7.359498	e	14.346	WDKAAR+CMBX12	6.584814
401.4856	698.917	6.3696246	s	14.346	WDKAAR+CMBX12	6.584814
413.235	698.917	11.218573	T	14.346	WDKAAR+CMBX12	9.712242
423.1194	698.917	7.359498	e	14.346	WDKAAR+CMBX12	6.584814
430.47888	698.917	8.521524	x	14.346	WDKAAR+CMBX12	6.3696246
439.00043	698.917	6.2835484	t	14.346	WDKAAR+CMBX12	9.195786
450.6637	698.917	11.663298	C	14.346	WDKAAR+CMBX12	10.18566
462.32703	698.917	4.4902983	l	14.346	WDKAAR+CMBX12	9.956124
466.81732	698.917	7.8472624	a	14.346	WDKAAR+CMBX12	6.584814
474.66455	698.917	6.3696246	s	14.346	WDKAAR+CMBX12	6.584814
481.03418	698.917	6.3696246	s	14.346	WDKAAR+CMBX12	6.584814
487.4038	698.917	4.4902983	i	14.346	WDKAAR+CMBX12	9.97047
491.8941	698.917	8.966249	ﬁ	14.346	WDKAAR+CMBX12	10.0422
500.86035	698.917	7.359498	e	14.346	WDKAAR+CMBX12	6.584814
508.21985	698.917	6.584814	r	14.346	WDKAAR+CMBX12	6.4557
514.8047	698.917	6.3696246	s	14.346	WDKAAR+CMBX12	6.584814
55.440002	650.772	5.9180226	J	9.963	IYBKTJ+CMBX10	6.9442115
61.358025	650.772	5.5693173	a	9.963	IYBKTJ+CMBX10	4.573017
66.92734	650.772	4.5232024	s	9.963	IYBKTJ+CMBX10	4.573017
71.45055	650.772	5.7287254	o	9.963	IYBKTJ+CMBX10	4.573017
77.17927	650.772	6.366358	n	9.963	IYBKTJ+CMBX10	4.4833503
87.36145	650.772	8.787367	D	9.963	IYBKTJ+CMBX10	6.8346186
96.14882	650.772	3.1781971	.	9.963	IYBKTJ+CMBX10	1.5542281
103.1528	650.772	10.879597	M	9.963	IYBKTJ+CMBX10	6.8346186
114.0324	650.772	3.1781971	.	9.963	IYBKTJ+CMBX10	1.5542281
121.02643	650.772	8.588107	R	9.963	IYBKTJ+CMBX10	6.9442115
129.61453	650.772	5.250501	e	9.963	IYBKTJ+CMBX10	4.573017
134.86504	650.772	6.366358	n	9.963	IYBKTJ+CMBX10	4.4833503
141.23138	650.772	6.366358	n	9.963	IYBKTJ+CMBX10	4.4833503
147.59775	650.772	3.1781971	i	9.963	IYBKTJ+CMBX10	6.924286
150.77594	650.772	5.250501	e	9.963	IYBKTJ+CMBX10	4.573017";

            var result = data.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }

        private static IReadOnlyList<AssertablePositionData> GetXfiniumPositionData()
        {
            const string data = @"75.731	83.12866	11.218572	T	14.346	WDKAAR+CMBX12	9.956124
85.6153934	83.123866	7.847262	a	11.218572	WDKAAR+CMBX12	9.956124
93.462656	83.123866	7.173	c	11.218572	WDKAAR+CMBX12	9.956124
100.176584	83.123866	8.521524	k	11.218572	WDKAAR+CMBX12	9.956124
108.698108	83.123866	4.490298	l	11.218572	WDKAAR+CMBX12	9.956124";

            var result = data.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }
    }
}