// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace UglyToad.PdfPig.Tests.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using PdfPig.IO;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    public class CoreTokenScannerTests
    {
        private readonly Func<IInputBytes, CoreTokenScanner> scannerFactory;

        public CoreTokenScannerTests()
        {
            scannerFactory = x => new CoreTokenScanner(x);
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

            AssertCorrectToken<NumericToken, decimal>(tokens[0], 549);
            AssertCorrectToken<NumericToken, decimal>(tokens[1], 3.14m);
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
            AssertCorrectToken<NumericToken, decimal>(tokens[5], 0.01m);
            AssertCorrectToken<NameToken, string>(tokens[6], "IntegerItem");
            AssertCorrectToken<NumericToken, decimal>(tokens[7], 12m);
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

            AssertCorrectToken<NumericToken, decimal>(tokens[0], 12);
            AssertCorrectToken<NumericToken, decimal>(tokens[1], 0);
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
            AssertCorrectToken<NumericToken, decimal>(tokens[28], 0);
            AssertCorrectToken<NumericToken, decimal>(tokens[27], 16.12m);
            AssertCorrectToken<OperatorToken, string>(tokens[26], "Tw");

            var array = Assert.IsType<ArrayToken>(tokens[21]);

            AssertCorrectToken<StringToken, string>(array.Data[array.Data.Count - 1], ")");
            AssertCorrectToken<NumericToken, decimal>(array.Data[array.Data.Count - 2], 1.9m);
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
            AssertCorrectToken<NumericToken, decimal>(tokens[3], -91);
        }
        
        private static void AssertCorrectToken<T, TData>(IToken token, TData expected) where T : IDataToken<TData>
        {
            var cast = Assert.IsType<T>(token);

            Assert.Equal(expected, cast.Data);
        }
    }
}
