// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.PdfPig.Tests.Tokenization.Tokens
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

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
            var dictionary = new DictionaryToken(new Dictionary<IToken, IToken>());

            Assert.Empty(dictionary.Data);
        }

        [Fact]
        public void TryGetByName_EmptyDictionary()
        {
            var dictionary = new DictionaryToken(new Dictionary<IToken, IToken>());

            var result = dictionary.TryGet(NameToken.ActualText, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Fact]
        public void TryGetByName_NullName_Throws()
        {
            var dictionary = new DictionaryToken(new Dictionary<IToken, IToken>());

            Action action = () => dictionary.TryGet(null, out var _);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void TryGetByName_NonEmptyDictionaryNotContainingKey()
        {
            var dictionary = new DictionaryToken(new Dictionary<IToken, IToken>
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
            var dictionary = new DictionaryToken(new Dictionary<IToken, IToken>
            {
                { NameToken.Create("Fish"), new NumericToken(420) },
                { NameToken.Create("Registry"), new StringToken("None") }
            });

            var result = dictionary.TryGet(NameToken.Registry, out var token);

            Assert.True(result);
            Assert.Equal("None", Assert.IsType<StringToken>(token).Data);
        }
    }
}
