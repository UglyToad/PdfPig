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
        private readonly PageContentParser parser = new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance);
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

        private static string LineEndingsToWhiteSpace(string str)
        {
            return str.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
