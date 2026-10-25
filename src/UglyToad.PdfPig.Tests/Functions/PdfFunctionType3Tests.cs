namespace UglyToad.PdfPig.Tests.Functions
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;
    using Xunit;

    public class PdfFunctionType3Tests
    {
        private static ArrayToken Numbers(params double[] values)
        {
            return new ArrayToken(values.Select(v => (IToken)new NumericToken(v)).ToArray());
        }

        private static DictionaryToken Type2(double c0, double c1)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, Numbers(0, 1) },
                { NameToken.C0, Numbers(c0) },
                { NameToken.C1, Numbers(c1) },
                { NameToken.N, new NumericToken(1) },
            });
        }

        // Bounds and Encode sized to the sub-functions, splitting [0, 1] evenly between them.
        private static DictionaryToken Type3(params IToken[] subFunctions)
        {
            var count = subFunctions.Length;

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(3) },
                { NameToken.Domain, Numbers(0, 1) },
                { NameToken.Functions, new ArrayToken(subFunctions) },
                { NameToken.Bounds, Numbers(Enumerable.Range(1, count - 1).Select(i => (double)i / count).ToArray()) },
                { NameToken.Encode, Numbers(Enumerable.Repeat(new[] { 0.0, 1.0 }, count).SelectMany(x => x).ToArray()) },
            });
        }

        [Fact]
        public void StitchesTwoSubFunctions()
        {
            DictionaryToken function = Type3(Type2(0, 1), Type2(1, 0));

            var result = PdfFunctionParser.Create(function, new TestPdfTokenScanner(), new TestFilterProvider());

            Assert.Equal(FunctionTypes.Stitching, result.FunctionType);
            Assert.Equal(0.2, result.Eval(0.1)[0], 4); // first sub-function on [0, 0.5)
            Assert.Equal(0.2, result.Eval(0.9)[0], 4); // second sub-function on [0.5, 1], mirrored
        }

        [Fact]
        public void NestingUpToTheLimitParses()
        {
            var result = PdfFunctionParser.Create(Nested(PdfFunctionParser.MaxFunctionDepth), new TestPdfTokenScanner(), new TestFilterProvider());

            Assert.Equal(FunctionTypes.Stitching, result.FunctionType);
        }

        [Fact]
        public void NestingPastTheLimitThrows()
        {
            Assert.Throws<PdfDocumentFormatException>(
                () => PdfFunctionParser.Create(Nested(PdfFunctionParser.MaxFunctionDepth + 1), new TestPdfTokenScanner(), new TestFilterProvider()));
        }

        [Fact]
        public void SubFunctionNamedTwiceAtEveryLevelIsBuiltOnce()
        {
            // Function i (object i + 1) stitches function i + 1 twice, 40 levels deep, around a type 2 function.
            // Built again each time it is named, that would be 2^40 sub-functions.
            const int levels = 40;
            var scanner = new TestPdfTokenScanner();
            for (var i = 0; i < levels; i++)
            {
                var reference = new IndirectReference(i + 1, 0);
                var next = new IndirectReferenceToken(new IndirectReference(i + 2, 0));
                scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, Type3(next, next));
            }

            var leaf = new IndirectReference(levels + 1, 0);
            scanner.Objects[leaf] = new ObjectToken(XrefLocation.File(0), leaf, Type2(0, 1));

            var result = (PdfFunctionType3)PdfFunctionParser.Create(
                new IndirectReferenceToken(new IndirectReference(1, 0)), scanner, new TestFilterProvider());

            Assert.Same(result.FunctionsArray[0], result.FunctionsArray[1]);

            // Each level maps its half of [0, 1] onto [0, 1], and the type 2 function returns its input.
            var expected = 0.3;
            for (var i = 0; i < levels; i++)
            {
                expected = expected < 0.5
                    ? (expected - 0) * (1 - 0) / (0.5 - 0)
                    : (expected - 0.5) * (1 - 0) / (1 - 0.5);
            }

            Assert.Equal(expected, result.Eval(0.3)[0]);
        }

        // Type 3 functions nested inline, each wrapping the next, around a type 2 function at the given depth.
        private static DictionaryToken Nested(int depth)
        {
            DictionaryToken function = Type2(0, 1);
            for (var i = 0; i < depth; i++)
            {
                function = Type3(function);
            }

            return function;
        }

        [Fact]
        public void SubFunctionReferencingItselfDoesNotOverflowTheStack()
        {
            var reference = new IndirectReference(1, 0);
            DictionaryToken function = Type3(new IndirectReferenceToken(reference));

            var scanner = new TestPdfTokenScanner();
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, function);

            Assert.Throws<PdfDocumentFormatException>(
                () => PdfFunctionParser.Create(function, scanner, new TestFilterProvider()));
        }

        [Fact]
        public void SubFunctionsReferencingEachOtherDoNotOverflowTheStack()
        {
            // A stitches B, B stitches A.
            var referenceA = new IndirectReference(1, 0);
            var referenceB = new IndirectReference(2, 0);
            DictionaryToken functionA = Type3(new IndirectReferenceToken(referenceB));
            DictionaryToken functionB = Type3(new IndirectReferenceToken(referenceA));

            var scanner = new TestPdfTokenScanner();
            scanner.Objects[referenceA] = new ObjectToken(XrefLocation.File(0), referenceA, functionA);
            scanner.Objects[referenceB] = new ObjectToken(XrefLocation.File(0), referenceB, functionB);

            Assert.Throws<PdfDocumentFormatException>(
                () => PdfFunctionParser.Create(functionA, scanner, new TestFilterProvider()));
        }
    }
}
