namespace UglyToad.PdfPig.Parser.FileStructure
{
    using Core;
    using Tokenization.Scanner;

    internal class CrossReferenceOffsetValidator
    {
        private readonly XrefOffsetValidator offsetValidator;

        public CrossReferenceOffsetValidator(XrefOffsetValidator offsetValidator)
        {
            this.offsetValidator = offsetValidator;
        }

        public long Validate(long crossReferenceOffset, ISeekableTokenScanner scanner, IInputBytes bytes, bool isLenientParsing)
        {
            long fixedOffset = offsetValidator.CheckXRefOffset(crossReferenceOffset, scanner, bytes, isLenientParsing);
            if (fixedOffset > -1)
            {
                crossReferenceOffset = fixedOffset;
            }

            return crossReferenceOffset;
        }
    }
}
