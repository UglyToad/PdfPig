// ReSharper disable ObjectCreationAsStatement

namespace UglyToad.Pdf.Tests.Parser.Parts
{
    using System;
    using Cos;
    using IO;
    using Pdf.Cos;
    using Pdf.Parser.Parts;
    using Xunit;

    public class CosDictionaryParserTests
    {
        private readonly CosNameParser nameParser = new CosNameParser();
        private readonly CosDictionaryParser parser;

        public CosDictionaryParserTests()
        {
            parser = new CosDictionaryParser(nameParser, new TestingLog());
        }

        [Fact]
        public void NameParserIsNull_Throws()
        {
            Action action = () => new CosDictionaryParser(null, new TestingLog());

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void RandomAccessReadIsNull_Throws()
        {
            var baseParser = new CosBaseParser(nameParser, new CosStringParser(), parser, new CosArrayParser());

            Action action = () => parser.Parse(null, baseParser, new CosObjectPool());

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BaseParserIsNull_Throws()
        {
            Action action = () => parser.Parse(new RandomAccessBuffer(), null, new CosObjectPool());

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void DocumentIsNull_Throws()
        {
            var baseParser = new CosBaseParser(nameParser, new CosStringParser(), parser, new CosArrayParser());

            Action action = () => parser.Parse(new RandomAccessBuffer(), baseParser, null);

            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
