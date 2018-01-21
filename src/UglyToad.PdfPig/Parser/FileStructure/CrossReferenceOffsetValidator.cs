namespace UglyToad.PdfPig.Parser.FileStructure
{
    using IO;
    using Tokenization.Scanner;

    internal class CrossReferenceOffsetValidator
    {
        private readonly XrefOffsetValidator offsetValidator;

        public CrossReferenceOffsetValidator(XrefOffsetValidator offsetValidator)
        {
            this.offsetValidator = offsetValidator;
        }

        public long Validate(long crossReferenceOffset, ISeekableTokenScanner scanner, IRandomAccessRead reader, bool isLenientParsing)
        {
            long fixedOffset = offsetValidator.CheckXRefOffset(crossReferenceOffset, scanner, reader, isLenientParsing);
            if (fixedOffset > -1)
            {
                crossReferenceOffset = fixedOffset;
            }

            return crossReferenceOffset;
        }
    }
}
