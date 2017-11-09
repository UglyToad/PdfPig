namespace UglyToad.Pdf.Tests.Text
{
    using System.Collections.Generic;
    using System.Linq;
    using Pdf.Text;
    using Pdf.Util;
    using Xunit;
    using ComponentType = Pdf.Text.TextObjectComponentType;

    public class ByteTextScannerTests
    {
        [Fact]
        public void ParseSimpleTest()
        {
            const string text = @"
BT 
   /F13 12 Tf 
   288 720 Td 
   (ABC) Tj 
ET";

            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));
            
            var components = new List<ITextObjectComponent>();

            while (scanner.Read())
            {
                components.Add(scanner.CurrentComponent);
            }

            var expected = new[]
            {
                ComponentType.BeginText,
                ComponentType.Font,
                ComponentType.Numeric,
                ComponentType.TextFont,
                ComponentType.Numeric,
                ComponentType.Numeric,
                ComponentType.MoveTextPosition,
                ComponentType.String,
                ComponentType.ShowText,
                ComponentType.EndText
            };

            Assert.Equal(expected, components.Select(x => x.Type));
        }

        [Fact]
        public void ParseStyledText()
        {
            const string text = @"BT 
/F13 48 Tf
0 40 Td
0 Tr
0.5 g
(Some Text) Tj 
ET";

            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));

            var components = new List<ITextObjectComponent>();

            while (scanner.Read())
            {
                components.Add(scanner.CurrentComponent);
            }

            var expected = new[]
            {
                ComponentType.BeginText,
                ComponentType.Font,
                ComponentType.Numeric,
                ComponentType.TextFont,
                ComponentType.Numeric,
                ComponentType.Numeric,
                ComponentType.MoveTextPosition,
                ComponentType.Numeric,
                ComponentType.SetTextRenderingMode,
                ComponentType.Numeric,
                ComponentType.SetGrayNonStroking,
                ComponentType.String,
                ComponentType.ShowText,
                ComponentType.EndText
            };

            Assert.Equal(expected, components.Select(x => x.Type));
        }

        [Fact]
        public void ParseTextAsPath()
        {
            const string text = @"BT 
/F13 48 Tf 20 38 Td  1 Tr 2 w <0053> Tj ET";

            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));

            var components = new List<ITextObjectComponent>();

            while (scanner.Read())
            {
                components.Add(scanner.CurrentComponent);
            }

            var expected = new[]
            {
                ComponentType.BeginText,
                ComponentType.Font,
                ComponentType.Numeric,
                ComponentType.TextFont,
                ComponentType.Numeric,
                ComponentType.Numeric,
                ComponentType.MoveTextPosition,
                ComponentType.Numeric,
                ComponentType.SetTextRenderingMode,
                ComponentType.Numeric,
                ComponentType.SetLineWidth,
                ComponentType.String,
                ComponentType.ShowText,
                ComponentType.EndText
            };

            Assert.Equal(expected, components.Select(x => x.Type));
        }

        [Fact]
        public void ParseTextMissingFont()
        {
            const string text = @"
BT
    40 50 Td 
(Some more text which
includes a line break, if valid?) Tj 
ET";

            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));

            var components = new List<ITextObjectComponent>();

            while (scanner.Read())
            {
                components.Add(scanner.CurrentComponent);
            }

            var expected = new[]
            {
                ComponentType.BeginText,
                ComponentType.Numeric,
                ComponentType.Numeric,
                ComponentType.MoveTextPosition,
                ComponentType.String,
                ComponentType.ShowText,
                ComponentType.EndText
            };

            Assert.Equal(expected, components.Select(x => x.Type));
        }

        [Fact]
        public void ParseTextMatrix()
        {
            const string text = @"BT
1 0 67473.567 -1 0 140 Tm
ET";

            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));

            var components = new List<ITextObjectComponent>();

            while (scanner.Read())
            {
                components.Add(scanner.CurrentComponent);
            }

            var expected = new[]
            {
                ComponentType.BeginText,
                ComponentType.Numeric, ComponentType.Numeric, ComponentType.Numeric,
                ComponentType.Numeric, ComponentType.Numeric, ComponentType.Numeric,
                ComponentType.SetTextMatrix,
                ComponentType.EndText
            };

            Assert.Equal(expected, components.Select(x => x.Type));
        }

        [Fact]
        public void ParseSimpleGoogleDocsCase()
        {
            const string text = @"BT
/F0 21.33 Tf
1 0 0 -1 0 140 Tm
96 0 Td <0037> Tj
13.0280762 0 Td <004B> Tj
11.8616943 0 Td <004C> Tj
4.7384338 0 Td <0056> Tj
ET";

            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));

            var components = new List<ITextObjectComponent>();

            while (scanner.Read())
            {
                components.Add(scanner.CurrentComponent);
            }

            var expected = new[]
            {
                ComponentType.BeginText,
                ComponentType.Font, ComponentType.Numeric, ComponentType.TextFont,
                ComponentType.Numeric, ComponentType.Numeric, ComponentType.Numeric, ComponentType.Numeric, ComponentType.Numeric, ComponentType.Numeric, ComponentType.SetTextMatrix,
                ComponentType.Numeric, ComponentType.Numeric, ComponentType.MoveTextPosition, ComponentType.String, ComponentType.ShowText,
                ComponentType.Numeric, ComponentType.Numeric, ComponentType.MoveTextPosition, ComponentType.String, ComponentType.ShowText,
                ComponentType.Numeric, ComponentType.Numeric, ComponentType.MoveTextPosition, ComponentType.String, ComponentType.ShowText,
                ComponentType.Numeric, ComponentType.Numeric, ComponentType.MoveTextPosition, ComponentType.String, ComponentType.ShowText,
                ComponentType.EndText
            };

            Assert.Equal(expected, components.Select(x => x.Type));
        }

        [Theory]
        [InlineData("BT", ComponentType.BeginText)]
        [InlineData("ET", ComponentType.EndText)]
        [InlineData("Tf", ComponentType.TextFont)]
        [InlineData("Tj", ComponentType.ShowText)]
        [InlineData("Td", ComponentType.MoveTextPosition)]
        [InlineData(" Tm", ComponentType.SetTextMatrix)]
        [InlineData(" T* ", ComponentType.MoveToNextLineStart)]
        [InlineData("\r\n   \nTs ", ComponentType.SetTextRise)]
        public void RecognisesSingleOperatorAsOnlyStringItem(string text, ComponentType textObjectComponentType)
        {
            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));

            var result = new List<ITextObjectComponent>();
            while (scanner.Read())
            {
                result.Add(scanner.CurrentComponent);
            }

            Assert.Single(result);
            Assert.Equal(textObjectComponentType, result[0].Type);
        }

        [Theory]
        [InlineData("ETe")]
        [InlineData("Tff")]
        [InlineData("T j")]
        [InlineData(" Ta ")]
        [InlineData(" t*")]
        [InlineData("\rT\ns")]
        [InlineData("no")]
        public void SkipsSimilarOperator(string text)
        {
            var scanner = new ByteTextScanner(OtherEncodings.StringAsLatin1Bytes(text));

            var result = new List<ITextObjectComponent>();
            while (scanner.Read())
            {
                result.Add(scanner.CurrentComponent);
            }

            Assert.Empty(result);
        }
    }
}
