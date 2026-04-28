namespace UglyToad.PdfPig.Tests.Tokens
{
    using PdfPig.Tokens;

    public class StreamTokenTests
    {
        private static DictionaryToken EmptyDictionary => new DictionaryToken(new Dictionary<NameToken, IToken>());

        [Fact]
        public void ByteArrayConstructorWorks()
        {
            var data = new byte[] { 1, 2, 3 };
            var token = new StreamToken(EmptyDictionary, data);

            Assert.Equal(3, token.Data.Length);
            Assert.Equal(1, token.Data.Span[0]);
        }

        [Fact]
        public void MemoryConstructorWorks()
        {
            Memory<byte> data = new byte[] { 4, 5, 6 };
            var token = new StreamToken(EmptyDictionary, data);

            Assert.Equal(3, token.Data.Length);
            Assert.Equal(4, token.Data.Span[0]);
        }

        [Fact]
        public void EmptyMemoryConstructorWorks()
        {
            var token = new StreamToken(EmptyDictionary, Memory<byte>.Empty);

            Assert.True(token.Data.IsEmpty);
        }

        [Fact]
        public void LazyDataNotLoadedBeforeAccess()
        {
            var called = false;
            var token = new StreamToken(EmptyDictionary, () =>
            {
                called = true;
                return new byte[] { 10, 20 };
            });

            Assert.False(called);
            Assert.False(token.IsDataLoaded);
        }

        [Fact]
        public void LazyDataLoadedOnAccess()
        {
            var token = new StreamToken(EmptyDictionary, () => (Memory<byte>)new byte[] { 10, 20 });

            var data = token.Data;

            Assert.True(token.IsDataLoaded);
            Assert.Equal(2, data.Length);
            Assert.Equal(10, data.Span[0]);
            Assert.Equal(20, data.Span[1]);
        }

        [Fact]
        public void LazyFactoryCalledOnce()
        {
            var callCount = 0;
            var token = new StreamToken(EmptyDictionary, () =>
            {
                callCount++;
                return new byte[] { 1 };
            });

            _ = token.Data;
            _ = token.Data;
            _ = token.Data;

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void ToStringShowsDeferredBeforeLoad()
        {
            var token = new StreamToken(EmptyDictionary, () => (Memory<byte>)new byte[] { 1, 2, 3 });

            Assert.Contains("deferred", token.ToString());
        }

        [Fact]
        public void ToStringShowsLengthAfterLoad()
        {
            var token = new StreamToken(EmptyDictionary, () => (Memory<byte>)new byte[] { 1, 2, 3 });

            _ = token.Data;

            Assert.Contains("3", token.ToString());
            Assert.DoesNotContain("deferred", token.ToString());
        }
    }
}
