namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using Parser.Parts;
    using PdfPig.ContentStream;
    using PdfPig.Filters;
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
            Action action = () => resolver.GetFilterParameters(new PdfDictionary(), -1);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void EmptyDictionary_ReturnsEmptyDictionary()
        {
            var result = resolver.GetFilterParameters(new PdfDictionary(), 0);

            Assert.Empty(result);
        }
    }
}
