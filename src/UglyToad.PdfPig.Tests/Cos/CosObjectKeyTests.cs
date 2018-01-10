// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.PdfPig.Tests.Cos
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Cos;
    using Xunit;

    public class CosObjectKeyTests
    {
        [Fact]
        public void NullObjectThrows()
        {
            Action action = () => new CosObjectKey(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CanLookupInDictionary()
        {
            var key1 = new CosObjectKey(3, 0);
            var key2 = new CosObjectKey(3, 0);

            var dictionary = new Dictionary<CosObjectKey, long>
            {
                {key1, 5}
            };

            var result = dictionary[key2];

            Assert.Equal(5, result);
        }
    }
}
