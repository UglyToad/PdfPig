namespace UglyToad.PdfPig.Tests.Parser
{
    using PdfPig.Graphics;
    using PdfPig.Graphics.Core;
    using PdfPig.Graphics.Operations.General;
    using PdfPig.Graphics.Operations.TextObjects;
    using PdfPig.Graphics.Operations.TextPositioning;
    using PdfPig.Graphics.Operations.TextShowing;
    using PdfPig.Graphics.Operations.TextState;
    using PdfPig.Parser;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    public class PageContentParserTests
    {
        private readonly PageContentParser parser = new PageContentParser(new ReflectionGraphicsStateOperationFactory());

        [Fact]
        public void CorrectlyExtractsOperations()
        {
            var input = StringBytesTestConverter.Convert(SimpleGoogleDocPageContent, false);

            var result = parser.Parse(input.Bytes);
        }

        [Fact]
        public void CorrectlyExtractsOptionsInTextContext()
        {
            const string s = @"BT
/F13 48 Tf
20 38 Td
1 Tr
2 w
(ABC) Tj
ET";
            var input = StringBytesTestConverter.Convert(s, false);

            var result = parser.Parse(input.Bytes);

            Assert.Equal(7, result.Count);

            Assert.Equal(BeginText.Value, result[0]);

            var font = Assert.IsType<SetFontAndSize>(result[1]);
            Assert.Equal(NameToken.Create("F13"), font.Font);
            Assert.Equal(48, font.Size);

            var nextLine = Assert.IsType<MoveToNextLineWithOffset>(result[2]);
            Assert.Equal(20, nextLine.Tx);
            Assert.Equal(38, nextLine.Ty);

            var renderingMode = Assert.IsType<SetTextRenderingMode>(result[3]);
            Assert.Equal(RenderingMode.Stroke, renderingMode.Mode);

            var lineWidth = Assert.IsType<SetLineWidth>(result[4]);
            Assert.Equal(2, lineWidth.Width);

            var text = Assert.IsType<ShowText>(result[5]);
            Assert.Equal("ABC", text.Text);

            Assert.Equal(EndText.Value, result[6]);
        }

        [Fact]
        public void SkipsComments()
        {
            const string s = @"BT
21 32 Td %A comment here
0 Tr
ET";

            var input = StringBytesTestConverter.Convert(s, false);

            var result = parser.Parse(input.Bytes);

            Assert.Equal(4, result.Count);

            Assert.Equal(BeginText.Value, result[0]);

            var moveLine = Assert.IsType<MoveToNextLineWithOffset>(result[1]);
            Assert.Equal(21, moveLine.Tx);
            Assert.Equal(32, moveLine.Ty);

            var renderingMode = Assert.IsType<SetTextRenderingMode>(result[2]);
            Assert.Equal(RenderingMode.Fill, renderingMode.Mode);

            Assert.Equal(EndText.Value, result[3]);
        }

        private const string SimpleGoogleDocPageContent = @"
1 0 0 -1 0 792 cm
q
0 0 612 792 re
W* n
q
.75 0 0 .75 0 0 cm
1 1 1 RG 1 1 1 rg
/G0 gs
0 0 816 1056 re
f
0 0 816 1056 re
f
0 0 816 1056 re
f
Q
Q
q
0 0 612 791.25 re
W* n
q
.75 0 0 .75 0 0 cm
1 1 1 RG 1 1 1 rg
/G0 gs
0 0 816 1055 re
f
0 96 816 960 re
f
0 0 0 RG 0 0 0 rg
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
96 0 Td <0037> Tj
13.0280762 0 Td <004B> Tj
11.8616943 0 Td <004C> Tj
4.7384338 0 Td <0056> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
136.292267 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
136.292267 0 Td <0003> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
142.217911 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
142.217911 0 Td <004C> Tj
4.7384338 0 Td <0056> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
157.620407 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
157.620407 0 Td <0003> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
163.546051 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
163.546051 0 Td <0057> Tj
5.9256439 0 Td <004B> Tj
11.8616943 0 Td <0048> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
193.19508 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
193.19508 0 Td <0003> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
199.12073 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
199.12073 0 Td <0047> Tj
11.8616943 0 Td <0052> Tj
11.8616943 0 Td <0046> Tj
10.6640625 0 Td <0058> Tj
11.8616943 0 Td <0050> Tj
17.766479 0 Td <0048> Tj
11.8616943 0 Td <0051> Tj
11.8616943 0 Td <0057> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
292.7854 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
292.7854 0 Td <0003> Tj
ET
BT
/F1 21.33 Tf
1 0 0 -1 0 140 Tm
298.71106 0 Td <0001> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
298.71106 0 Td <0057> Tj
5.9256287 0 Td <004C> Tj
4.7384338 0 Td <0057> Tj
5.9256592 0 Td <004F> Tj
4.7384033 0 Td <0048> Tj
ET
BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
331.89063 0 Td <0003> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 171 Tm
96 0 Td <0003> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
96 0 Td <0037> Tj
8.9526215 0 Td <004B> Tj
8.1511078 0 Td <0048> Tj
8.1511078 0 Td <0055> Tj
4.8806458 0 Td <0048> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
134.286591 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
134.286591 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
138.358566 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
138.358566 0 Td <004C> Tj
3.2561493 0 Td <0056> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
148.942841 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
148.942841 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
153.014816 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
153.014816 0 Td <0056> Tj
7.328125 0 Td <0052> Tj
8.1511078 0 Td <0050> Tj
12.2087708 0 Td <0048> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
188.85393 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
188.85393 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
192.9259 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
192.9259 0 Td <004F> Tj
3.2561493 0 Td <0048> Tj
8.1511078 0 Td <0047> Tj
8.1511078 0 Td <0048> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
220.63538 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
220.63538 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
224.70735 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
224.70735 0 Td <0057> Tj
4.0719757 0 Td <0048> Tj
8.1511078 0 Td <005B> Tj
7.328125 0 Td <0057> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
248.33054 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
248.33054 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 191 Tm
252.40251 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
252.40251 0 Td <004B> Tj
8.1511078 0 Td <0048> Tj
8.1510925 0 Td <0055> Tj
4.8806763 0 Td <0048> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 191 Tm
281.73438 0 Td <0003> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 211 Tm
96 0 Td <0003> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
96 0 Td <0024> Tj
9.7756042 0 Td <0051> Tj
8.1511078 0 Td <0047> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
122.07782 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
122.07782 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
126.149796 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
126.149796 0 Td <0057> Tj
4.0719757 0 Td <004B> Tj
8.1511078 0 Td <0048> Tj
8.1511078 0 Td <0051> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
154.675095 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
154.675095 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
158.74707 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
158.74707 0 Td <0044> Tj
8.1511078 0 Td <0051> Tj
8.1511078 0 Td <0052> Tj
8.1511078 0 Td <0057> Tj
4.0719757 0 Td <004B> Tj
8.1511078 0 Td <0048> Tj
8.1511078 0 Td <0055> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
208.45523 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
208.45523 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
212.52721 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
212.52721 0 Td <004F> Tj
3.2561493 0 Td <004C> Tj
3.2561493 0 Td <0051> Tj
8.1511078 0 Td <0048> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
235.34172 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
235.34172 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
239.4137 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
239.4137 0 Td <0052> Tj
8.1511078 0 Td <0049> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
251.63678 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
251.63678 0 Td <0003> Tj
ET
BT
/F1 14.6599998 Tf
1 0 0 -1 0 231 Tm
255.70876 0 Td <0001> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
255.70876 0 Td <0057> Tj
4.0719757 0 Td <0048> Tj
8.1510925 0 Td <005B> Tj
7.328125 0 Td <0057> Tj
4.071991 0 Td <0011> Tj
ET
BT
/F0 14.6599998 Tf
1 0 0 -1 0 231 Tm
283.39063 0 Td <0003> Tj
ET
Q
Q
";
    }
}
