namespace UglyToad.PdfPig.Tests.Tokens
{
    using PdfPig.Tokens;
    using PdfPig.Core;
    using System.Collections.Generic;

    public class TokenEqualityTests
    {
        [Fact]
        public void BooleanTokenEquals()
        {
            Assert.Equal(BooleanToken.True, BooleanToken.True);
            Assert.Equal(BooleanToken.True.GetHashCode(), BooleanToken.True.GetHashCode());
            Assert.NotEqual(BooleanToken.True, BooleanToken.False);
            Assert.False(BooleanToken.True.Equals(null));
        }

        [Fact]
        public void CommentTokenEquals()
        {
            var t1 = new CommentToken("test");
            var t2 = new CommentToken("test");
            var t3 = new CommentToken("other");

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            Assert.NotEqual(t1, t3);
            Assert.False(t1.Equals(null));
        }

        [Fact]
        public void EndOfLineTokenEquals()
        {
            var t1 = EndOfLineToken.Token;
            var t2 = EndOfLineToken.Token;
            
            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
        }

        [Fact]
        public void IndirectReferenceTokenEquals()
        {
            var t1 = new IndirectReferenceToken(new IndirectReference(1, 0));
            var t2 = new IndirectReferenceToken(new IndirectReference(1, 0));
            var t3 = new IndirectReferenceToken(new IndirectReference(2, 0));

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            Assert.NotEqual(t1, t3);
        }

        [Fact]
        public void InlineImageDataTokenEquals()
        {
            var data1 = new byte[] { 1, 2, 3 };
            var data2 = new byte[] { 1, 2, 3 };
            var data3 = new byte[] { 1, 2, 4 };

            var t1 = new InlineImageDataToken(data1);
            var t2 = new InlineImageDataToken(data1); // Same reference
            var t3 = new InlineImageDataToken(data2); // Same content, different reference
            var t4 = new InlineImageDataToken(data3);

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            
            Assert.Equal(t1, t3);
            Assert.NotEqual(t1, t4);
        }

        [Fact]
        public void NameTokenEquals()
        {
            var t1 = NameToken.Create("Test");
            var t2 = NameToken.Create("Test");
            var t3 = NameToken.Create("Other");

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            Assert.NotEqual(t1, t3);
        }

        [Fact]
        public void NullTokenEquals()
        {
            var t1 = NullToken.Instance;
            var t2 = NullToken.Instance;

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
        }

        [Fact]
        public void NumericTokenEquals()
        {
            var t1 = new NumericToken(1);
            var t2 = new NumericToken(1);
            var t3 = new NumericToken(2);

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            Assert.NotEqual(t1, t3);
        }

        [Fact]
        public void ObjectTokenEquals()
        {
            var t1 = new ObjectToken(XrefLocation.File(0), new IndirectReference(1, 0), new NumericToken(1));
            var t2 = new ObjectToken(XrefLocation.File(0), new IndirectReference(1, 0), new NumericToken(1));
            var t3 = new ObjectToken(XrefLocation.File(0), new IndirectReference(2, 0), new NumericToken(1));

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            Assert.NotEqual(t1, t3);
        }

        [Fact]
        public void OperatorTokenEquals()
        {
            var t1 = OperatorToken.Create("w".AsSpan());
            var t2 = OperatorToken.Create("w".AsSpan());
            var t3 = OperatorToken.Create("W".AsSpan());

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            Assert.NotEqual(t1, t3);
        }

        [Fact]
        public void StreamTokenEquals()
        {
            var dict1 = new DictionaryToken(new Dictionary<NameToken, IToken> { { NameToken.Length, new NumericToken(1) } });
            var dict2 = new DictionaryToken(new Dictionary<NameToken, IToken> { { NameToken.Length, new NumericToken(1) } });
            var data1 = new byte[] { 65 };
            var data2 = new byte[] { 65 };

            var t1 = new StreamToken(dict1, data1);
            var t2 = new StreamToken(dict2, data2);

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
        }

        [Fact]
        public void StringTokenEquals()
        {
            var t1 = new StringToken("test");
            var t2 = new StringToken("test");
            var t3 = new StringToken("other");

            Assert.Equal(t1, t2);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
            Assert.NotEqual(t1, t3);
        }

        [Fact]
        public void StringTokenNotEqualWhenEncodingDiffers()
        {
            var iso = new StringToken("test", StringToken.Encoding.Iso88591);
            var utf16 = new StringToken("test", StringToken.Encoding.Utf16BE);

            Assert.NotEqual(iso, utf16);
            Assert.NotEqual(utf16, iso);
        }

        [Fact]
        public void NumericTokenIntAndDoubleWithSameValueAreEqual()
        {
            var integer = new NumericToken(1);
            var floating = new NumericToken(1.0);

            Assert.Equal(integer, floating);
            Assert.Equal(integer.GetHashCode(), floating.GetHashCode());
        }

        [Fact]
        public void StreamTokenNotEqualWhenDictionaryOrDataDiffers()
        {
            var dict = new DictionaryToken(new Dictionary<NameToken, IToken> { { NameToken.Length, new NumericToken(1) } });
            var otherDict = new DictionaryToken(new Dictionary<NameToken, IToken> { { NameToken.Length, new NumericToken(2) } });

            var token = new StreamToken(dict, new byte[] { 65 });
            var differentDictionary = new StreamToken(otherDict, new byte[] { 65 });
            var differentData = new StreamToken(dict, new byte[] { 66 });

            Assert.NotEqual(token, differentDictionary);
            Assert.NotEqual(token, differentData);
            Assert.False(token.Equals(null));
        }

        [Fact]
        public void IndirectReferenceTokenNotEqualWhenGenerationDiffers()
        {
            var t1 = new IndirectReferenceToken(new IndirectReference(1, 0));
            var t2 = new IndirectReferenceToken(new IndirectReference(1, 1));

            Assert.NotEqual(t1, t2);
        }

        [Fact]
        public void ObjectTokenNotEqualWhenDataDiffers()
        {
            var t1 = new ObjectToken(XrefLocation.File(0), new IndirectReference(1, 0), new NumericToken(1));
            var t2 = new ObjectToken(XrefLocation.File(0), new IndirectReference(1, 0), new NumericToken(2));

            Assert.NotEqual(t1, t2);
        }

        [Fact]
        public void TokensOfDifferentTypesAreNeverEqual()
        {
            var tokens = new IToken[]
            {
                new NumericToken(1),
                new StringToken("1"),
                new HexToken("31".ToCharArray()),
                NameToken.Create("1"),
                new CommentToken("1"),
                BooleanToken.True,
                NullToken.Instance,
                EndOfLineToken.Token,
                OperatorToken.Create("1".AsSpan()),
                new ArrayToken(new IToken[] { new NumericToken(1) }),
                new DictionaryToken(new Dictionary<NameToken, IToken>()),
                new IndirectReferenceToken(new IndirectReference(1, 0)),
                new InlineImageDataToken(new byte[] { 49 })
            };

            for (var i = 0; i < tokens.Length; i++)
            {
                Assert.False(tokens[i].Equals(null), $"{tokens[i].GetType().Name} should not equal null.");

                for (var j = 0; j < tokens.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Assert.False(
                        tokens[i].Equals(tokens[j]),
                        $"{tokens[i].GetType().Name} should not equal {tokens[j].GetType().Name}.");
                }
            }
        }
    }
}
