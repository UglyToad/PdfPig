// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.PdfPig.Tests.Tokens
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using PdfPig.Tokens;
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
    }
}
