// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Core;
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

    public class DictionaryTokenizerTests
    {
        private readonly DictionaryTokenizer tokenizer = new DictionaryTokenizer(true, new StackDepthGuard(256));

        [Theory]
        [InlineData("[rjee]")]
        [InlineData("\r\n")]
        [InlineData("<AE>")]
        [InlineData("<[p]>")]
        public void IncorrectStartCharacters_ReturnsFalse(string s)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Fact]
        public void SkipsWhitespaceInStartSymbols()
        {
            var input = StringBytesTestConverter.Convert("< < /Name (Barry Scott) >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<StringToken, string>(dictionary, NameToken.Name, "Barry Scott");
        }

        [Fact]
        public void SimpleNameDictionary()
        {
            var input = StringBytesTestConverter.Convert("<< /Type /Example>>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, string>(dictionary, NameToken.Type, "Example");
        }

        [Fact]
        public void StreamDictionary()
        {
             var input = StringBytesTestConverter.Convert("<< /Filter /FlateDecode /S 36 /Length 53 >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, string>(dictionary, NameToken.Filter, NameToken.FlateDecode.Data);
            AssertDictionaryEntry<NumericToken, double>(dictionary, NameToken.S, 36);
            AssertDictionaryEntry<NumericToken, double>(dictionary, NameToken.Length, 53);
        }

        [Fact]
        public void CatalogDictionary()
        {
            var input = StringBytesTestConverter.Convert("<</Pages 14 0 R /Type /Catalog >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            var reference = new IndirectReference(14, 0);

            AssertDictionaryEntry<IndirectReferenceToken, IndirectReference>(dictionary, NameToken.Pages, reference);
            AssertDictionaryEntry<NameToken, string>(dictionary, NameToken.Type, NameToken.Catalog);
        }

        [Fact]
        public void SpecificationExampleDictionary()
        {
            const string s = @"<< /Type /Example
 /Subtype /DictionaryExample
 /Version 0.01
 /IntegerItem 12
 /StringItem (a string)
 /Subdictionary 
    <<  /Item1 0.4
        /Item2 true
        /LastItem (not!)
        /VeryLastItem (OK)
    >>
>>";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, string>(dictionary, NameToken.Type, "Example");
            AssertDictionaryEntry<NameToken, string>(dictionary, NameToken.Subtype, "DictionaryExample");
            AssertDictionaryEntry<NumericToken, double>(dictionary, NameToken.Version, 0.01);
            AssertDictionaryEntry<NumericToken, double>(dictionary, NameToken.Create("IntegerItem"), 12);
            AssertDictionaryEntry<StringToken, string>(dictionary, NameToken.Create("StringItem"), "a string");

            var subDictionary = GetIndex(5, dictionary);

            Assert.Equal("Subdictionary", subDictionary.Key);

            var subDictionaryValue = Assert.IsType<DictionaryToken>(subDictionary.Value);

            AssertDictionaryEntry<NumericToken, double>(subDictionaryValue, NameToken.Create("Item1"), 0.4);
            AssertDictionaryEntry<BooleanToken, bool>(subDictionaryValue, NameToken.Create("Item2"), true);
            AssertDictionaryEntry<StringToken, string>(subDictionaryValue, NameToken.Create("LastItem"), "not!");
            AssertDictionaryEntry<StringToken, string>(subDictionaryValue, NameToken.Create("VeryLastItem"), "OK");
        }

        [Fact]
        public void ExitsDictionaryParsingSingleLevel()
        {
            var input = StringBytesTestConverter.Convert(@"<< /Pages 69 0 R /Type /Catalog >>
endobj
5 0 obj");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            var reference = new IndirectReference(69, 0);

            AssertDictionaryEntry<IndirectReferenceToken, IndirectReference>(dictionary, NameToken.Pages, reference);
            AssertDictionaryEntry<NameToken, string>(dictionary, NameToken.Type, NameToken.Catalog.Data);

            Assert.Equal(2, dictionary.Data.Count);
        }

        [Fact]
        public void ParseNestedDictionary()
        {
            var input = StringBytesTestConverter.Convert(@"<< /Count 12 /Definition << /Name (Glorp)>> /Type /Catalog >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NumericToken, double>(dictionary, NameToken.Count, 12);

            var subDictionaryToken = GetIndex(1, dictionary);

            Assert.Equal("Definition", subDictionaryToken.Key);

            var subDictionary = Assert.IsType<DictionaryToken>(subDictionaryToken.Value);

            AssertDictionaryEntry<StringToken, string>(subDictionary, NameToken.Name, "Glorp");

            AssertDictionaryEntry<NameToken, string>(dictionary, NameToken.Type, NameToken.Catalog.Data);

            Assert.Equal(3, dictionary.Data.Count);
        }

        [Fact]
        public void SupportTicket29()
        {
            var input = StringBytesTestConverter.Convert("<< /Type /Page /Parent 4 0 R /MediaBox [ 0 0      \r\n   100.28 841.89 ] /Resources >>");

            tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            var dict = AssertDictionaryToken(token);

            var mediaBox = dict.Data["MediaBox"] as ArrayToken;

            Assert.NotNull(mediaBox);

            Assert.Equal(4, mediaBox.Length);
        }

        [Fact]
        public void CommentsInsideDictionaryFromSap()
        {
            const string s = @"<<
/Author (ABCD )
/CreationDate (D:20150505083655)
/Creator (Form 2014 EN)
/Producer (SAP NetWeaver 700 )
%SAPinfoStart TOA_DARA
%FUNCTION=( )
%MANDANT=( )
%DEL_DATE=( )
%SAP_OBJECT=( )
%AR_OBJECT=( )
%OBJECT_ID=( )
%FORM_ID=( )
%FORMARCHIV=( )
%RESERVE=( )
%NOTIZ=( )
%-( )
%-( )
%-( )
%SAPinfoEnd TOA_DARA
>>";

            var input = StringBytesTestConverter.Convert(s);

            Assert.True(tokenizer.TryTokenize(input.First, input.Bytes, out var token));

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<StringToken, string>(dictionary, NameToken.Producer, "SAP NetWeaver 700 ");
        }

        private static void AssertDictionaryEntry<TValue, TValueData>(DictionaryToken dictionary, NameToken key,
            TValueData value) where TValue : IDataToken<TValueData>
        {
            var result = dictionary.Data[key.Data];

            var valueToken = Assert.IsType<TValue>(result);

            Assert.Equal(value, valueToken.Data);
        }

        private static KeyValuePair<string, IToken> GetIndex(int index, DictionaryToken dictionary)
        {
            int i = 0;
            foreach (var pair in dictionary.Data)
            {
                if (i == index)
                {
                    return pair;
                }

                i++;
            }

            throw new ArgumentException("The dictionary did not contain an index: " + index);
        }

        private static DictionaryToken AssertDictionaryToken(IToken token)
        {
            Assert.NotNull(token);

            var result = Assert.IsType<DictionaryToken>(token);

            return result;
        }
    }
}
