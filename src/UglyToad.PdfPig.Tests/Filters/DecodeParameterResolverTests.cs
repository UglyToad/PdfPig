namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Tokens;
    using PdfPig.Filters;
    using Xunit;

    public class DecodeParameterResolverTests
    {
        [Fact]
        public void NullDictionary_Throws()
        {
            Action action = () => DecodeParameterResolver.GetFilterParameters(null, 0);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void NegativeIndex_Throws()
        {
            Action action = () => DecodeParameterResolver.GetFilterParameters(new DictionaryToken(new Dictionary<NameToken, IToken>()), -1);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void EmptyDictionary_ReturnsEmptyDictionary()
        {
            var result = DecodeParameterResolver.GetFilterParameters(new DictionaryToken(new Dictionary<NameToken, IToken>()), 0);

            Assert.Empty(result.Data);
        }
    }
}
