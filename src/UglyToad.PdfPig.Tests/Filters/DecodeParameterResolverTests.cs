namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Filters;
    using PdfPig.Tokens;
    using Xunit;

    public class DecodeParameterResolverTests
    {
        private readonly DecodeParameterResolver resolver=  new DecodeParameterResolver(new TestingLog());

        [Fact]
        public void NullDictionary_Throws()
        {
            Action action = () => resolver.GetFilterParameters(null, 0);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void NegativeIndex_Throws()
        {
            Action action = () => resolver.GetFilterParameters(new DictionaryToken(new Dictionary<IToken, IToken>()), -1);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void EmptyDictionary_ReturnsEmptyDictionary()
        {
            var result = resolver.GetFilterParameters(new DictionaryToken(new Dictionary<IToken, IToken>()), 0);

            Assert.Empty(result.Data);
        }
    }
}
