// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.PdfPig.Tests.Tokens
{
    using PdfPig.Core;
    using PdfPig.Tokens;
 
    public class DictionaryTokenTests
    {
        [Fact]
        public void NullDictionaryThrows()
        {
            Action action = () => new DictionaryToken(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void EmptyDictionaryValid()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());

            Assert.Empty(dictionary.Data);
        }

        [Fact]
        public void TryGetByName_EmptyDictionary()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());

            var result = dictionary.TryGet(NameToken.ActualText, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Fact]
        public void TryGetByName_NullName_Throws()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());

            Action action = () => dictionary.TryGet(null, out var _);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void TryGetByName_NonEmptyDictionaryNotContainingKey()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Registry"), new StringToken("None") }
            });

            var result = dictionary.TryGet(NameToken.ActualText, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Fact]
        public void TryGetByName_ContainingKey()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Fish"), new NumericToken(420) },
                { NameToken.Create("Registry"), new StringToken("None") }
            });

            var result = dictionary.TryGet(NameToken.Registry, out var token);

            Assert.True(result);
            Assert.Equal("None", Assert.IsType<StringToken>(token).Data);
        }
        
        [Fact]
        public void GetWithObjectNotOfTypeOrReferenceThrows()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Count, new StringToken("twelve") }
            });

            Action action = () => dictionary.Get<NumericToken>(NameToken.Count, new TestPdfTokenScanner());

            Assert.Throws<PdfDocumentFormatException>(action);
        }

        [Fact]
        public void WithCorrectlyAddsKey()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Count, new StringToken("12") }
            });

            var newDictionary = dictionary.With(NameToken.ActualText, new StringToken("The text"));

            Assert.True(newDictionary.ContainsKey(NameToken.ActualText));
            Assert.Equal("The text", newDictionary.Get<StringToken>(NameToken.ActualText, new TestPdfTokenScanner()).Data);
        }

        [Fact]
        public void EqualsAndGetHashCode()
        {
            var dict1 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Key1"), new NumericToken(1) },
                { NameToken.Create("Key2"), new NumericToken(2) }
            });
            var dict2 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Key1"), new NumericToken(1) },
                { NameToken.Create("Key2"), new NumericToken(2) }
            });
            var dict3 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Key1"), new NumericToken(1) },
                { NameToken.Create("Key2"), new NumericToken(3) }
            });

            Assert.Equal(dict1, dict2);
            Assert.Equal(dict1.GetHashCode(), dict2.GetHashCode());
            Assert.NotEqual(dict1, dict3);
            Assert.False(dict1.Equals(null));
            Assert.False(dict1.Equals(new object()));
        }

        [Fact]
        public void EqualsAndGetHashCodeIgnoreInsertionOrder()
        {
            var dict1 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Key1"), new NumericToken(1) },
                { NameToken.Create("Key2"), new NumericToken(2) }
            });
            var dict2 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Key2"), new NumericToken(2) },
                { NameToken.Create("Key1"), new NumericToken(1) }
            });

            Assert.Equal(dict1, dict2);
            Assert.Equal(dict2, dict1);
            Assert.Equal(dict1.GetHashCode(), dict2.GetHashCode());
        }

        [Fact]
        public void EqualsComparesNestedTokensByValue()
        {
            var dict1 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Kids"), new ArrayToken(new IToken[] { new NumericToken(1), new StringToken("a") }) }
            });
            var dict2 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Kids"), new ArrayToken(new IToken[] { new NumericToken(1), new StringToken("a") }) }
            });
            var dict3 = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Kids"), new ArrayToken(new IToken[] { new NumericToken(2), new StringToken("a") }) }
            });

            Assert.Equal(dict1, dict2);
            Assert.Equal(dict1.GetHashCode(), dict2.GetHashCode());
            Assert.NotEqual(dict1, dict3);
        }

        [Fact]
        public void EqualsIsCountAndKeySensitive()
        {
            var empty = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var single = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Key1"), new NumericToken(1) }
            });
            var sameValueOtherKey = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Create("Key2"), new NumericToken(1) }
            });

            Assert.Equal(empty, new DictionaryToken(new Dictionary<NameToken, IToken>()));
            Assert.NotEqual(empty, single);
            Assert.NotEqual(single, empty);
            Assert.NotEqual(single, sameValueOtherKey);
        }
    }
}
