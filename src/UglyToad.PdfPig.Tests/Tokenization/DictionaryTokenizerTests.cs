// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace UglyToad.PdfPig.Tests.Tokenization
{
    using System;
    using System.Collections.Generic;
    using PdfPig.ContentStream;
    using PdfPig.Cos;
    using PdfPig.Tokenization;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    public class DictionaryTokenizerTests
    {
        private readonly DictionaryTokenizer tokenizer = new DictionaryTokenizer();

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

            AssertDictionaryEntry<StringToken, string>(dictionary, CosName.NAME, "Barry Scott");
        }

        [Fact]
        public void SimpleNameDictionary()
        {
            var input = StringBytesTestConverter.Convert("<< /Type /Example>>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, CosName>(dictionary, CosName.TYPE,
                CosName.Create("Example"));
        }

        [Fact]
        public void StreamDictionary()
        {
             var input = StringBytesTestConverter.Convert("<< /Filter /FlateDecode /S 36 /Length 53 >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, CosName>(dictionary, CosName.FILTER, CosName.FLATE_DECODE);
            AssertDictionaryEntry<NumericToken, decimal>(dictionary, CosName.S, 36);
            AssertDictionaryEntry<NumericToken, decimal>(dictionary, CosName.LENGTH, 53);
        }

        [Fact]
        public void CatalogDictionary()
        {
            var input = StringBytesTestConverter.Convert("<</Pages 14 0 R /Type /Catalog >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            var reference = new IndirectReference(14, 0);

            AssertDictionaryEntry<IndirectReferenceToken, IndirectReference>(dictionary, CosName.PAGES, reference);
            AssertDictionaryEntry<NameToken, CosName>(dictionary, CosName.TYPE, CosName.CATALOG);
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
            
            AssertDictionaryEntry<NameToken, CosName>(dictionary, CosName.TYPE, CosName.Create("Example"));
            AssertDictionaryEntry<NameToken, CosName>(dictionary, CosName.SUBTYPE, CosName.Create("DictionaryExample"));
            AssertDictionaryEntry<NumericToken, decimal>(dictionary, CosName.VERSION, 0.01m);
            AssertDictionaryEntry<NumericToken, decimal>(dictionary, CosName.Create("IntegerItem"), 12m);
            AssertDictionaryEntry<StringToken, string>(dictionary, CosName.Create("StringItem"), "a string");

            var subDictionary = GetIndex(5, dictionary);

            Assert.Equal("Subdictionary", subDictionary.Key);

            var subDictionaryValue = Assert.IsType<DictionaryToken>(subDictionary.Value);
            
            AssertDictionaryEntry<NumericToken, decimal>(subDictionaryValue, CosName.Create("Item1"), 0.4m);
            AssertDictionaryEntry<BooleanToken, bool>(subDictionaryValue, CosName.Create("Item2"), true);
            AssertDictionaryEntry<StringToken, string>(subDictionaryValue, CosName.Create("LastItem"), "not!");
            AssertDictionaryEntry<StringToken, string>(subDictionaryValue, CosName.Create("VeryLastItem"), "OK");
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

            AssertDictionaryEntry<IndirectReferenceToken, IndirectReference>(dictionary, CosName.PAGES, reference);
            AssertDictionaryEntry<NameToken, CosName>(dictionary, CosName.TYPE, CosName.CATALOG);

            Assert.Equal(2, dictionary.Data.Count);
        }

        [Fact]
        public void ParseNestedDictionary()
        {
            var input = StringBytesTestConverter.Convert(@"<< /Count 12 /Definition << /Name (Glorp)>> /Type /Catalog >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NumericToken, decimal>(dictionary, CosName.COUNT, 12);

            var subDictionaryToken = GetIndex(1, dictionary);

            Assert.Equal("Definition", subDictionaryToken.Key);

            var subDictionary = Assert.IsType<DictionaryToken>(subDictionaryToken.Value);

            AssertDictionaryEntry<StringToken, string>(subDictionary, CosName.NAME, "Glorp");

            AssertDictionaryEntry<NameToken, CosName>(dictionary, CosName.TYPE, CosName.CATALOG);

            Assert.Equal(3, dictionary.Data.Count);
        }

        private static void AssertDictionaryEntry<TValue, TValueData>(DictionaryToken dictionary, CosName key,
            TValueData value) where TValue : IDataToken<TValueData>
        {
            var result = dictionary.Data[key.Name];

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
