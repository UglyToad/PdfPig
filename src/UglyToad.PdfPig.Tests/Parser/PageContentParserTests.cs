namespace UglyToad.PdfPig.Tests.Parser
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Core;
    using PdfPig.Graphics.Operations.General;
    using PdfPig.Graphics.Operations.TextObjects;
    using PdfPig.Graphics.Operations.TextPositioning;
    using PdfPig.Graphics.Operations.TextShowing;
    using PdfPig.Graphics.Operations.TextState;
    using PdfPig.Parser;
    using PdfPig.Tokens;
    using PdfPig.Util;
    using Xunit;

    public class PageContentParserTests
    {
        private readonly PageContentParser parser = new PageContentParser(new ReflectionGraphicsStateOperationFactory());

        [Fact]
        public void CorrectlyExtractsOperations()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parser", "SimpleGoogleDocPageContent.txt");
            var content = File.ReadAllText(path);
            var input = StringBytesTestConverter.Convert(content, false);

            var result = parser.Parse(input.Bytes);

            Assert.NotEmpty(result);
        }

        [Fact]
        public void CorrectlyWritesOperations()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parser", "SimpleGoogleDocPageContent.txt");
            var content = File.ReadAllText(path);
            var input = StringBytesTestConverter.Convert(content, false);

            var result = parser.Parse(input.Bytes);

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

            var result = parser.Parse(input.Bytes);

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

            var result = parser.Parse(input.Bytes);

            Assert.Equal(4, result.Count);

            Assert.Equal(BeginText.Value, result[0]);

            var moveLine = Assert.IsType<MoveToNextLineWithOffset>(result[1]);
            Assert.Equal(21, moveLine.Tx);
            Assert.Equal(32, moveLine.Ty);

            var renderingMode = Assert.IsType<SetTextRenderingMode>(result[2]);
            Assert.Equal(TextRenderingMode.Fill, renderingMode.Mode);

            Assert.Equal(EndText.Value, result[3]);
        }

        private static string LineEndingsToWhiteSpace(string str)
        {
            return str.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
