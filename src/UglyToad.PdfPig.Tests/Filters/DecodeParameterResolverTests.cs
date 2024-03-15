namespace UglyToad.PdfPig.Tests.Filters
{
    using PdfPig.Tokens;
    using PdfPig.Filters;

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

        [Fact]
        public void SingleFilter_ReturnsParameterDictionary()
        {
            var filter = NameToken.CcittfaxDecode;
            var filterParameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.K, new NumericToken(-1) },
                { NameToken.Columns, new NumericToken(1800) },
                { NameToken.Rows, new NumericToken(3113) },
                { NameToken.BlackIs1, BooleanToken.True }
            });

            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.F, filter },
                { NameToken.DecodeParms, filterParameters }
            };

            var result = DecodeParameterResolver.GetFilterParameters(new DictionaryToken(dictionary), 0);

            Assert.Equal(filterParameters, result);
        }

        [Fact]
        public void SingleFilter_SpecifiedInArray_ReturnsParameterDictionary()
        {
            var filter = NameToken.CcittfaxDecode;
            var filterParameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.K, new NumericToken(-1) },
                { NameToken.Columns, new NumericToken(1800) },
                { NameToken.Rows, new NumericToken(3113) },
                { NameToken.BlackIs1, BooleanToken.True }
            });

            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.F, new ArrayToken(new [] { filter }) },
                { NameToken.DecodeParms, new ArrayToken(new [] { filterParameters }) }
            };

            var result = DecodeParameterResolver.GetFilterParameters(new DictionaryToken(dictionary), 0);

            Assert.Equal(filterParameters, result);
        }

        [Fact]
        public void MultipleFilters_WhenParameterIsNull_ReturnsEmptyDictionary()
        {
            var filter1 = NameToken.FlateDecode;
            var filter1Parameters = NullToken.Instance;

            var filter2 = NameToken.CcittfaxDecode;
            var filter2Parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.K, new NumericToken(-1) },
                { NameToken.Columns, new NumericToken(1800) },
                { NameToken.Rows, new NumericToken(3113) },
                { NameToken.BlackIs1, BooleanToken.True }

            });

            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.F, new ArrayToken(new [] { filter1, filter2 }) },
                { NameToken.DecodeParms, new ArrayToken(new IToken[] { filter1Parameters, filter2Parameters }) }
            };

            var result = DecodeParameterResolver.GetFilterParameters(new DictionaryToken(dictionary), 0);

            Assert.Equal(new DictionaryToken(new Dictionary<NameToken, IToken>()), result);
        }

        [Fact]
        public void MultipleFilters_ReturnsParameterDictionary()
        {
            var filter1 = NameToken.FlateDecode;
            var filter1Parameters = NullToken.Instance;

            var filter2 = NameToken.CcittfaxDecode;
            var filter2Parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.K, new NumericToken(-1) },
                { NameToken.Columns, new NumericToken(1800) },
                { NameToken.Rows, new NumericToken(3113) },
                { NameToken.BlackIs1, BooleanToken.True }
            });

            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.F, new ArrayToken(new [] { filter1, filter2 }) },
                { NameToken.DecodeParms, new ArrayToken(new IToken[] { filter1Parameters, filter2Parameters }) }
            };

            var result = DecodeParameterResolver.GetFilterParameters(new DictionaryToken(dictionary), 1);

            Assert.Equal(filter2Parameters, result);
        }

    }
}

