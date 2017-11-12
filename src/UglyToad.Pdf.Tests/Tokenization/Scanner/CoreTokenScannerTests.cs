// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace UglyToad.Pdf.Tests.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using IO;
    using Parser.Parts;
    using Pdf.Cos;
    using Pdf.Parser.Parts;
    using Pdf.Tokenization.Scanner;
    using Pdf.Tokenization.Tokens;
    using Xunit;

    public class CoreTokenScannerTests
    {
        private readonly CosDictionaryParser dictionaryParser = new CosDictionaryParser(new CosNameParser(), new TestingLog());
        private readonly CosArrayParser arrayParser = new CosArrayParser();

        private readonly Func<IInputBytes, CoreTokenScanner> scannerFactory;

        public CoreTokenScannerTests()
        {
            scannerFactory = x => new CoreTokenScanner(x, dictionaryParser, arrayParser);
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
            AssertCorrectToken<NameToken, CosName>(tokens[4], CosName.Create("SomeName"));
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

            AssertCorrectToken<NameToken, CosName>(tokens[0], CosName.TYPE);
            AssertCorrectToken<NameToken, CosName>(tokens[1], CosName.Create("Example"));
            AssertCorrectToken<NameToken, CosName>(tokens[2], CosName.SUBTYPE);
            AssertCorrectToken<NameToken, CosName>(tokens[3], CosName.Create("DictionaryExample"));
            AssertCorrectToken<NameToken, CosName>(tokens[4], CosName.VERSION);
            AssertCorrectToken<NumericToken, decimal>(tokens[5], 0.01m);
            AssertCorrectToken<NameToken, CosName>(tokens[6], CosName.Create("IntegerItem"));
            AssertCorrectToken<NumericToken, decimal>(tokens[7], 12m);
            AssertCorrectToken<NameToken, CosName>(tokens[8], CosName.Create("StringItem"));
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
            Assert.Equal(tokens[2], ObjectDelimiterToken.StartObject);
            AssertCorrectToken<StringToken, string>(tokens[3], "Brillig");
            Assert.Equal(tokens[4], ObjectDelimiterToken.EndObject);
        }


        private static void AssertCorrectToken<T, TData>(IToken token, TData expected) where T : IDataToken<TData>
        {
            var cast = Assert.IsType<T>(token);

            Assert.Equal(expected, cast.Data);
        }
    }
}
