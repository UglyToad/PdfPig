namespace UglyToad.PdfPig.Tests.Parser
{
    using System.Text.RegularExpressions;
    using Logging;
    using PdfPig.Core;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Operations.General;
    using PdfPig.Graphics.Operations.SpecialGraphicsState;
    using PdfPig.Graphics.Operations.TextObjects;
    using PdfPig.Graphics.Operations.TextPositioning;
    using PdfPig.Graphics.Operations.TextShowing;
    using PdfPig.Graphics.Operations.TextState;
    using PdfPig.Parser;
    using PdfPig.Tokens;

    public class PageContentParserTests
    {
        private readonly PageContentParser parser = new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256));
        private readonly ILog log = new NoOpLog();

        [Fact]
        public void CorrectlyExtractsOperations()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parser", "SimpleGoogleDocPageContent.txt");
            var content = File.ReadAllText(path);
            var input = StringBytesTestConverter.Convert(content, false);

            var result = parser.Parse(1, input.Bytes, log);

            Assert.NotEmpty(result);
        }

        [Fact]
        public void CorrectlyWritesOperations()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parser", "SimpleGoogleDocPageContent.txt");
            var content = File.ReadAllText(path);
            var input = StringBytesTestConverter.Convert(content, false);

            var result = parser.Parse(1, input.Bytes, log);

            var replacementRegex = new Regex(@"\s(\.\d+)\b");

            using (var stream = new MemoryStream())
            {
                foreach (var operation in result)
                {
                    operation.Write(stream);
                }

                var text = OtherEncodings.BytesAsLatin1String(stream.ToArray());

                text = LineEndingsToWhiteSpace(text);
                content = LineEndingsToWhiteSpace(content);
                content = replacementRegex.Replace(content, " 0$1");

                Assert.Equal(content, text);
            }
        }

        [Fact]
        public void CorrectlyWritesSmallTextContent()
        {
            const string s = @"BT
/F13 48 Tf
20 38 Td
1 Tr
2 w
(ABC) Tj
ET";
            var input = StringBytesTestConverter.Convert(s, false);

            var result = parser.Parse(1, input.Bytes, log);

            using (var stream = new MemoryStream())
            {
                foreach (var operation in result)
                {
                    operation.Write(stream);
                }

                var text = OtherEncodings.BytesAsLatin1String(stream.ToArray());

                text = LineEndingsToWhiteSpace(text).Trim();
                var expected = LineEndingsToWhiteSpace(s);

                Assert.Equal(expected, text);
            }
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

            var result = parser.Parse(1, input.Bytes, log);

            Assert.Equal(7, result.Count);

            Assert.Equal(BeginText.Value, result[0]);

            var font = Assert.IsType<SetFontAndSize>(result[1]);
            Assert.Equal(NameToken.Create("F13"), font.Font);
            Assert.Equal(48, font.Size);

            var nextLine = Assert.IsType<MoveToNextLineWithOffset>(result[2]);
            Assert.Equal(20, nextLine.Tx);
            Assert.Equal(38, nextLine.Ty);

            var renderingMode = Assert.IsType<SetTextRenderingMode>(result[3]);
            Assert.Equal(TextRenderingMode.Stroke, renderingMode.Mode);

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

            var result = parser.Parse(1, input.Bytes, log);

            Assert.Equal(4, result.Count);

            Assert.Equal(BeginText.Value, result[0]);

            var moveLine = Assert.IsType<MoveToNextLineWithOffset>(result[1]);
            Assert.Equal(21, moveLine.Tx);
            Assert.Equal(32, moveLine.Ty);

            var renderingMode = Assert.IsType<SetTextRenderingMode>(result[2]);
            Assert.Equal(TextRenderingMode.Fill, renderingMode.Mode);

            Assert.Equal(EndText.Value, result[3]);
        }

        [Fact]
        public void HandlesEscapedLineBreaks()
        {
            const string s = @"q 1 0 0 1 48 434
cm BT 0.0001 Tc 19 0 0 19 0 0 Tm /Tc1 1 Tf (   \(sleep 1; printf ""QUIT\\r\\n""\) | )
            Tj ET Q";

            var input = StringBytesTestConverter.Convert(s, false);

            var result = parser.Parse(1, input.Bytes, log);

            Assert.Equal(9, result.Count);

            Assert.IsType<Push>(result[0]);
            Assert.IsType<ModifyCurrentTransformationMatrix>(result[1]);
            Assert.IsType<BeginText>(result[2]);
            Assert.IsType<SetCharacterSpacing>(result[3]);
            Assert.IsType<SetTextMatrix>(result[4]);

            Assert.IsType<SetFontAndSize>(result[5]);
            var text = Assert.IsType<ShowText>(result[6]);
            Assert.IsType<EndText>(result[7]);
            Assert.IsType<Pop>(result[8]);

            Assert.Equal(@"   (sleep 1; printf ""QUIT\r\n"") | ", text.Text);
        }

        [Fact]
        public void HandlesWeirdNumber()
        {
            // Issue 453
            const string s = @"/Alpha1
gs
0
0
0
rg
0.00-90
151555.0
m
302399.97
151555.0
l";

            var input = StringBytesTestConverter.Convert(s, false);

            var result = parser.Parse(1, input.Bytes, log);

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void CorrectlyHandlesFile0007511CorruptInlineImage()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parser", "0007511-page-2.txt");
            var content = File.ReadAllText(path);
            var input = StringBytesTestConverter.Convert(content, false);

            var lenientParser = new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256), true);
            var result = lenientParser.Parse(1, input.Bytes, log);

            Assert.NotEmpty(result);
        }

        [Fact]
        public void HandlesIssue953_IntOverflowContent()
        {
            // After ( + ) Tj operator the content stream becomes corrupt, our current parser therefore reads wrong
            // values for operations and this results in a problem when applying the show text operations, we should safely discard or recover on BT/ET boundaries.
            const string s =
                """
                BT
                /TT6 1 Tf
                12.007 0 0 12.007 163.2j
                -0.19950 Tc
                0 Tw
                (x)Tj
                -0.1949 1.4142 TD
                (H)Tj
                /TT7 1 Tf
                12.031 0 0 12.031 157.38 85.2 Tm
                <0077>Tj
                -0.1945 1.4114 TD
                <0077>Tj
                /TT4 1 Tf
                12.007 0 0 12.007 174.42 94.5601 Tm
                0.0004 Tc
                -0.0005 Tw
                ( + )Tj
                E9 478l)]T862.68E9 478E9 484.54 9 155l)]T862.6av9 478E9 15.2(
                ET
                154.386( i92 m
                171.6 97.62 l
                S
                BT
                /TT6 28 Tf
                12.03128 T2002.0307 163.2j
                -0.19950 DAc
                0 Tw853Tj
                0.1945 1.4142 om)873j
                -0.574142 om)68.80
                -0.5797 0 TD
                (f)Tj
                /TT( )7Tf
                0.31945 1.5341 TD371.4j
                2.82
                8.2652 0 5.724 TD
                0 Tc
                -0.0001 2748.3( = 091ity )-27483
                [(te27483
                [(te27483
                [(te27483
                [(te27483
                [(te27483
                [(Eq.)52   \(2.1
                ( 
                """;

            var input = StringBytesTestConverter.Convert(s, false);

            var lenientParser = new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256), true);
            var result = lenientParser.Parse(1, input.Bytes, log);

            Assert.NotEmpty(result);
        }

        private static string LineEndingsToWhiteSpace(string str)
        {
            return str.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
