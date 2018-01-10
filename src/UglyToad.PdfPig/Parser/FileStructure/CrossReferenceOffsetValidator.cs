namespace UglyToad.PdfPig.Parser.FileStructure
{
    internal class CrossReferenceOffsetValidator
    {
        private readonly XrefOffsetValidator offsetValidator;

        public CrossReferenceOffsetValidator(XrefOffsetValidator offsetValidator)
        {
            this.offsetValidator = offsetValidator;
        }

        public long Validate(long crossReferenceOffset, bool isLenientParsing)
        {
            long fixedOffset = offsetValidator.CheckXRefOffset(crossReferenceOffset, isLenientParsing);
            if (fixedOffset > -1)
            {
                crossReferenceOffset = fixedOffset;
            }

            return crossReferenceOffset;
        }
    }
}
