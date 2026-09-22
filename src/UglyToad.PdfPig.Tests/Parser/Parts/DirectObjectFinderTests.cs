namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using PdfPig.Core;
    using PdfPig.Parser.Parts;
    using PdfPig.Tokens;
    using Tokens;

    public class DirectObjectFinderTests
    {
        private readonly TestPdfTokenScanner scanner = new TestPdfTokenScanner();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReferenceDepthIsBoundedAndRestored(bool cycle)
        {
            scanner.StackDepthGuard = new StackDepthGuard(4);
            for (var i = 1; i <= 8; i++)
            {
                var reference = new IndirectReference(i, 0);
                IToken value = i == 8
                    ? (cycle ? new IndirectReferenceToken(new IndirectReference(1, 0)) : new NumericToken(42))
                    : new IndirectReferenceToken(new IndirectReference(cycle ? i : i + 1, 0));
                scanner.Objects[reference] = new ObjectToken(XrefLocation.File(i), reference, value);
            }

            var first = new IndirectReference(1, 0);
            Assert.False(DirectObjectFinder.TryGet(new IndirectReferenceToken(first), scanner, out NumericToken missing));
            Assert.Null(missing);
            Assert.Equal(4, scanner.GetCallCount);
            Assert.Throws<PdfDocumentStackDepthException>(() => DirectObjectFinder.Get<NumericToken>(first, scanner));
            Assert.Equal(8, scanner.GetCallCount);

            // Failure must not consume the budget for a subsequent valid lookup.
            scanner.Objects[first] = new ObjectToken(XrefLocation.File(1), first, new NumericToken(42));
            Assert.True(DirectObjectFinder.TryGet(new IndirectReferenceToken(first), scanner, out NumericToken result));
            Assert.Equal(42, result.Int);
            Assert.Equal(42, DirectObjectFinder.Get<NumericToken>(first, scanner).Int);
            scanner.StackDepthGuard.Enter();
            scanner.StackDepthGuard.Enter();
            scanner.StackDepthGuard.Enter();
            scanner.StackDepthGuard.Enter();
            Assert.Throws<PdfDocumentStackDepthException>(() => scanner.StackDepthGuard.Enter());
            for (var i = 0; i < 4; i++) scanner.StackDepthGuard.Exit();
        }

        [Fact]
        public void TryGetCanFollowMultipleReferenceLinks()
        {
            var reference1 = new IndirectReference(7, 0);
            var reference2 = new IndirectReference(9, 0);

            scanner.Objects[reference1] = new ObjectToken(XrefLocation.File(10), reference1, new IndirectReferenceToken(reference2));
            scanner.Objects[reference2] = new ObjectToken(XrefLocation.File(12), reference2, new NumericToken(69));

            Assert.True(DirectObjectFinder.TryGet(new IndirectReferenceToken(reference1), scanner, out NumericToken result));

            Assert.Equal(69, result.Int);
        }

        [Fact]
        public void GetCanFollowMultipleReferenceLinks()
        {
            var reference1 = new IndirectReference(7, 0);
            var reference2 = new IndirectReference(9, 0);

            scanner.Objects[reference1] = new ObjectToken(XrefLocation.File(10), reference1, new IndirectReferenceToken(reference2));
            scanner.Objects[reference2] = new ObjectToken(XrefLocation.File(12), reference2, new NumericToken(69));

            var result = DirectObjectFinder.Get<NumericToken>(reference1, scanner);

            Assert.Equal(69, result.Int);
        }

        [Fact]
        public void GetTokenCanFollowMultipleReferenceLinks()
        {
            var reference1 = new IndirectReference(7, 0);
            var reference2 = new IndirectReference(9, 0);

            scanner.Objects[reference1] = new ObjectToken(XrefLocation.File(10), reference1, new IndirectReferenceToken(reference2));
            scanner.Objects[reference2] = new ObjectToken(XrefLocation.File(12), reference2, new NumericToken(69));

            var result = DirectObjectFinder.Get<NumericToken>(new IndirectReferenceToken(reference1), scanner);

            Assert.Equal(69, result.Int);
        }

        [Fact]
        public void GetReturnsSingleItemFromArray()
        {
            var reference = new IndirectReference(10, 0);

            const string expected = "Goopy";
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(10), reference, new ArrayToken(new []
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
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(10), reference, new ArrayToken(new[]
            {
                new IndirectReferenceToken(reference2) 
            }));

            scanner.Objects[reference2] = new ObjectToken(XrefLocation.File(69), reference2, new StringToken(expected));

            var result = DirectObjectFinder.Get<StringToken>(reference, scanner);

            Assert.Equal(expected, result.Data);
        }

        [Fact]
        public void GetThrowsOnInvalidArray()
        {
            var reference = new IndirectReference(10, 0);

            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(10), reference, new ArrayToken(new[]
            {
                new NumericToken(5), new NumericToken(6), new NumericToken(0)   
            }));

            Action action = () => DirectObjectFinder.Get<StringToken>(reference, scanner);

            Assert.Throws<PdfDocumentFormatException>(action);
        }
    }
}
