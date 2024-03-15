namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using PdfPig.Core;
    using PdfPig.Parser.Parts;
    using PdfPig.Tokens;
    using Tokens;

    public class DirectObjectFinderTests
    {
        private readonly TestPdfTokenScanner scanner = new TestPdfTokenScanner();

        [Fact]
        public void TryGetCanFollowMultipleReferenceLinks()
        {
            var reference1 = new IndirectReference(7, 0);
            var reference2 = new IndirectReference(9, 0);

            scanner.Objects[reference1] = new ObjectToken(10, reference1, new IndirectReferenceToken(reference2));
            scanner.Objects[reference2] = new ObjectToken(12, reference2, new NumericToken(69));

            Assert.True(DirectObjectFinder.TryGet(new IndirectReferenceToken(reference1), scanner, out NumericToken result));

            Assert.Equal(69, result.Int);
        }

        [Fact]
        public void GetCanFollowMultipleReferenceLinks()
        {
            var reference1 = new IndirectReference(7, 0);
            var reference2 = new IndirectReference(9, 0);

            scanner.Objects[reference1] = new ObjectToken(10, reference1, new IndirectReferenceToken(reference2));
            scanner.Objects[reference2] = new ObjectToken(12, reference2, new NumericToken(69));

            var result = DirectObjectFinder.Get<NumericToken>(reference1, scanner);

            Assert.Equal(69, result.Int);
        }

        [Fact]
        public void GetTokenCanFollowMultipleReferenceLinks()
        {
            var reference1 = new IndirectReference(7, 0);
            var reference2 = new IndirectReference(9, 0);

            scanner.Objects[reference1] = new ObjectToken(10, reference1, new IndirectReferenceToken(reference2));
            scanner.Objects[reference2] = new ObjectToken(12, reference2, new NumericToken(69));

            var result = DirectObjectFinder.Get<NumericToken>(new IndirectReferenceToken(reference1), scanner);

            Assert.Equal(69, result.Int);
        }

        [Fact]
        public void GetReturnsSingleItemFromArray()
        {
            var reference = new IndirectReference(10, 0);

            const string expected = "Goopy";
            scanner.Objects[reference] = new ObjectToken(10, reference, new ArrayToken(new []
            {
                new StringToken(expected)
            }));

            var result = DirectObjectFinder.Get<StringToken>(reference, scanner);

            Assert.Equal(expected, result.Data);
        }

        [Fact]
        public void GetFollowsSingleIndirectReferenceFromArray()
        {
            var reference = new IndirectReference(10, 0);
            var reference2 = new IndirectReference(69, 0);

            const string expected = "Goopy";
            scanner.Objects[reference] = new ObjectToken(10, reference, new ArrayToken(new[]
            {
                new IndirectReferenceToken(reference2) 
            }));

            scanner.Objects[reference2] = new ObjectToken(69, reference2, new StringToken(expected));

            var result = DirectObjectFinder.Get<StringToken>(reference, scanner);

            Assert.Equal(expected, result.Data);
        }

        [Fact]
        public void GetThrowsOnInvalidArray()
        {
            var reference = new IndirectReference(10, 0);

            scanner.Objects[reference] = new ObjectToken(10, reference, new ArrayToken(new[]
            {
                new NumericToken(5), new NumericToken(6), new NumericToken(0)   
            }));

            Action action = () => DirectObjectFinder.Get<StringToken>(reference, scanner);

            Assert.Throws<PdfDocumentFormatException>(action);
        }
    }
}
