// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace UglyToad.Pdf.Tests.Tokenization
{
    using System;
    using System.Collections.Generic;
    using Pdf.ContentStream;
    using Pdf.Cos;
    using Pdf.Tokenization;
    using Pdf.Tokenization.Tokens;
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

            AssertDictionaryEntry<NameToken, CosName, StringToken, string>(dictionary, 0, CosName.NAME, "Barry Scott");
        }

        [Fact]
        public void SimpleNameDictionary()
        {
            var input = StringBytesTestConverter.Convert("<< /Type /Example>>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, CosName, NameToken, CosName>(dictionary, 0, CosName.TYPE,
                CosName.Create("Example"));
        }

        [Fact]
        public void StreamDictionary()
        {
             var input = StringBytesTestConverter.Convert("<< /Filter /FlateDecode /S 36 /Length 53 >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, CosName, NameToken, CosName>(dictionary, 0, CosName.FILTER, CosName.FLATE_DECODE);
            AssertDictionaryEntry<NameToken, CosName, NumericToken, decimal>(dictionary, 1, CosName.S, 36);
            AssertDictionaryEntry<NameToken, CosName, NumericToken, decimal>(dictionary, 2, CosName.LENGTH, 53);
        }

        [Fact]
        public void CatalogDictionary()
        {
            var input = StringBytesTestConverter.Convert("<</Pages 14 0 R /Type /Catalog >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            var reference = new IndirectReference(14, 0);

            AssertDictionaryEntry<NameToken, CosName, IndirectReferenceToken, IndirectReference>(dictionary, 0, CosName.PAGES, reference);
            AssertDictionaryEntry<NameToken, CosName, NameToken, CosName>(dictionary, 1, CosName.TYPE, CosName.CATALOG);
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
            
            AssertDictionaryEntry<NameToken, CosName, NameToken, CosName>(dictionary, 0, CosName.TYPE, CosName.Create("Example"));
            AssertDictionaryEntry<NameToken, CosName, NameToken, CosName>(dictionary, 1, CosName.SUBTYPE, CosName.Create("DictionaryExample"));
            AssertDictionaryEntry<NameToken, CosName, NumericToken, decimal>(dictionary, 2, CosName.VERSION, 0.01m);
            AssertDictionaryEntry<NameToken, CosName, NumericToken, decimal>(dictionary, 3, CosName.Create("IntegerItem"), 12m);
            AssertDictionaryEntry<NameToken, CosName, StringToken, string>(dictionary, 4, CosName.Create("StringItem"), "a string");

            var subDictionary = GetIndex(5, dictionary);

            Assert.Equal(CosName.Create("Subdictionary"), Assert.IsType<NameToken>(subDictionary.Key).Data);

            var subDictionaryValue = Assert.IsType<DictionaryToken>(subDictionary.Value);
            
            AssertDictionaryEntry<NameToken, CosName, NumericToken, decimal>(subDictionaryValue, 0, CosName.Create("Item1"), 0.4m);
            AssertDictionaryEntry<NameToken, CosName, BooleanToken, bool>(subDictionaryValue, 1, CosName.Create("Item2"), true);
            AssertDictionaryEntry<NameToken, CosName, StringToken, string>(subDictionaryValue, 2, CosName.Create("LastItem"), "not!");
            AssertDictionaryEntry<NameToken, CosName, StringToken, string>(subDictionaryValue, 3, CosName.Create("VeryLastItem"), "OK");
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

            AssertDictionaryEntry<NameToken, CosName, IndirectReferenceToken, IndirectReference>(dictionary, 0, CosName.PAGES, reference);
            AssertDictionaryEntry<NameToken, CosName, NameToken, CosName>(dictionary, 1, CosName.TYPE, CosName.CATALOG);

            Assert.Equal(2, dictionary.Data.Count);
        }

        [Fact]
        public void ParseNestedDictionary()
        {
            var input = StringBytesTestConverter.Convert(@"<< /Count 12 /Definition << /Name (Glorp)>> /Type /Catalog >>");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            var dictionary = AssertDictionaryToken(token);

            AssertDictionaryEntry<NameToken, CosName, NumericToken, decimal>(dictionary, 0, CosName.COUNT, 12);

            var subDictionaryToken = GetIndex(1, dictionary);

            Assert.Equal(CosName.Create("Definition"), Assert.IsType<NameToken>(subDictionaryToken.Key).Data);

            var subDictionary = Assert.IsType<DictionaryToken>(subDictionaryToken.Value);

            AssertDictionaryEntry<NameToken, CosName, StringToken, string>(subDictionary, 0, CosName.NAME, "Glorp");

            AssertDictionaryEntry<NameToken, CosName, NameToken, CosName>(dictionary, 2, CosName.TYPE, CosName.CATALOG);

            Assert.Equal(3, dictionary.Data.Count);
        }

        private static void AssertDictionaryEntry<TKey, TKeyData, TValue, TValueData>(
            DictionaryToken dictionary, int index, TKeyData key,
            TValueData value) where TKey : IDataToken<TKeyData> where TValue : IDataToken<TValueData>
        {
            KeyValuePair<IToken, IToken> data = GetIndex(index, dictionary);

            var keyToken = Assert.IsType<TKey>(data.Key);

            Assert.Equal(key, keyToken.Data);

            var valueToken = Assert.IsType<TValue>(data.Value);

            Assert.Equal(value, valueToken.Data);
        }

        private static KeyValuePair<IToken, IToken> GetIndex(int index, DictionaryToken dictionary)
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
