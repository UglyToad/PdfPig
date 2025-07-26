// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace UglyToad.PdfPig.Tests.Tokenization.Scanner
{
    using PdfPig.Core;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    public class CoreTokenScannerTests
    {
        private readonly Func<IInputBytes, CoreTokenScanner> scannerFactory;

        public CoreTokenScannerTests()
        {
            scannerFactory = x => new CoreTokenScanner(x, true);
        }

        [Fact]
        public void ScansSpecificationArrayExampleContents()
        {
            const string s = "549 3.14 false (Ralph) /SomeName";

            var tokens = new List<IToken>();

            var scanner = scannerFactory(StringBytesTestConverter.Convert(s, false).Bytes);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            AssertCorrectToken<NumericToken, double>(tokens[0], 549);
            AssertCorrectToken<NumericToken, double>(tokens[1], 3.14);
            AssertCorrectToken<BooleanToken, bool>(tokens[2], false);
            AssertCorrectToken<StringToken, string>(tokens[3], "Ralph");
            AssertCorrectToken<NameToken, string>(tokens[4], "SomeName");
        }

        [Fact]
        public void ScansSpecificationSimpleDictionaryExampleContents()
        {
            const string s = @"/Type /Example
        /Subtype /DictionaryExample
        /Version 0.01
        /IntegerItem 12
        /StringItem(a string)";

            var tokens = new List<IToken>();

            var scanner = scannerFactory(StringBytesTestConverter.Convert(s, false).Bytes);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            AssertCorrectToken<NameToken, string>(tokens[0], NameToken.Type.Data);
            AssertCorrectToken<NameToken, string>(tokens[1], "Example");
            AssertCorrectToken<NameToken, string>(tokens[2], NameToken.Subtype.Data);
            AssertCorrectToken<NameToken, string>(tokens[3], "DictionaryExample");
            AssertCorrectToken<NameToken, string>(tokens[4], NameToken.Version.Data);
            AssertCorrectToken<NumericToken, double>(tokens[5], 0.01);
            AssertCorrectToken<NameToken, string>(tokens[6], "IntegerItem");
            AssertCorrectToken<NumericToken, double>(tokens[7], 12);
            AssertCorrectToken<NameToken, string>(tokens[8], "StringItem");
            AssertCorrectToken<StringToken, string>(tokens[9], "a string");
        }

        [Fact]
        public void ScansIndirectObjectExampleContents()
        {
            const string s = @"12 0 obj
    (Brillig)
endobj";

            var tokens = new List<IToken>();

            var scanner = scannerFactory(StringBytesTestConverter.Convert(s, false).Bytes);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            AssertCorrectToken<NumericToken, double>(tokens[0], 12);
            AssertCorrectToken<NumericToken, double>(tokens[1], 0);
            Assert.Equal(tokens[2], OperatorToken.StartObject);
            AssertCorrectToken<StringToken, string>(tokens[3], "Brillig");
            Assert.Equal(tokens[4], OperatorToken.EndObject);
        }

        [Fact]
        public void ScansArrayInSequence()
        {
            const string s = @"/Bounds [12 15 19 1455.3]/Font /F1 /Name (Bob)[16]";

            var tokens = new List<IToken>();

            var scanner = scannerFactory(StringBytesTestConverter.Convert(s, false).Bytes);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            AssertCorrectToken<NameToken, string>(tokens[0], "Bounds");
            Assert.IsType<ArrayToken>(tokens[1]);
            AssertCorrectToken<NameToken, string>(tokens[2], "Font");
            AssertCorrectToken<NameToken, string>(tokens[3], "F1");
            AssertCorrectToken<NameToken, string>(tokens[4], "Name");
            AssertCorrectToken<StringToken, string>(tokens[5], "Bob");
            Assert.IsType<ArrayToken>(tokens[6]);
        }

        [Fact]
        public void CorrectlyScansArrayWithEscapedStrings()
        {
            const string s = @"<0078>Tj
/TT0 1 Tf
0.463 0 Td
( )Tj
-0.002 Tc 0.007 Tw 11.04 -0 0 11.04 180 695.52 Tm
[(R)2.6(eg)-11.3(i)2.7(s)-2(t)4.2(r)-5.9(at)-6.6(i)2.6(on S)2(e)10.5(r)-6(v)8.9(i)2.6(c)-2(e S)1.9(o)10.6(f)-17.5(t)4.3(w)13.4(ar)-6(e \()-6(R)2.6(S)2(S)1.9(\))]TJ
0 Tc 0 Tw 16.12 0 Td";

            var tokens = new List<IToken>();

            var scanner = scannerFactory(StringBytesTestConverter.Convert(s, false).Bytes);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            Assert.Equal(30, tokens.Count);

            AssertCorrectToken<OperatorToken, string>(tokens[29], "Td");
            AssertCorrectToken<NumericToken, double>(tokens[28], 0);
            AssertCorrectToken<NumericToken, double>(tokens[27], 16.12);
            AssertCorrectToken<OperatorToken, string>(tokens[26], "Tw");

            var array = Assert.IsType<ArrayToken>(tokens[21]);

            AssertCorrectToken<StringToken, string>(array.Data[array.Data.Count - 1], ")");
            AssertCorrectToken<NumericToken, double>(array.Data[array.Data.Count - 2], 1.9);
        }

        [Fact]
        public void ScansStringWithoutWhitespacePreceding()
        {
            const string s = @"T*() Tj
-91";

            var tokens = new List<IToken>();

            var scanner = scannerFactory(StringBytesTestConverter.Convert(s, false).Bytes);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            Assert.Equal(4, tokens.Count);

            AssertCorrectToken<OperatorToken, string>(tokens[0], "T*");
            AssertCorrectToken<StringToken, string>(tokens[1], "");
            AssertCorrectToken<OperatorToken, string>(tokens[2], "Tj");
            AssertCorrectToken<NumericToken, double>(tokens[3], -91);
        }

        [Fact]
        public void ScansStringWithWeirdWeirdDoubleSymbolNumerics()
        {
            const string content = @"
                0.00 --21.72 TD
                /F1 8.00 Tf";

            var tokens = new List<IToken>();

            var scanner = scannerFactory(StringBytesTestConverter.Convert(content, false).Bytes);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            Assert.Equal(6, tokens.Count);

            AssertCorrectToken<NumericToken, double>(tokens[0], 0);
            AssertCorrectToken<NumericToken, double>(tokens[1], -21.72);
            AssertCorrectToken<OperatorToken, string>(tokens[2], "TD");
            AssertCorrectToken<NameToken, string>(tokens[3], "F1");
            AssertCorrectToken<NumericToken, double>(tokens[4], 8);
            AssertCorrectToken<OperatorToken, string>(tokens[5], "Tf");
        }

        [Fact]
        public void SkipsCommentsInStreams()
        {
            const string content = 
                """
                % 641 0 obj
                <<
                /Type /Encoding
                /Differences [16/quotedblleft/quotedblright 21/endash 27/ff/fi/fl/ffi 39/quoteright/parenleft/parenright 43/plus/comma/hyphen/period/slash/zero/one/two/three/four/five/six/seven/eight/nine/colon 64/at/A/B/C/D/E/F/G/H/I/J/K/L/M/N/O/P/Q/R/S/T/U/V/W/X/Y/Z/bracketleft 93/bracketright 97/a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/p/q/r/s/t/u/v/w/x/y/z/braceleft 125/braceright 225/aacute 232/egrave/eacute 252/udieresis]
                >>
                % 315 0 obj
                <<
                /Type /Font
                /Subtype /Type1
                /BaseFont /IXNPPI+CMEX10
                /FontDescriptor 661 0 R
                /FirstChar 80
                /LastChar 88
                /Widths 644 0 R
                /ToUnicode 699 0 R
                >>
                % 306 0 obj
                <<
                /Type /Font
                /Subtype /Type1
                /BaseFont /MSNKTF+CMMI10
                /FontDescriptor 663 0 R
                /FirstChar 58
                /LastChar 119
                /Widths 651 0 R
                /ToUnicode 700 0 R
                >>
                """;

            var tokens = new List<IToken>();

            var scanner = new CoreTokenScanner(
                StringBytesTestConverter.Convert(content, false).Bytes,
                true,
                isStream: true);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }

            Assert.Equal(3, tokens.Count);

            Assert.All(tokens, x => Assert.IsType<DictionaryToken>(x));

            tokens.Clear();

            var nonStreamScanner = new CoreTokenScanner(
                StringBytesTestConverter.Convert(content, false).Bytes,
                true,
                isStream: false);

            while (nonStreamScanner.MoveNext())
            {
                tokens.Add(nonStreamScanner.CurrentToken);
            }

            Assert.Equal(6, tokens.Count);

            Assert.Equal(3, tokens.OfType<CommentToken>().Count());
            Assert.Equal(3, tokens.OfType<DictionaryToken>().Count());
        }

        [Fact]
        public void Document006324Test()
        {
            const string content =
                """
                q
                    1 0 0 1 248.6304 572.546 cm
                    0 0 m
                        0.021 -0.007 l
                        3 -0.003 -0.01 0 0 0 c
                    f
                Q
                q
                    1 0 0 1 2489394 57249855 cm
                    0 0 m
                        -0.046 -0.001 -0.609 0.029 -0.286 -0.014 c
                        -02.61 -0.067 -0.286 -0. .61 -0 0 c
                    f
                Q
                q
                    1 0 0 1 24862464 572. .836 cm
                    0 0 m
                        0.936 -0.029 l
                        0.038 -0.021 0.55 -0.014 0 0 c
                    f
                Q
                """;

            var tokens = new List<IToken>();

            var scanner = new CoreTokenScanner(
                StringBytesTestConverter.Convert(content, false).Bytes,
                true,
                isStream: true);

            while (scanner.MoveNext())
            {
                tokens.Add(scanner.CurrentToken);
            }
        }

        private static void AssertCorrectToken<T, TData>(IToken token, TData expected) where T : IDataToken<TData>
        {
            var cast = Assert.IsType<T>(token);

            Assert.Equal(expected, cast.Data);
        }
    }
}
